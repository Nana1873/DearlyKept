# Dearly Kept

**A gift journal for the little things people give you.**

Eat Mom's cookies. Put a friend's parcel away for later. Dearly Kept remembers
the gift in a separate journal: who gave it, what it was, how many you received,
and the in-game date and occasion. Press **K** to revisit those memories.

The tested runtime and current acceptance scope are recorded in
[validation](docs/validation.md).

## Features

- Records supported gifts as you receive them, starting after installation.
- Keeps the journal after you store, sell, eat, craft with, or give away a gift.
- Leaves item stacking and inventory space unchanged.
- Opens a standalone menu with item previews, sender names, available portraits,
  quantities, dates, and occasions. Newest entries appear first.
- Select memories by occasion, sender, year and season, or search items, senders
  and saved message text. Filters can be combined.
- Change capture options and the journal hotkey through its Settings button.
- Keeps the actual gift letter or displayed gift dialogue for later reading
  in a separate, paginated message view.
- Keeps a separate journal for each player in each save, persisted during the game's regular save.
- Supports keyboard, mouse, and controller navigation, with English and German
  text. Other languages fall back to English.

Saved messages keep the language and personalized wording shown when received,
even if you later change the game's language.
Unavailable custom characters and items retain their recorded names. Messages
which exceed the 16,000-character storage limit show an explicit notice in the
reader. An archive which cannot safely load shows a recording-paused notice.

The current 0.5 candidate and its remaining acceptance gates are tracked in
[the readiness checklist](docs/readiness-0.5.md).

## Install and use

Requires **Stardew Valley 1.6.15** and **SMAPI 4.5.0 or later**.

1. Install SMAPI.
2. Extract the release ZIP into `Stardew Valley/Mods`.
3. Launch through SMAPI and receive a supported gift.
4. Press **K** while no other menu or event is open, or use `dk` in the SMAPI
   console, to open your journal.

No other mod is required for vanilla gift mail. Gifts remain ordinary items:
they can stack together and do not need to stay in your backpack. Use the
**Occasion** and **Sender** buttons to narrow the list, select a gift, and choose
**Read message**. The archive is read-only.

New memories persist at the next regular game save; quitting without saving
also discards that day's journal changes. Older entries without message text
remain readable and show a placeholder in the message view.

## Which gifts count?

- **Vanilla gift mail:** 26 known gift and thank-you letter IDs, including Mom's
  cookies and Evelyn's thank-you gift from the leek order.
- **Content Patcher mail:** authors can extend the exact letter-to-sender
  dictionary for attachments delivered through the normal mail viewer.
- **Happy Birthday 3.21.4, optionally:** ordinary birthday gifts given through
  NPC dialogue, plus its parent and belated birthday gift letters. The adapter
  activates only for that exact version of `Omegasis.HappyBirthday`.
- **Marriage Overhaul 1.7.4, optionally:** birthday gifts and delayed project
  rewards, including delivery to the fridge when the backpack is full.
- **Wedding Anniversaries 2.1, optionally:** gifts from its anniversary dialogue,
  including the normal inventory overflow menu. These use **Anniversary**.

The new spouse adapters have a separate [support and validation scope](docs/spouse-integrations.md).

See [mail support](docs/mail-support.md) for the included IDs and extension
format. The journal records actual gifts, not money, invitations, or previews.
Rereading a letter in Collections does not create another entry.

A supported ordinary letter from your current spouse appears under **Spouse**.
Happy Birthday gifts retain the **Birthday** occasion, including gifts from
your spouse. This classification does not add support for other delivery paths.

Happy Birthday's spouse-party event is not supported. Direct vanilla spouse
handoffs, the Feast of the Winter Star, quest hand-ins, custom mail-framework menus,
and unlisted gift mods are not captured automatically. Existing items and past
gifts are not reconstructed.

## Settings

Use the journal's **Settings** button (F2), or edit `config.json` while the game
is closed after its first launch. The in-game rebind accepts one key; the file
also accepts SMAPI key combinations.

| Setting | Default | Effect |
| --- | --- | --- |
| `OpenKeepsakes` | `K` | SMAPI key binding for the gift journal. |
| `CaptureMailGifts` | `true` | Record newly received supported mail attachments. |
| `CaptureBirthdayGifts` | `true` | Record gifts supported by the Happy Birthday adapter. |
| `CaptureMarriageOverhaulGifts` | `true` | Record supported Marriage Overhaul birthday and project gifts. |
| `CaptureAnniversaryGifts` | `true` | Record Wedding Anniversaries dialogue gifts. |

Happy Birthday mail requires both capture settings. Disabling capture does not
remove existing journal entries. `dk_status` reports the current journal and
adapter status. If K is used by another mod, choose another key or combination.

## Compatibility and removal

Single-player and host/farmhand network play are supported. Install Dearly Kept
on each player who wants to record gifts. Each player sees their own archive;
the game synchronizes its storage with the host. Native mail, disconnect/rejoin,
and saving/restarting were tested with two local network processes. The supported
Happy Birthday, Marriage Overhaul and Wedding Anniversaries deliveries were also
tested on both peers, including separate texts and inventories. These checks do
not establish every producer's multiplayer calendar or quest behavior. Selected
natural birthdays, anniversaries and complete delayed requests passed in the
0.5 fixture. Host-only and farmhand-only installation still lack live acceptance;
the installed SDVKit release stages identical mod sets on both peers.
Splitscreen state is isolated in code, but live
acceptance is pending because SDVKit 0.9.0 rejects that test topology.

Dearly Kept does not add item tags, patch stacking, or alter normal inventory
tooltips. It snapshots gifts at supported delivery points and stores the journal
in the receiving farmer's save data. The host automatically imports a pre-0.4
save-level archive when no player archive exists; farmhands never import it.
Malformed or unsupported data is preserved with recording disabled for that
session. There are no new game items, maps, textures, currencies, or
friendship bonuses.

Removing the mod leaves your items unchanged; the journal UI is unavailable
without it. Version 0.1 was an internal prototype. Its old item tags are neither
imported nor modified, and they are not used to guess historical journal entries.

## Integration and development

Other mods can read the loaded journal through the read-only SMAPI API methods
`GetGiftCount()` and `GetGiftsJson()`. This API does not add, remove, or modify
entries. Its JSON includes optional saved message text. See
[the data and API reference](docs/mail-support.md#data-and-read-only-api).

Build steps and acceptance cases: [CONTRIBUTING.md](CONTRIBUTING.md).
Novelty and adjacent-mod research: [research notes](docs/novelty-research.md).

Code was authored with Codex and developed using
[SDVKit](https://github.com/Nana1873/SDVKit). No generated artwork or redistributed
game textures are included. The interface draws from the installed game's assets.

MIT license. Stardew Valley and its assets belong to their respective owners.
