using System.Text.Json;

namespace DearlyKept;

public static class StampCodec
{
    public static string Encode(GiftStamp stamp)
    {
        if (!stamp.IsValid)
            throw new ArgumentException("The gift stamp is not valid.", nameof(stamp));
        return JsonSerializer.Serialize(stamp);
    }

    public static bool TryDecode(string? value, out GiftStamp stamp)
    {
        stamp = null!;
        if (string.IsNullOrEmpty(value) || value.Length > 4096)
            return false;
        try
        {
            GiftStamp? parsed = JsonSerializer.Deserialize<GiftStamp>(value);
            if (parsed?.IsValid != true)
                return false;
            stamp = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // Compare even unreadable or newer stamps: merging them would discard data.
    public static bool CanMerge(string? first, string? second)
        => string.Equals(first, second, StringComparison.Ordinal);
}
