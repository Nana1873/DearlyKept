# Contributing

Use .NET SDK 8 and the installed SDVKit release. In the development workspace,
run these commands from the workspace root:

```powershell
.\sdvkit.cmd project check .\workspaces\DearlyKept --json
.\sdvkit.cmd project build .\workspaces\DearlyKept --project DearlyKept.csproj --json
.\sdvkit.cmd project package .\workspaces\DearlyKept --project DearlyKept.csproj --json
```

Pure provenance checks, from the mod root:

```powershell
dotnet build .\tests\Core\DearlyKept.CoreTests.csproj --configuration Release --artifacts-path .\.sdvkit\tests
dotnet .\.sdvkit\tests\bin\DearlyKept.CoreTests\release\DearlyKept.CoreTests.dll
```

The independent live harness is under `tests/LiveHarness`. Stage it only as an
explicit companion in an SDVKit-owned disposable-world review. It refuses
commands outside that fixture. Never install it into a normal game's Mods folder.
Follow [the live evidence](docs/validation.md) for the exercised scenarios.

SDVKit 0.8.0's review project discovery requires a single code-mod project even
when a build project is selected explicitly. This repository also contains test
projects, so review the **extracted package**, which verifies the distributed
artifact directly. From the workspace root, with no review running:

```powershell
Expand-Archive -LiteralPath '.\workspaces\DearlyKept\.sdvkit\packages\DearlyKept 0.1.0.zip' -DestinationPath '.\workspaces\DearlyKept\.sdvkit\review-new'
.\sdvkit.cmd lab test-save --topology single --json
.\sdvkit.cmd project review start .\workspaces\DearlyKept\.sdvkit\review-new\DearlyKept --topology single --test-save --companion .\workspaces\DearlyKept\tests\LiveHarness --json
.\sdvkit.cmd project review status --json
# Wait for the exact fixture's identityVerified=true and phase=passed.
# Run the documented acceptance cases through review command and input.
.\sdvkit.cmd project review stop --json
.\sdvkit.cmd project review reset --topology single --json
```

Use a new extraction directory for each candidate. Stop/start without reset when
checking saved-item persistence, and reset only after all acceptance work ends.

Generated builds, test output, packages, logs, and screenshots belong below the
owning project's ignored `.sdvkit/` directory. Keep source and in-game text in
English, with localized strings under `i18n/`.

When changing capture or stacking, test receipt beside an identical ordinary
stack, different senders, split and rejoin, closing an unclaimed letter, full
inventory, and persistence through an actual save and a process restart.
Build success and SMAPI loading alone do not verify these behaviors.
