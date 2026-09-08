# Spouse gift integrations

Both adapters are optional. They observe the producer's actual item handoff,
snapshot the item before stack merging, and retain prepared dialogue pages only
as the game displays them. They never run dialogue commands to reconstruct text.
Unknown versions disable that adapter with a warning; existing memories remain
readable without the producer installed.

| Producer | Supported deliveries | Occasion | Setting |
| --- | --- | --- | --- |
| Marriage Overhaul 1.7.4 (`TitanmasterRy.MarriageOverhaul`) | Player birthday and delayed project rewards; inventory or fridge fallback | `birthday` or `spouse` | `CaptureMarriageOverhaulGifts` |
| Wedding Anniversaries 2.1 (`Kantrip.WeddingAnniversaries`, normalized as 2.1.0 by SMAPI) | Gifts in its anniversary dialogue, including queued vanilla overflow | `anniversary` | `CaptureAnniversaryGifts` |

Both settings default to `true`. No producer is required for ordinary mail.
Wedding Anniversaries reminders create no receipts. Pushing an anniversary
dialogue onto an NPC alone creates no receipt: the native gift command must
actually deliver the item. Multiple callbacks for the same delivery retain one
receipt. Gifts from repeated genuine deliveries retain separate identities.

Marriage Overhaul birthday and project dialogue can be deferred until the player
has control. The observer follows that exact queued display, without matching
unrelated dialogue by text. If no matching dialogue is displayed, the gift stays
in the archive with no message. Forage chores, cooking chores, other Marriage
Overhaul systems, and Happy Birthday spouse-party events are outside this scope.
These producers have been tested in single-player. Dearly Kept isolates capture
state per local screen, but this does not establish the producers' multiplayer
or splitscreen compatibility.

## Dependency provenance

The isolated development companions were built from upstream source:

- [Marriage Overhaul](https://github.com/TitanmasterRy/MarriageOverhaul), commit
  `f9e2f0e2673acce93cf616b44bb67e04dfbc647d`, original manifest 1.7.4.
- [Wedding Anniversaries](https://github.com/Kantrip-Mods/SDV), commit
  `c7fafd097d6c1cebf5887f6ddda9615545d22bc3`, whose patch notes identify 2.1.
  This checkout omits a manifest; the isolated companion uses a reconstructed
  manifest with its documented mod ID and version. Producer C# was unchanged.

The initial 0.3 tests used these source builds, not binary equivalence verification
against the downloaded Nexus archives. Neither companion, its source, nor the
QA harness is included in the Dearly Kept release ZIP.

The owned live fixture invokes the real birthday, pending-reward, and anniversary
producer methods. It supplies a QA-authored two-page project reward and otherwise
uses the companions' normal text and gift selection. This verifies delivery and
display paths. The additional 0.4 original-Nexus and calendar checks are recorded
in [validation](validation.md).
