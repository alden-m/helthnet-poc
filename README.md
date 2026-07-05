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
- **Settings** — tune the AI without touching code: the system instruction, an optional reference-material
  block (with `.txt`/`.md` upload), and the model. Saved to `settings.json`, survives restarts.
- **History** — every generation is saved as a JSON snapshot (the answers, the categorized output phases, the
  API cost, and the exact prompt used). Click any submission to see the full snapshot.

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

  The committed `appsettings.json` ships with an **empty** `ApiKey`. The local copy is flagged
  `git update-index --skip-worktree`, so your real key is never staged or pushed. (You can also set the
  `ANTHROPIC_API_KEY` environment variable instead — it's used as a fallback.)

- **Azure App Service** — do **not** put the key in the repo. Set it under
  **Configuration → Application settings** as `Anthropic__ApiKey` (double underscore maps to the `:`
  separator). The same code reads it.

The key is never written to `app_data` and never appears in history snapshots.

## Deploying (Azure App Service or any host)

The app is Blazor **Interactive Server**, so all interactivity — including the live token-by-token
streaming while a roadmap is generated — runs over a SignalR circuit that needs **WebSockets**.

- **Azure App Service ships with WebSockets OFF by default.** Before the demo, turn it on:
  **Configuration → General settings → Web sockets → On** (or set `webSocketsEnabled: true` in
  ARM/Bicep). Without it the circuit falls back to long-polling behind the App Service proxy, which
  can stutter or drop ("Attempting to reconnect…") mid-generation. If streaming ever misbehaves it
  still auto-falls-back to a non-streaming call, but enable WebSockets for the smooth demo.
- Set the API key as an application setting named `Anthropic__ApiKey` (double underscore) — never in
  the repo.
- Currency/number/date formatting is pinned to a fixed culture in `Program.cs`, so cost figures show
  `$` regardless of the host's locale.

## Persistence

Runtime data is stored as JSON in a **project-relative** `FindMyPath.Poc/app_data/` folder (gitignored):

- `app_data/settings.json` — the Settings-tab tuning (system instruction, model, reference material).
- `app_data/history/*.json` — one snapshot per generation (answers + phases + cost + prompt + model).

This folder lives under the app's content root, so it travels and persists with the app on Azure App
Service — unlike the OS roaming app-data folder, which is ephemeral there.

## Run it

```bash
cd FindMyPath.Poc
dotnet run
```

Then open the URL printed in the console. To pin a port: `dotnet run --urls http://localhost:5080`.

Tune the prompt and pick the model on the **Settings** tab.

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
- A few field types differ between spec and mockups (e.g. profession as radio vs. dropdown, qualification
  country as dropdown vs. free text, experience buckets). The spec's option lists were kept; dropdowns were
  used where the mockups showed them.
- The mockups' contextual "why this matters" hint boxes were intentionally omitted.

## Throwaway by design

No tests, no CI, no auth, no database, no persistence of user answers beyond the local history snapshots.
This code is meant to be discarded once the client decides whether to build the real product.
