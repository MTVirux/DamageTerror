namespace DamageTerror.Models;

/// <summary>
/// How one name metric is drawn. Each metric owns a text node, since a single Atk text node
/// has one font size and one colour, so every metric can be styled on its own.
/// </summary>
public sealed class NameMetricStyle
{
    /// <summary>Where the first metric sits, and how far apart the ones after it are placed
    /// when they have no position of their own. Past the name box, which the row's own
    /// artwork puts at x 76 and 184 wide.</summary>
    public const float DefaultOffsetX = 190f;
    public const float DefaultOffsetY = 22f;
    public const float ColumnStep = 55f;

    /// <summary>The nth column's default position, so metrics seeded together don't overlap.</summary>
    public static float ColumnX(int index) => DefaultOffsetX + (ColumnStep * Math.Max(0, index));

    /// <summary>Offset from the name's font size, which the metric otherwise copies.</summary>
    public int FontDelta { get; set; } = -2;

    /// <summary>Where the metric sits, measured from the row's top left corner. Nothing about
    /// the name is read, so a long name no longer moves it.</summary>
    public float OffsetX { get; set; } = DefaultOffsetX;
    public float OffsetY { get; set; } = DefaultOffsetY;

    /// <summary>Metrics used to be chained off the end of the name's text, each with a gap
    /// before it. A gap says nothing about where the metric sat, so a config carrying one is
    /// given a column of its own by <see cref="PartyListOverlaySettings"/> instead.</summary>
    [JsonProperty("Gap", NullValueHandling = NullValueHandling.Ignore)]
    internal float? LegacyGap;

    /// <summary>Off follows the name's own colour, which is what the game gives the row.</summary>
    public bool UseCustomColor { get; set; } = false;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>Off follows the name's own outline. The game drives that outline from the row's
    /// timeline, so it shifts between two blues as the row changes state - a cast bar taking the
    /// name over is the usual way to see it. On pins the outline and the metric stops following.
    /// The default is the blue the game paints a resting name with.</summary>
    public bool UseCustomOutlineColor { get; set; } = false;
    public Vector4 OutlineColor { get; set; } = new(49f / 255f, 97f / 255f, 134f / 255f, 1f);
}
