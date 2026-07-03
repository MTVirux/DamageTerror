namespace DamageTerror.Models;

internal static class DictionaryHelpers
{
    /// <summary>
    /// Returns a case-insensitive (OrdinalIgnoreCase) copy of <paramref name="dict"/>,
    /// or the original if it is already case-insensitive. Newtonsoft.Json deserializes
    /// dictionaries with the default (case-sensitive) comparer, so per-name lookups
    /// need this fix-up after deserialization.
    /// </summary>
    public static Dictionary<string, List<TValue>> EnsureCaseInsensitive<TValue>(
        Dictionary<string, List<TValue>>? dict)
    {
        if (dict is null)
            return new Dictionary<string, List<TValue>>(StringComparer.OrdinalIgnoreCase);
        if (dict.Count > 0 && dict.Comparer != StringComparer.OrdinalIgnoreCase)
            return new Dictionary<string, List<TValue>>(dict, StringComparer.OrdinalIgnoreCase);
        return dict;
    }
}
