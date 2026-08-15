namespace DamageTerror.Models;

/// <summary>Font size and vertical offset for one gauge's numbers, HP or MP.</summary>
public sealed class GaugeNumberStyle
{
    public bool Enabled { get; set; } = false;

    /// <summary>Added to the game's own font size for the numbers.</summary>
    public int FontDelta { get; set; } = 0;
    public float OffsetY { get; set; } = 0f;
}
