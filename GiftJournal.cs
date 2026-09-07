using System.Text.Json;
using StardewModdingAPI;
using StardewValley;

namespace DearlyKept;

internal sealed class GiftJournal
{
    private const string SaveKey = "gift-journal";
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly GiftLedger ledger = new();
    private Farmer? owner;

    public IReadOnlyList<GiftEntry> Entries => ledger.Entries;
    public int Revision => ledger.Revision;
    public bool CanRecord => owner is not null && Context.IsWorldReady && Context.IsMainPlayer
        && !Context.IsMultiplayer && ReferenceEquals(owner, Game1.player);

    public GiftJournal(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
        helper.Events.GameLoop.SaveLoaded += (_, _) => Load();
        helper.Events.GameLoop.Saving += (_, _) => Save();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { owner = null; ledger.Clear(); };
    }

    private void Load()
    {
        owner = null;
        ledger.Clear();
        if (!Context.IsMainPlayer || Context.IsMultiplayer)
            return;
        try
        {
            JournalData data = helper.Data.ReadSaveData<JournalData>(SaveKey) ?? new JournalData();
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
            helper.Data.WriteSaveData(SaveKey, ledger.Snapshot());
    }

    public GiftEntry? CreateEntry(Item item, string senderId, string sourceId, string origin, string? sourceModId)
    {
        if (!CanRecord || item is null || item.Stack <= 0)
            return null;
        GiftEntry entry = new(Guid.NewGuid().ToString("N"), senderId, sourceId, origin, sourceModId,
            Game1.year, Game1.currentSeason, Game1.dayOfMonth, item.QualifiedItemId, item.DisplayName, item.Stack, item.Quality);
        return entry.IsValid() ? entry : null;
    }

    public void Record(GiftEntry entry)
    {
        if (CanRecord && ledger.Add(entry))
            monitor.Log($"Recorded {entry.QualifiedItemId} x{entry.Quantity} from {entry.SenderId} ({entry.SourceId}) in the gift journal.", LogLevel.Trace);
    }

    public bool Remove(string id) => CanRecord && ledger.Remove(id);

    public string GetGiftsJson() => JsonSerializer.Serialize(Entries);
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
