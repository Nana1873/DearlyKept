# Validation

## Archive 0.2.0

Date: **2026-09-08**. Target runtime: Stardew Valley **1.6.15.24356**,
SMAPI **4.5.2**, Windows x64. Build and the completed baseline game review used
the installed **SDVKit 0.8.0** release.

The current archive candidate adds actual message text, occasion/sender filters,
and a message reader. It removes entry deletion completely. Its package is
`.sdvkit/evidence/archive/candidate-1.zip`, SHA-256:

`0f3aa45625685f5567e02de0dc42a90671700051c6b3bf68453c842788df72fe`

Completed checks for this candidate:

- SDVKit schema validation passed for the manifest and English/German i18n.
- Production package build passed with zero warnings and zero errors.
- All **63** journal-model checks passed: independent receipt identities,
  duplicate callback rejection, detached save snapshots, exact text and date
  persistence, backward-compatible schema-1 records without text, and bounded
  message enrichment without changing other receipt fields.
- Package contains only the production DLL, manifest and two locale files.
  Test harnesses and integration dependencies are not distributed.
- English/German translation keys and placeholders match.
- Installed game IL and the supported Happy Birthday source were checked for
  text and delivery semantics. Message observers do not evaluate raw commands
  or advance dialogue.

Evidence: `.sdvkit/logs/package.log`,
`.sdvkit/tests/journal-core-build/results-archive.log`, and the JSON round trips
under `.sdvkit/tests/journal-core-evidence/`.

Actual game acceptance passed for this unchanged package:

- A clearly QA-authored long letter passed through the real vanilla mail
  viewer: three original pages, 1,100 prepared characters, the actual player's
  name, and one ordinary Cookie. Its full archived text matched the independent
  menu snapshot, including the final sentence and signature.
- Mom's actual vanilla letter preserved its full personalized message and
  ordinary Cookie receipt. Neither letter's item/title commands leaked into
  the archive. The exact mail-to-sender extension was exercised by the QA letter.
- Happy Birthday's actual Content Patcher preview created no gift or history.
- A two-page QA greeting supplied through Happy Birthday's real Evelyn asset
  was displayed and advanced with actual input. Both personalized pages, without
  dialogue commands, matched the archived transcript exactly after the actual
  gift handoff. The harness did not construct a journal entry or call gift methods.
- Gus's unmodified birthday greeting used the real full-backpack debris path.
  Its actual displayed text and Cookie were recorded once; normal pickup did
  not duplicate the receipt. Both birthday assertions passed.
- Occasion filtering selected two birthday gifts out of four receipts; adding
  the Evelyn sender filter selected exactly her birthday receipt.
- English at 100% and German at 150% UI scale were visually inspected at a
  physical 1280x720 viewport. The long message reader paginated to three and
  five pages respectively; its ending remained readable. Item quantities,
  dates, source labels, filters and buttons fit the inspected layouts.
- Actual controller A opened a message, B returned to the archive, and a right
  shoulder press advanced exactly one reader page. With the game's controller
  mode fixed on for the synthetic-input fixture, a second B closed to the world;
  the menu observation confirmed `menuOpen=false`.
- Clearing the isolated inventory left all four receipts and texts unchanged.
  All reading/filter navigation preserved their complete JSON. Normal sleep
  raised SMAPI `Saved` at 12:09:32 local time. A new process loaded the same
  fixture **without Happy Birthday or its dependencies**; the full journal JSON
  and empty inventory matched the checkpoint exactly.

Launches: `6135726392a948439a45947f46ad6f65` (PID 9016) and
`a2c4edf5dee2473b8c486b10b7ac57eb` (PID 7808), same registered fixture as the
baseline below. Both loaded target build identity
`sha256:501c4069efd36a2713b67a8646a4abb38cefbf6f5eb74eb11a82d038b9454ef1`.
Evidence is under `.sdvkit/evidence/archive/`: launch/status JSON,
`run-1-smapi.log`, `run-2-smapi.log`, `run-1-checkpoint.json`,
`controller-exit-menu.json`, and the `SDVKit-archive-*.png` screenshots.
The screenshot named `archive-reloaded-de-100` actually shows the game's
English startup locale; it is not German-100% acceptance evidence.

One attempted shoulder input used an invalid SMAPI button name and was rejected
by SDVKit. The corrected `RightShoulder` input passed. Synthetic controller
disconnects in Auto mode also opened the vanilla pause menu; commands requiring
a closed menu were rejected and retried. The final controller replay used a
runtime-only ForceOn setting, not a product-code workaround.

After all functional assertions and the final controller exit observation,
the second game process was stopped outside this task's lifecycle commands at
12:11:50; the source of that stop was not established. Logs and screenshots were
secured. The isolated `default_options` file still contained `Auto` and
`gamepadControls=false`, with a timestamp before the temporary controller setting.
A subsequent public CLI stop/reset reported no problems,
`stagingRemoved=true`, and `fixtureReset=true`; PID 7808 was absent. See
`run-2-external-stop-status.json`, `run-2-stop.json`, and `final-reset.json`.
Normal saves and normal Mods were not selected or modified.

## Completed journal baseline

The immediately preceding 0.2.0 journal candidate was tested in the actual game
before text capture and the new archive UI were added. These results establish
the receipt and persistence baseline; they do not substitute for the changed
archive capabilities above.

Package `.sdvkit/evidence/journal/candidate-2.zip`, SHA-256:

`993bf29ba41ddab561f9a3e58d9e2ca6fb1a4400708dad85be0793134b3ac552`

Observed results:

- Mom's actual Cookie attachment merged with ordinary Cookies. Opening the
  letter alone created no memory; its real handoff created exactly one.
- Clicking Evelyn's actual Bread attachment and then closing the letter
  created one receipt. Repeated gifts from Mom remained separate receipts.
- Collections replay did not create an attachment or a journal entry.
- A full backpack used the real mail overflow menu; taking its attachment
  did not create a second receipt.
- Happy Birthday's real Evelyn greeting queued a Cookie, then delivered and
  merged it once. An actual Content Patcher token preview produced neither
  an item nor a journal entry.
- Happy Birthday's actual Mom letter delivered one Pink Cake; Robin's belated
  letter delivered 50 Wood. Their journal quantities and sources matched.
- Happy Birthday's real Gus greeting with a full backpack provided Cookie
  debris. Vanilla pickup did not create a duplicate journal entry.
- Removing every inventory item from the verified disposable fixture left all
  seven journal entries unchanged. These entries had actual delivery evidence;
  the harness did not manufacture journal history.
- Normal vanilla sleep raised SMAPI `Saved`. After stopping the process and
  restarting the same fixture **without Happy Birthday or its dependencies**,
  the complete journal JSON and empty inventory matched their checkpoint.
- Native journal screenshots were visually inspected in English at 100% UI
  scale and German at 100% and 150%. These screenshots show the previous UI.

The controlled fixture was `c5913bb5d67f4f8c8e82dfc3036ef040`.
The first game launch was `ab288caf65724cba8c0027d0ff9b57c5` (PID 44832);
the restart was `f2dd3e1e71a34b77abea209e5e5e3c2c` (PID 47716).
Target build identity for both:
`sha256:168a302f3da6eae70f9c4af74f510098d92ab5ddade7306069dbdc9c88bc6650`.

Evidence is under `.sdvkit/evidence/journal/`: `run-1-smapi.log`,
`run-2-smapi.log`, `run-1-checkpoint.json`, launch/status/stop JSON, screenshots,
and `final-reset-before-text.json`. Both owned processes were stopped through
the public CLI; the final reset reported `fixtureReset=true`,
`stagingRemoved=true`, and no problems. Normal saves and normal Mods were not used.

## Integration provenance and limits

The optional dependency set is source-built Happy Birthday **3.21.4**,
source-built Stardust Core **3.1.1**, Content Patcher **2.9.1**, and the author's
English content pack **2.0.4**. Happy Birthday/Stardust Core source files match
author commit `a36cbed260e7aedc311ec182b436dd331d39472a`; only local build
configuration was adjusted. These are **not verified Nexus release binaries**,
so live results apply to this documented source build. Provenance and hashes
are in `.sdvkit/integration-dependencies/README.md`.

Happy Birthday creates stock JSON under a `Configs` subdirectory on startup.
SDVKit 0.8.0 treats those new paths as staging drift. The tested review input
includes the exact stock files from a startup probe. Happy Birthday's ordinary
per-player `data` writes at day end still trigger that guard. After the actual
`Saved` event, the public CLI stop succeeded and removed staging. This is not a
claim that all SDVKit review queries remain available after those writes.

Happy Birthday also logged parent-token warnings before the world loaded;
its actual later parent-letter delivery passed. A few harness commands were
rejected while a native gift popup was still open, then retried after actual
input closed it. These are recorded in the full logs, not hidden as clean runs.

The separate Happy Birthday spouse-party path, direct vanilla spouse gifts,
other gift mods, multiplayer and split-screen have no acceptance claim.
The source/API assessment is distinct from actual game behavior, and build
success is distinct from visual acceptance. No download count or absolute
novelty has been verified.

## Earlier prototype

[The 0.1.0 evidence](validation-0.1.0.md) is retained for the older item-note
prototype. Its archive is unchanged and does not establish 0.2.0 behavior.
