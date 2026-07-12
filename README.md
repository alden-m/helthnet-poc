# Find My Path — AI Proof of Concept

A **throwaway proof-of-concept** for HealthNet Canada's flagship feature, *Find My Path*: an internationally
educated health professional (IEHP) completes a short guided assessment, and Claude drafts a personalised
Canadian licensing roadmap. The point of this PoC is to answer one question live, in front of the client —
*is the AI output good enough to build the real product on?* Everything else (auth, database, advisor-review
workflow) is deliberately out of scope.

It is a single ASP.NET Core **Blazor Web App** (Interactive Server), talking to the Claude API through the
official Anthropic C# SDK. There is no database — settings and submission history are plain JSON files under
your app-data folder.

## What's in it

Three tabs in the sidebar:

- **Find My Path** — an 8-step wizard (progress bar on top, Back/Next, per-step validation, conditional
  branching) ending in a review screen and a **Generate My Pathway** button. Three **sample profiles** on the
  first screen fill every answer and jump straight to review — one click to the AI during a demo. Generation
  streams the model's output live, then renders a phase-by-phase roadmap with timelines, costs, checkable
  steps, and a "What was sent to the AI" panel.
- **Settings** — tune the AI without touching code: model, reasoning effort, maximum output tokens, system
  instruction, and an explicit switch for uploaded HealthNet reference files. Text, Word, PDF, and image
  uploads are supported. Settings survive restarts.
- **History** — every generation is saved as a JSON snapshot (the answers, the categorized output phases, the
  API cost, exact prompt, questionnaire version, and effective tuning controls). Click any submission to see
  the full snapshot or print a clean roadmap-only PDF.

The API request uses Anthropic structured outputs, so every current model is constrained to the roadmap JSON
schema instead of relying on best-effort fenced JSON. A tolerant parser remains for old history and defensive
fallback rendering.

## Prerequisites

- **.NET 10 SDK** (any recent SDK works). Check with `dotnet --version`; install from
  <https://dotnet.microsoft.com/download> if missing.
- **An Anthropic API key** from <https://console.anthropic.com>.

## The API key (never committed)

The key comes from **configuration**, read as `Anthropic:ApiKey`.

- **Local dev** — put it in `FindMyPath.Poc/appsettings.json`:

  ```json
  {
    "Anthropic": {
      "ApiKey": "sk-ant-...",
      "Model": "claude-opus-4-8"
    }
  }
  ```

  The committed `appsettings.json` ships with an **empty** `ApiKey`. This checkout's local copy can be flagged
  `git update-index --skip-worktree`, so your real key is never staged or pushed. (You can also set the
  `ANTHROPIC_API_KEY` environment variable instead — it's used as a fallback.)

- **Azure App Service** — do **not** put the key in the repo. Set it under
  **Configuration → Application settings** as `Anthropic__ApiKey` (double underscore maps to the `:`
  separator). The same code reads it.

The key is never written to `app_data` and never appears in history snapshots.
`appsettings.json` is also explicitly excluded from publish output; deployed hosts must provide the key through
environment/application settings.

## Deploying (Azure App Service or any host)

The app is Blazor **Interactive Server**, so all interactivity — including the live token-by-token
streaming while a roadmap is generated — runs over a SignalR circuit that needs **WebSockets**.

- Before the demo, verify **Configuration → General settings → Web sockets** is **On** (or set
  `webSocketsEnabled: true` in ARM/Bicep). This lets the Blazor Server circuit use WebSockets instead
  of a fallback transport for the smoothest live-streaming demo. Generation still auto-falls back
  to a non-streaming API call if the model stream itself fails.
- Set the API key as an application setting named `Anthropic__ApiKey` (double underscore) — never in
  the repo.
- Currency/number/date formatting is pinned to a fixed culture in `Program.cs`, so cost figures show
  `$` regardless of the host's locale.

## Persistence

Runtime data is stored as JSON in a **project-relative** `FindMyPath.Poc/app_data/` folder (gitignored):

- `app_data/settings.json` — model, prompt, reference-file mode, effort, and output-token cap.
- `app_data/knowledge-base/*` — files available to attach when reference-file mode is enabled.
- `app_data/history/*.json` — one atomic snapshot per generation (answers + phases + cost + prompt + controls).

The entire `app_data` tree is gitignored and excluded from build/publish artifacts because it can contain
questionnaire data or private uploads. For a hosted POC, mount persistent storage if history must survive a
redeploy.

## Run it

```bash
cd FindMyPath.Poc
dotnet run
```

Then open the URL printed in the console. To pin a port: `dotnet run --urls http://localhost:5080`.

On Windows, `pwsh ./utils/run.ps1` performs a scoped clean relaunch, verifies a healthy page response, and opens
the app. Tune generation on the **Settings** tab.

## Tests

```bash
dotnet test FindMyPath.Poc.slnx
```

The focused suite covers the exact questionnaire catalogs and AI payload wording, conditional clearing,
settings migration/persistence, roadmap parsing and null hardening, knowledge-base file safety, model pricing,
and atomic/corrupt history behavior. UI and live-model behavior are verified separately with browser dry runs.

## Notes on the spec vs. the client mockups

The build follows the written spec (`docs/find-my-path-poc.md`) as the source of truth and adopts the client's
design mockups (`docs/suggested-screenshots/`) for the visual style. Where they disagreed, the resolved choices
were:

- **App shell** uses the standard Blazor sidebar template (three tabs); the **wizard and result content** are
  styled to match the polished mockups (purple accents, card selectors, green gradient result page, stat chips,
  phase cards).
- The **full 8-section questionnaire** from the spec is kept (richer input → better AI output), plus a
  "Target Province" question folded in from the mockups. The mockups showed a leaner 6-step flow; the spec's
  richer set was used.
- Question wording, order, required behavior, option lists, and single/multi-select controls follow the written
  questionnaire. Review and AI-payload labels use the same wording, not abbreviated aliases.
- The mockups' contextual "why this matters" hint boxes were intentionally omitted.

## Throwaway by design

There is no auth, database, advisor workflow, back office, or production persistence. The small automated suite
exists only to make the client demo repeatable and protect the questionnaire/generation seams. This code is
still meant to be discarded once the client decides whether to build the real product.
