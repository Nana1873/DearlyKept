namespace DearlyKept;

/// <summary>A receipt snapshot, independent of any live item or inventory slot.</summary>
public sealed record GiftEntry(
    string Id,
    string SenderId,
    string SourceId,
    string Origin,
    string? SourceModId,
    int Year,
    string Season,
    int Day,
    string QualifiedItemId,
    string ItemName,
    int Quantity,
    int Quality,
    string? MessageText = null,
    bool MessageIncomplete = false,
    string? SenderDisplayName = null,
    string? SourceDisplayName = null)
{
    public bool IsValid() => Guid.TryParseExact(Id, "N", out _)
        && Text(SenderId, 128) && Text(SourceId, 512)
        && Origin is "mail" or "birthday" or "spouse" or "anniversary" or "other"
        && (SourceModId is null || Text(SourceModId, 256))
        && Year is >= 1 and <= 9999 && Day is >= 1 and <= 28
        && Season is "spring" or "summer" or "fall" or "winter"
        && Text(QualifiedItemId, 256) && Text(ItemName, 512)
        && Quantity > 0 && Quality >= 0
        && (MessageText is null || MessageText.Length <= 16000)
        && (SenderDisplayName is null || Text(SenderDisplayName, 512))
        && (SourceDisplayName is null || Text(SourceDisplayName, 512));

    private static bool Text(string? value, int limit) => !string.IsNullOrWhiteSpace(value) && value.Length <= limit;
}

public sealed class JournalData
{
    public int SchemaVersion { get; set; } = 1;
    public List<GiftEntry> Gifts { get; set; } = new();
}
