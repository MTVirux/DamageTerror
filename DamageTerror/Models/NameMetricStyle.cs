namespace DamageTerror.Models;

/// <summary>
/// How one name metric is drawn. Each metric owns a text node, since a single Atk text node
/// has one font size and one colour, so every metric can be styled on its own.
/// </summary>
public sealed class NameMetricStyle
{
    /// <summary>Offset from the name's font size, which the metric otherwise copies.</summary>
    public int FontDelta { get; set; } = -2;

    /// <summary>Space before this metric - from the name's text, or from the previous metric.</summary>
    public float Gap { get; set; } = 7f;

    /// <summary>Lifts this metric off the name's line without moving the ones after it.</summary>
    public float OffsetY { get; set; } = 0f;

    /// <summary>Off follows the name's own colour, which is what the game gives the row.</summary>
    public bool UseCustomColor { get; set; } = false;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 1f);
}
