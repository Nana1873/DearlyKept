using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept.MultiplayerHarness;

internal sealed partial class ModEntry
{
    private string? giftBefore;
    private Dictionary<string, int> giftItems = new();
    private readonly List<string> giftPages = new();
    private string? giftPageKey;
    private string giftSender = "";
    private string giftProvider = "";
    private string giftOrigin = "";

    private object ModInstance(string id)
    {
        object metadata = Helper.ModRegistry.Get(id) ?? throw new InvalidOperationException("Missing " + id);
        return metadata.GetType().GetProperty("Mod")!.GetValue(metadata)!;
    }

    private static void Property(object target, string name, object value) => target.GetType().GetProperty(name)!.SetValue(target, value);
    private static object? Invoke(object target, string name, params object[] args) => target.GetType()
        .GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args.Select(a => a.GetType()).ToArray(), null)!.Invoke(target, args);
    private static Dictionary<string, int> Quantities() => Game1.player.Items.Where(i => i is not null)
        .GroupBy(i => i.QualifiedItemId).ToDictionary(g => g.Key, g => g.Sum(i => i.Stack));

    // The command dispatcher has verified the exact owned network fixture.
    private void StartGift(string kind)
    {
        NoMenu();
        giftProvider = kind switch
        {
            "birthday" or "reward" => "TitanmasterRy.MarriageOverhaul",
            "anniversary" => "Kantrip.WeddingAnniversaries",
            "happy-birthday" => "Omegasis.HappyBirthday",
            _ => throw new InvalidOperationException("Unknown gift producer.")
        };
        object mod = ModInstance(giftProvider);
        giftSender = kind == "happy-birthday" ? (Context.IsMainPlayer ? "Evelyn" : "Gus") : (Context.IsMainPlayer ? "Leah" : "Penny");
        NPC npc = Game1.getCharacterFromName(giftSender);
        giftOrigin = kind == "reward" ? "spouse" : kind == "anniversary" ? "anniversary" : "birthday";
        giftBefore = Journal; giftItems = Quantities(); giftPages.Clear(); giftPageKey = null;
        if (kind == "anniversary")
        {
            Invoke(mod, "PushAnniversaryText", npc, 12);
            Game1.drawDialogue(npc);
        }
        else if (kind == "happy-birthday")
        {
            object manager = mod.GetType().GetField("birthdayManager")!.GetValue(mod)!;
            Invoke(manager, "setBirthday", Game1.currentSeason, Game1.dayOfMonth);
            Invoke(manager, "resetVillagerQueue");
            Game1.player.friendshipData[giftSender] = new Friendship(2500);
            npc.resetCurrentDialogue(); npc.resetSeasonalDialogue();
            Game1.drawDialogue(npc);
        }
        else
        {
            object config = mod.GetType().GetProperty("Config")!.GetValue(mod)!;
            object data = mod.GetType().GetProperty("Data")!.GetValue(mod)!;
            if (kind == "birthday")
            {
                Property(config, "EnableBirthdaySystem", true);
                Property(config, "PlayerBirthdayDay", Game1.dayOfMonth);
                Property(config, "PlayerBirthdaySeason", Game1.currentSeason);
                Property(config, "BirthdayGiftChance", 0f);
                Property(data, "LastBirthdayYearProcessed", -1);
                Invoke(mod, "Birthday_OnDayStarted", npc);
            }
            else
            {
                Property(data, "PendingRewardItem", "(O)223");
                Property(data, "PendingRewardQty", Context.IsMainPlayer ? 2 : 3);
                Property(data, "PendingRewardDay", -1);
                Property(data, "PendingRewardLine", "A little thank-you, @.#$b#This second page belongs to your own gift. $h");
                Invoke(mod, "Requests_DeliverPendingReward", npc);
            }
        }
    }

    private void ObserveGiftPages()
    {
        if (giftBefore is null || !Context.IsWorldReady || Game1.activeClickableMenu is not DialogueBox box
            || box.characterDialogue?.speaker?.Name != giftSender || box.characterDialoguesBrokenUp.Count == 0) return;
        string page = box.getCurrentString().Replace('^', '\n');
        string key = box.characterDialogue.currentDialogueIndex + "|" + string.Join("|", box.characterDialoguesBrokenUp);
        if (key == giftPageKey || string.IsNullOrWhiteSpace(page)) return;
        giftPageKey = key; giftPages.Add(page);
        Monitor.Log($"DKNET GIFT PAGE {Role}: {page}", LogLevel.Info);
    }

    private void AssertGift()
    {
        NoMenu();
        using JsonDocument before = JsonDocument.Parse(giftBefore ?? throw new InvalidOperationException("Start a gift first."));
        using JsonDocument after = JsonDocument.Parse(Journal);
        JsonElement[] entries = after.RootElement.EnumerateArray().ToArray();
        int count = before.RootElement.GetArrayLength();
        Check(entries.Length == count + 1, "one producer delivery creates exactly one local receipt");
        Check(entries.Take(count).Select(e => e.GetRawText()).SequenceEqual(before.RootElement.EnumerateArray().Select(e => e.GetRawText())), "other prior gifts remain unchanged");
        JsonElement gift = entries.Last();
        Check(gift.GetProperty("SenderId").GetString() == giftSender && gift.GetProperty("SourceModId").GetString() == giftProvider
            && gift.GetProperty("Origin").GetString() == giftOrigin, "sender, provider and occasion belong to this player's gift");
        Check(giftPages.Count > 0 && gift.GetProperty("MessageText").GetString() == string.Join("\n\n", giftPages), "all independently observed local dialogue pages match");
        string item = gift.GetProperty("QualifiedItemId").GetString()!;
        Check(Quantities().GetValueOrDefault(item) - giftItems.GetValueOrDefault(item) == gift.GetProperty("Quantity").GetInt32(), "actual local inventory delta equals the receipt quantity");
        Check(RawEntries(Game1.player) == Journal, "the producer receipt is serialized on its receiving farmer");
        Monitor.Log($"DKNET GIFT PASS: {Role}, {giftProvider}, {giftOrigin}, {giftSender}", LogLevel.Info);
        giftBefore = null;
    }
}
