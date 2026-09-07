# Supported gift mail

Dearly Kept maps exact, case-sensitive mail IDs to sender IDs. It only marks real
attachments in a mail `LetterViewerMenu`, before the game transfers them into an
inventory. It skips Collections replays, items already held in an inventory, and
items with an existing Dearly Kept note.

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
tests. The executable test cases are listed in [validation](validation.md).

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

## Data format

The original item's `modData["Nana1873.DearlyKept/Provenance"]` stores a JSON object
with `SenderId`, `MailId`, `Year`, `Season`, and `Day`. No custom item types are
serialized. Unrecognized or malformed notes are left intact. Stacking preserves
different raw note values even if this version cannot display them.
