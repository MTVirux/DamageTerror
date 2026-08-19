namespace DamageTerror.Models;

/// <summary>
/// Position, size and colour for one party list row part - the name, the HP bar or the MP
/// bar. Each part is its own node, so all three can be placed and coloured separately.
/// The name takes its size from its font instead, so <see cref="Scale"/> is only read for
/// the gauge bars. <see cref="ShieldStyle"/> extends this for the shield over the HP bar.
/// </summary>
public class RowPartStyle
{
    public bool Enabled { get; set; } = true;
    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = -5f;
    public float Scale { get; set; } = 1f;

    /// <summary>Off leaves the game's own colour - role tinting on the name, artwork on the bars.</summary>
    public bool UseCustomColor { get; set; } = false;
    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 1f);
}
