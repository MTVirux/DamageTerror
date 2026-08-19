namespace DamageTerror.Enums;

/// <summary>
/// Where a gauge bar's outline takes its colour from. The outline is the game's own
/// empty-bar artwork, so every mode is a tint over it rather than a repaint.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum GaugeOutlineColorMode
{
    FollowBar = 0,
    GameArtwork = 1,
    Custom = 2,
}
