using StardewValley;

namespace DearlyKept;

public static class GiftTagService
{
    public const string DataKey = "Nana1873.DearlyKept/Provenance";

    public static bool TryRead(Item item, out GiftStamp stamp)
        => StampCodec.TryDecode(Raw(item), out stamp);

    public static void Stamp(Item item, GiftStamp stamp)
    {
        if (!item.modData.ContainsKey(DataKey))
            item.modData[DataKey] = StampCodec.Encode(stamp);
    }

    public static void Remove(Item item) => item.modData.Remove(DataKey);

    public static bool CanMerge(Item first, Item second)
        => StampCodec.CanMerge(Raw(first), Raw(second));

    private static string? Raw(Item item)
        => item.modData.TryGetValue(DataKey, out string value) ? value : null;
}
