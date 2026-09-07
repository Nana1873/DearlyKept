using System.Text.Json.Serialization;

namespace DearlyKept;

public sealed record GiftStamp(string SenderId, string MailId, int Year, string Season, int Day)
{
    [JsonIgnore]
    public bool IsValid => !string.IsNullOrWhiteSpace(SenderId) && SenderId.Length <= 128
        && !string.IsNullOrWhiteSpace(MailId) && MailId.Length <= 256
        && Year is >= 1 and <= 9999 && Day is >= 1 and <= 28
        && Season is "spring" or "summer" or "fall" or "winter";
}
