using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using FindMyPath.Poc.Components;
using FindMyPath.Poc.Services;

// Pin the formatting culture so currency ("$"), numbers and dates render identically on every host.
// Without this, a Linux/Azure App Service instance with no locale set falls back to the invariant
// culture, whose currency symbol is the generic "¤" — so demo cost figures would show "¤0.09".
var appCulture = new CultureInfo("en-CA");
CultureInfo.DefaultThreadCurrentCulture = appCulture;
CultureInfo.DefaultThreadCurrentUICulture = appCulture;
CultureInfo.CurrentCulture = appCulture;
CultureInfo.CurrentUICulture = appCulture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddRazorPages();

const string accessCookieScheme = AccessGateService.AuthenticationScheme;
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = accessCookieScheme;
        options.DefaultChallengeScheme = accessCookieScheme;
        options.DefaultSignInScheme = accessCookieScheme;
    })
    .AddCookie(accessCookieScheme, options =>
    {
        options.Cookie.Name = AccessGateService.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // The local demo runner intentionally supports HTTP. Outside Development, the
        // cookie is always HTTPS-only even when the app sits behind a reverse proxy.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/access";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromDays(180);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<AppPaths>();
builder.Services.AddSingleton<AccessGateService>();
builder.Services.AddSingleton<PromptSettingsService>();
builder.Services.AddSingleton<KnowledgeBaseService>();
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<RoadmapService>();
// Per-circuit wizard/roadmap state so it survives navigating to Settings/History and back.
builder.Services.AddScoped<WizardState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// The demo runner intentionally supports an HTTP-only profile. Registering the redirect
// middleware without an HTTPS listener produces a warning on every fresh local launch and
// can never redirect successfully. When the -Https profile binds an HTTPS URL, keep the
// normal redirect behaviour. Production reverse proxies can own HTTPS redirection upstream.
var hasHttpsEndpoint = (app.Configuration["urls"] ?? string.Empty)
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
if (hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.Run();
