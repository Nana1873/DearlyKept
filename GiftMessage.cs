namespace DearlyKept;

internal static class GiftMessage
{
    public const int MaximumLength = 16000;

    public static string? Normalize(string? text)
    {
        // The game uses ^ as a visible line break. Never re-run dialogue or mail commands.
        string? result = text?.Replace('^', '\n');
        return string.IsNullOrWhiteSpace(result) || result.Length > MaximumLength ? null : result;
    }
}
