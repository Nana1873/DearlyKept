using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private JournalSnapshot? spouseBaseline;
    private string spouseOrigin = "";
    private string spouseProvider = "";
    private readonly List<string> spousePages = new();
    private string? spousePageKey;
    private Dictionary<string, int> spouseInventory = new();

    // Run() has already verified the exact owned disposable fixture before this method.
    private void RunSpouseCommand(string[] args)
    {
        RequireNoMenu();
        string action = args.ElementAtOrDefault(1) ?? "assert";
        if (action == "assert")
        {
            Check(spouseBaseline is not null, "a spouse producer attempt exists");
            JournalEntry[] entries = NewJournalEntries(spouseBaseline!);
            Check(entries.Length == 1, "one actual spouse delivery creates exactly one receipt");
            JournalEntry entry = entries.Single();
            Check(entry.SourceModId == spouseProvider && entry.Origin == spouseOrigin && entry.SenderId == "Leah", "provider, occasion and sender match the actual producer");
            Check(spousePages.Count > 0 && entry.MessageText == string.Join("\n\n", spousePages), "stored message exactly matches independently observed displayed pages");
            Dictionary<string, int> after = SpouseItems();
            Check(after.GetValueOrDefault(entry.QualifiedItemId) - spouseInventory.GetValueOrDefault(entry.QualifiedItemId) == entry.Quantity,
                "actual inventory and fridge quantity delta equals the receipt");
            Check(Game1.player.Items.Where(i => i is not null).All(i => Raw(i) is null), "gift capture writes no inventory metadata");
            AssertJournalEntryDate(entry);
            Monitor.Log($"DKQA SPOUSE ASSERT PASS: {spouseProvider}, {spouseOrigin}, {spousePages.Count} displayed pages.", LogLevel.Info);
            return;
        }
        NPC spouse = Game1.getCharacterFromName("Leah");
        spousePages.Clear();
        spousePageKey = null;
        spouseBaseline = CaptureJournal();
        spouseInventory = SpouseItems();
        spouseOrigin = action == "anniversary" ? "anniversary" : action == "reward" ? "spouse" : "birthday";
        spouseProvider = action == "anniversary" ? "Kantrip.WeddingAnniversaries" : "TitanmasterRy.MarriageOverhaul";
        object info = Helper.ModRegistry.Get(spouseProvider) ?? throw new InvalidOperationException("Required companion is missing.");
        object mod = info.GetType().GetProperty("Mod")?.GetValue(info) ?? throw new InvalidOperationException("SMAPI mod instance is unavailable.");
        if (action == "anniversary")
        {
            HbMethod(mod, "PushAnniversaryText", new[] { typeof(NPC), typeof(int) }, spouse, 12);
            Check(CaptureJournal() == spouseBaseline, "pushing anniversary dialogue alone creates no receipt");
            Game1.drawDialogue(spouse);
        }
        else
        {
            object config = mod.GetType().GetProperty("Config")!.GetValue(mod)!;
            object data = mod.GetType().GetProperty("Data")!.GetValue(mod)!;
            if (action == "birthday")
            {
                SetSpouseProperty(config, "EnableBirthdaySystem", true);
                SetSpouseProperty(config, "PlayerBirthdayDay", Game1.dayOfMonth);
                SetSpouseProperty(config, "PlayerBirthdaySeason", Game1.currentSeason);
                SetSpouseProperty(config, "BirthdayGiftChance", 0f);
                SetSpouseProperty(data, "LastBirthdayYearProcessed", -1);
                HbMethod(mod, "Birthday_OnDayStarted", new[] { typeof(NPC) }, spouse);
            }
            else if (action == "reward")
            {
                SetSpouseProperty(data, "PendingRewardItem", "(O)223");
                SetSpouseProperty(data, "PendingRewardQty", 3);
                SetSpouseProperty(data, "PendingRewardDay", -1);
                SetSpouseProperty(data, "PendingRewardLine", "Here you go, @. This is an authored QA project reward.#$b#Three Cookies, with a second page to remember. $h");
                HbMethod(mod, "Requests_DeliverPendingReward", new[] { typeof(NPC) }, spouse);
            }
            else throw new InvalidOperationException("Use spouse birthday|reward|anniversary|assert.");
        }
        Monitor.Log("DKQA invoked the real companion producer in the verified fixture; no journal API mutation or synthetic receipt. Close actual dialogue and claim any queued item, then spouse assert.", LogLevel.Info);
    }
    private static void SetSpouseProperty(object owner, string name, object value) => owner.GetType().GetProperty(name)!.SetValue(owner, value);
    private static Dictionary<string, int> SpouseItems()
    {
        var items = Game1.player.Items.Where(i => i is not null).ToList();
        if (Utility.getHomeOfFarmer(Game1.player) is FarmHouse house && house.fridge.Value is { } fridge)
            items.AddRange(fridge.Items.Where(i => i is not null));
        return items.GroupBy(i => i.QualifiedItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Stack));
    }
    private void ObserveSpouseText()
    {
        if (spouseBaseline is null || !Context.IsWorldReady || Game1.activeClickableMenu is not DialogueBox box
            || box.characterDialogue?.speaker?.Name != "Leah" || box.characterDialoguesBrokenUp.Count == 0) return;
        string page = box.getCurrentString().Replace('^', '\n');
        string key = box.characterDialogue.currentDialogueIndex + "|" + string.Join("|", box.characterDialoguesBrokenUp);
        if (key == spousePageKey || string.IsNullOrWhiteSpace(page)) return;
        spousePageKey = key;
        spousePages.Add(page);
        Monitor.Log("DKQA SPOUSE PAGE: " + page, LogLevel.Info);
    }
}
