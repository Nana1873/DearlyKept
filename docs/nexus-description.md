# Dearly Kept — A Lasting Gift Journal

Unreleased 0.3.0 description draft. See the validation report for the accepted
artifact, actual gameplay checks, and source-built integration test scope.

**Those cookies came from Mom. Now you can keep that little memory.**

Dearly Kept keeps a gift journal with the sender, item, quantity, in-game date,
and occasion. Press **K** to browse your memories with item previews and familiar
villager portraits, filter by sender or occasion, and read the message again.

You can eat the cookies. Store, sell, craft with, or give away the other gifts.
Their memories remain in the journal, and the items stack and work as usual.
You do not have to keep anything in your backpack or set aside inventory space.

## What you get

- A persistent journal for 26 supported vanilla gift and thank-you letter IDs.
- Optional support for Happy Birthday **3.21.4**: normal NPC dialogue gifts,
  parent gift mail, and belated birthday mail.
- Optional Marriage Overhaul **1.7.4** birthday and delayed project gifts.
- Optional Wedding Anniversaries **2.1** gifts, with a wedding anniversary filter.
- A separate journal menu with the newest memories first.
- Sender, item, quantity, date, and occasion for each receipt.
- Saved personalized letters and the gift dialogue pages actually shown.
- Available occasion and sender filters, plus a paginated message reader.
- English and German translations.
- Keyboard, mouse, and controller navigation.
- No item tags, stacking changes, or required mod beyond SMAPI for vanilla mail.

## Install

Requires Stardew Valley **1.6.15** and SMAPI **4.5.0+**.
Unzip the `DearlyKept` folder into your game's `Mods` folder and launch with SMAPI.
Press **K** while no other menu or event is open, or enter `dk` in the SMAPI
console. The hotkey can be changed in `config.json`.

The journal starts with gifts received after installation. It does not guess who
gave you an old item or reconstruct past gifts. Each save has its own journal,
saved when the game normally saves. Quitting without saving discards that day's
journal changes too.

The archive is read-only. Select a gift and choose **Read message**; older entries
without saved text show a placeholder. `CaptureMailGifts` and
`CaptureBirthdayGifts` can disable future recording without erasing your memories.
Birthday mail needs both options enabled.

## Compatibility

The current scope is single-player; recording is disabled in multiplayer and
split-screen. Mod authors can extend the exact letter-to-sender dictionary
through Content Patcher for the normal mail viewer. A read-only SMAPI API exposes
`GetGiftCount()` and `GetGiftsJson()` for other mods.

Supported ordinary mail from your current spouse appears under **Spouse**;
birthday gifts remain under **Birthday**. Only categories with matching entries
appear in the filter.

Happy Birthday support is limited to exactly **3.21.4**. Its spouse-party event
is not supported. Direct vanilla spouse handoffs, Winter Star gifts, quest hand-ins,
custom mail-framework menus, and unlisted gift mods are not captured automatically.

Removing Dearly Kept leaves your items unchanged; its journal menu is unavailable
without the mod. Version 0.1 was an internal prototype. Its old item tags are
neither imported into the journal nor modified.

## Development and credits

Code authored with Codex and developed using
[SDVKit](https://github.com/Nana1873/SDVKit). No generated artwork,
voices, dialogue packs, or game textures are distributed.

See the source project's validation report for the exact test scope.
MIT licensed. Game art shown by the interface comes from your installed copy of
Stardew Valley.

---

## Upload preparation notes (remove this section before posting)

- Short description: **Remember gifts and their messages. Browse by sender or
  occasion, read letters again, and use your gifts normally. English and German.**
- Suggested category: User Interface; choose current applicable SMAPI/Quality of
  Life/1.6 compatibility tags in the upload form.
- Lead screenshot: the journal with Mom and Evelyn entries.
- Second screenshot: the message reader showing an actual recorded letter.
- Optional third screenshot: the German archive with an active filter.
- Use the accepted 0.3.0 integration screenshots and its exact tested ZIP; the old
  item-tooltip screenshots describe the internal prototype.
- Add the actual repository and Nexus update IDs only once those pages exist.
- The September 8 journal research found adjacent letter/dialogue archives;
  those features alone are not novel. Avoid advertising a provable "world first"
  or promising any download count.
