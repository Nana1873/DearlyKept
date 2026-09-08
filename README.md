# Dearly Kept

**A gift journal for the little things people give you.**

Eat Mom's cookies. Store a friend's parcel. Dearly Kept remembers who gave you
what, when, and the original letter or displayed gift dialogue. Press **K** to
revisit those memories without keeping anything in your backpack.

## Features

- A separate journal for each player in each save, with newest gifts first.
- Item previews, available villager portraits, quantities, dates and occasions.
- Combined sender, occasion, year and season filters, plus message/item search.
- A paginated reader that keeps the original captured language and wording.
- In-game settings and hotkey rebinding (F2); keyboard, mouse and controller input.
- English and German. No deletion feature, inventory tags or stacking changes.

## Install

Requires **Stardew Valley 1.6.15** and **SMAPI 4.5.0+**.

1. Install SMAPI.
2. Extract the `DearlyKept` folder into `Stardew Valley/Mods`.
3. Launch through SMAPI and receive a supported gift.
4. Press **K** when no other menu or event is open, or enter `dk` in the SMAPI console.

Recording starts after installation. Past gifts are not reconstructed. Memories
persist with the normal game save; quitting without saving discards that day's
changes. Change capture options and the hotkey in **Settings**, or edit
`config.json` while the game is closed. Removing the mod leaves items unchanged.

## Supported gifts

| Source | Supported deliveries |
| --- | --- |
| Vanilla mail | 26 mapped gift and thank-you letters, including Mom and Evelyn. |
| Happy Birthday **3.21.4** (optional) | Normal NPC birthday gifts, parent and belated gift mail. |
| Marriage Overhaul **1.7.4** (optional) | Birthday gifts and delayed project rewards, including fridge delivery. |
| Wedding Anniversaries **2.1** (optional) | Anniversary dialogue gifts, including normal overflow. |

Optional mods and their requirements are installed separately. Vanilla mail
requires only SMAPI. Other mods can extend the exact mail mapping; see the
[mail and read-only API reference](docs/mail-support.md).

Direct vanilla spouse handoffs, Winter Star gifts, quest hand-ins, custom
mail-framework menus, unlisted gift mods and Happy Birthday's spouse-party event
are not automatically captured. Only displayed dialogue pages can be recorded.
Messages exceeding 16,000 characters show an incomplete-text notice. Missing
custom content retains its saved names.

Single-player and two-player host/farmhand network play were tested, including
saving with the farmhand offline, rejoining and restarting. The tested network
setup has the mod on both peers. One-sided installation and splitscreen are not
yet live-verified. Other gift mods retain their own multiplayer requirements.
Marriage Overhaul milestone dialogue can replace a queued Wedding Anniversaries
gift; Dearly Kept does not change that interaction or invent an undelivered gift.

## Development and credits

[Build and contribute](CONTRIBUTING.md) · [Report an issue](https://github.com/Nana1873/DearlyKept/issues)

Code authored with Codex and developed using [SDVKit](https://github.com/Nana1873/SDVKit).
No generated artwork or redistributed game textures are included. The interface
uses your installed game's assets. Thanks to the SMAPI and gift-mod authors.

[MIT license](LICENSE), copyright 2026 Nana1873. Stardew Valley and its assets
belong to their respective owners.
