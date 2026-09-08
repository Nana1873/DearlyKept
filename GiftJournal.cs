using System.Text.Json;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace DearlyKept;

internal sealed class GiftJournal
{
    private const string SaveKey = "gift-journal";
    internal const string PlayerDataKey = "Nana1873.DearlyKept/GiftJournal";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly PerScreen<PlayerState> screens = new(() => new PlayerState());
    private GiftLedger ledger => screens.Value.Ledger;
    private Farmer? owner { get => screens.Value.Owner; set => screens.Value.Owner = value; }

    public IReadOnlyList<GiftEntry> Entries => ledger.Entries;
    public int Revision => ledger.Revision;
    public bool CanRecord => owner is not null && Context.IsWorldReady
        && Game1.player.IsLocalPlayer && ReferenceEquals(owner, Game1.player);

    public GiftJournal(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) => Save();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => screens.Value = new PlayerState();
    }

    private void Load()
    {
        owner = null;
        ledger.Clear();
        if (!Game1.player.IsLocalPlayer)
            return;
        try
        {
            // Farmer.modData is synchronized by the game and survives farmhand disconnects.
            // Only the host's local player may import the pre-multiplayer save-level archive.
            JournalData data = Game1.player.modData.TryGetValue(PlayerDataKey, out string raw)
                ? JsonSerializer.Deserialize<JournalData>(raw) ?? throw new InvalidDataException("Null player journal.")
                : Context.IsMainPlayer ? helper.Data.ReadSaveData<JournalData>(SaveKey) ?? new JournalData() : new JournalData();
            int rejected = ledger.Load(data);
            if (rejected > 0)
            {
                monitor.Log($"Gift journal contains {rejected} invalid or duplicate entries. It is read-only for this session to preserve the original save data.", LogLevel.Warn);
                return;
            }
            owner = Game1.player;
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't read the gift journal. Recording and saving are disabled for this session to preserve its data: {ex.Message}", LogLevel.Error);
        }
    }

    private void Save()
    {
        if (CanRecord)
        {
            try
            {
                string raw = JsonSerializer.Serialize(ledger.Snapshot());
                if (!owner!.modData.TryGetValue(PlayerDataKey, out string previous) || previous != raw)
                    owner.modData[PlayerDataKey] = raw;
            }
            catch (Exception ex)
            {
                monitor.Log($"Couldn't synchronize the current player's gift journal: {ex.Message}", LogLevel.Error);
            }
        }
    }

    public GiftEntry? CreateEntry(Item item, string senderId, string sourceId, string origin, string? sourceModId, string? messageText = null)
    {
        if (!CanRecord || item is null || item.Stack <= 0)
            return null;
        GiftEntry entry = new(Guid.NewGuid().ToString("N"), senderId, sourceId, origin, sourceModId,
            Game1.year, Game1.currentSeason, Game1.dayOfMonth, item.QualifiedItemId, item.DisplayName, item.Stack, item.Quality, messageText);
        return entry.IsValid() ? entry : null;
    }

    public void Record(GiftEntry entry)
    {
        if (CanRecord && ledger.Add(entry))
        {
            Save();
            monitor.Log($"Recorded {entry.QualifiedItemId} x{entry.Quantity} from {entry.SenderId} ({entry.SourceId}) in the gift journal.", LogLevel.Trace);
        }
    }

    public bool UpdateMessage(string id, string messageText)
    {
        if (!CanRecord || !ledger.UpdateMessage(id, messageText)) return false;
        Save();
        return true;
    }

    public string GetGiftsJson() => JsonSerializer.Serialize(Entries);

    private sealed class PlayerState
    {
        public GiftLedger Ledger { get; } = new();
        public Farmer? Owner { get; set; }
    }
}

/// <summary>Read-only journal access for integrations and diagnostics.</summary>
public sealed class JournalApi
{
    private readonly Func<string> read;
    private readonly Func<int> count;
    internal JournalApi(Func<string> read, Func<int> count) { this.read = read; this.count = count; }
    public int GetGiftCount() => count();
    public string GetGiftsJson() => read();
}
