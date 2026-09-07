# Dearly Kept

**Remember who sent your gifts.**

The cookies Mom mailed you look just like any other cookies. Dearly Kept keeps a
small note on the actual gift: who sent it, when you opened the letter, and that
it arrived by mail. Hover over it in your inventory, or press **K** while playing
to browse the keepsakes in your backpack.

## Features

- Automatically records the sender and in-game date on supported mail attachments.
- Adds the note to the game's normal item tooltip.
- Keeps gifts from different senders or dates separate from ordinary item stacks.
- Provides a keepsake browser with item icons, villager portraits where available,
  and a deliberate two-step option to remove a note.
- Stores notes on the original items using SMAPI's normal `modData`; splitting an
  item stack retains its note.
- Includes English and German text. Other languages fall back to English.

## Install and use

Requires **Stardew Valley 1.6.15** and **SMAPI 4.5.0 or later**. Tested runtime and
acceptance scope are recorded in [validation](docs/validation.md).

1. Install SMAPI.
2. Extract the release ZIP into `Stardew Valley/Mods`.
3. Launch through SMAPI and collect a supported gift letter.
4. Hover over the gift, or press **K** when no other menu is open.

No other mod is required. There is no new game item, currency, or friendship
bonus. The sender is determined from the letter's exact internal ID.

The keepsake browser shows your **current backpack**. Store a gift in a chest and
its note stays with it; put it back in your backpack to browse or remove the note.
Removing a note does not delete the item. It allows normal stacking again.

## Which gifts count?

Version 0.1.0 supports 26 known vanilla gift and thank-you letter IDs, including
Mom's cookies, regular friendship parcels, and Evelyn's thank-you gift from the
leek order. See the exact list and extension format in
[mail support](docs/mail-support.md).

Direct spouse gifts, the Feast of the Winter Star, quest hand-ins, old items you
already own, and arbitrary mail-framework attachments are outside this version's
automatic capture. A note records the **opening date**, not the delivery date.
Rereading a letter in Collections does not create or stamp items.

## Settings

After the first launch, edit `config.json` while the game is closed:

| Setting | Default | Effect |
| --- | --- | --- |
| `OpenKeepsakes` | `K` | SMAPI key binding for the backpack keepsake browser. |
| `CaptureMailGifts` | `true` | Add notes to newly opened supported gifts. |
| `ShowGiftNotesInTooltips` | `true` | Display notes in normal item tooltips. |

SMAPI console: `dk` opens the browser; `dk_status` reports tagged backpack stacks.
If K is used by another mod, choose a different key or combination in the config.

## Compatibility and removal

The mod uses narrow Harmony hooks for letter attachment capture, item stacking,
and normal item tooltip text. It does not replace maps, textures, schedules, or
gift tastes. Mods which replace those methods or skip the standard stacking API
need separate compatibility testing. Multiplayer and split-screen are not yet
validated; this release is intended for single-player.

To uninstall, remove the mod folder. Items remain ordinary vanilla items; the
small metadata entry is inert without the mod. Vanilla stacking can then merge
previously separate gifts and discard their distinction. Reinstalling cannot
recover a lost note. Consuming, crafting with, or selling a gift is still allowed;
this mod is not an item lock or an archive of consumed gifts.

## Development

Source, build steps, and test procedures: [CONTRIBUTING.md](CONTRIBUTING.md).
Novelty and adjacent-mod research: [research notes](docs/novelty-research.md).

Code was authored with Codex and tested with
[SDVKit](https://github.com/Nana1873/SDVKit). No generated artwork or redistributed
game textures are included. The interface draws from the installed game's assets.

MIT license. Stardew Valley and its assets belong to their respective owners.
