# Contributing

Use .NET SDK 8, an installed copy of Stardew Valley, and the installed SDVKit
release. Keep normal saves and normal Mods outside development operations.

From the Stardew development workspace root:

```powershell
.\sdvkit.cmd project check .\workspaces\DearlyKept --json
.\sdvkit.cmd project build .\workspaces\DearlyKept --project DearlyKept.csproj --json
.\sdvkit.cmd project package .\workspaces\DearlyKept --project DearlyKept.csproj --json
```

From the mod root, run the standalone core checks without game assemblies:

```powershell
dotnet build .\tests\Core\DearlyKept.CoreTests.csproj -c Release --artifacts-path .\.sdvkit\tests
dotnet .\.sdvkit\tests\bin\DearlyKept.CoreTests\release\DearlyKept.CoreTests.dll
```

QA companions under `tests/LiveHarness` and `tests/MultiplayerHarness` are for
explicit SDVKit-owned disposable fixtures only. Never install them in a normal
Mods folder. Use the installed release's public review start/status/stop/reset
lifecycle and distinguish compilation from actual gameplay and visual checks.
Use borderless mode only in the shared workspace, without desktop resolution
changes. Keep generated builds, saves, logs and screenshots under `.sdvkit/`.

Before reporting a change as tested, exercise its actual affected delivery,
message, UI or save path. The core suite currently has 74 checks. For network
persistence, test separate receipts on both peers, normal saving, disconnect /
rejoin and complete process restart. A build alone does not prove those paths.

Keep code, commits and documentation in English. Translate visible text through
`i18n/`. New integrations must identify real deliveries and preserve ordinary
item behavior. The journal has no delete feature and its public API is read-only.

The [mail/API reference](docs/mail-support.md) describes the stored schema and
extension points. Contributions are under this repository's MIT license.
