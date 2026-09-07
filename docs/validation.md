# Validation

## Gift journal 0.2.0

The 0.2.0 candidate replaces inventory-bound provenance with an independent,
save-specific journal. Its gameplay and visual checks are being performed afresh;
the older prototype evidence does not validate the new capture or persistence.

Date: **2026-09-08**. Installed tooling: **SDVKit 0.8.0**; target runtime:
Stardew Valley **1.6.15.24356**, SMAPI **4.5.2**, Windows x64.

Completed so far:

- SDVKit schema check passed for the manifest and English/German i18n.
- Production build/package passed with zero warnings and zero errors.
- All 47 independent journal model tests passed, including durable JSON data,
  repeated callbacks, separate identical gifts, deletion and save separation.
- Package contains only the production DLL, manifest and two locale files.
- Local game IL confirms the two observed letter methods hand attachments to
  the farmer or overflow menu. Postfix observers run after successful return.
- Happy Birthday hooks were checked against the author's exact source commit
  `a36cbed260e7aedc311ec182b436dd331d39472a`, version 3.21.4.

The optional live dependency set is source-built Happy Birthday 3.21.4,
source-built Stardust Core 3.1.1, Content Patcher 2.9.1 and the author's English
content pack 2.0.4. Happy Birthday/Stardust Core source files match that commit;
only build configuration was adjusted. These are **not verified Nexus release
binaries**, so any live result applies to this documented source build.
Provenance and hashes: `.sdvkit/integration-dependencies/README.md`.

Pending: real mail and birthday receipt, preview exclusion, normal stacking,
overflow, saved history after inventory removal and process restart, plus
English/German journal controls and layout at 100%/150% UI scale.

## Earlier prototype

[The 0.1.0 evidence](validation-0.1.0.md) is retained for the previous item-note
prototype. Its archive is unchanged. It is not a release acceptance result for
0.2.0.
