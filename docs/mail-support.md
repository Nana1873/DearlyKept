# Supported gift mail

Dearly Kept 0.3.0 maps exact, case-sensitive mail IDs to sender IDs. It snapshots
real attachments in the normal mail `LetterViewerMenu` before transfer can merge
their stacks, then records the handoff in a separate gift journal. It skips
Collections replays and recovered items that have already been in an inventory.
Opening a preview alone does not create a memory.

For supported letters, the archive stores the viewer's complete personalized
message by concatenating its prepared text pages. The game's `^` line breaks
become newlines; the archive does not rerun mail commands. An ordinary mapped
letter is classified as `spouse` when its resolved sender matches
`Game1.player.spouse` at receipt. Happy Birthday classification takes priority
and remains `birthday`.
This labels already supported mail; direct vanilla spouse handoffs are outside
automatic capture.

Items receive no Dearly Kept tags. Moving, stacking, selling, or consuming an item
does not change its journal entry. The date is the in-game receipt date, not a
reconstructed delivery date. Capture starts after installation; old gifts are
not inferred from inventories or previously read mail.

## Included mappings

These IDs were inspected in the local Stardew Valley 1.6.15 `Data/mail` asset:

| Mail IDs | Sender |
| --- | --- |
| `Caroline`, `Clint`, `Demetrius`, `Emily`, `Evelyn`, `George`, `Gus`, `Jodi`, `Kent`, `Linus`, `Marnie`, `Pam`, `Robin`, `Sandy`, `Shane`, `Wizard` | Same as the mail ID |
| `mom1`, `mom4` | Mom |
| `dad4` | Dad |
| `ClintReward2` | Clint |
| `emilyStones` | Emily |
| `georgeGifts` | **Evelyn**, who sends the thank-you letter |
| `gusGiantOmelet` | Gus |
| `MSB_Lewis` | Lewis |
| `MSB_Pierre` | Pierre |
| `WillyTropicalFish` | Willy |

This is source coverage, not a claim that all 26 letters received individual live
tests. Candidate-specific receipt, persistence, text, and UI evidence is listed
in [validation](validation.md).

## Happy Birthday 3.21.4

The optional adapter requires the exact mod ID `Omegasis.HappyBirthday` and
version `3.21.4`. It records the mod's normal NPC dialogue gifts and these
birthday-mail identities:

| Mail ID | Sender |
| --- | --- |
| `Omegasis.HappyBirthday_Mom` | Mom |
| `Omegasis.HappyBirthday_Dad`, `Omegasis.HappyBirthday_Dad_Married` | Dad |
| `Omegasis.HappyBirthday_BelatedBirthdayWish_<Name>` | The NPC whose exact internal name is `<Name>`, if that character can be resolved |

Both `CaptureMailGifts` and `CaptureBirthdayGifts` must be enabled for birthday
mail. Only real item attachments count: money and invitation-only letters do
not produce entries. Generic fallback text is not treated as a sender.

Normal dialogue gifts are recorded when Happy Birthday completes its handoff,
including its full-backpack debris path. CP token previews and gift getters on
their own do not constitute receipt. The spouse-party event uses a separate
delivery path and is not supported. Other Happy Birthday versions and other
gift mods do not become compatible automatically.

Birthday transcripts contain only pages actually displayed in the associated
dialogue box. The observer neither advances dialogue nor prepares future pages
or evaluates their commands. The stored text is therefore a record of what was
shown, not a reconstruction from raw dialogue tokens.

## Content Patcher extension

A content pack can edit this mod-provided dictionary:

```json
{
  "Action": "EditData",
  "Target": "Mods/Nana1873.DearlyKept/MailSenders",
  "Entries": {
    "Example.Author.ExactMailId": "ExampleNpcInternalName"
  }
}
```

Use the actual sender's internal character name; `Mom` and `Dad` have localized
special labels. The sender's NPC display name and portrait are used when present.
Only add a mapping if the letter actually gives a new gift. Unknown letters are
intentionally left alone. A mod that changes who sent a vanilla letter should
also update or remove its mapping.

The extension uses the standard mail viewer. A separate framework's custom UI or
direct inventory delivery is not automatically supported. This integration
format is provided for authors and has not been tested against third-party packs.

## Data and read-only API

The journal is stored on the receiving farmer in
`Farmer.modData["Nana1873.DearlyKept/GiftJournal"]`. Schema version 1 contains
a `Gifts` list of receipt snapshots. Native multiplayer synchronization carries
that data to the host, including the farmhand's history before disconnecting.
The regular game save persists it to disk; an unsaved session does not persist
its new memories or message updates. The archive UI is read-only. Only the host's
local player can import a pre-0.4 SMAPI `gift-journal` save-data archive, and only
when no player-owned archive exists.

Each entry contains:

| Fields | Meaning |
| --- | --- |
| `Id` | Unique journal-entry ID; separate receipts have separate IDs. |
| `SenderId` | Internal NPC name, or `Mom` / `Dad`. |
| `SourceId` | Exact mail ID or the supported adapter's delivery identifier. |
| `Origin` | `mail`, `birthday`, `spouse`, `anniversary`, or `other`. The UI offers only occasions represented by matching entries. |
| `SourceModId` | The supported producer's mod ID, or `null` for ordinary mapped mail. |
| `Year`, `Season`, `Day` | In-game receipt date. |
| `QualifiedItemId`, `ItemName` | Item identity and a saved name for missing-item fallback. |
| `Quantity`, `Quality` | The received amount and item quality before stack merging. |
| `MessageText` | Optional actual letter text or observed gift dialogue; `null` when unavailable. |
| `MessageIncomplete` | Optional flag for text exceeding the capture limit; defaults to `false` in older entries. |
| `SenderDisplayName`, `SourceDisplayName` | Optional receipt-time display names used when original content is unavailable. |

`MessageText` is an optional, backward-compatible addition to schema 1. Older
entries retain their data and show a translated missing-text placeholder.
Messages are limited to 16,000 characters: an unusually long letter keeps its
gift entry with no body, while a birthday transcript keeps complete displayed
pages that fit the limit. New captures set `MessageIncomplete` in either case
and the reader explains the limitation. The reader wraps and paginates stored text without
requiring the original letter or inventory item to remain available.

The menu creates separate preview items and never moves inventory items. A
removed content mod may make an icon unavailable, but the saved name and receipt
remain readable. No custom game item types are serialized into the journal.

Other SMAPI mods can declare this interface and retrieve it through
`Helper.ModRegistry.GetApi<IDearlyKeptApi>("Nana1873.DearlyKept")` after mods have
initialized:

```csharp
public interface IDearlyKeptApi
{
    int GetGiftCount();
    string GetGiftsJson();
}
```

`GetGiftCount()` returns the number of entries in the loaded journal.
`GetGiftsJson()` returns a JSON array of its entries in stored chronological
order; the menu displays them newest first. The returned entries include optional
`MessageText`. This API provides read-only snapshots of the current local player's
loaded archive. It does not enumerate other players' journals.

Version 0.1 was an internal item-note prototype. Its legacy item tags are neither
imported nor modified. They are not a reliable receipt history and do not become
journal entries retroactively.
