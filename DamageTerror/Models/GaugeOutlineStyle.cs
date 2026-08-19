namespace DamageTerror.Models;

/// <summary>
/// The outline around an HP or MP bar. The game draws it as part of the bar's backdrop -
/// the empty groove the fill sits in - rather than as a node of its own, so it can be
/// tinted, faded or hidden, but not made thicker.
/// </summary>
public sealed class GaugeOutlineStyle
{
    /// <summary>
    /// Follows the bar by default: the backdrop used to be tinted along with the fill, so a
    /// config written before the two were split keeps the look it was saved with.
    /// </summary>
    public GaugeOutlineColorMode ColorMode { get; set; } = GaugeOutlineColorMode.FollowBar;

    public Vector4 Color { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>Multiplied over the alpha the game gives the artwork, so 1 leaves it alone.</summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>Hides the outline outright, leaving the fill over the bare row.</summary>
    public bool Hidden { get; set; } = false;
}
