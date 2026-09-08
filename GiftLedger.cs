namespace DearlyKept;

/// <summary>Preserves separate receipts even when they describe identical gifts.</summary>
internal sealed class GiftLedger
{
    private readonly List<GiftEntry> entries = new();
    private readonly HashSet<string> receiptIds = new(StringComparer.Ordinal);
    public IReadOnlyList<GiftEntry> Entries { get; }
    public int Revision { get; private set; }

    public GiftLedger() => Entries = entries.AsReadOnly();

    public bool Add(GiftEntry? entry)
    {
        if (entry is null || !entry.IsValid() || !receiptIds.Add(entry.Id))
            return false;
        entries.Add(entry);
        Revision++;
        return true;
    }

    public bool UpdateMessage(string id, string messageText)
    {
        if (messageText is null || !receiptIds.Contains(id))
            return false;
        int index = entries.FindIndex(entry => entry.Id == id);
        GiftEntry original = entries[index];
        GiftEntry updated = original with { MessageText = messageText };
        if (!updated.IsValid() || original.MessageText == messageText)
            return false;
        entries[index] = updated;
        Revision++;
        return true;
    }

    public void Clear()
    {
        entries.Clear();
        receiptIds.Clear();
        Revision++;
    }

    public bool MarkMessageIncomplete(string id)
    {
        int index = entries.FindIndex(e => e.Id == id);
        if (index < 0 || entries[index].MessageIncomplete) return false;
        entries[index] = entries[index] with { MessageIncomplete = true };
        Revision++;
        return true;
    }

    public int Load(JournalData data)
    {
        if (data.SchemaVersion != 1 || data.Gifts is null)
            throw new InvalidDataException("Unsupported or invalid gift journal format.");
        Clear();
        int rejected = 0;
        foreach (GiftEntry? entry in data.Gifts)
            if (!Add(entry))
                rejected++;
        return rejected;
    }

    public JournalData Snapshot() => new() { Gifts = entries.ToList() };
}
