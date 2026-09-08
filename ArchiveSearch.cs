namespace DearlyKept;

internal static class ArchiveSearch
{
    public static bool Matches(string query, params string?[] fields) => query.Length == 0
        || fields.Any(field => field?.Contains(query, StringComparison.OrdinalIgnoreCase) == true);
}
