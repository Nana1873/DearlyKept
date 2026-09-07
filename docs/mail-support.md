# Supported gift mail

Dearly Kept 0.2.0 maps exact, case-sensitive mail IDs to sender IDs. It snapshots
real attachments in the normal mail `LetterViewerMenu` before transfer can merge
their stacks, then records the handoff in a separate gift journal. It skips
Collections replays and recovered items that have already been in an inventory.
Opening a preview alone does not create a memory.

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
tests. Version 0.2.0's journal acceptance is pending. The candidate-specific
evidence is listed in [validation](validation.md).

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

The journal is stored per save through SMAPI's save-data API under this mod's
`gift-journal` key. Schema version 1 contains a `Gifts` list of receipt snapshots.
The regular `Saving` event persists new entries and confirmed deletions; an
unsaved session does not persist them.

Each entry contains:

| Fields | Meaning |
| --- | --- |
| `Id` | Unique journal-entry ID; separate receipts have separate IDs. |
| `SenderId` | Internal NPC name, or `Mom` / `Dad`. |
| `SourceId` | Exact mail ID or the supported adapter's delivery identifier. |
| `Origin` | `mail` or `birthday`. |
| `SourceModId` | `Omegasis.HappyBirthday` for its supported gifts; otherwise `null`. |
| `Year`, `Season`, `Day` | In-game receipt date. |
| `QualifiedItemId`, `ItemName` | Item identity and a saved name for missing-item fallback. |
| `Quantity`, `Quality` | The received amount and item quality before stack merging. |

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
order; the menu displays them newest first. These are read-only snapshots, not
an API for adding, editing, or deleting gifts. Only the current single-player
save is supported.

Version 0.1 was an internal item-note prototype. Its legacy item tags are neither
imported nor modified. They are not a reliable receipt history and do not become
journal entries retroactively.
