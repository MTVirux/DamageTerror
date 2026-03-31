namespace DamageTerror.Enums;

/// <summary>
/// Controls how numeric values (DPS/HPS/damage) are formatted on bars and the status bar.
/// </summary>
public enum ValueDisplayFormat
{
    /// <summary>Abbreviated with K/M suffixes (e.g. 12.3K, 1.50M).</summary>
    Abbreviated,

    /// <summary>Full number with comma separators (e.g. 12,345).</summary>
    Commas,

    /// <summary>Raw number with no formatting (e.g. 12345.6).</summary>
    Raw,
}
