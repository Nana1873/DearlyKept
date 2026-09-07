using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private const string BirthdayModId = "Omegasis.HappyBirthday";
    private const string BirthdayCoreType = "Omegasis.HappyBirthday.HappyBirthdayModCore";
    private const string BirthdayGiftAssetPrefix = "Mods/Omegasis.HappyBirthday/Gifts/";
    private static readonly string[] BirthdayNpcs = { "Evelyn", "Gus", "Robin" };
    private string? birthdayFixtureId;
    private BirthdayAttempt? birthdayAttempt;

    private void RunBirthdayCommand(string[] args, GuardContext owned)
    {
        string action = args.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "status";
        object core = GetBirthdayCore();
        object manager = HbField<object>(core, "birthdayManager");
        object gifts = HbField<object>(core, "giftManager");
        switch (action)
        {
            case "prepare":
                RequireNoMenu();
                Check(HbField<Item?>(gifts, "BirthdayGiftToReceive") is null, "no birthday delivery is pending before fixture setup");
                birthdayFixtureId = owned.Identity.FixtureId;
                birthdayAttempt = null;
                HbMethod(manager, "setBirthday", new[] { typeof(string), typeof(int) }, Game1.currentSeason, Game1.dayOfMonth);
                HbMethod(manager, "resetVillagerQueue", Type.EmptyTypes);
                foreach (string name in BirthdayNpcs)
                {
                    _ = GetBirthdayNpc(name);
                    if (!Game1.player.friendshipData.TryGetValue(name, out Friendship friendship))
                        Game1.player.friendshipData[name] = friendship = new Friendship();
                    friendship.Points = 2500;
                    Helper.GameContent.InvalidateCache(BirthdayGiftAssetPrefix + name);
                }
                Check((bool)HbMethod(manager, "isBirthday", Type.EmptyTypes)!, "Happy Birthday reports today as the player's birthday");
                Monitor.Log("DKQA birthday fixture configured through real BirthdayManager state: today, ten hearts with Evelyn/Gus/Robin, their real HB gift assets contain one Cookie. Use prepare for ordinary comparison items, then birthday preview or birthday talk <NPC>.", LogLevel.Info);
                break;
            case "talk":
                RequireNoMenu();
                RequireBirthdayFixture(owned);
                string sender = RequireBirthdayNpcArgument(args);
                Check((bool)HbMethod(manager, "isBirthday", Type.EmptyTypes)!, "today is the configured Happy Birthday date");
                Check(!(bool)HbMethod(manager, "hasGivenBirthdayGift", new[] { typeof(string) }, sender)!, $"{sender} has not already delivered today's birthday gift");
                Check(HbField<Item?>(gifts, "BirthdayGiftToReceive") is null, "no previous Happy Birthday item is pending");
                NPC npc = GetBirthdayNpc(sender);
                // Start vanilla dialogue. Happy Birthday's own MenuChanged handler
                // replaces it with its greeting and calls both real gift overloads.
                npc.resetCurrentDialogue();
                npc.resetSeasonalDialogue();
                birthdayAttempt = new BirthdayAttempt(owned.Identity.FixtureId, sender, CookieCount(), CaptureJournal(), Game1.player.isInventoryFull(), Game1.currentLocation.debris.Select(p => p.item).OfType<Item>().ToArray());
                Game1.drawDialogue(npc);
                Check(Game1.activeClickableMenu is DialogueBox && Game1.currentSpeaker == npc, $"vanilla dialogue opened with the actual {sender} NPC");
                Monitor.Log($"DKQA birthday dialogue requested for {sender}; inventoryFull={birthdayAttempt.InventoryWasFull}. Wait for the genuine HB greeting, then close it with actual game input. No gift method was called by QA.", LogLevel.Info);
                break;
            case "preview":
                RequireNoMenu();
                RequireBirthdayFixture(owned);
                string previewNpc = RequireBirthdayNpcArgument(args);
                InventoryLine[] inventoryBefore = Snapshot();
                DebrisLine[] debrisBefore = BirthdayDebrisSnapshot();
                Item? pendingBefore = HbField<Item?>(gifts, "BirthdayGiftToReceive");
                JournalSnapshot journalBefore = CaptureJournal();
                IContentPatcherPreviewApi api = Helper.ModRegistry.GetApi<IContentPatcherPreviewApi>("Pathoschild.ContentPatcher")
                    ?? throw new InvalidOperationException("Content Patcher API is unavailable.");
                // Preview HB's registered token in its real owner context, whose
                // manifest declares the Content Patcher dependency required by this API.
                IManifest ownerManifest = Helper.ModRegistry.Get(BirthdayModId)!.Manifest;
                IPreviewTokenString token = api.ParseTokenString(ownerManifest, "{{Omegasis.HappyBirthday/NPCGift:" + previewNpc + "}}", new SemanticVersion("2.9.0"), new[] { BirthdayModId });
                token.UpdateContext();
                Check(token.IsValid && token.IsReady, $"actual Content Patcher NPCGift token is valid and ready ({token.ValidationError ?? "no validation error"})");
                Check(token.Value == "(O)223 1", $"actual HB NPCGift preview returned deterministic Cookie x1 ({token.Value})");
                Check(inventoryBefore.SequenceEqual(Snapshot()), "CP preview leaves every inventory item unchanged");
                Check(debrisBefore.SequenceEqual(BirthdayDebrisSnapshot()), "CP preview creates no dropped gifts");
                Check(ReferenceEquals(pendingBefore, HbField<Item?>(gifts, "BirthdayGiftToReceive")), "CP preview creates no pending birthday delivery");
                Check(journalBefore == CaptureJournal(), "CP preview creates no gift journal entry");
                journalUnchangedBaseline = journalBefore;
                Monitor.Log("DKQA BIRTHDAY PREVIEW PASS: the actual registered Content Patcher token ran without awarding an item or adding history.", LogLevel.Info);
                break;
            case "fill":
                RequireNoMenu();
                RequireBirthdayFixture(owned);
                // Fill Cookie capacity too: otherwise ordinary automatic stacking
                // could collect real HB debris before it can be inspected.
                for (int slot = 0; slot < Game1.player.Items.Count; slot++)
                {
                    if (Game1.player.Items[slot] is null)
                        Game1.player.Items[slot] = ItemRegistry.Create("(O)390", 999);
                    else if (Game1.player.Items[slot].QualifiedItemId == "(O)223")
                        Game1.player.Items[slot].Stack = Game1.player.Items[slot].maximumStackSize();
                }
                Check(Game1.player.isInventoryFull(), "actual inventory is full before the real HB debris path");
                Monitor.Log("DKQA filled ordinary fixture stacks and empty slots; birthday talk <unused NPC> now exercises HB's real debris branch. Inspect birthday status before birthday room.", LogLevel.Info);
                break;
            case "room":
                RequireNoMenu();
                RequireBirthdayFixture(owned);
                int fillerSlot = Enumerable.Range(0, Game1.player.Items.Count).Reverse()
                    .FirstOrDefault(slot => Game1.player.Items[slot] is Item item && item.QualifiedItemId == "(O)390" && item.Stack == 999 && Raw(item) is null, -1);
                Check(fillerSlot >= 0, "a disposable ordinary Stone filler stack can be removed to make room");
                Game1.player.Items[fillerSlot] = null;
                Monitor.Log($"DKQA removed only ordinary fixture filler in slot {fillerSlot}. Walk to the real dropped gift and let vanilla pickup collect it; QA does not add the gift.", LogLevel.Info);
                break;
            case "status":
                Monitor.Log($"DKQA Happy Birthday {Helper.ModRegistry.Get(BirthdayModId)!.Manifest.Version}; isBirthday={HbMethod(manager, "isBirthday", Type.EmptyTypes)}; fixturePrepared={birthdayFixtureId == owned.Identity.FixtureId}; journal={CaptureJournal().Json}", LogLevel.Info);
                Item? pending = HbField<Item?>(gifts, "BirthdayGiftToReceive");
                Monitor.Log($"DKQA birthday pending={DescribeBirthdayItem(pending)}; attempt={birthdayAttempt?.Sender ?? "none"}; observedPath={birthdayAttempt?.ObservedPath ?? "none"}", LogLevel.Info);
                foreach (DebrisLine line in BirthdayDebrisSnapshot())
                    Monitor.Log($"DKQA birthday debris={line.ItemId} x{line.Stack}; raw={line.RawStamp ?? "<none>"}", LogLevel.Info);
                break;
            default:
                throw new InvalidOperationException("Usage: dkqa birthday prepare | talk Evelyn|Gus|Robin | preview Evelyn|Gus|Robin | fill | room | status.");
        }
    }

    private void EditBirthdayFixtureGifts(object? sender, AssetRequestedEventArgs e)
    {
        if (birthdayFixtureId is null || !BirthdayNpcs.Any(name => e.NameWithoutLocale.IsEquivalentTo(BirthdayGiftAssetPrefix + name)))
            return;
        try
        {
            RequireBirthdayFixture(RequireOwnedFixture());
            e.Edit(asset =>
            {
                // Recheck ownership when SMAPI executes the deferred edit.
                RequireBirthdayFixture(RequireOwnedFixture());
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                data.Clear();
                data["(O)223"] = "1";
            }, AssetEditPriority.Late + 100);
        }
        catch (Exception ex)
        {
            Monitor.Log($"DKQA birthday asset edit rejected: {ex.Message}", LogLevel.Error);
        }
    }

    private void ClearBirthdayFixtureCache()
    {
        bool hadFixture = birthdayFixtureId is not null;
        birthdayFixtureId = null;
        birthdayAttempt = null;
        journalReceiptBaseline = null;
        journalUnchangedBaseline = null;
        mailAttempts.Clear();
        // Cleanup only: never leave an edited in-memory gift asset available to
        // a later save after the verified disposable world has been unloaded.
        if (hadFixture)
            foreach (string name in BirthdayNpcs)
                Helper.GameContent.InvalidateCache(BirthdayGiftAssetPrefix + name);
    }

    private void ObserveBirthdayDelivery(object? sender, UpdateTickedEventArgs e)
    {
        if (birthdayAttempt is null || birthdayAttempt.ObservedPath is not null)
            return;
        try
        {
            GuardContext owned = RequireOwnedFixture();
            RequireBirthdayFixture(owned);
            CheckBirthdayAttemptOwner(owned);
            object core = GetBirthdayCore();
            object gifts = HbField<object>(core, "giftManager");
            object manager = HbField<object>(core, "birthdayManager");
            if (!(bool)HbMethod(manager, "hasGivenBirthdayWish", new[] { typeof(string) }, birthdayAttempt.Sender)!)
                return;
            Item? pending = HbField<Item?>(gifts, "BirthdayGiftToReceive");
            Item? dropped = Game1.currentLocation.debris.Select(p => p.item).FirstOrDefault(p => p?.QualifiedItemId == "(O)223" && !birthdayAttempt.ExistingDebris.Contains(p));
            if (pending is null && dropped is null)
                return;
            birthdayAttempt.ObservedPath = pending is not null ? "HB pending gift" : "HB dropped gift";
            Monitor.Log($"DKQA observed {birthdayAttempt.ObservedPath} after the real dialogue for {birthdayAttempt.Sender}: {DescribeBirthdayItem(pending ?? dropped)}. Inventory cookies={CookieCount()}; journal={CaptureJournal().Json}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            birthdayAttempt = null;
            Monitor.Log($"DKQA birthday observation rejected: {ex.Message}", LogLevel.Error);
        }
    }

    private object GetBirthdayCore()
    {
        IModInfo info = Helper.ModRegistry.Get(BirthdayModId)
            ?? throw new InvalidOperationException("Happy Birthday is not loaded.");
        if (info.Manifest.Version.ToString() != "3.21.4")
            throw new InvalidOperationException($"This bounded fixture expects actual Happy Birthday 3.21.4, found {info.Manifest.Version}.");
        Type type = AppDomain.CurrentDomain.GetAssemblies().Select(p => p.GetType(BirthdayCoreType, false)).OfType<Type>().Single();
        return type.GetField("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
            ?? throw new InvalidOperationException("Happy Birthday's named core Instance is unavailable.");
    }

    private static T HbField<T>(object owner, string name) => (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(owner))!;

    private static object? HbMethod(object owner, string name, Type[] parameters, params object[] arguments)
    {
        MethodInfo method = owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null)
            ?? throw new InvalidOperationException($"Expected Happy Birthday method {owner.GetType().FullName}.{name} is missing.");
        return method.Invoke(owner, arguments);
    }

    private void RequireBirthdayFixture(GuardContext owned)
    {
        if (birthdayFixtureId != owned.Identity.FixtureId)
            throw new InvalidOperationException("Run dkqa birthday prepare in this exact verified fixture first.");
    }

    private void CheckBirthdayAttemptOwner(GuardContext owned)
    {
        if (birthdayAttempt is null || birthdayAttempt.FixtureId != owned.Identity.FixtureId)
            throw new InvalidOperationException("No birthday attempt belongs to this verified fixture.");
    }

    private static string RequireBirthdayNpcArgument(string[] args)
    {
        if (args.Length != 3 || !BirthdayNpcs.Contains(args[2], StringComparer.Ordinal))
            throw new InvalidOperationException("Choose exactly Evelyn, Gus or Robin for the bounded birthday fixture.");
        return args[2];
    }

    private static NPC GetBirthdayNpc(string name) => Game1.getCharacterFromName(name)
        ?? throw new InvalidOperationException($"The real {name} NPC is unavailable in this fixture.");

    private JournalSnapshot CaptureJournal()
    {
        IDearlyKeptJournalApi api = Helper.ModRegistry.GetApi<IDearlyKeptJournalApi>("Nana1873.DearlyKept")
            ?? throw new InvalidOperationException("Dearly Kept's read-only journal API is unavailable.");
        string json = api.GetGiftsJson();
        using JsonDocument _ = JsonDocument.Parse(json);
        return new JournalSnapshot(api.GetGiftCount(), json);
    }

    private static int CookieCount() => Game1.player.Items.Where(p => p?.QualifiedItemId == "(O)223").Sum(p => p.Stack);
    private static string DescribeBirthdayItem(Item? item) => item is null ? "none" : $"{item.QualifiedItemId} x{item.Stack}; raw={Raw(item) ?? "<none>"}";
    private static DebrisLine[] BirthdayDebrisSnapshot() => Game1.currentLocation.debris.Where(p => p.item is not null)
        .Select(p => new DebrisLine(p.item.QualifiedItemId, p.item.Stack, Raw(p.item))).ToArray();

    private sealed record JournalSnapshot(int Count, string Json);
    private sealed record DebrisLine(string ItemId, int Stack, string? RawStamp);
    private sealed record BirthdayAttempt(string FixtureId, string Sender, int CookiesBefore, JournalSnapshot JournalBefore, bool InventoryWasFull, Item[] ExistingDebris)
    {
        public string? ObservedPath { get; set; }
    }
}

public interface IDearlyKeptJournalApi
{
    int GetGiftCount();
    string GetGiftsJson();
}

// Minimal public Content Patcher contract copied from the API shipped with HB.
// The actual CP engine resolves its registered HB token; QA supplies no token.
public interface IContentPatcherPreviewApi
{
    IPreviewTokenString ParseTokenString(IManifest manifest, string rawValue, ISemanticVersion formatVersion, string[]? assumeModIds = null);
}

public interface IPreviewTokenString
{
    bool IsValid { get; }
    string? ValidationError { get; }
    bool IsReady { get; }
    string? Value { get; }
    IEnumerable<int> UpdateContext();
}
