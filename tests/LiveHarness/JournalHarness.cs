using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private JournalSnapshot? journalReceiptBaseline;
    private JournalSnapshot? journalUnchangedBaseline;
    private readonly List<MailReceipt> mailAttempts = new();

    private void RunJournalCommand(string[] args)
    {
        string action = args.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "status";
        switch (action)
        {
            case "status":
                JournalSnapshot current = CaptureJournal();
                Check(ReadJournalEntries(current).Length == current.Count, "read-only API count matches its complete journal array");
                Monitor.Log($"DKQA journal count={current.Count}; entries={current.Json}", LogLevel.Info);
                break;
            case "clearinventory":
                RequireNoMenu();
                journalUnchangedBaseline = CaptureJournal();
                Check(journalUnchangedBaseline.Count > 0, "the fixture has actual gift history before removing inventory");
                for (int slot = 0; slot < Game1.player.Items.Count; slot++)
                    Game1.player.Items[slot] = null;
                Check(Game1.player.Items.All(p => p is null), "the isolated fixture's inventory is empty");
                Check(journalUnchangedBaseline == CaptureJournal(), "every journal entry remains after its physical items are removed");
                Monitor.Log("DKQA journal retention checked. Open the real gift journal with empty inventory for visual acceptance; checkpoint/save/persist can now verify durable history.", LogLevel.Info);
                break;
            case "assertunchanged":
                RequireNoMenu();
                Check(journalUnchangedBaseline is not null, "a preview or inventory-removal baseline exists in this run");
                Check(journalUnchangedBaseline == CaptureJournal(), "the complete journal remains unchanged after the deferred menu/update path");
                break;
            default:
                throw new InvalidOperationException("Usage: dkqa journal status | clearinventory | assertunchanged.");
        }
    }

    private void AssertBirthdayReceipt(GuardContext owned)
    {
        RequireBirthdayFixture(owned);
        CheckBirthdayAttemptOwner(owned);
        BirthdayAttempt attempt = birthdayAttempt!;
        Check(attempt.ObservedPath == (attempt.InventoryWasFull ? "HB dropped gift" : "HB pending gift"), "the real Happy Birthday delivery path was observed for this attempt");
        object gifts = HbField<object>(GetBirthdayCore(), "giftManager");
        Check(HbField<Item?>(gifts, "BirthdayGiftToReceive") is null, "the real Happy Birthday pending gift has been delivered");
        Check(CookieCount() == attempt.CookiesBefore + 1, "vanilla receipt or debris pickup added exactly one Cookie to inventory");
        JournalEntry[] received = NewJournalEntries(attempt.JournalBefore);
        Check(received.Length == 1, "the real birthday gift created exactly one journal entry");
        JournalEntry entry = received[0];
        Check(entry.SenderId == attempt.Sender && entry.Origin == "birthday"
            && entry.SourceModId == BirthdayModId && entry.SourceId == "birthday:" + attempt.Sender,
            "the birthday journal entry identifies the actual sender, occasion and Happy Birthday source");
        Check(entry.QualifiedItemId == "(O)223" && entry.Quantity == 1 && entry.Quality == 0, "birthday history records the actual one-Cookie gift");
        Check(!string.IsNullOrWhiteSpace(attempt.FinalDialogueText) && entry.MessageText == attempt.FinalDialogueText,
            "birthday history preserves all actually displayed Happy Birthday pages observed independently by QA");
        if (attempt.ExpectMultipage)
        {
            Check(attempt.DisplayedPages.Count == 2, "both distinct pages of the actual HB QA greeting were displayed and observed");
            Check(attempt.DisplayedPages.All(page => page.Contains(Game1.player.Name, StringComparison.Ordinal)
                && !page.Contains('@') && !page.Contains("#$b#", StringComparison.Ordinal) && !page.Contains("$h", StringComparison.Ordinal)),
                "both actual HB pages personalize the player name and omit dialogue commands");
        }
        AssertJournalEntryDate(entry);
        Item[] cookies = Game1.player.Items.Where(p => p?.QualifiedItemId == "(O)223").ToArray()!;
        Check(cookies.All(p => Raw(p) is null), "birthday Cookies contain no Dearly Kept provenance metadata");
        Item ordinary = ItemRegistry.Create("(O)223");
        Check(cookies.All(p => p.canStackWith(ordinary) && ordinary.canStackWith(p)), "actual birthday Cookies can stack normally with ordinary Cookies in both directions");
        if (!attempt.InventoryWasFull && attempt.CookiesBefore is > 0 and < 999)
            Check(cookies.Length == 1, "the actual birthday Cookie merged into the pre-existing ordinary stack");
        journalUnchangedBaseline = CaptureJournal();
        Monitor.Log($"DKQA BIRTHDAY ASSERT PASS: {attempt.Sender}, {attempt.ObservedPath}, actual inventory receipt and independent journal history. QA did not fabricate a gift or history entry.", LogLevel.Info);
    }

    private JournalEntry[] NewJournalEntries(JournalSnapshot baseline)
    {
        JournalEntry[] before = ReadJournalEntries(baseline);
        JournalSnapshot current = CaptureJournal();
        JournalEntry[] after = ReadJournalEntries(current);
        Check(after.Length == current.Count && before.Length == baseline.Count, "journal API counts agree with the complete entry arrays");
        Check(after.Length >= before.Length && after.Take(before.Length).SequenceEqual(before), "earlier chronological gift history is preserved unchanged");
        Check(after.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() == after.Length
            && after.All(p => !string.IsNullOrWhiteSpace(p.Id)), "all journal entries have distinct stable identities");
        return after.Skip(before.Length).ToArray();
    }

    private void AssertJournalEntryDate(JournalEntry entry)
    {
        Check(!string.IsNullOrWhiteSpace(entry.SenderId) && !string.IsNullOrWhiteSpace(entry.ItemName), "the actual gift history retains sender and item name");
        Check(entry.Year == Game1.year && entry.Season == Game1.currentSeason && entry.Day == Game1.dayOfMonth,
            "history records the actual in-game receipt date (assert before sleeping)");
    }

    private static JournalEntry[] ReadJournalEntries(JournalSnapshot snapshot) => JsonSerializer.Deserialize<JournalEntry[]>(snapshot.Json, JsonOptions)
        ?? throw new InvalidOperationException("The journal API did not return an entry array.");

    private sealed record MailReceipt(string SourceId, string ItemId, int Quantity, int Quality, string? MessageText);
    private sealed record JournalEntry(string Id, string SenderId, string SourceId, string Origin, string? SourceModId,
        int Year, string Season, int Day, string QualifiedItemId, string ItemName, int Quantity, int Quality, string? MessageText = null);
}
