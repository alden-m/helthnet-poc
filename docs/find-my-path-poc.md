# Find My Path - AI Proof of Concept (Execution Plan)

**Status:** Ready to execute. **This is a plan for a separate, throwaway system - it is NOT part of the HealthNet codebase or PRD.**

This document is a complete, self-contained prompt for an AI agent working in a **brand-new, empty repository**. It embeds everything needed - question set, option lists, conditional logic, API details, sample data, exact commands - so the agent does not need access to the HealthNet repo, the internet, or any clarification from anyone. Execute the steps **in order**; finish and verify each step before starting the next. If anything below seems to require a decision not covered here, default to the simplest option per the Ground Rules and Appendix B - do not stop to ask.

---

## 0. Prerequisites and setup (do this before Step 1)

- **.NET SDK.** Run `dotnet --version`. If the command is not found, install the .NET SDK (version 10 or later; any recent LTS also works) from `https://dotnet.microsoft.com/download`, then re-check.
- **Anthropic API key.** Obtain one from `https://console.anthropic.com` (Settings → API Keys). Do not put it in any file that gets committed. Set it as an environment variable in the shell you'll run the app from:
  - macOS / Linux (bash/zsh): `export ANTHROPIC_API_KEY="sk-ant-..."`
  - Windows PowerShell: `$env:ANTHROPIC_API_KEY = "sk-ant-..."`
  - Windows cmd.exe: `set ANTHROPIC_API_KEY=sk-ant-...`
  These only last for the current shell session. For a persistent setting, use your OS's environment-variable settings (System Properties on Windows, `~/.zshrc`/`~/.bash_profile` on macOS/Linux) - but never write the key into a project file.
- **Repository.** Create a brand-new, empty Git repository for this PoC (unrelated to and not inside the HealthNet repository). Everything in this plan happens inside that new repository.
- **Editor/IDE.** Any editor that can run `dotnet` commands (VS Code, Visual Studio, Rider, or a plain terminal) works - nothing IDE-specific is used.

---

## 1. Context - why this exists

HealthNet's flagship feature is **Find My Path**: an internationally educated health professional (IEHP) completes a guided assessment, an AI drafts a personalised Canadian licensing roadmap, and (in the real product) a human advisor reviews it before delivery.

The client's open question is not the UI or the wizard - it is the AI itself: *"if the AI output is not good, there is no point in building the system."* This PoC exists to answer exactly that question, live, in front of the client:

1. The user fills the Find My Path questionnaire (with its conditional logic).
2. The app sends ONE message to the Claude API: the user's answers + a tunable system instruction + optional additional reference material.
3. The app renders the AI's roadmap nicely.

That is the entire product. Nothing else.

### Ground rules (read before every step)

- **Throwaway by design.** This code will be discarded. No unit tests, no CI, no layered architecture, no repositories, no database. A single Blazor project is correct here.
- **Zero security, zero users.** No sign-up, no login, no profiles, no persistence of user answers. Anyone who opens the site sees the wizard. This is intentional - do not add auth.
- **Default Microsoft template look.** Keep the styling that ships with the Blazor template (Bootstrap, default layout, default nav). Do not redesign it; it should look like a quick prototype, not a finished product.
- **It must not crash during a live demo.** The one place robustness matters: the API call. Handle failures gracefully (friendly error + retry button), never an unhandled exception page.
- **The API key is never committed.** It comes from the `ANTHROPIC_API_KEY` environment variable only.
- **Commit after every step** with a short message like `step 3: sections 1-2 with location branching`.

### Tech baseline

- .NET 10 SDK (or the latest installed SDK), Blazor Web App template, **Interactive Server** render mode, no auth:
  `dotnet new blazor -n FindMyPath.Poc -int Server -au None`
- Claude API via the official **Anthropic C# SDK** (`dotnet add package Anthropic`).
- All app state in memory (scoped/singleton services). One small JSON file for prompt settings so tuning survives an app restart.

---

## 2. The questionnaire (embed exactly this)

The assessment has **8 sections** shown one at a time in a wizard with a progress indicator, Back/Next buttons, and per-section required-field validation.

**Section 1 - Professional Background**

| Question | Type | Options |
|---|---|---|
| What is your healthcare profession? | Radio | Physician, Nurse, Pharmacist, Dentist, Physiotherapist, Occupational Therapist, Medical Laboratory Technologist, Other |
| In which country did you receive your professional qualification? | Dropdown | Country list - use the exact list in **Appendix C** |
| Have you completed your internship or residency (if applicable)? | Radio | Yes / No |
| How many years of clinical experience do you have? | Radio | Less than 1 year, 1-3 years, 4-7 years, 8+ years |

**Section 2 - Current Location**

| Question | Type | Conditional logic |
|---|---|---|
| Where are you currently living? | Radio | Canada / Outside Canada |
| Province | Dropdown - the 13 entries in **Appendix C** | Shown only if "Canada" |
| City | Short text (max 255) | Shown only if "Canada" |
| Country | Dropdown - the list in **Appendix C** | Shown only if "Outside Canada" |

**Section 3 - Immigration Status** (content depends on Section 2)

| Question | Type | Conditional logic |
|---|---|---|
| What is your current immigration status? | Radio: Citizen, Permanent Resident, Work Permit, Study Permit, Visitor, Refugee, Other | Shown only if living in Canada |
| Are you planning to immigrate to Canada? | Radio: Yes / No / Undecided | Shown only if living outside Canada |

**Section 4 - Licensing Status**

| Question | Type | Options / logic |
|---|---|---|
| Have you started your licensing process? | Radio | Yes / No |
| Which licensing exams have you completed? | Checkboxes | MCCQE Part I, NAC OSCE, NCLEX, PEBC, OSCE, IELTS, CELBAN, OET, None. **Selecting "None" clears and disables the others; selecting any other clears "None".** |
| Are you currently registered with any Canadian regulatory body? | Radio | Yes / No |

**Section 5 - Language Proficiency**

| Question | Type | Conditional logic |
|---|---|---|
| Have you completed an English language test? | Radio | Yes / No |
| Which test? | Radio: IELTS, CELBAN, OET, TOEFL, Other | Shown only if Yes |
| Score | Short text, optional | Shown only if Yes |

**Section 6 - Career Goals**

| Question | Type | Options |
|---|---|---|
| What is your primary goal? | Checkboxes (multi) | Obtain professional licence, Practise in Canada, Prepare for examinations, Find employment, Explore alternative careers, Improve communication skills, Build professional network |

**Section 7 - Learning Needs**

| Question | Type | Options |
|---|---|---|
| Which areas would you like support with? | Checkboxes (multi) | Canadian Healthcare System, Clinical Skills, Communication Skills, Clinical Ethics, Documentation, Interview Preparation, Resume Development, Career Planning, Cultural Competence, Emotional Intelligence, Leadership, Research Skills, Exam Preparation |

**Section 8 - Additional Information**

- One optional long-text field (max 1,000 characters): "Is there anything else you would like HealthNet to know to better personalise your learning experience?"

**Conditional-logic summary** (hardcode it; no rules engine - this is a PoC):

- "Canada" selected: show province/city + immigration status; hide country + immigration planning.
- "Outside Canada" selected: the reverse.
- Language test = "No": nothing extra shown; the fact itself is included in the AI input so the roadmap can flag language preparation.
- "None" in the exams checkbox group is mutually exclusive with all other options.

**Deliberate simplification (not an omission).** The full PRD also mentions profession-specific question branching (e.g. distinct physician vs. nurse licensing questions) and per-admin-configurable rules. Section 4 above is intentionally kept as **one shared exam checklist for every profession** - the PRD doesn't define separate question sets, and building a rules engine is explicitly out of scope for a throwaway PoC (see Appendix B). The AI is told the person's profession and answers, and is responsible for applying profession-specific knowledge (e.g. MCCQE/NAC OSCE for physicians, NCLEX/CNO/CRNBC for nurses) when it drafts the roadmap - see Appendix A. Do not add UI branching by profession.

---

## 3. Execution steps

### Step 1 - Scaffold

- `git init`, then `dotnet new blazor -n FindMyPath.Poc -int Server -au None` (project at repo root or under `src/` - keep it simple, one project).
- Add a `.gitignore` for .NET (`dotnet new gitignore`).
- Trim the template: remove the Counter and Weather sample pages; keep the layout and nav. Nav gets three links: **Assessment** (home, `/`), **Prompt Settings** (`/settings`), and nothing else.
- **Done when:** `dotnet run` serves the app, the nav shows the two links, sample pages are gone. Commit.

### Step 2 - Answer model and wizard shell

- Create `AssessmentAnswers` - one plain class with a property per question (strings, bools, `List<string>` for multi-selects). Add `OptionCatalog` static class holding all option lists (professions, countries, provinces, exams, goals, learning needs).
- Create the wizard page at `/`: a component that owns an `AssessmentAnswers` instance and a `CurrentStep` int; renders a progress indicator ("Step X of 8" plus a Bootstrap progress bar), the current section's title, and Back/Next buttons. Section bodies are placeholder markup for now.
- Next is disabled or shows validation messages when the current section's required questions are unanswered. Keep validation minimal: required radios/dropdowns must have a value.
- State lives in the page component (Interactive Server keeps it alive across steps). No persistence.
- **Done when:** you can click through 8 empty steps with a working progress bar and Back/Next. Commit.

### Step 3 - Sections 1 and 2 (professional background + location branching)

- Implement Section 1 and Section 2 exactly per the tables above.
- Section 2 must show/hide province+city vs. country instantly when the Canada / Outside Canada radio changes, and clear the now-hidden answers so stale values never reach the AI.
- **Done when:** both sections work, the branch switches correctly both ways, hidden answers are cleared. Commit.

### Step 4 - Sections 3, 4, 5 (immigration, licensing, language)

- Section 3 renders the in-Canada or outside-Canada variant based on the Section 2 answer.
- Section 4 implements the "None is exclusive" checkbox behaviour.
- Section 5 shows test + score only when the language-test answer is Yes (and clears them when switched to No).
- **Done when:** all conditional behaviour verified manually in the browser, both branches. Commit.

### Step 5 - Sections 6, 7, 8 and the review step

- Implement the two multi-select sections and the free-text section (with a live character counter, 1,000 max).
- After Section 8, add a **Review** screen: a read-only summary of every answered (visible) question, grouped by section, with an Edit link per section that jumps back to it. The primary button on this screen is **"Generate my pathway"** (wired in Step 8).
- **Done when:** a full run-through ends at an accurate review screen; editing a section and returning updates the summary. Commit.

### Step 6 - Prompt Settings page (`/settings`)

This page is how the client tunes the AI without touching code. Three parts:

1. **System instruction** - a large textarea, prefilled with the default in Appendix A.
2. **Additional reference material** - a second large textarea whose content is appended to the message sent to the AI. Next to it, a file upload (`InputFile`, accept `.txt,.md`, max ~1 MB) that reads the file's text into this textarea (replacing its content). This lets the client drop in e.g. a document about licensing bodies and see how it changes the output.
3. Buttons: **Save**, **Reset to default** (system instruction only).

- Persist both values to a `prompt-settings.json` file next to the app (e.g. in `ContentRootPath`) via a small singleton `PromptSettingsService` (load at startup, save on demand). If the file is missing or unreadable, fall back to defaults - never crash.
- **Done when:** edits survive an app restart; a `.md` upload fills the textarea; reset restores the default. Commit.

### Step 7 - Claude API client

- `dotnet add package Anthropic` (official Anthropic C# SDK).
- Create `RoadmapService` (singleton) with one method:
  `Task<RoadmapResult> GenerateAsync(AssessmentAnswers answers, PromptSettings settings, CancellationToken ct)`.
- Compose **one request**:
  - **System** = the system instruction from settings.
  - **One user message** containing: (a) the answers serialized as a readable Q&A block - only questions that were visible/answered, grouped by section, using the exact question wording; (b) if non-empty, the additional reference material under a heading like `Reference material provided by HealthNet:`.
- Call parameters: model `claude-opus-4-8`, `MaxTokens = 16000`. The SDK reads `ANTHROPIC_API_KEY` from the environment automatically (`new AnthropicClient()`). Minimal shape:

  ```csharp
  using Anthropic;
  using Anthropic.Models.Messages;

  var response = await client.Messages.Create(new MessageCreateParams
  {
      Model = Model.ClaudeOpus4_8,
      MaxTokens = 16000,
      System = settings.SystemInstruction,
      Messages = [new() { Role = Role.User, Content = userMessage }],
  });
  var text = string.Concat(response.Content
      .Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
  ```

- Error handling: catch the SDK's typed exceptions (`Anthropic.Exceptions` namespace - rate limit, auth, 5xx, network) and return a `RoadmapResult` in a Failed state with a one-line human message ("The AI service is busy, try again" / "API key missing or invalid - check ANTHROPIC_API_KEY"). Never let an exception escape to the UI. If `ANTHROPIC_API_KEY` is unset, detect it at startup and show a banner on the wizard rather than failing on first generate.
- **Done when:** a quick manual test (temporary button or test page is fine, remove after) returns model text end to end. Commit.

### Step 8 - Generate flow and roadmap rendering

- Wire "Generate my pathway" on the review screen: show a full-screen-ish loading state ("Building your personalised pathway... this can take a minute") while the call runs; the call can legitimately take tens of seconds.
- The system instruction (Appendix A) asks the model for a fenced JSON block with this shape:

  ```json
  {
    "summary": "2-3 sentence overview of the recommended path",
    "recommendedPathway": "Name of the licensing route",
    "estimatedTotalTimeline": "e.g. 18-30 months",
    "estimatedTotalCost": "e.g. CAD 8,000-15,000",
    "phases": [
      {
        "title": "Phase name",
        "description": "What this phase achieves",
        "steps": [
          {
            "title": "Actionable step",
            "description": "What to do and why",
            "estimatedTimeline": "e.g. 2-4 months",
            "estimatedCost": "e.g. CAD 1,200 (or empty)"
          }
        ]
      }
    ],
    "notes": ["Flags such as language preparation recommended"]
  }
  ```

- **Tolerant parsing:** extract the first fenced ```json block (or the first `{...}` spanning to the last `}`), deserialize case-insensitively. **If parsing fails, do not error - render the raw response text** in a card instead. A live demo must show *something* either way.
- **Roadmap view** (on success): summary + recommended pathway at the top with a "Draft - AI generated, pending advisor review" badge; then each phase as a card, each step as a checkable item (checkbox + title + description + timeline/cost line). Checkbox state is view-only fun - purely local, no persistence.
- Below the roadmap, a collapsed `<details>` section **"What was sent to the AI"** showing the exact system instruction and user message. This is gold in the client meeting - it makes the tuning conversation concrete.
- Buttons: **Regenerate** (same answers, new call) and **Start over** (reset wizard).
- **Done when:** full happy path works end to end; unplugging the API key produces the friendly error + retry, not a crash. Commit.

### Step 9 - Demo quick wins

Four small additions, each cheap to build, chosen purely to make the live demo land harder. Build them in this order; each is independent, so if one turns out not to be quick, skip it and move on.

1. **Sample profiles (fastest win, do first).** On the first wizard screen, add a small "Try a sample profile" area with buttons for the personas below. Clicking one fills every answer in `AssessmentAnswers` exactly as specified and jumps straight to the Review screen. In the meeting this means: one click, straight to the AI - no typing through 8 sections while the client watches. Use these exact values (don't invent your own):

   **Persona A - "Physician from Egypt"**
   Profession: Physician · Qualification country: Egypt · Completed internship/residency: Yes · Clinical experience: 8+ years · Location: Outside Canada · Destination country field: Egypt (current residence) · Immigration: planning to immigrate = Yes · Licensing started: Yes · Exams completed: MCCQE Part I, IELTS · Registered with a regulatory body: No · Language test: Yes · Test: IELTS · Score: 7.5 · Goals: Obtain professional licence, Practise in Canada, Prepare for examinations · Learning needs: Canadian Healthcare System, Exam Preparation, Interview Preparation · Additional info: "I completed my MBBCh in Cairo in 2014 and have worked in internal medicine since. I am aiming to settle in Ontario."

   **Persona B - "Nurse from the Philippines, already in Ontario"**
   Profession: Nurse · Qualification country: Philippines · Completed internship/residency: Yes · Clinical experience: 4-7 years · Location: Canada · Province: Ontario · City: Toronto · Immigration status: Work Permit · Licensing started: No · Exams completed: None · Registered with a regulatory body: No · Language test: Yes · Test: CELBAN · Score: Advanced · Goals: Obtain professional licence, Find employment · Learning needs: Canadian Healthcare System, Documentation, Resume Development, Cultural Competence · Additional info: "I have a BSN and 5 years of hospital experience in Manila. Currently working as a personal support worker while I sort out my licensing."

   **Persona C - "Pharmacist from India, undecided"**
   Profession: Pharmacist · Qualification country: India · Completed internship/residency: No · Clinical experience: 1-3 years · Location: Outside Canada · Destination country field: India (current residence) · Immigration: planning to immigrate = Undecided · Licensing started: No · Exams completed: None · Registered with a regulatory body: No · Language test: No · Goals: Explore alternative careers, Improve communication skills, Build professional network · Learning needs: Career Planning, Communication Skills, Emotional Intelligence · Additional info: (leave blank)
2. **Live streaming while generating.** Switch `RoadmapService` to the SDK's streaming call (`client.Messages.CreateStreaming`) and surface the text as it arrives: the loading screen shows the model's output growing in real time (a scrolling, muted "raw draft" panel). When the stream completes, parse the full text as before and swap in the rendered roadmap. This turns up to a minute of dead air - the worst moment of any AI demo - into the most impressive part. Keep the non-streaming code path as a fallback if this fights back; it is a nice-to-have, not a foundation.
3. **Pathway progress bar.** At the top of the rendered roadmap, show "0 of N steps complete" with a progress bar that updates live as the demo driver ticks the step checkboxes. Trivial (the checkboxes already exist) and it demos the real product's "track your progress" promise.
4. **Print / Save as PDF.** A button on the roadmap view that calls `window.print()`, plus a small print stylesheet that hides nav/buttons/the details panel so only the roadmap prints. The client leaves the meeting with a physical artifact of what their AI produced.

Also extend the JSON contract with two rollup fields (already reflected in Appendix A): `estimatedTotalTimeline` and `estimatedTotalCost`. Render them as two stat chips next to the summary - "Estimated total: 18-30 months / CAD 8,000-15,000" is the single most concrete, screenshot-able thing on the page.

- **Done when:** a sample profile reaches a rendered roadmap in two clicks; text visibly streams during generation; ticking steps moves the progress bar; printing yields a clean one-pager-ish roadmap. Commit after each win.

### Step 10 - Final pass and README

- Click through the whole flow at mobile width and desktop width; fix anything broken (template CSS should mostly handle it).
- Do a full demo dry run with each sample profile from Step 9 and read the output critically. If it is weak, tune the default system instruction in Appendix A / settings until a cold run produces a demo-worthy roadmap. **This tuning IS the point of the PoC - budget real time for it.**
- `README.md`: what this is (throwaway PoC, one paragraph), prerequisites (.NET SDK, `ANTHROPIC_API_KEY`), how to run (`dotnet run`), where to tune the prompt (`/settings`), and a note that the key must never be committed.
- **Done when:** dry run produces a convincing roadmap; README lets a stranger run it in under 5 minutes. Commit.

---

## Appendix A - Default system instruction (starting point, tune in Step 9)

```
You are the pathway-planning engine for HealthNet Canada, a platform that guides
internationally educated health professionals (IEHPs) to licensure and practice
in Canada.

You receive one message containing (1) a user's answers to a structured intake
questionnaire and (2) optionally, reference material provided by HealthNet.

Your job:
- Identify the applicable Canadian licensing pathway for this person's
  profession and situation (e.g. IMG route via MCC/CaRMS for physicians,
  NNAS + provincial college for nurses, PEBC for pharmacists), taking their
  province (or likely destination) into account.
- Map their foreign credentials and completed exams to Canadian equivalents,
  and skip steps they have already completed.
- Produce a gap analysis: exams still required, credential assessments,
  documentation, language requirements, and realistic timelines and costs.
- If they have not completed an English language test, include language
  preparation early in the plan.
- Reflect their stated goals and learning needs in the later phases
  (e.g. exam preparation, interview preparation, networking).
- If reference material is provided, treat it as authoritative HealthNet
  guidance and prefer it over your general knowledge where they conflict.

Rules:
- Be specific and actionable: name the actual organizations, exams, and
  documents (MCC, physiciansapply.ca, NNAS, PEBC, regulatory college of the
  relevant province).
- Use realistic timelines and CAD cost estimates; give ranges, never false
  precision. If something varies by province or year, say so in the step
  description.
- Do not invent requirements. If information is uncertain or the user's
  situation is ambiguous, put a note in "notes" rather than guessing silently.
- Write for the user: encouraging, plain language, second person.

Output format:
Respond with a single fenced JSON code block and nothing else, using exactly
this shape:
{
  "summary": "...",
  "recommendedPathway": "...",
  "estimatedTotalTimeline": "...",
  "estimatedTotalCost": "...",
  "phases": [ { "title": "...", "description": "...",
      "steps": [ { "title": "...", "description": "...",
          "estimatedTimeline": "...", "estimatedCost": "..." } ] } ],
  "notes": [ "..." ]
}
Aim for 3-6 phases with 2-6 steps each, ordered chronologically.
```

## Appendix B - Out of scope (do not build)

User accounts, authentication, saving/resuming assessments, databases, advisor
review workflow, back office, admin-configurable questions, notifications,
progress tracking over time, file upload of user credentials, tests, CI/CD,
deployment infrastructure. If a step seems to need any of these, it is being
over-built - stop and simplify.

## Appendix C - Option lists (use exactly these, do not invent alternatives)

**Provinces and territories (13)** - use for the Section 2 "Province" dropdown:

Alberta, British Columbia, Manitoba, New Brunswick, Newfoundland and Labrador,
Nova Scotia, Ontario, Prince Edward Island, Quebec, Saskatchewan,
Northwest Territories, Nunavut, Yukon

**Countries** - use for the Section 1 "qualification country" dropdown and the
Section 2 "Outside Canada" country dropdown (identical list in both places):

Afghanistan, Albania, Algeria, Andorra, Angola, Antigua and Barbuda, Argentina,
Armenia, Australia, Austria, Azerbaijan, Bahamas, Bahrain, Bangladesh,
Barbados, Belarus, Belgium, Belize, Benin, Bhutan, Bolivia, Bosnia and
Herzegovina, Botswana, Brazil, Brunei, Bulgaria, Burkina Faso, Burundi,
Cabo Verde, Cambodia, Cameroon, Canada, Central African Republic, Chad, Chile,
China, Colombia, Comoros, Congo (Republic of the), Congo (Democratic Republic
of the), Costa Rica, Croatia, Cuba, Cyprus, Czechia, Denmark, Djibouti,
Dominica, Dominican Republic, Ecuador, Egypt, El Salvador, Equatorial Guinea,
Eritrea, Estonia, Eswatini, Ethiopia, Fiji, Finland, France, Gabon, Gambia,
Georgia, Germany, Ghana, Greece, Grenada, Guatemala, Guinea, Guinea-Bissau,
Guyana, Haiti, Honduras, Hungary, Iceland, India, Indonesia, Iran, Iraq,
Ireland, Israel, Italy, Ivory Coast, Jamaica, Japan, Jordan, Kazakhstan,
Kenya, Kiribati, Kosovo, Kuwait, Kyrgyzstan, Laos, Latvia, Lebanon, Lesotho,
Liberia, Libya, Liechtenstein, Lithuania, Luxembourg, Madagascar, Malawi,
Malaysia, Maldives, Mali, Malta, Marshall Islands, Mauritania, Mauritius,
Mexico, Micronesia, Moldova, Monaco, Mongolia, Montenegro, Morocco,
Mozambique, Myanmar, Namibia, Nauru, Nepal, Netherlands, New Zealand,
Nicaragua, Niger, Nigeria, North Korea, North Macedonia, Norway, Oman,
Pakistan, Palau, Palestine, Panama, Papua New Guinea, Paraguay, Peru,
Philippines, Poland, Portugal, Qatar, Romania, Russia, Rwanda,
Saint Kitts and Nevis, Saint Lucia, Saint Vincent and the Grenadines, Samoa,
San Marino, Sao Tome and Principe, Saudi Arabia, Senegal, Serbia, Seychelles,
Sierra Leone, Singapore, Slovakia, Slovenia, Solomon Islands, Somalia,
South Africa, South Korea, South Sudan, Spain, Sri Lanka, Sudan, Suriname,
Sweden, Switzerland, Syria, Taiwan, Tajikistan, Tanzania, Thailand,
Timor-Leste, Togo, Tonga, Trinidad and Tobago, Tunisia, Turkey, Turkmenistan,
Tuvalu, Uganda, Ukraine, United Arab Emirates, United Kingdom, United States,
Uruguay, Uzbekistan, Vanuatu, Vatican City, Venezuela, Vietnam, Yemen, Zambia,
Zimbabwe, Other

Sort alphabetically in the dropdown; keep "Other" last regardless of sort.
