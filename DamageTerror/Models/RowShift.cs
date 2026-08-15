namespace DamageTerror.Models;

/// <summary>
/// One party list row part's vertical nudge. Each part is moved on its own node, so the
/// name, the HP bar and the MP bar can sit at different heights.
/// </summary>
public sealed class RowShift
{
    public bool Enabled { get; set; } = true;
    public float OffsetY { get; set; } = -5f;
}
