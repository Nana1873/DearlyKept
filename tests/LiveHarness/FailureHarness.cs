using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private static object? failingLedger;
    private JournalSnapshot? overlongBefore;
    private int overlongCookies;
    private static void FailSnapshot(object __instance)
    {
        if (ReferenceEquals(__instance, failingLedger)) throw new IOException("Explicit isolated QA serialization failure.");
    }

    private void SaveFault()
    {
        RequireNoMenu();
        object journal = ProductionJournal();
        Check((bool)journal.GetType().GetProperty("CanRecord")!.GetValue(journal)!, "fault test starts with a writable journal");
        string? before = Game1.player.modData.GetValueOrDefault("Nana1873.DearlyKept/GiftJournal");
        object ledger = journal.GetType().GetProperty("ledger", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(journal)!;
        MethodInfo snapshot = ledger.GetType().GetMethod("Snapshot")!;
        var harmony = new Harmony("Nana1873.DearlyKept.LiveHarness.SerializationFault");
        try
        {
            failingLedger = ledger;
            harmony.Patch(snapshot, prefix: new HarmonyMethod(typeof(ModEntry), nameof(FailSnapshot)));
            journal.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(journal, null);
            Check((string?)journal.GetType().GetProperty("StatusKey")!.GetValue(journal) == "menu.status-save-error", "production saver exposes the injected failure to the menu");
            Check(Game1.player.modData.GetValueOrDefault("Nana1873.DearlyKept/GiftJournal") == before, "serialization failure leaves prior serialized data unchanged");
        }
        finally { harmony.Unpatch(snapshot, HarmonyPatchType.Prefix, harmony.Id); failingLedger = null; }
        OpenArchiveTimed();
    }

    private void SaveRecover()
    {
        RequireNoMenu();
        object journal = ProductionJournal();
        journal.GetType().GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(journal, null);
        Check(journal.GetType().GetProperty("StatusKey")!.GetValue(journal) is null, "a successful retry clears the save-error notice");
    }

    private void OverlongMail(string[] args)
    {
        RequireNoMenu();
        if (args.ElementAtOrDefault(1) == "assert")
        {
            var entries = NewJournalEntries(overlongBefore ?? throw new InvalidOperationException("Open the overlength fixture first."));
            Check(entries.Length == 1 && entries[0].QualifiedItemId == "(O)223" && entries[0].Quantity == 1,
                "overlength native mail still records exactly the received Cookie");
            using JsonDocument document = JsonDocument.Parse(CaptureJournal().Json);
            var entry = document.RootElement.EnumerateArray().Last();
            Check(entry.GetProperty("MessageIncomplete").GetBoolean() && entry.GetProperty("MessageText").ValueKind == JsonValueKind.Null,
                "actual overlength capture records an explicit incomplete-text flag");
            Check(CookieCount() == overlongCookies + 1, "actual letter delivery adds exactly one ordinary Cookie");
            return;
        }
        overlongBefore = CaptureJournal(); overlongCookies = CookieCount();
        string text = "Dear @,^" + string.Concat(Enumerable.Repeat("This is an explicitly authored overlength QA letter. ", 340))
            + "^Mom%item object 223 1 %%";
        var letter = new LetterViewerMenu(text, "mom1");
        letter.page = letter.mailMessage.Count - 1;
        letter.OnPageChange();
        Game1.activeClickableMenu = letter;
        Check(string.Concat(letter.mailMessage).Length > 16000, "native letter preparation retains more than 16000 characters");
        Monitor.Log("DKOVERLONG prepared the real letter at its final page. Receive/close through normal input, then overlong assert.", LogLevel.Info);
    }
}
