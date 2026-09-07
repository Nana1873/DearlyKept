using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private const string LongMailId = "DKQA_long";
    private const string MailSendersAsset = "Mods/Nana1873.DearlyKept/MailSenders";
    private string? archiveFixtureId;
    private const string LongMailText = "Dear @,^This is a QA-authored letter for an isolated test farm. "
        + "It is deliberately longer than one page so the gift journal can be checked against every part of the real letter, including the final sentence.^"
        + "The garden looked lovely this morning. I put a chair beside the flowers and watched the bees wander from one blossom to the next. "
        + "A little patience makes such a difference, whether you are growing something new or learning your way around the valley.^"
        + "I thought you might enjoy a Cookie while you take a break. You can eat it, share it, or put it away for later. "
        + "The letter should still be waiting in your journal long after the last crumb is gone. Its words belong with the memory of receiving it.^"
        + "Before you finish, turn through the remaining pages and check that this paragraph appears in the archive too. "
        + "The name at the top should already be yours, the paragraph breaks should remain readable, and the attachment command should never become part of the message.^"
        + "This is the final sentence of the long QA letter; keeping it proves the snapshot includes the ending.^Warm wishes,^Evelyn"
        + "%item object 223 1 %%[#]QA archive pagination letter";

    private void RunArchiveCommand(string[] args, GuardContext owned)
    {
        RequireNoMenu();
        switch (args.ElementAtOrDefault(1)?.ToLowerInvariant())
        {
            case "longmail":
                archiveFixtureId = owned.Identity.FixtureId;
                Helper.GameContent.InvalidateCache("Data/mail");
                Helper.GameContent.InvalidateCache(MailSendersAsset);
                Monitor.Log("DKQA enabled the explicitly QA-authored DKQA_long letter and its Evelyn sender mapping only for this verified fixture. Run mail DKQA_long, turn through the actual pages and receive its real Cookie, then archive assert. No journal entry or marriage state was fabricated.", LogLevel.Info);
                break;
            case "assert":
                JournalSnapshot baseline = journalReceiptBaseline
                    ?? throw new InvalidOperationException("Run prepare before the actual archive receipt scenario.");
                JournalEntry[] entries = NewJournalEntries(baseline);
                Check(entries.Length > 0, "actual new gift receipts exist for the archive scenario");
                JournalEntry[] mailEntries = entries.Where(entry => mailAttempts.Any(attempt => attempt.SourceId == entry.SourceId)).ToArray();
                var expectedMail = mailAttempts.OrderBy(p => p.SourceId).ThenBy(p => p.ItemId).ThenBy(p => p.Quantity).ToArray();
                var actualMail = mailEntries.Select(entry => new MailReceipt(entry.SourceId, entry.QualifiedItemId, entry.Quantity, entry.Quality, entry.MessageText))
                    .OrderBy(p => p.SourceId).ThenBy(p => p.ItemId).ThenBy(p => p.Quantity).ToArray();
                Check(expectedMail.SequenceEqual(actualMail), "every actual opened letter has exactly matching archived source, attachment and full prepared text");
                if (birthdayAttempt?.FinalDialogueText is { } birthdayText)
                {
                    JournalEntry? birthdayEntry = entries.LastOrDefault(entry => entry.SourceId == "birthday:" + birthdayAttempt.Sender);
                    Check(birthdayEntry?.MessageText == birthdayText, "the received birthday gift retains the exact independently observed displayed greeting");
                }
                foreach (JournalEntry entry in entries)
                {
                    Check(!string.IsNullOrWhiteSpace(entry.MessageText), $"new archive receipt {entry.SourceId} contains its message");
                    Check(!entry.MessageText!.Contains("%item", StringComparison.OrdinalIgnoreCase)
                        && !entry.MessageText.Contains("[#]", StringComparison.Ordinal)
                        && !entry.MessageText.Contains("#$b#", StringComparison.Ordinal)
                        && !entry.MessageText.Contains("#$e#", StringComparison.Ordinal)
                        && !entry.MessageText.Contains('^'), $"archive receipt {entry.SourceId} contains no item command, title marker or unprepared line breaks");
                    Monitor.Log($"DKQA archived receipt={JsonSerializer.Serialize(entry)}", LogLevel.Info);
                }
                Check(Game1.player.Items.Where(p => p is not null).All(p => Raw(p) is null), "archive capture leaves ordinary item metadata untouched");
                Monitor.Log("DKQA ARCHIVE ASSERT PASS. Legacy records before prepare may correctly lack MessageText; this check only covers new actual receipts with independent view snapshots.", LogLevel.Info);
                break;
            default:
                throw new InvalidOperationException("Usage: dkqa archive longmail | assert.");
        }
    }

    private void EditArchiveFixtureMail(object? sender, AssetRequestedEventArgs e)
    {
        if (archiveFixtureId is null || (!e.NameWithoutLocale.IsEquivalentTo("Data/mail") && !e.NameWithoutLocale.IsEquivalentTo(MailSendersAsset)))
            return;
        try
        {
            RequireArchiveFixture();
            bool senderMap = e.NameWithoutLocale.IsEquivalentTo(MailSendersAsset);
            e.Edit(asset =>
            {
                RequireArchiveFixture();
                asset.AsDictionary<string, string>().Data[LongMailId] = senderMap ? "Evelyn" : LongMailText;
            }, AssetEditPriority.Late + 100);
        }
        catch (Exception ex)
        {
            Monitor.Log($"DKQA archive asset edit rejected: {ex.Message}", LogLevel.Error);
        }
    }

    private void RequireArchiveFixture()
    {
        if (RequireOwnedFixture().Identity.FixtureId != archiveFixtureId)
            throw new InvalidOperationException("No archive test assets belong to this exact verified fixture.");
    }

    private void ClearArchiveFixtureCache()
    {
        bool hadFixture = archiveFixtureId is not null;
        archiveFixtureId = null;
        if (hadFixture)
        {
            Helper.GameContent.InvalidateCache("Data/mail");
            Helper.GameContent.InvalidateCache(MailSendersAsset);
        }
    }

    private static string SnapshotMailText(LetterViewerMenu letter) => string.Concat(letter.mailMessage).Replace('^', '\n');
}
