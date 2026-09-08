using System.Reflection;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;

namespace DearlyKept.MultiplayerHarness;

internal sealed partial class ModEntry : Mod
{
    private const string JournalKey = "Nana1873.DearlyKept/GiftJournal";
    private const string ModId = "Nana1873.DearlyKept";
    private readonly List<ExpectedGift> expected = new();
    private string? baseline;
    private long reconnectPlayer;
    private string? reconnectJournal;
    private Client? reconnectClient;
    private int reconnectStep;
    private int reconnectTicks;
    private bool strictReconnect;
    private string Role => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SDVKIT_NETWORK_TWO_ROLE"))
        ? "single" : Environment.GetEnvironmentVariable("SDVKIT_NETWORK_TWO_ROLE")!;
    private string Lab
    {
        get
        {
            for (DirectoryInfo? part = new(Helper.DirectoryPath); part is not null; part = part.Parent)
                if (part.Name == ".sdvkit") return Path.Combine(part.FullName, "lab");
            throw new InvalidOperationException("Companion is outside an SDVKit staging directory.");
        }
    }
    private string CheckpointPath => Path.Combine(Lab, "network-2", Role, "runtime", "dearly-kept-checkpoint.json");
    public override void Entry(IModHelper helper)
    {
        helper.ConsoleCommands.Add("dkn", "Owned network fixture: prepare | mail <key> | assert | gift <birthday|reward|anniversary|happy-birthday> | giftassert | peers <host> <farmhand> | checkpoint | persist | sleep | reconnect | reconnect-strict | report", Run);
        helper.Events.GameLoop.UpdateTicked += (_, _) => UpdateReconnect();
        RegisterSplit(helper);
        helper.Events.GameLoop.UpdateTicked += (_, _) => ObserveGiftPages();
    }
    private void Guard()
    {
        if (!Context.IsWorldReady || !Game1.player.IsLocalPlayer || !Context.IsMultiplayer
            || Role != (Context.IsMainPlayer ? "host" : "farmhand")
            || Environment.GetEnvironmentVariable("SDVKIT_PROJECT_REVIEW") != "1")
            throw new InvalidOperationException("A local player in the owned network review is required.");
        string fixture = Environment.GetEnvironmentVariable("SDVKIT_NETWORK_TWO_FIXTURE_ID") ?? "";
        using JsonDocument registration = JsonDocument.Parse(File.ReadAllText(Path.Combine(Lab, "single", "test-save", "fixture.json")));
        JsonElement registered = registration.RootElement;
        if (!Guid.TryParseExact(fixture, "N", out _) || registered.GetProperty("fixtureId").GetString() != fixture
            || registered.GetProperty("uniqueGameId").GetUInt64() != Game1.uniqueIDForThisGame
            || !Game1.MasterPlayer.modData.TryGetValue("SDVKit/FixtureId", out string marker) || marker != fixture
            || !Game1.player.modData.TryGetValue("SDVKit/FixtureId", out string localMarker) || localMarker != fixture)
            throw new InvalidOperationException("Registered fixture and both live player identities must match.");
        using JsonDocument status = JsonDocument.Parse(File.ReadAllText(Path.Combine(Lab, "network-2", Role, "runtime", "always-on-status.json")));
        JsonElement root = status.RootElement;
        JsonElement network = root.GetProperty("networkTwo");
        if (root.GetProperty("processId").GetInt32() != Environment.ProcessId || root.GetProperty("phase").GetString() == "exiting"
            || network.GetProperty("fixtureId").GetString() != fixture || network.GetProperty("role").GetString() != Role
            || network.GetProperty("localPlayerId").GetInt64() != Game1.player.UniqueMultiplayerID
            || !root.GetProperty("projectMod").GetProperty("loadConfirmed").GetBoolean()
            || root.GetProperty("projectMod").GetProperty("loadedUniqueId").GetString() != ModId)
            throw new InvalidOperationException("Owned process, role and target markers do not match.");
    }
    private void NoMenu()
    {
        if (!Context.IsPlayerFree || Game1.activeClickableMenu is not null) throw new InvalidOperationException("Close the current menu first.");
    }
    private IDearlyKeptApi Api => Helper.ModRegistry.GetApi<IDearlyKeptApi>(ModId)!;
    private string Journal => Api.GetGiftsJson();
    private void Check(bool success, string text)
    {
        if (!success) throw new InvalidOperationException(text);
        Monitor.Log("DKNET PASS: " + text, LogLevel.Info);
    }
    private void Run(string command, string[] args)
    {
        try
        {
            Guard();
            switch (args.FirstOrDefault() ?? "report")
            {
                case "gift": StartGift(args[1]); break;
                case "giftassert": AssertGift(); break;
                case "prepare":
                    NoMenu(); baseline = Journal; expected.Clear();
                    for (int i = 0; i < Game1.player.Items.Count; i++) Game1.player.Items[i] = null;
                    Game1.player.Items[0] = ItemRegistry.Create("(O)223", 5);
                    break;
                case "mail":
                    NoMenu();
                    if (baseline is null || args.Length != 2 || args[1] is not ("mom1" or "georgeGifts" or "mom4"))
                        throw new InvalidOperationException("Prepare, then choose mom1, mom4 or georgeGifts.");
                    var letter = new LetterViewerMenu(Game1.content.Load<Dictionary<string, string>>("Data/mail")[args[1]], args[1]);
                    foreach (var component in letter.itemsToGrab.Where(c => c.item is not null))
                        expected.Add(new ExpectedGift(args[1], component.item.QualifiedItemId, component.item.Stack,
                            string.Concat(letter.mailMessage).Replace('^', '\n')));
                    Game1.activeClickableMenu = letter;
                    break;
                case "assert":
                    NoMenu();
                    using (JsonDocument before = JsonDocument.Parse(baseline ?? throw new InvalidOperationException("Prepare first.")))
                    using (JsonDocument now = JsonDocument.Parse(Journal))
                    {
                        JsonElement[] entries = now.RootElement.EnumerateArray().ToArray();
                        int offset = before.RootElement.GetArrayLength();
                        Check(entries.Length == offset + expected.Count, "exactly the locally received gifts were recorded");
                        Check(entries.Take(offset).Select(e => e.GetRawText()).SequenceEqual(before.RootElement.EnumerateArray().Select(e => e.GetRawText())), "prior player history is unchanged");
                        for (int i = 0; i < expected.Count; i++)
                        {
                            ExpectedGift gift = expected[i]; JsonElement entry = entries[offset + i];
                            Check(entry.GetProperty("SourceId").GetString() == gift.Source
                                && entry.GetProperty("QualifiedItemId").GetString() == gift.Item
                                && entry.GetProperty("Quantity").GetInt32() == gift.Quantity
                                && entry.GetProperty("MessageText").GetString() == gift.Message, "actual local item, quantity and prepared text match");
                        }
                        Check(entries.Select(e => e.GetProperty("Id").GetString()).Distinct().Count() == entries.Length, "receipt identities are unique");
                    }
                    Check(Game1.player.Items.Where(i => i is not null).All(i => !i.modData.Keys.Any(k => k.StartsWith(ModId))), "items contain no provenance tags");
                    Check(RawEntries(Game1.player) == Journal, "the current player's synchronized data equals the API archive");
                    break;
                case "peers":
                    Farmer host = Game1.MasterPlayer;
                    Farmer hand = Game1.getAllFarmers().Single(f => f.UniqueMultiplayerID != host.UniqueMultiplayerID);
                    Check(Count(host) == int.Parse(args[1]) && Count(hand) == int.Parse(args[2]), "host and farmhand archives synchronize independently");
                    break;
                case "checkpoint":
                    NoMenu(); File.WriteAllText(CheckpointPath, JsonSerializer.Serialize(new Checkpoint(Game1.uniqueIDForThisGame, Game1.player.UniqueMultiplayerID, Journal, Items())));
                    break;
                case "persist":
                    NoMenu(); Checkpoint saved = JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(CheckpointPath))!;
                    Check(saved.Game == Game1.uniqueIDForThisGame && saved.Player == Game1.player.UniqueMultiplayerID, "checkpoint belongs to the exact local player and world");
                    Check(saved.Journal == Journal && saved.Items == Items(), "full player journal and inventory survived restart");
                    break;
                case "sleep":
                    NoMenu(); Check(Game1.currentLocation.answerDialogueAction("Sleep_Yes", Array.Empty<string>()), "native sleep accepted"); break;
                case "reconnect-strict":
                case "reconnect":
                    NoMenu();
                    if (Context.IsMainPlayer || reconnectStep != 0) throw new InvalidOperationException("Only the joined farmhand can reconnect.");
                    reconnectPlayer = Game1.player.UniqueMultiplayerID; reconnectJournal = Journal;
                    strictReconnect = args[0] == "reconnect-strict";
                    reconnectTicks = 0; reconnectStep = 1; Game1.ExitToTitle(); break;
                case "report": break;
                default: throw new InvalidOperationException("Unknown fixture command.");
            }
            Monitor.Log($"DKNET STATE {Role}: player={Game1.player.UniqueMultiplayerID}; journal={Journal}", LogLevel.Info);
        }
        catch (Exception ex) { Monitor.Log("DKNET FAIL: " + ex, LogLevel.Error); }
    }
    private static string RawEntries(Farmer player)
    {
        if (!player.modData.TryGetValue(JournalKey, out string raw)) return "[]";
        using JsonDocument data = JsonDocument.Parse(raw);
        return data.RootElement.GetProperty("Gifts").GetRawText();
    }
    private static int Count(Farmer player) { using JsonDocument d = JsonDocument.Parse(RawEntries(player)); return d.RootElement.GetArrayLength(); }
    private static string Items() => JsonSerializer.Serialize(Game1.player.Items.Select(i => i is null ? null : new { i.QualifiedItemId, i.Stack, i.Quality }));
    private void UpdateReconnect()
    {
        if (reconnectStep == 0) return;
        try
        {
            if (++reconnectTicks > 7200) throw new TimeoutException("Owned farmhand reconnect timed out.");
            if (reconnectStep == 1 && reconnectTicks > 180 && Game1.activeClickableMenu is TitleMenu && TitleMenu.subMenu is null)
            {
                reconnectClient = (Client)typeof(Multiplayer).GetMethod("InitClient", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(typeof(Game1).GetField("multiplayer", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(null), new object[] { new LidgrenClient("127.0.0.1") })!;
                TitleMenu.subMenu = new FarmhandMenu(reconnectClient); reconnectStep = 2;
            }
            else if (reconnectStep == 2)
            {
                var available = (List<Farmer>?)typeof(Client).GetField("availableFarmhands", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(reconnectClient);
                if (available is null) return;
                if (available.Count != 1 || available[0].UniqueMultiplayerID != reconnectPlayer
                    || !available[0].modData.TryGetValue("SDVKit/FixtureId", out string marker)
                    || marker != Environment.GetEnvironmentVariable("SDVKIT_NETWORK_TWO_FIXTURE_ID"))
                    throw new InvalidOperationException("Loopback offered a different farmer or world; refusing selection.");
                Type slotType = typeof(FarmhandMenu).GetNestedType("FarmhandSlot", BindingFlags.NonPublic | BindingFlags.Public)!;
                object slot = Activator.CreateInstance(slotType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { TitleMenu.subMenu, available[0] }, null)!;
                slotType.GetMethod("Activate", BindingFlags.Public | BindingFlags.Instance)!.Invoke(slot, null);
                Monitor.Log($"DKNET reconnect options after native activation: pauseWhenOutOfFocus={Game1.options.pauseWhenOutOfFocus}.", LogLevel.Info);
                // Native activation replaces Options before the next SMAPI update.
                // Keep this explicitly unfocused fixture running through that transition.
                if (strictReconnect)
                    Check(!Game1.options.pauseWhenOutOfFocus, "SDVKit preserves background execution without a QA options override");
                else
                    Game1.options.pauseWhenOutOfFocus = false;
                reconnectStep = 3;
            }
            else if (reconnectStep == 3 && Context.IsWorldReady)
            {
                Guard(); Check(Journal == reconnectJournal, "farmhand left and rejoined without losing or mixing its journal");
                reconnectStep = 0;
            }
        }
        catch (Exception ex) { reconnectStep = 0; Monitor.Log("DKNET RECONNECT FAIL: " + ex, LogLevel.Error); }
    }
    private sealed record ExpectedGift(string Source, string Item, int Quantity, string Message);
    private sealed record Checkpoint(ulong Game, long Player, string Journal, string Items);
}

public interface IDearlyKeptApi
{
    int GetGiftCount();
    string GetGiftsJson();
}
