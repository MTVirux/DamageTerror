namespace DamageTerror.Models;

/// <summary>Font size, offset and colour for one gauge's numbers, HP or MP.</summary>
public sealed class GaugeNumberStyle
{
    public bool Enabled { get; set; } = false;

    /// <summary>Added to the game's own font size for the numbers.</summary>
    public int FontDelta { get; set; } = 0;
    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = 0f;

    /// <summary>Off leaves the game's own colour, which turns red as HP drops.</summary>
    public bool UseCustomColor { get; set; } = false;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 1f);
}
