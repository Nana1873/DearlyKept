# 0.5 readiness work

The authorized scope groups the audit and display matrix into fourteen items.
No deletion feature is included. Splitscreen acceptance belongs to the separate
SDVKit work already in progress.

| # | Work | Status |
| --- | --- | --- |
| 1 | Correct sender positioning in archive rows | Passed at normal and compact UI sizes |
| 2 | Consistent producer labels | Passed, including unavailable-content snapshots |
| 3 | Visible read-only / save-error status | Passed malformed archive and injected serialization failure, preserved data and successful retry |
| 4 | Selectable sender/occasion filters | Passed mouse/keyboard/controller selection, combined filtering and right-click cancellation |
| 5 | Search items, senders and saved messages | Pure checks and live native-widget search passed |
| 6 | 1,000 / 10,000-entry performance and storage checks | Passed live open/search and full 10,000-entry save/restart |
| 7 | Host-only/farmhand-only installation and offline-farmhand save | Offline save, rejoin and full pair restart passed; asymmetric installation blocked by installed SDVKit capability |
| 8 | Natural multiplayer birthday/anniversary and project progression | Passed on both peers with original Nexus producers; fixture scope below |
| 9 | Overlapping birthday producer compatibility | Original HB + MO same-NPC delivery passed; milestone/anniversary conflict found (below) |
| 10 | Missing custom NPC/item fallbacks | Passed with synthetic archived IDs absent from the live game |
| 11 | Transparent overlength-message handling | Passed flag roundtrip, reader notice and actual native overlength mail delivery |
| 12 | Year and season selection | Passed with combined live filters |
| 13 | In-game settings | Capture toggle, persisted config and rebound hotkey passed |
| 14 | Borderless, representative UI sizes, UI scale and input | EN/DE, keyboard/controller and 854x480 through 2560x1440 UI sizes passed; exclusive fullscreen excluded by user request |

Record actual evidence and limits here before claiming a row complete.

Thirteen rows are complete within their stated scope. Row 7 remains partially
open: the installed release requires the same target/companion set on both
peers. Neither disabling capture nor removing staged files would establish an
accepted host-only/farmhand-only installation test. No such substitute was used.

## Packaged candidate

`.sdvkit/releases/DearlyKept-0.5.0.zip`, SHA-256:

`6b53a51c68070faef2fc88f14fed99bc0de6c995b49fcebe24263c715484fffa`

Final target identity:
`sha256:407dc21a055b6208848f193231ce88ed818f43f2e00fe12c6fb43e186a5c081f`.
The extracted package loaded in SMAPI, passed the final compact picker check
and the clean offline-save/pair-restart test. It contains only the production
DLL, manifest and two translation files; QA companions are excluded. The last
package change shortened only the EN/DE picker hint, after full functionality
passed on the preceding package. SDVKit schema checks and the package build
passed with no problems; the final core run passed all 74 checks.

## Evidence so far (2026-09-08)

- 74 pure core checks passed. A 10,000-entry serialization/roundtrip/search fixture
  took 57 ms and used 3,439,919 bytes (short synthetic messages).
- In the actual game, 1,000 synthetic receipts loaded in 2 ms and opened in less
  than 1 ms; 10,000 loaded in 37 ms and opened in 6 ms, with 5,055,628 JSON
  characters. Searching the final message returned exactly one receipt. A normal
  overnight save and complete process restart preserved the entire 10,000-entry
  journal. These timings are observations on this machine, not guarantees.
- Search text was supplied through a guarded QA fixture calling the native
  TextBox input callback after normal F3 selection. SDVKit 0.9's public text
  adapter supports NamingMenu only; this was not an OS-level typing test.
- Year 10 plus Summer yielded the expected single synthetic receipt. Settings
  toggled mail capture, wrote config.json, rebound K to L and opened using L.
  The default binding and capture option were restored.
- Original Nexus HB 3.21.4 and MO 1.7.4 delivered Salad x1 and Cookie x1 from Leah
  in the same birthday interaction: two distinct receipts, exact quantities,
  producer identities and distinct original text. WA 2.1 was co-loaded too.
- The normal multiplayer overnight transition triggered MO birthday delivery on
  both players: Leah/Salad for the host, Penny/Pancakes for the farmhand. Both
  independent message and quantity assertions passed, including fridge storage.
- With MO milestones disabled in the fixture, the next calendar anniversary
  delivered Leah's Thorns Ring to the host and Penny's Protection Ring to the
  farmhand. All two/three displayed pages and the actual quantities matched.
- MO's original `emily_cloth` and `seb_part` requests created native tracked
  quests on day start. Native NPC gift handover consumed Cloth / Refined Quartz
  and the original completion handler scheduled rewards three days later.
  Three normal overnight transitions delivered Skull Shirt / Mini-Jukebox;
  both complete two-page messages, quantities and player-owned receipts passed.
  The fixture selects authored requests and marriage prerequisites; it does not
  prove random request selection or every request's content.
- Injecting an IOException into the exact production ledger serialization path
  left prior serialized data unchanged and displayed the save-error notice.
  Removing that fixture injection and retrying the real saver cleared the notice.
- A QA-authored native letter exceeding 16,000 characters delivered exactly one
  Cookie through normal close input. The actual receipt retained the gift and
  marked unavailable overlength text explicitly. This is a boundary fixture,
  not an assertion that a particular third-party mail contains that much text.
- Keyboard selection of Birthday yielded six synthetic receipts; selecting Leah
  by mouse narrowed that to two. Right-click closed the selector without changing
  those filters. Controller selection also yielded six Birthday entries at
  854x480 logical UI size. The shortened German picker hint fits in full there.
- A host saved normally while the farmhand was disconnected; the synchronized
  offline journal was unchanged and rejoining restored the same farmhand journal.
  An earlier combined restart assertion failed and is retained as failed
  evidence. Journal and occupied-slot assertions were separated; trailing empty
  inventory padding no longer counts as a changed item. The final isolated run
  with only Dearly Kept and its QA companion passed both checks independently:
  host received Mom's Cookie, farmhand received Evelyn's Coffee Maker, host saved
  during the farmhand's absence, then the farmhand rejoined. Both processes
  stopped normally and restarted the same fixture without reset. The exact
  complete journal JSON and every occupied inventory slot matched their original
  pre-disconnect checkpoints on both peers. The older combined failure's precise
  cause is not claimed as established by this clean repeat.
  See `final-offline-{host,farmhand}.log`, `final-restart-{host,farmhand}.log`
  and `final-checkpoint-{host,farmhand}.json` in the evidence directory.

The final public stop/reset succeeded with `state=stopped`, `fixtureReset=true`,
`stagingRemoved=true` and no problems. The shared lab was released.

Evidence logs and screenshots are under `.sdvkit/evidence/readiness-05/` and the
shared lab's isolated screenshot directory. Older failed UI probes ran while a
queued Happy Birthday notification was open; corrected archive probes were
performed after dismissing the real notification.

## Display boundary

After an exclusive-fullscreen test affected another application's display, the
user restricted all further runs to **borderless**, without desktop resolution
changes. The harness no longer exposes exclusive fullscreen or display-resolution
commands. Large UI scales (200% and 300%) provide 1280x720 and 854x480 logical UI
viewports on the unchanged 2560x1440 desktop. These exercise native menu layout,
not a physical 720p monitor or exclusive-fullscreen compatibility claim.

## Original-mod interaction

MO's first-year milestone speech can replace WA's queued anniversary dialogue;
then WA's gift never arrives and Dearly Kept correctly records no invented gift.
The successful calendar run disabled MO's `EnableMilestones` setting in the isolated
fixture to test WA's normal anniversary delivery. Dearly Kept does not modify
the other mods' settings or create substitute gifts.

## Recovered fixture failure and remaining limits

The second network calendar fixture constructed the farmhand's past wedding
date while that peer still had year 1 locally. This produced an invalid negative
season and blocked the game's new-day XML synchronization. The QA fixture now
sets year 2 on both peers and validates the calculated date before mutation.
This is a harness defect, not evidence of a Dearly Kept storage failure.

Public `review stop --topology network-2` timed out. Its exact-process close
fallback returned `MultipleCloseableWindows`. The user closed the two test
instances; both process exits were verified before public reset succeeded.
The corrected calendar/project run completed and stopped/reset normally.
No force kill or guard bypass was used. The failed run is retained in
`.sdvkit/evidence/readiness-05/fixture-invalid-date.log`.

SDVKit 0.9 stages
the same mod set on both network peers, so host-only/farmhand-only installation
is not yet an executable review topology and has no live acceptance claim.
