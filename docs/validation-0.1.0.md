# Validation

Date: **2026-09-07**. Platform: Windows x64, Stardew Valley **1.6.15.24356**,
SMAPI **4.5.2**, .NET SDK **8.0.419**, installed **SDVKit 0.8.0**.

## Delivery artifact

- Version: `0.1.0`, ID: `Nana1873.DearlyKept`.
- ZIP: `.sdvkit/releases/DearlyKept-0.1.0.zip`.
- SHA-256: `f35d315104c06b4fdcd17e4b6d003a39778b53aa67f95b5ed56c340c30ba56f4`.
- Contents: one mod folder, `DearlyKept.dll`, `manifest.json`, and English/German
  i18n files. No QA harness, config, test saves, game art, or development files.
- Final compact-row and footer adjustment: accepted in a fresh review of the
  extracted ZIP, including German at 100% and 150% UI scale and compact paging.
- After that review, the unsupported en dash in the page range was replaced with
  a spaced ASCII hyphen in both translations. SDVKit schema/build/package checks
  passed again. Archive comparison proved that only those two `menu.range` values
  changed: the DLL and manifest are byte-identical to the accepted build. This
  typography-only revision did not receive another game run; saved screenshots
  retain the earlier range glyph. The comparison is recorded in
  `.sdvkit/evidence/final-typography-package-check.json`.

## Static checks

- SDVKit schema checks passed for the manifest and both locale files.
- Production build: **0 warnings, 0 errors**.
- Independent provenance executable: **29 checks passed**, including malformed
  data, escaped identifiers, different senders/dates, and preservation of unknown
  metadata during stacking decisions.
- English/German key and placeholder parity checked independently.
- Local game IL confirmed that `Item.canStackWith` ignores `modData`, whereas
  `getOne` copies it. The stacking patch only changes an original `true` to
  `false`; it never forces incompatible vanilla items to stack.

## Isolated gameplay evidence

All gameplay used the exact registered SDVKit disposable fixture
`c5913bb5d67f4f8c8e82dfc3036ef040`, save `SDVKit_5302327774374365924`.
Normal saves and the normal Mods directory were not selected.

The two earlier candidates and final UI candidate were extracted from
SDVKit-produced ZIPs and used directly as
ready-mod review targets. The QA companion created test inventory and opened
the game's real `Data/mail` entries through `LetterViewerMenu`; it never wrote
gift provenance itself. Receipt, closing, overflow handling, and menu controls
used SDVKit's input transport and the normal game UI.

| Check | Observed result |
| --- | --- |
| SMAPI loading | Target ID/version and controlled staged build identity confirmed. |
| Mom's `mom1` letter | One actual Cookie received with Mom, exact mail ID, and actual date; five pre-existing ordinary Cookies stayed separate and unmarked. |
| Evelyn's real gift letter | Actual random attachments collected, including Cookies. Mom and Evelyn Cookies remained distinct. |
| Closing unclaimed mail | Vanilla close path delivered the marked attachment; no gift was lost. |
| Native split/copy | `getOne` returned a distinct single-item copy preserving the exact raw note. |
| Native stacking | Same-note copies stack in both directions; different senders and marked/unmarked Cookies do not; ordinary Cookies still stack. |
| Full backpack | A new marked Cookie reached the normal `ItemGrabMenu`. After a disposable filler stack was trashed through the UI, the Cookie entered the freed slot with its note and the original five ordinary Cookies unchanged. |
| Standard tooltip | Actual game screenshot shows the note along with vanilla description, energy, and health values. |
| Real save | Vanilla `Sleep_Yes` initiated the normal day transition; the game logged save completion and SMAPI raised `Saved`. |
| Process restart | The next review loaded the same saved fixture. Independent checkpoint comparison matched every inventory slot, item ID, count, and raw provenance string. |
| Collections replay | `fromCollection=true` produced zero attachments and no inventory change. Closing it still passed the saved inventory checkpoint comparison. |
| Controller cancellation | Actual injected Controller X opened confirmation; one B cancelled confirmation while leaving the main menu open. |
| Confirmed removal | Right + Enter on the German confirmation removed only the selected Cookie's note. The Cookie stayed in the same slot with count one; other gifts and ordinary items remained unchanged. |
| Localization | Actual English and German game languages displayed their respective mod text. |
| 150% UI scale | Game-reported UI viewport was 854×480 in a 1280×720 window. The final two-row layout, German footer, details, and paging fit; the German confirmation had already passed in Candidate 2. |
| Final gameplay regression | New real Mom and Evelyn letters passed the independent capture, date, copy, and symmetric stacking assertions again in the final UI candidate. |
| Diagnostics | Candidate 2 and the final UI candidate selected-mod diagnostics returned no warnings or errors, with no truncation. |
| Cleanup | All three owned review processes stopped; staging removed. Explicit single resets returned `fixtureReset=true`, `problems=[]`. Final PID 34132 was absent after cleanup. |

Candidate 1 launch: `c1010a54955a44c79439e15693e71faa`, PID 9620.
Candidate 2 launch: `2db2c9388658470daa7643af58638a7b`, PID 46680.
Final UI candidate launch: `17a86495ed014a00bb99b3aa7966684a`, PID 34132.
Restart evidence spans these distinct processes. Candidate 2 also read the
earlier serialized note format without discarding its extra computed field.

The final UI candidate ZIP hash was
`0ea5a434ef96a9bf6e189486a1b75399f4cb1376115a0137270ccbb6f1cdcaa9`;
its accepted staged file-set identity was
`sha256:7a685fa795dfd5e866c00df44ee7f3354aaaf32ee510cfe71ab61528bfa8b110`.
The earlier archive is retained as `.sdvkit/evidence/visually-accepted-package.zip`.

Evidence files are retained below `.sdvkit/evidence/`; screenshots below
`.sdvkit/screenshots/`. Shared-lab original logs/captures are below the workspace
root's `.sdvkit/lab/`. `candidate-1-smapi.log`, `candidate-2-smapi.log`, checkpoint,
diagnostic, stop, and reset JSON files record the observations above.
`final-smapi.log`, `final-status.json`, `final-diagnostics.json`, `final-stop.json`,
and `final-reset.json` retain the final UI candidate's evidence before the shared
game slot was handed back to the next task.

## Scope limits

These are separate proofs of schema/build, SMAPI loading, selected gameplay,
visual behavior, and cleanup. This is not a full playthrough or a test of every
supported letter. Multiplayer, split-screen, physical-controller hardware,
other operating systems, game 1.7, third-party inventory/mail mods, and Content
Patcher sender-map integrations have not been accepted.

The novelty search and download hypothesis are documented separately in
[novelty research](novelty-research.md). They are not runtime test results.
