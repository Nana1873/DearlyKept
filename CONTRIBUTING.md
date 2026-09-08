# Contributing

Use .NET SDK 8 and the installed SDVKit release. In the development workspace,
run these commands from the workspace root:

```powershell
.\sdvkit.cmd project check .\workspaces\DearlyKept --json
.\sdvkit.cmd project build .\workspaces\DearlyKept --project DearlyKept.csproj --json
.\sdvkit.cmd project package .\workspaces\DearlyKept --project DearlyKept.csproj --json
```

Run the pure journal checks from the mod root:

```powershell
dotnet build .\tests\Core\DearlyKept.CoreTests.csproj --configuration Release --artifacts-path .\.sdvkit\tests
dotnet .\.sdvkit\tests\bin\DearlyKept.CoreTests\release\DearlyKept.CoreTests.dll
```

The independent live harness is under `tests/LiveHarness`. Stage it only as an
explicit companion in an SDVKit-owned disposable-world review. It refuses
commands outside that fixture. Never install it into a normal game's Mods folder.
For synthetic controller input, `dkqa controller on` selects the game's ForceOn
mode for the current runtime to avoid Auto-mode disconnect pauses. Use
`dkqa controller restore` before saving or stopping. This is a fixture setting,
not a mod requirement or production behavior change.
Acceptance is recorded per candidate and capability in
[validation](docs/validation.md). Earlier receipt and persistence results do not
establish the newer text-capture, filter, or message-view behavior.

## Isolated review

Review an extracted package to exercise the distributed artifact. This also
exercises the same packaged production files that users receive. From the
workspace root, with no review running:

```powershell
Expand-Archive -LiteralPath '.\workspaces\DearlyKept\.sdvkit\packages\DearlyKept 0.2.0.zip' -DestinationPath '.\workspaces\DearlyKept\.sdvkit\review-new'
.\sdvkit.cmd lab test-save --topology single --json
.\sdvkit.cmd project review start .\workspaces\DearlyKept\.sdvkit\review-new\DearlyKept --topology single --test-save --companion .\workspaces\DearlyKept\tests\LiveHarness --json
.\sdvkit.cmd project review status --json
# Wait for the exact fixture's identityVerified=true and phase=passed.
# Exercise the acceptance cases through review command and actual game input.
.\sdvkit.cmd project review stop --json
.\sdvkit.cmd project review reset --topology single --json
```

Use a new extraction directory for each candidate. For persistence, first save
through normal sleep, then stop and restart without resetting the fixture.
Reset only after all acceptance work ends. A forced save helper alone does not
exercise the regular `Saving` lifecycle.

Generated builds, test output, packages, logs, and screenshots belong below the
owning project's ignored `.sdvkit/` directory. Keep source and documentation in
English, with localized in-game strings under `i18n/`.

## Journal acceptance cases

- Receive a supported letter beside an identical ordinary stack. Verify normal
  merging and one journal entry with the sender, quantity, quality, item, date,
  and occasion captured before that merge changes the live item.
- Receive the same item from different senders, and receive another gift from
  the same sender. These are separate memories even if the items share a stack.
- Open a letter without claiming its attachment, claim it, close it with an
  attachment remaining, and replay it in Collections. Verify the standard
  handoff behavior and no preview-only or duplicate entries.
- Fill the backpack and exercise the real overflow flow. Verify receipt at the
  supported handoff and no duplicate when the item is later moved or collected.
- Store, move, consume, sell, and craft with received items. Journal entries
  must remain; no item metadata or stack rules may be changed by Dearly Kept.
- Save through normal sleep and restart the game. Verify journal persistence,
  save isolation, and that quitting an unsaved day does not preserve its entries.
- Filter by occasion and exact sender identity. Offer only available choices,
  preserve selection when possible, and verify browsing changes neither journal
  contents nor inventory items. Supported ordinary spouse mail uses `spouse`;
  Happy Birthday receipts retain `birthday` even when the sender is the spouse.
- Check newest-first ordering, stable selection when the journal changes,
  quantity display, missing-mod-item name fallback, and the empty state. Exercise
  English and German at 1280x720 and 1920x1080, including 150% UI scale, with
  single keyboard and controller presses, available filters, and message pages.
- Compare stored letters with the actual personalized viewer text, including all
  letter pages and visible line breaks. For Happy Birthday, compare only pages
  actually displayed; observation must not advance dialogue or prepare future
  pages. Exercise long messages, pagination, and entries without saved text.
- Load schema-1 entries without `MessageText`, save again, and verify their
  identities and other fields remain intact. Test the 16,000-character text limit:
  an oversized letter must retain its gift entry with a null body; birthday
  transcripts retain complete displayed pages that fit.
- Toggle each capture setting and inspect `GetGiftCount()` / `GetGiftsJson()`.
  Existing entries remain readable, and the API exposes no mutation methods.
- With Happy Birthday exactly 3.21.4 staged as an explicit companion, exercise
  normal NPC dialogue gifts, full-backpack delivery, parent mail, and belated
  mail. Check CP previews and direct getter calls do not create journal entries.
  Verify that a missing or unsupported Happy Birthday version leaves vanilla
  mail working and its optional adapter disabled.

See [spouse integrations](docs/spouse-integrations.md) for the two optional
spouse adapters and their source-built test dependencies.

Happy Birthday spouse-party events, unlisted gift mods, multiplayer, and
split-screen are outside the current supported scope. Do not turn these cases
into compatibility claims without implementing and separately accepting their
delivery paths.

Build success, schema checks, confirmed SMAPI loading, actual gameplay, and visual
acceptance are separate results. Report each at the scope actually exercised.
