# Novelty research

Research date: **2026-09-07**

## Scope and conclusion

Dearly Kept's selected concept is **automatically preserving the sender and the
in-game receipt date on existing items received as mail gifts**. A normal item
can therefore carry a personal history such as "From Evelyn · Spring 8, Year 2"
without introducing a replacement item or changing its value.

The research found **no direct functional match for this specific concept** in
the reviewed web results and mod descriptions. That is a bounded search finding,
not proof that no such mod exists. Unindexed, unpublished, deleted, differently
named, or insufficiently documented projects may have been missed. A complete
title or source-code audit of the Stardew mod dataset was **not performed**.

The novelty claim concerns automatic provenance attached to the received item.
It does not claim that NPC gifts, keepsake objects, mail delivery, item labels,
or journals are new concepts. Spouse dialogue gifts, festival gifts, and arbitrary
third-party gift systems are outside this selected initial scope.

## Closest reviewed alternatives

| Existing project | Documented behavior | Difference from the selected concept |
| --- | --- | --- |
| [Jas's Keepsake](https://www.curseforge.com/stardewvalley/mods/cp-jass-keepsake) | Adds a cupcake recipe and a personal stuffed bunny decoration unlocked through friendship and letters. | Creates particular new keepsake content; its description does not document automatic sender/date provenance for existing mail-gift items. |
| [Relationship Tooltips](https://www.curseforge.com/stardewvalley/mods/relationship-tooltips) | Displays relationship information and tracks learned reactions to gifts the player gives NPCs. | Tracks the opposite direction of gifting and NPC preferences, rather than the history of an item the player receives. |
| [Pen Pals](https://github.com/focustense/StardewPenPals) | Lets players mail gifts to NPCs, including delivery rules and returns. | Provides outgoing gift delivery; its documented features do not include provenance labels on incoming gifts. |
| [Simple Bags and Baskets](https://www.nexusmods.com/stardewvalley/mods/45590) | Adds portable containers, including a Keepsakes Box for museum donations. | The keepsake terminology describes storage, not sender/date records attached to received items. |

These comparisons are based on the linked public descriptions. They are not
claims that every implementation detail or optional integration was inspected.

## Other concepts rejected during selection

| Considered direction | Existing project and reason for rejection |
| --- | --- |
| Actual walking routes becoming visible over time | [Pathfinder Valley](https://www.nexusmods.com/stardewvalley/mods/50678) records visits to outdoor tiles and develops frequently used routes into paths. A purely analytical heatmap would differ, but the underlying repeated-footstep concept overlapped too much for this project's strict novelty requirement. |
| Automatic daily recap or return-to-save summary | [ValleyRecap](https://www.nexusmods.com/stardewvalley/mods/51271) already records daily financial and progression changes and presents a recent journal. [Farmer's Notebook](https://www.nexusmods.com/stardewvalley/mods/48121) and [Stardew Dashboard](https://www.nexusmods.com/stardewvalley/mods/43158) also occupy nearby planning and summary territory. |
| Interacting with residents behind locked doors | [Door Knock](https://www.nexusmods.com/stardewvalley/mods/46880) makes residents answer doors; [Closed Door Gifting](https://www.nexusmods.com/stardewvalley/mods/44119) supports gifting through locked doors. |
| Queuing several jobs in a busy machine | [Load It Up](https://www.nexusmods.com/stardewvalley/mods/51221) already provides queued machine jobs and reserves their ingredients. |
| A shopping cart before checkout | [Joja Express](https://www.nexusmods.com/stardewvalley/mods/23614) documents a cart and checkout workflow for its shopping services. |

Finding recent counterexamples, particularly Pathfinder Valley and ValleyRecap,
changed the concept selection. Those ideas were not retained under new names.

## Compact query log

The following search families were used during concept selection. Queries were
run through web search; some used explicit Nexus or GitHub domain restrictions.
Results were reviewed for the described function, not just matching names.

| Purpose | Representative queries |
| --- | --- |
| Gift provenance | `Stardew mod "gift" "provenance"`; `Stardew "mod" "item provenance"`; `site:nexusmods.com/stardewvalley "gifted by"`; `Stardew mod "gift" "origin" tooltip` |
| Sentimental item alternatives | `Stardew mod "sentimental"`; `Stardew mod "memento"`; `Stardew mod "gift" "keepsake"`; `site:github.com Stardew "keepsake"`; `"Stardew" mod "keepsakes" -memento -Birdie` |
| Item annotations and memories | `"Stardew" mod "gift tags"`; `"Stardew" mod "item notes"`; `"Stardew" mod "souvenir"`; `Stardew "mod" "item memories"`; `Stardew "mod" "gift memories"`; `Stardew "mod" "personalized items"` |
| Incoming-gift tracking | `"Stardew" mod "received gifts"`; `"Stardew" mod "gift tracker" "received"`; `"Stardew" mod "signed" "gift"`; `"Stardew" mod "gift" "timestamp"` |
| Walking-route counterexamples | `Stardew Valley mod "heatmap"`; `"Stardew" "heat map"`; `Stardew Valley mod "desire paths"`; `site:github.com Stardew "heatmap"` |
| Recap counterexamples | `site:nexusmods.com/stardewvalley "recap"`; `site:nexusmods.com/stardewvalley "session" "summary"` |

The [Stardew mod dataset](https://github.com/Pathoschild/StardewModDataset) was
identified as a possible source for a broader future metadata audit, but was not
downloaded or exhaustively searched for this assessment. Search results are not
an inventory of every mod on Nexus Mods, CurseForge, ModDrop, or GitHub.

## Product hypothesis and limits

There is a plausible audience for a small, unobtrusive mod that makes an ordinary
gift feel personal and lets players distinguish it from otherwise identical
items. The benefit is easy to demonstrate with a before/after inventory image,
and it fits players who keep gifts as reminders of their relationships.

This is a product hypothesis, not a download forecast. The research does not
establish a conversion rate, discovery reach, time to adoption, or expected
unique-download count. No Nexus unique-download statistics were verified for
this assessment. Presentation, compatibility, localization, discoverability,
and the quality of the released mod will affect whether the idea finds users.

The implementation must earn the promise: only attribute a sender when the
source is known, preserve the identity of the actual received items through
stacking and storage, and keep uncertain sources unlabeled. Build success,
confirmed SMAPI loading, observed gameplay behavior, and visual acceptance
remain separate evidence requirements; this research establishes none of those
implementation results.
