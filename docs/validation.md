# Validation

## Player archives 0.4.0

Date: **2026-09-08**. Same Stardew/SMAPI runtime as below, installed SDVKit
**0.9.0**. Tested ZIP: `.sdvkit/releases/DearlyKept-0.4.0.zip`, SHA-256:

`ab8d0e6ef2c826924b2ab13658adf36836ff405417193717caf2604f2102eda0`

Production target identity:
`sha256:ef81c4d5a88c8505ba4df3f8f2207f5642c1cb1356e62bd3ab493594432ffdfe`.
The unchanged extracted package was used throughout these checks. All 65 pure
journal checks and SDVKit project checks passed. Evidence is retained under
`.sdvkit/evidence/multiplayer/`.

- **Network run 2:** two real local host/farmhand processes received different
  native letters (Mom's Cookies and Evelyn's Coffee Maker). Actual item,
  quantity, personalized full message, unique receipts, prior-history retention,
  ordinary item metadata, and each farmer's serialized archive matched.
  Both processes observed independent host and farmhand archives through native
  game synchronization. The farmhand left and rejoined the same player with its
  exact archive intact. Both players slept normally; after clean stop and pair
  restart without reset, each full archive and inventory matched its checkpoint.
- **Original Nexus binaries:** the user-provided Happy Birthday 3.21.4, Stardust
  Core 3.1.1, English pack 2.0.4, Marriage Overhaul 1.7.4 and Wedding
  Anniversaries 2.1 ZIPs were copied unchanged and hashed in
  `.sdvkit/integration-dependencies/nexus/checksums.json`. Original DLLs were
  staged with Content Patcher 2.9.1; Happy Birthday received previously generated
  stock configuration files for staging consistency. No third-party files are
  distributed in this release.
- Original Happy Birthday NPC delivery and CP token preview passed: one real
  Cookie, full displayed greeting, ordinary stacking, and no preview receipt.
  Its parent tokens emitted upstream startup warnings in the blank fixture;
  original-binary parent mail and full Happy Birthday calendar scheduling were
  not established by this run. Concurrent birthday dialogue producers can
  replace each other's dialogue; this run does not establish their mutual
  compatibility.
- Original Marriage Overhaul birthday passed after native sleep into the
  configured birthday, with its own DayStarted handler and normal dialogue.
  The original delayed-reward producer delivered three Cookies and retained
  both independently observed QA-authored dialogue pages. This reward check
  invokes the actual delivery method; it does not play a complete project quest.
- Original Wedding Anniversaries passed native sleep into day 112 of a fixture
  marriage, its own calendar handler, native spouse dialogue, one actual gift,
  and both displayed pages. Exact inventory/fridge deltas, sender, occasion,
  source, date and no item tags were checked for each spouse attempt.
- **Storage regression:** using actual received history, the real production
  loader/saver imported the host's legacy archive unchanged. Malformed JSON,
  JSON null and an unsupported schema disabled recording and preserved raw
  player data exactly instead of falling back to legacy data. The test restored
  its original fixture state afterwards. This is an in-process loader test,
  separate from the real restart check above.

### Limits and rejected attempts

Network coverage is one host plus one local loopback farmhand, using native
mail. Third-party producer multiplayer, internet/Steam/GOG transport, late
messages during abrupt process loss and splitscreen visual behavior are not
verified. Capture caches and journal ownership are per screen in production;
this alone is not a splitscreen acceptance claim.

The splitscreen probe created a real second GameRunner instance, but SDVKit's
single fixture then rejected `singlePlayer`. No further input bypassed that
guard. The fixture was stopped/reset. Network run 1 stalled after native rejoin
replaced Options and enabled background pause; the ordinary stop failed because
its fallback found both a console and a game window. Recovery terminated only
the exact PID/start-time-verified owned farmhand, then used CLI stop/reset.
Run 2 kept the explicitly unfocused QA fixture running immediately after native
activation and passed rejoin/save/restart. This QA workaround is not in Dearly
Kept. A corresponding SDVKit fix is [draft PR #180](https://github.com/Nana1873/SDVKit/pull/180),
not merged and not yet live-accepted as a new SDVKit build.

The first calendar marriage probe used an unupgraded house and caused a vanilla
missing marriage-map error. The corrected fixture upgrades the house first;
the replacement MO calendar and WA runs passed without that error. Earlier
menu-open assertion rejections are not counted as passes.

Final owned network and single reviews were stopped/reset, with fixture reset,
staging removal and no remaining problems confirmed. Normal saves and Mods were
not used. Prior UI/controller/overflow coverage below remains scoped to the
unchanged UI and capture implementations; it is not new multiplayer UI proof.

## Spouse integrations 0.3.0

Date: **2026-09-08**. Stardew Valley **1.6.15.24356**, SMAPI **4.5.2**,
installed SDVKit **0.9.0**, Windows x64. Accepted package:
`.sdvkit/releases/DearlyKept-0.3.0.zip`, SHA-256:

`ec6b3ecd7e4f578170d0fbae165900dce666670ae9e412b233456416617f3a48`

The package was extracted into `.sdvkit/review-spouse-3/DearlyKept`. Its target
build identity is
`sha256:a5ebab5c3e3d0b67c71e08fe2b69e8beabb1b4f78963ebadbbb01ca20b386b24`.
The production ZIP contains only its DLL, manifest, and two locale files.
Manifest/i18n schema checks and all **65** pure journal checks passed.

Actual gameplay evidence is under `.sdvkit/evidence/spouse/`:

- Run 2: Marriage Overhaul birthday gift to inventory, three-Cookie project
  reward with two displayed pages, birthday gift to the fridge with a full
  backpack, and Wedding Anniversaries normal inventory delivery passed.
  Each assertion compares the actual inventory/fridge quantity delta, exact
  independently observed pages, sender, provider, occasion, and unique receipt.
- Run 2 found a missing receipt in Wedding Anniversaries' native overflow path.
  The corrected adapter observes `addItemsByMenuIfNecessary`, the actual
  collection handoff, instead of its small single-item forwarding method.
- Run 3 rechecked the corrected packaged artifact: Wedding Anniversaries gave
  an Artist Bookcase through the actual full-inventory ItemGrabMenu. Three
  displayed pages were preserved. Real mouse input claimed the item after
  discarding fixture Stone; Escape closed the emptied menu. The quantity delta
  and exactly one receipt passed after collection. Marriage Overhaul birthday
  and two-page project-reward assertions also passed on this artifact.
- The German journal shows three actual receipts. The new Hochzeitstag filter
  shows exactly one of three. The reader shows the correct bookcase and all
  three original English pages, retaining the receipt-time language.
- Normal vanilla sleep fired Saving and Saved. Run 4 loaded the same fixture
  without either producer installed and verified the full journal and inventory
  against the pre-save checkpoint.

Run 1 exposed a normalized version mismatch (`2.1` versus SMAPI's `2.1.0`) and
a QA setup float mismatch; both were corrected before accepted gameplay.
Run 3 also contains rejected QA commands issued before an inventory menu was
closed; they performed no fixture mutation. Later assertions passed after
closing the actual menu. These earlier failures are retained in the logs.

The companions were source-built, and the fixture invokes their real producer
methods directly. See [provenance and supported paths](spouse-integrations.md):
calendar scheduling, equality to Nexus release binaries, forage/cooking chores,
and unsupported versions are not claimed as gameplay-tested. The unchanged
mail, Happy Birthday, controller and 150% layout gates retain their 0.2.0 evidence
below; they were not exhaustively repeated for this integration change.

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
