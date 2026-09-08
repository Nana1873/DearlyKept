using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;

namespace DearlyKept.MultiplayerHarness;

internal sealed partial class ModEntry
{
    private readonly PerScreen<SplitAttempt> splitStates = new(() => new SplitAttempt());
    private bool splitActive;
    private string? splitFixture;
    private long splitFarmer;
    private readonly Dictionary<int, string> splitActions = new();
    private void RegisterSplit(IModHelper helper)
    {
        helper.ConsoleCommands.Add("dks", "Owned single fixture: start | mail | close | assert | menu | reader | sleep | end", (_, args) =>
        {
            try
            {
                GuardSplit();
                string action = args.FirstOrDefault() ?? "assert";
                if (action == "start") StartSplit();
                else if (action == "end")
                {
                    Game1 other = GameRunner.instance.gameInstances.Single(i => !i.IsMainInstance);
                    GameRunner.instance.RemoveGameInstance(other);
                    splitActive = false;
                }
                else
                {
                    if (!splitActive) throw new InvalidOperationException("Start the owned split-screen fixture first.");
                    foreach (var pair in splitStates.GetActiveValues()) splitActions[pair.Key] = action;
                }
            }
            catch (Exception ex) { Monitor.Log("DKSPLIT FAIL: " + ex, LogLevel.Error); }
        });
        helper.Events.GameLoop.UpdateTicked += (_, _) => UpdateSplit();
    }
    private void GuardSplit()
    {
        if (Role != "single" || Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_MODE") != "review"
            || !Context.IsWorldReady || !Game1.player.IsLocalPlayer) throw new InvalidOperationException("Owned local fixture required.");
        string fixture = Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_FIXTURE_ID") ?? "";
        using JsonDocument registration = JsonDocument.Parse(File.ReadAllText(Path.Combine(Lab, "single", "test-save", "fixture.json")));
        if (!Guid.TryParseExact(fixture, "N", out _) || registration.RootElement.GetProperty("fixtureId").GetString() != fixture
            || registration.RootElement.GetProperty("uniqueGameId").GetUInt64() != Game1.uniqueIDForThisGame
            || !Game1.MasterPlayer.modData.TryGetValue("SDVKit/FixtureId", out string actual) || actual != fixture)
            throw new InvalidOperationException("The exact registered world must be loaded.");
        using JsonDocument status = JsonDocument.Parse(File.ReadAllText(Path.Combine(Lab, "single", "runtime", "always-on-status.json")));
        if (status.RootElement.GetProperty("processId").GetInt32() != Environment.ProcessId
            || status.RootElement.GetProperty("projectMod").GetProperty("loadedUniqueId").GetString() != ModId)
            throw new InvalidOperationException("The owned process and target must match.");
    }
    private void StartSplit()
    {
        NoMenu();
        if (!Context.IsMainPlayer || GameRunner.instance.gameInstances.Count != 1 || Game1.getAllFarmhands().Any())
            throw new InvalidOperationException("Start from the registered baseline with one local instance and no farmhands.");
        splitFixture = Environment.GetEnvironmentVariable("SDVKIT_TEST_SAVE_FIXTURE_ID");
        Farm farm = Game1.getFarm();
        var positions = (List<Vector2>)typeof(GameLocation).GetField("_startingCabinLocations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(farm)!;
        var layer = farm.Map.GetLayer("Paths");
        var candidates = new List<Vector2>();
        for (int x = 0; x < layer.LayerWidth; x++)
            for (int y = 0; y < layer.LayerHeight; y++)
                if (layer.Tiles[x, y] is { TileIndex: 29 } tile && tile.Properties.TryGetValue("Order", out var order) && order.ToString() == "1")
                    candidates.Add(new Vector2(x, y));
        if (positions.Count != 0 || candidates.Count != 1) throw new InvalidOperationException("Unexpected cabin baseline.");
        positions.Add(candidates.Single()); farm.BuildStartingCabins();
        Farmer hand = Game1.getAllFarmhands().Single();
        hand.Name = "DearlySplit"; hand.displayName = hand.Name; hand.isCustomized.Value = true;
        hand.modData["SDVKit/FixtureId"] = splitFixture;
        splitFarmer = hand.UniqueMultiplayerID;
        splitActive = true;
        GameRunner.instance.AddGameInstance(PlayerIndex.Two);
        Monitor.Log("DKSPLIT started a real second GameRunner instance in the owned fixture.", LogLevel.Info);
    }
    private void UpdateSplit()
    {
        if (!splitActive) return;
        try
        {
            if (!Game1.game1.IsMainInstance && !Context.IsWorldReady && Game1.activeClickableMenu is FarmhandMenu menu)
            {
                Client client = Helper.Reflection.GetField<Client>(menu, "client").GetValue();
                var available = Helper.Reflection.GetField<List<Farmer>?>(client, "availableFarmhands").GetValue();
                if (available is null) return;
                Farmer hand = available.Single(f => f.UniqueMultiplayerID == splitFarmer);
                if (!hand.modData.TryGetValue("SDVKit/FixtureId", out string marker) || marker != splitFixture)
                    throw new InvalidOperationException("Local join offered a different fixture farmer.");
                new FarmhandMenu.FarmhandSlot(menu, hand).Activate();
                Game1.options.pauseWhenOutOfFocus = false;
                Game1.options.gamepadMode = Options.GamepadModes.ForceOn;
                Game1.options.gamepadControls = true;
                return;
            }
            if (!Context.IsWorldReady) return;
            GuardSplit();
            SplitAttempt state = splitStates.Value;
            if (!state.Ready)
            {
                state.Ready = true;
                Monitor.Log($"DKSPLIT ready: screen={Context.ScreenId}, player={Game1.player.UniqueMultiplayerID}, viewport={Game1.uiViewport.Width}x{Game1.uiViewport.Height}", LogLevel.Info);
            }
            if (!splitActions.Remove(Context.ScreenId, out string? action)) return;
            if (action == "mail")
            {
                NoMenu(); state.Before = Journal;
                string id = Context.IsMainPlayer ? "mom1" : "georgeGifts";
                var letter = new LetterViewerMenu(Game1.content.Load<Dictionary<string, string>>("Data/mail")[id], id);
                state.Source = id; state.Message = string.Concat(letter.mailMessage).Replace('^', '\n');
                Game1.activeClickableMenu = letter;
            }
            else if (action == "close") Helper.Input.Press(SButton.Escape);
            else if (action == "menu") Helper.Input.Press(SButton.K);
            else if (action == "reader") Helper.Input.Press(SButton.Enter);
            else if (action == "sleep") Game1.currentLocation.answerDialogueAction("Sleep_Yes", Array.Empty<string>());
            else if (action == "assert")
            {
                NoMenu();
                using JsonDocument before = JsonDocument.Parse(state.Before ?? "[]");
                using JsonDocument now = JsonDocument.Parse(Journal);
                Check(now.RootElement.GetArrayLength() == before.RootElement.GetArrayLength() + 1, $"screen {Context.ScreenId} has only its own new gift");
                JsonElement entry = now.RootElement.EnumerateArray().Last();
                Check(entry.GetProperty("SourceId").GetString() == state.Source && entry.GetProperty("MessageText").GetString() == state.Message,
                    $"screen {Context.ScreenId} retains its own actual letter and personalized text");
                Check(RawEntries(Game1.player) == Journal, $"screen {Context.ScreenId} serializes to the correct farmer");
            }
            Monitor.Log($"DKSPLIT action={action}, screen={Context.ScreenId}, count={Api.GetGiftCount()}, menu={Game1.activeClickableMenu?.GetType().Name}", LogLevel.Info);
        }
        catch (Exception ex) { splitActive = false; Monitor.Log("DKSPLIT FAIL: " + ex, LogLevel.Error); }
    }
    private sealed class SplitAttempt
    {
        public bool Ready { get; set; }
        public string? Before { get; set; }
        public string? Source { get; set; }
        public string? Message { get; set; }
    }
}
