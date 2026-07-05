#Requires -Version 7
<#
.SYNOPSIS
    Bulletproof local relaunch for the Find My Path POC (FindMyPath.Poc).

.DESCRIPTION
    Every run does the same thing, no matter what stale or stuck state is left behind:

      1. Kills every stale/stuck Find My Path app-host that was launched FROM THIS REPO --
         whether it is still holding our port or has crashed onto a different one (a stuck
         host still locks this repo's build output, so it must go).
      2. Rebuilds the project. If the build fails, it stops here -- it never launches a
         broken build.
      3. Relaunches the app in its own window, in the Development environment (required for
         Blazor interactivity to work).
      4. Waits until the app actually answers, then opens Microsoft Edge on it.

    Kills are scoped to processes whose executable lives under THIS repo root. That is
    deliberate: an identically-named host from a parallel git worktree (or any unrelated
    dotnet.exe) is left untouched, so this script can never take down work running out of
    another checkout. If our port is held by such a foreign process, the script stops and
    names it rather than killing someone else's work -- unless you pass -Force, which kills
    whatever owns the port regardless of origin.

.PARAMETER SkipBuild
    Relaunch without rebuilding (uses the last build output).

.PARAMETER Https
    Launch and open the app on its HTTPS URL instead of HTTP.

.PARAMETER Force
    Also kill foreign processes (outside this repo) that hold our port, instead of stopping
    and reporting them. Use when you are sure nothing else needs it.

.EXAMPLE
    pwsh ./utils/run.ps1
.EXAMPLE
    pwsh ./utils/run.ps1 -Https
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [switch]$Https,
    [switch]$Force
)

Set-StrictMode -Version 3
$ErrorActionPreference = 'Stop'

# --- Repo paths (this script lives in <repo>/utils) ---
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Project  = Join-Path $RepoRoot 'FindMyPath.Poc'

# --- App URLs. These MUST match Properties/launchSettings.json. ---
$App = [pscustomobject]@{
    Name = 'FindMyPath'
    Http = 'http://localhost:5027'
    Https = 'https://localhost:7265'
}

function Write-Step { param([string]$Message) Write-Host "==> $Message" -ForegroundColor Cyan }

# Only the ports we are about to bind for the selected scheme. The HTTPS profile binds
# both schemes, so it needs both ports; the HTTP run binds only HTTP. Scoping to ports we
# will actually use avoids falsely blocking on an unused port.
$TargetPorts = @([int]([uri]$App.Http).Port)
if ($Https) { $TargetPorts += [int]([uri]$App.Https).Port }
$TargetPorts = $TargetPorts | Sort-Object -Unique

# Returns the full executable path of a process, or $null if it can't be read.
function Get-ProcessPath {
    param([int]$ProcessId)
    $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $proc) { return $null }
    try { return $proc.Path } catch { return $null }
}

# True when a process's executable lives under this repo root.
function Test-OwnedByRepo {
    param([int]$ProcessId)
    $path = Get-ProcessPath -ProcessId $ProcessId
    if (-not $path) { return $false }
    return $path.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase)
}

# Frees a port. Repo-owned owners are always killed. A foreign owner is killed only with
# -Force; otherwise it is returned as a blocker so the caller can stop and report it.
function Stop-PortOwner {
    param([int]$Port)
    $blockers = @()
    $owners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $owners) {
        if (-not $processId) { continue }
        $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
        $name = if ($proc) { $proc.ProcessName } else { "PID $processId" }
        if ((Test-OwnedByRepo -ProcessId $processId) -or $Force) {
            $why = if ($Force -and -not (Test-OwnedByRepo -ProcessId $processId)) { ' [forced, foreign]' } else { '' }
            Write-Host "    freeing port $Port (killing $name)$why" -ForegroundColor DarkYellow
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }
        else {
            $path = Get-ProcessPath -ProcessId $processId
            $blockers += "port $Port held by foreign process $name (PID $processId)$(if ($path) { " at $path" })"
        }
    }
    return $blockers
}

function Wait-PortFree {
    param([int]$Port, [int]$TimeoutSeconds = 15)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $listening = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if (-not $listening) { return $true }
        Start-Sleep -Milliseconds 250
    }
    return $false
}

function Wait-UrlReady {
    param([string]$Url, [int]$TimeoutSeconds = 90)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            # Any HTTP response (even 4xx) means Kestrel is up and serving.
            Invoke-WebRequest -Uri $Url -Method Head -TimeoutSec 5 -SkipCertificateCheck -SkipHttpErrorCheck | Out-Null
            return $true
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
}

# === 1. Kill this repo's stale host, plus free our port ===
Write-Step 'Stopping running Find My Path app (this repo only)'

# A stale host from THIS repo can be stuck on any port (or none) yet still lock our build
# output -- kill it by name + repo path, regardless of which port it grabbed.
Get-Process -Name 'FindMyPath.*' -ErrorAction SilentlyContinue |
    Where-Object { Test-OwnedByRepo -ProcessId $_.Id } |
    ForEach-Object {
        Write-Host "    killing stale host $($_.ProcessName) (PID $($_.Id))" -ForegroundColor DarkYellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    }

# Free the ports we're about to bind. Foreign owners become blockers unless -Force.
$blockers = @()
foreach ($port in $TargetPorts) { $blockers += Stop-PortOwner -Port $port }
if ($blockers) {
    $list = ($blockers | ForEach-Object { "  - $_" }) -join "`n"
    throw "Cannot free required port(s) without touching another checkout's process:`n$list`n" +
          "Re-run with -Force to kill them anyway, or stop that process yourself."
}

foreach ($port in $TargetPorts) {
    if (-not (Wait-PortFree -Port $port)) {
        throw "Port $port is still in use after kill attempt. Aborting so we don't launch onto a stuck process."
    }
}

# === 2. Build (skip launching if it fails) ===
if ($SkipBuild) {
    Write-Step 'Skipping build (-SkipBuild)'
}
else {
    Write-Step "Building $Project"
    dotnet build $Project
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE). App was NOT launched." }
}

# === 3. Launch the built host exe directly (Development env, so Blazor works) ===
Write-Step 'Launching app'
# One process: the apphost exe. No 'dotnet run' / shell wrapper, so the killer above
# terminates the app completely and its console window closes with it -- repeated
# relaunches never leave orphaned windows behind. Kestrel honours ASPNETCORE_URLS.
$env:ASPNETCORE_ENVIRONMENT = 'Development'

# Resolves the freshest built host exe for a project (robust to TFM/config changes).
function Resolve-AppHost {
    param([string]$ProjectDir)
    $leaf = Split-Path $ProjectDir -Leaf            # e.g. FindMyPath.Poc
    $binDir = Join-Path $ProjectDir 'bin'
    if (-not (Test-Path $binDir)) { return $null }
    return Get-ChildItem -Path $binDir -Recurse -File -Filter "$leaf.exe" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
}

$url = if ($Https) { "$($App.Https);$($App.Http)" } else { $App.Http }
$exe = Resolve-AppHost -ProjectDir $Project
if (-not $exe) {
    throw "Could not find a built host exe for $($App.Name) under $Project\bin. " +
          "Run without -SkipBuild so it gets built first."
}

$env:ASPNETCORE_URLS = $url   # inherited by the child started below
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe -Parent) | Out-Null
Write-Host "    started $($App.Name) -> $url" -ForegroundColor Green
Remove-Item Env:\ASPNETCORE_URLS -ErrorAction SilentlyContinue

# === 4. Wait for readiness, then open Edge ===
Write-Step 'Waiting for app to respond'
$openUrl = if ($Https) { $App.Https } else { $App.Http }
if (Wait-UrlReady -Url $openUrl) {
    Write-Host "    $($App.Name) is up" -ForegroundColor Green
}
else {
    Write-Warning "$($App.Name) did not respond in time -- opening its URL anyway; check its window for errors."
}

Write-Step 'Opening Microsoft Edge'
# Edge is usually not on PATH; resolve it from the App Paths registry entry, then the known
# install locations, before falling back to the default browser.
function Resolve-EdgePath {
    $appPaths = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe'
    if (Test-Path $appPaths) {
        $p = (Get-ItemProperty -Path $appPaths).'(default)'
        if ($p -and (Test-Path $p)) { return $p }
    }
    foreach ($p in @(
            "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
            "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe")) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

$edge = Resolve-EdgePath
if ($edge) {
    Start-Process -FilePath $edge -ArgumentList $openUrl | Out-Null
}
else {
    Write-Warning "Microsoft Edge not found; opening the URL in your default browser instead."
    Start-Process $openUrl | Out-Null
}

Write-Step 'Done. Find My Path relaunched.'
