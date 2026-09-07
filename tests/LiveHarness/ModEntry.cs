using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry : Mod
{
    private const string GiftKey = "Nana1873.DearlyKept/Provenance";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public override void Entry(IModHelper helper)
    {
        helper.ConsoleCommands.Add("dkqa", "Disposable SDVKit fixture only: prepare | fill | mail <key> | collection <key> | birthday <action> | assertBirthday | journal <action> | locale en|de | ui 100|150 | status | assert | checkpoint | persist | save", Run);
        helper.Events.GameLoop.Saved += (_, _) => Monitor.Log("DKQA observed SMAPI Saved event; use persist after restarting the same fixture.", LogLevel.Info);
        helper.Events.Content.AssetRequested += EditBirthdayFixtureGifts;
        helper.Events.GameLoop.UpdateTicked += ObserveBirthdayDelivery;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => ClearBirthdayFixtureCache();
    }

    private void Run(string command, string[] args)
    {
        try
        {
            GuardContext owned = RequireOwnedFixture();
            string action = args.FirstOrDefault()?.ToLowerInvariant() ?? "status";
            switch (action)
            {
                case "birthday":
                    RunBirthdayCommand(args, owned);
                    break;
                case "assertbirthday":
                    RequireNoMenu();
                    AssertBirthdayReceipt(owned);
                    break;
                case "journal":
                    RunJournalCommand(args);
                    break;
                case "prepare":
                    RequireNoMenu();
                    journalReceiptBaseline = CaptureJournal();
                    mailAttempts.Clear();
                    for (int slot = 0; slot < Game1.player.Items.Count; slot++)
                        Game1.player.Items[slot] = null;
                    Game1.player.Items[0] = ItemRegistry.Create("(O)223", 5);
                    Game1.player.Items[1] = ItemRegistry.Create("(O)390", 5);
                    Monitor.Log("DKQA prepared this verified disposable fixture: 5 ordinary Cookies and 5 ordinary Stone. No provenance was written.", LogLevel.Info);
                    break;
                case "fill":
                    RequireNoMenu();
                    for (int slot = 0; slot < Game1.player.Items.Count; slot++)
                    {
                        if (Game1.player.Items[slot] is null)
                            Game1.player.Items[slot] = ItemRegistry.Create("(O)390", 999);
                    }
                    Monitor.Log("DKQA filled empty slots with ordinary full Stone stacks. Open mom1, close the real letter, and use actual inventory input to handle overflow.", LogLevel.Info);
                    break;
                case "locale":
                    RequireNoMenu();
                    if (args.Length != 2 || args[1] is not ("en" or "de"))
                        throw new InvalidOperationException("Usage: dkqa locale en|de.");
                    LocalizedContentManager.CurrentLanguageCode = args[1] == "de"
                        ? LocalizedContentManager.LanguageCode.de
                        : LocalizedContentManager.LanguageCode.en;
                    Monitor.Log($"DKQA actual game locale is {LocalizedContentManager.CurrentLanguageCode}. Open a fresh menu for visual verification.", LogLevel.Info);
                    break;
                case "ui":
                    RequireNoMenu();
                    if (args.Length != 2 || args[1] is not ("100" or "150"))
                        throw new InvalidOperationException("Usage: dkqa ui 100|150.");
                    // Use the vanilla options handler. Game1._update applies the
                    // desired scale and calls refreshWindowSettings next tick.
                    Game1.options.changeDropDownOption(Options.uiScaleSlider, args[1] + "%");
                    Monitor.Log($"DKQA requested vanilla UI scale {args[1]}%. Wait for a game update, then use status to verify the actual scale and viewport.", LogLevel.Info);
                    break;
                case "mail":
                case "collection":
                    RequireNoMenu();
                    if (args.Length != 2)
                        throw new InvalidOperationException($"Usage: dkqa {action} <exact Data/mail key>.");
                    Dictionary<string, string> mail = Helper.GameContent.Load<Dictionary<string, string>>("Data/mail");
                    if (!mail.TryGetValue(args[1], out string? text))
                        throw new InvalidOperationException($"No Data/mail entry named '{args[1]}'.");
                    InventoryLine[] beforeLetter = Snapshot();
                    JournalSnapshot beforeLetterJournal = CaptureJournal();
                    bool fromCollection = action == "collection";
                    var letter = new LetterViewerMenu(text, args[1], fromCollection);
                    Game1.activeClickableMenu = letter;
                    string attachments = string.Join(", ", letter.itemsToGrab.Where(p => p.item is not null).Select(p => $"{p.item.QualifiedItemId} x{p.item.Stack}"));
                    Monitor.Log($"DKQA opened real LetterViewerMenu '{args[1]}' fromCollection={fromCollection} with [{attachments}]. Close or interact using actual game input.", LogLevel.Info);
                    if (fromCollection)
                    {
                        journalUnchangedBaseline = beforeLetterJournal;
                        Check(letter.itemsToGrab.All(p => p.item is null), "collection preview has no attachment items to claim");
                        Check(beforeLetter.SequenceEqual(Snapshot()), "opening the collection preview leaves inventory and provenance unchanged");
                        Check(beforeLetterJournal == CaptureJournal(), "opening the collection preview leaves gift history unchanged");
                        Monitor.Log("DKQA close the actual collection letter, then run journal assertunchanged to verify the deferred close path too.", LogLevel.Info);
                    }
                    else
                        mailAttempts.AddRange(letter.itemsToGrab.Where(p => p.item is not null).Select(p => new MailReceipt(args[1], p.item.QualifiedItemId, p.item.Stack, p.item.Quality)));
                    break;
                case "status":
                    Monitor.Log($"DKQA fixture={owned.Identity.FixtureId}; game={Game1.uniqueIDForThisGame}; save={Constants.CurrentSavePath}; date={Game1.currentSeason} {Game1.dayOfMonth}, Y{Game1.year}; menu={Game1.activeClickableMenu?.GetType().Name ?? "none"}", LogLevel.Info);
                    Monitor.Log($"DKQA locale={LocalizedContentManager.CurrentLanguageCode}; desiredUiScale={Game1.options.desiredUIScale}; actualUiScale={Game1.options.uiScale}; uiViewport={Game1.uiViewport.Width}x{Game1.uiViewport.Height}", LogLevel.Info);
                    foreach (InventoryLine line in Snapshot())
                        Monitor.Log($"DKQA slot={line.Slot}; item={line.ItemId}; stack={line.Stack}; raw={line.RawStamp ?? "<none>"}", LogLevel.Info);
                    Monitor.Log($"DKQA journal={CaptureJournal().Json}", LogLevel.Info);
                    break;
                case "assert":
                    RequireNoMenu();
                    AssertNativeBehavior();
                    break;
                case "checkpoint":
                    RequireNoMenu();
                    InventoryLine[] items = Snapshot();
                    JournalSnapshot gifts = CaptureJournal();
                    Check(gifts.Count > 0, "checkpoint contains actual gift journal history");
                    var checkpoint = new Checkpoint(owned.Identity.FixtureId, Game1.uniqueIDForThisGame, items, gifts);
                    File.WriteAllText(owned.CheckpointPath, JsonSerializer.Serialize(checkpoint, JsonOptions));
                    Monitor.Log($"DKQA checkpoint recorded under the owned lab: {owned.CheckpointPath}", LogLevel.Info);
                    break;
                case "persist":
                    RequireNoMenu();
                    Checkpoint expected = Read<Checkpoint>(owned.CheckpointPath);
                    Check(expected.FixtureId == owned.Identity.FixtureId && expected.UniqueGameId == Game1.uniqueIDForThisGame, "checkpoint belongs to this exact fixture");
                    Check(expected.Items.SequenceEqual(Snapshot()), "inventory slots, item IDs, stacks and absence of provenance metadata match the pre-save checkpoint");
                    Check(expected.Journal == CaptureJournal(), "the complete gift journal matches the pre-save checkpoint after reloading");
                    Monitor.Log("DKQA PERSIST PASS. This comparison proves a restart only when the review was actually stopped and started between checkpoint and persist.", LogLevel.Info);
                    break;
                case "save":
                    RequireNoMenu();
                    if (Game1.currentLocation is not FarmHouse)
                        throw new InvalidOperationException("Vanilla sleep/save requires the disposable player's FarmHouse. Prefer walking into bed with actual input.");
                    bool accepted = Game1.currentLocation.answerDialogueAction("Sleep_Yes", Array.Empty<string>());
                    Check(accepted, "vanilla Sleep_Yes action accepted");
                    Monitor.Log("DKQA requested vanilla sleep. Saving is asynchronous; wait for the Saved event and the next playable day. SaveGame.Save was not called.", LogLevel.Info);
                    break;
                default:
                    throw new InvalidOperationException("Commands: prepare | fill | mail <key> | collection <key> | birthday <action> | assertBirthday | journal <action> | locale en|de | ui 100|150 | status | assert | checkpoint | persist | save.");
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"DKQA REJECT/FAIL: {ex.Message}", LogLevel.Error);
        }
    }

    private void AssertNativeBehavior()
    {
        JournalSnapshot baseline = journalReceiptBaseline ?? throw new InvalidOperationException("Run prepare before the real mail receipt scenario.");
        Check(mailAttempts.Count >= 2, "at least two real letter attachments were requested for the receipt scenario");
        JournalEntry[] received = NewJournalEntries(baseline);
        Check(received.Length == mailAttempts.Count, "exactly one history entry exists for each actual letter attachment");
        var expected = mailAttempts.OrderBy(p => p.SourceId).ThenBy(p => p.ItemId).ThenBy(p => p.Quantity).ToArray();
        var actual = received.Select(p => new MailReceipt(p.SourceId, p.QualifiedItemId, p.Quantity, p.Quality))
            .OrderBy(p => p.SourceId).ThenBy(p => p.ItemId).ThenBy(p => p.Quantity).ToArray();
        Check(expected.SequenceEqual(actual), "journal source letters, item IDs, quantities and qualities match the real attachments");
        foreach (JournalEntry entry in received)
        {
            Check(entry.Origin == "mail" && entry.SourceModId is null, "actual native mail receipts use mail origin without a source mod");
            if (entry.SourceId is "mom1" or "Evelyn")
                Check(entry.SenderId == (entry.SourceId == "mom1" ? "Mom" : "Evelyn"), "the journal preserves the actual known letter sender");
            AssertJournalEntryDate(entry);
        }
        Item[] all = Game1.player.Items.Where(p => p is not null).ToArray()!;
        Check(all.All(p => Raw(p) is null), "normal inventory items contain no Dearly Kept provenance metadata");
        var expectedInventory = mailAttempts.Select(p => (p.ItemId, p.Quantity, p.Quality))
            .Concat(new[] { (ItemId: "(O)223", Quantity: 5, Quality: 0), (ItemId: "(O)390", Quantity: 5, Quality: 0) })
            .GroupBy(p => (p.ItemId, p.Quality)).ToDictionary(p => p.Key, p => p.Sum(q => q.Quantity));
        var actualInventory = all.GroupBy(p => (ItemId: p.QualifiedItemId, p.Quality)).ToDictionary(p => p.Key, p => p.Sum(q => q.Stack));
        Check(expectedInventory.Count == actualInventory.Count && expectedInventory.All(p => actualInventory.TryGetValue(p.Key, out int quantity) && quantity == p.Value), "inventory contains the five ordinary Cookies and Stone plus every actual received attachment, without loss or duplication");
        Item[] cookies = all.Where(p => p.QualifiedItemId == "(O)223" && p.Quality == 0).ToArray();
        Check(cookies.Length == 1 && cookies[0].Stack > 5, "ordinary Cookies and the actual received Cookies merged into one native stack");
        foreach (Item item in all)
        {
            Item copy = item.getOne();
            Check(Raw(copy) is null && item.canStackWith(copy) && copy.canStackWith(item), "native item copies remain ordinary and can stack in both directions");
        }
        Monitor.Log("DKQA ASSERT PASS: actual mail receipts match independent journal history, inventory quantities are conserved and ordinary Cookie stacking works. QA wrote no journal entries.", LogLevel.Info);
    }

    private GuardContext RequireOwnedFixture()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Context.IsMultiplayer)
            throw new InvalidOperationException("Only a world-ready single-player SDVKit disposable review is allowed.");
        if (Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_MODE") != "review")
            throw new InvalidOperationException("This process was not launched as an SDVKit test-save review.");

        DirectoryInfo? ancestor = new(Helper.DirectoryPath);
        while (ancestor is not null && ancestor.Name != ".sdvkit")
            ancestor = ancestor.Parent;
        if (ancestor is null)
            throw new InvalidOperationException("The QA companion must be staged inside its owning workspace's .sdvkit lab.");
        string sdvkit = ancestor.FullName;
        string single = Path.Combine(sdvkit, "lab", "single");
        string fixturePath = Path.Combine(single, "test-save", "fixture.json");
        FixtureIdentity fixture = Read<FixtureIdentity>(fixturePath);
        if (fixture.SchemaVersion != 1 || string.IsNullOrWhiteSpace(fixture.FixtureId)
            || string.IsNullOrWhiteSpace(fixture.WorkspaceOwnerId) || string.IsNullOrWhiteSpace(fixture.SaveId)
            || fixture.SaveId != Path.GetFileName(fixture.SaveId))
            throw new InvalidOperationException("Missing or invalid SDVKit fixture registration.");
        if (fixture.SaveId != Constants.SaveFolderName
            || fixture.UniqueGameId != Game1.uniqueIDForThisGame
            || fixture.FixtureId != Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_FIXTURE_ID")
            || fixture.WorkspaceOwnerId != Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_WORKSPACE_OWNER_ID")
            || !Game1.player.modData.TryGetValue("SDVKit/FixtureId", out string liveFixture) || liveFixture != fixture.FixtureId
            || !Game1.player.modData.TryGetValue("SDVKit/WorkspaceOwnerId", out string liveOwner) || liveOwner != fixture.WorkspaceOwnerId)
            throw new InvalidOperationException("Live world identity does not match the registered disposable fixture.");

        string expectedSave = Path.Combine(sdvkit, "lab", "profiles", "single", "AppData", "Roaming", "StardewValley", "Saves", fixture.SaveId);
        string? currentSave = Constants.CurrentSavePath;
        if (string.IsNullOrWhiteSpace(currentSave) || !Directory.Exists(currentSave)
            || !Path.GetFullPath(currentSave).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(expectedSave), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Save path is not the exact isolated SDVKit profile fixture: {currentSave ?? "<none>"}.");
        string workCopy = Path.GetFullPath(Path.Combine(single, "test-save", "work"));
        FileSystemInfo? mountedTarget = new DirectoryInfo(currentSave).ResolveLinkTarget(true);
        if (mountedTarget is null || !mountedTarget.FullName.Equals(workCopy, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The exact profile save mount does not resolve to the registered disposable work copy.");
        // SDVKit deliberately mounts this one save through a junction. Its parents
        // and the registered target must still remain inside the owning lab.
        RejectReparsePoints(Directory.GetParent(currentSave)!.FullName, sdvkit);
        RejectReparsePoints(workCopy, sdvkit);

        using JsonDocument status = JsonDocument.Parse(File.ReadAllText(Path.Combine(single, "runtime", "always-on-status.json")));
        JsonElement root = status.RootElement;
        JsonElement testSave = root.GetProperty("testSave");
        if (root.GetProperty("processId").GetInt32() != Environment.ProcessId
            || root.GetProperty("phase").GetString() == "exiting"
            || !testSave.GetProperty("identityVerified").GetBoolean()
            || testSave.GetProperty("phase").GetString() != "passed"
            || testSave.GetProperty("fixtureId").GetString() != fixture.FixtureId
            || testSave.GetProperty("saveId").GetString() != fixture.SaveId)
            throw new InvalidOperationException("No passed, identity-verified SDVKit review marker exists for this process.");
        return new GuardContext(fixture, Path.Combine(single, "runtime", "dearly-kept-qa-checkpoint.json"));
    }

    private static void RejectReparsePoints(string directory, string owningRoot)
    {
        for (DirectoryInfo? part = new(directory); part is not null; part = part.Parent)
        {
            if ((part.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("A junction or symbolic link prevents verification of the isolated save path.");
            if (part.FullName.Equals(owningRoot, StringComparison.OrdinalIgnoreCase))
                return;
        }
        throw new InvalidOperationException("The save path escaped the owning .sdvkit root.");
    }

    private static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidOperationException($"Invalid JSON at {path}.");

    private void Check(bool result, string description)
    {
        if (!result)
            throw new InvalidOperationException($"Assertion failed: {description}.");
        Monitor.Log($"DKQA PASS: {description}.", LogLevel.Info);
    }

    private static void RequireNoMenu()
    {
        if (Game1.activeClickableMenu is not null || Game1.eventUp || SaveGame.IsProcessing || Game1.game1.IsSaving)
            throw new InvalidOperationException("Close the current menu and wait for a playable, non-saving world first.");
    }

    private static string? Raw(Item item) => item.modData.TryGetValue(GiftKey, out string value) ? value : null;

    private static InventoryLine[] Snapshot() => Game1.player.Items
        .Select((item, slot) => item is null ? null : new InventoryLine(slot, item.QualifiedItemId, item.Stack, Raw(item)))
        .OfType<InventoryLine>().ToArray();

    private sealed record FixtureIdentity(int SchemaVersion, string WorkspaceOwnerId, string FixtureId, ulong UniqueGameId, string SaveId);
    private sealed record GuardContext(FixtureIdentity Identity, string CheckpointPath);
    private sealed record InventoryLine(int Slot, string ItemId, int Stack, string? RawStamp);
    private sealed record Checkpoint(string FixtureId, ulong UniqueGameId, InventoryLine[] Items, JournalSnapshot Journal);
}
