# Find My Path POC — Demo Verification

Verified July 12, 2026 against both the original feature description at
`C:\Users\alden\Downloads\find-my-path.md` and the repository POC plan at
`docs/find-my-path-poc.md`.

## Executive result

The POC is ready for a controlled client demo. The complete eight-section questionnaire,
conditional branches, live Claude generation, advanced tuning, uploaded reference-file mode,
history, progress tracking, regeneration warnings, and print/PDF output were exercised locally.
The final automated suite passes 79/79, the Release build has zero warnings and errors, and the
published Production artifact passed a clean smoke test.

The active demo history intentionally contains three final, useful roadmaps (Physician, Nurse,
and Pharmacist). Three earlier quality-tuning generations were moved to the ignored quarantine
folder so weak drafts cannot be selected accidentally during the meeting.

## Questionnaire parity

All 19 questions/conditional fields in the original feature description are present in their
original order. Review labels and the message sent to the AI use the full original wording rather
than internal aliases.

| # | Original field | Implemented contract |
|---:|---|---|
| 1 | What is your healthcare profession? | Required radio; all 8 original options |
| 2 | In which country did you receive your professional qualification? | Required dropdown; exact Appendix C list |
| 3 | Have you completed your internship or residency (if applicable)? | Required Yes/No radio |
| 4 | How many years of clinical experience do you have? | Required radio; exact `Less than 1 year`, `1–3 years`, `4–7 years`, `8+ years` options |
| 5 | Where are you currently living? | Required `Canada` / `Outside Canada` radio |
| 6 | If Canada: Province | Required conditional dropdown; 13 provinces/territories |
| 7 | If Canada: City | Required conditional short text; 255-character limit |
| 8 | If Outside Canada: Country | Required conditional dropdown; exact country list |
| 9 | What is your current immigration status? | Required Canada-only radio; exact options including `PR` |
| 10 | Are you planning to immigrate to Canada? | Required outside-Canada radio |
| 11 | Have you started your licensing process? | Required Yes/No radio |
| 12 | Which licensing exams have you completed? | Checkbox list; exact options; `None` is mutually exclusive |
| 13 | Are you currently registered with any Canadian regulatory body? | Required Yes/No radio |
| 14 | Have you completed an English language test? | Required Yes/No radio |
| 15 | Which test? (if Yes) | Required conditional radio; exact 5 options |
| 16 | Score (if Yes) | Optional conditional short text; 255-character limit |
| 17 | What is your primary goal? | Required checkbox list; all 7 original options |
| 18 | Which areas would you like support with? | Required checkbox list; all 13 original options |
| 19 | Additional personalisation question | Optional long text; 1,000-character limit |

One extra required field, **Which province do you plan to practise in?**, is retained and clearly
labelled `(POC enhancement)` in the AI payload. It resolves destination ambiguity for applicants
outside Canada without replacing or changing any original question.

Hidden branch values are cleared before review and before API submission. For example, changing
from Canada to Outside Canada removes stale province/city/immigration-status values, and changing
the language-test answer to No removes stale test/score values.

## Advanced tuning and generation controls

Settings now expose:

- Claude model selection with current cost guidance.
- Reasoning effort: Low, Medium, High, or Max, constrained by model support.
- Maximum output tokens: 4,096, 8,192, or 16,000.
- **Use uploaded HealthNet reference files** switch, off by default.
- Editable system instruction plus reset-to-safe-default behavior.
- API-key readiness and truthful save status.

The reference switch is intentionally precise: Off sends the questionnaire and system
instruction; On also sends uploaded HealthNet material. It does **not** claim to turn the model's
pre-trained knowledge or live internet research on/off. Uploaded content is treated as data, and
prompt-injection instructions inside an uploaded file are explicitly ignored.

Generation uses Anthropic structured output for the roadmap schema, streams visible progress,
and falls back to a non-streaming request if the stream fails or cannot be parsed. Calls have a
three-minute cancellation boundary. A failed regeneration preserves the last good roadmap and its
checked progress; a valid replacement clears old completion state only after it exists.

## Live model dry runs

Six live generations were deliberately enough to exercise variation without running a large batch.
Combined recorded API cost: **$0.35593 USD**.

| Disposition | Persona / controls | Result |
|---|---|---|
| Retained | Nurse; Opus 4.8, High, 8K, references Off | 4 phases / 8 steps; $0.05737 |
| Retained | Pharmacist; Opus 4.8, High, 8K, references On | 4 phases / 9 steps; $0.06069; includes PEBC Evaluating Examination and separates national certification from provincial registration |
| Retained | Physician; Opus 4.8, High, 8K, references Off | 5 phases / 11 steps; $0.07236; completed exams are not repeated and MCCQE Part II is not recommended or budgeted |
| Archived after tuning | Physician; Opus 4.8, High, 8K, references Off | Exposed obsolete MCCQE Part II guidance; used to add the licensing guardrail |
| Archived after tuning | Pharmacist; Sonnet 5, Medium, 4K, references On | Exercised a cheaper/faster variation; exposed an unclear PEBC/provincial boundary |
| Archived after tuning | Pharmacist; Opus 4.8, High, 8K, references On | Confirmed the boundary fix, then exposed silent omission of the Evaluating Examination |

The tuned prompt now explicitly prevents MCCQE Part II actions, distinguishes PEBC national
certification from provincial requirements, requires the PEBC Evaluating Examination unless a
streamlined-path explanation is given, avoids completed-exam repetition, and asks for a final
obsolete/conflicting/duplicate-step proofread. These controls are based on current official MCC
and PEBC guidance:

- <https://mcc.ca/examinations-assessments/mccqe/>
- <https://mcc.ca/wp-content/uploads/MCC-Annual-Report-2021-2022.pdf>
- <https://pebc.ca/pharmacists/certification-pathway/international-graduates/>
- <https://pebc.ca/streamlined_pathway_ipg/>
- <https://pebc.ca/pharmacists/pharmacist-evaluating-examination/general-information/>

Final operator defaults are **Claude Opus 4.8 / High / 8,192 tokens / uploaded references Off**.
The pharmacist history entry remains available as the clean reference-On demonstration.

## Browser verification

The in-app browser control bridge was unavailable in this environment because its launcher was
missing required `sandboxPolicy` metadata. The same browser-level verification was completed with
local Chrome and Playwright 1.61.1 instead.

- **43 wizard checks passed:** all 8 sections, both location branches, required validation, branch
  clearing, 255/1,000 character boundaries, exam `None` exclusivity, exact review wording, answer
  editing, all three sample profiles, desktop layout, and 390px mobile layout.
- **17 history/result/PDF checks passed:** three-item order, questionnaire version, effective
  setting snapshots, reference manifest, licensing guardrails, progress controls, print invocation,
  roadmap-only print styling, and mobile overflow.
- Browser console/page errors: **0**.

## History, privacy, and failure hardening

- Each successful generation gets a collision-resistant, atomic JSON snapshot.
- History safely normalizes legacy nulls and skips malformed records.
- History IDs are validated; path traversal reads/writes are rejected.
- Settings saves are atomic and validated; corrupt or legacy files fall back safely.
- A failed save is reported truthfully instead of showing a false success message.
- Uploaded-file reads/deletes cannot escape the knowledge-base directory; size limits and unique
  names prevent partial or accidental overwrite behavior.
- Runtime `app_data`, uploads, histories, settings, temporary browser files, and generated output
  are gitignored. `app_data` and `appsettings.json` are excluded from build/publish output.
- An unrelated private PDF found in the old knowledge-base folder was quarantined outside the
  active demo data rather than deleted.
- The API key is never written into settings or history and the committed configuration contains
  an empty key.

## PDF verification

Both the current-result and History print paths produced a polished **2-page Letter PDF** with
navigation, controls, and prompt details hidden. Each page was rendered to PNG and visually
inspected for clipping, missing glyphs, awkward phase splitting, and overflow. The corresponding
current-result and History page images are pixel-identical.

- `output/pdf/find-my-path-latest.pdf`
- `output/pdf/find-my-path-history-latest.pdf`

## Automated and release evidence

- `dotnet format FindMyPath.Poc.slnx --verify-no-changes --no-restore`: passed.
- `dotnet test FindMyPath.Poc.slnx -c Release --no-restore`: **79 passed, 0 failed, 0 skipped**.
- `dotnet build FindMyPath.Poc.slnx -c Release --no-restore`: **0 warnings, 0 errors**.
- NuGet direct/transitive vulnerability audit: no known vulnerable packages from configured feeds.
- Release publish: 157 files; **0** `app_data` files and **0** `appsettings.json` files.
- Published app under `ASPNETCORE_ENVIRONMENT=Production`: Home, Settings, History, and all 6 linked
  static assets returned successfully; host emitted **0 warnings/errors**.

## Deliberate full-platform exclusions

This remains an AI proof of concept, not the production platform. The original PRD's authentication,
user-profile storage, auto-save/resume from Dashboard, advisor queue/approval workflow, back office,
admin-configurable rules/questions, encrypted database, notifications/nudges, advisor escalation,
Learning Hub integrations, and long-term progress persistence are intentionally not represented as
complete. The PRD mentions profession-specific branching but supplies no profession-specific intake
question sets; the repository's explicit POC contract therefore keeps the shared exam checklist and
uses the AI prompt to specialize the roadmap.

## Demo operator checklist

1. Run `pwsh ./utils/run.ps1` from the repository root.
2. Confirm Settings shows **API key configured** and the intended controls.
3. Start with a sample profile; the retained Pharmacist history entry is the clearest reference-On
   comparison, while Physician demonstrates references Off.
4. During generation, point out the live draft, then check roadmap steps to move the progress bar.
5. Open **What was sent to the AI** to demonstrate control and transparency.
6. Open History and use **Print / Save as PDF** for the leave-behind artifact.
7. Describe every roadmap as an AI draft pending advisor review; verify time-sensitive fees and
   regulator requirements before production use.
