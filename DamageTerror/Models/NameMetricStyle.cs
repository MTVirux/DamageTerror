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

    /// <summary>Takes the metric off the row for as long as a cast bar is up. The cast bar covers
    /// the name's line and is drawn under the metrics, so this is for anyone who would rather read
    /// the cast than the number.</summary>
    public bool HideWhileCasting { get; set; } = false;

    /// <summary>Where the metric sits, measured from the row's top left corner. Nothing about
    /// the name is read, so a long name no longer moves it.</summary>
    public float OffsetX { get; set; } = DefaultOffsetX;
    public float OffsetY { get; set; } = DefaultOffsetY;

    /// <summary>Metrics used to be chained off the end of the name's text, each with a gap
    /// before it. A gap says nothing about where the metric sat, so a config carrying one is
    /// given a column of its own by <see cref="PartyListOverlaySettings"/> instead.</summary>
    [JsonProperty("Gap", NullValueHandling = NullValueHandling.Ignore)]
    internal float? LegacyGap;

    /// <summary>How the game draws a resting party list name, kept here so a config can be read
    /// without the game's palette to hand. <see cref="GameUiColors"/> is the real source.</summary>
    public static readonly Vector4 DefaultColor = new(1f, 1f, 1f, 1f);
    public static readonly Vector4 DefaultOutlineColor = new(49f / 255f, 97f / 255f, 134f / 255f, 1f);

    /// <summary>Off draws the metric the way the game draws a resting party list name. Not the
    /// name node's live colour: the game drives that from the row's timeline, so it moves as the
    /// row changes state - a cast bar taking the name over is the usual way to see it, and the
    /// metrics used to be dragged along with it.</summary>
    public bool UseCustomColor { get; set; } = false;
    public Vector4 Color { get; set; } = DefaultColor;

    /// <summary>The outline's half of the same thing.</summary>
    public bool UseCustomOutlineColor { get; set; } = false;
    public Vector4 OutlineColor { get; set; } = DefaultOutlineColor;
}
