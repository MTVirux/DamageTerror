namespace DamageTerror.Models;

/// <summary>
/// The plate drawn behind a row's slot number. The game has no node for it, so it is one of
/// ours, sized to the number's own box and drawn from the container behind the rows - the
/// only place it can sit under the number, since our nodes are appended and an appended node
/// draws over the ones already there.
/// </summary>
public sealed class PartyIndexBadgeStyle
{
    public bool Enabled { get; set; } = false;

    /// <summary>Added to each side of the number's box. Negative pulls the plate inside it.</summary>
    public float PaddingX { get; set; } = 3f;
    public float PaddingY { get; set; } = 1f;

    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = 0f;

    public Vector4 Color { get; set; } = new(0.05f, 0.06f, 0.09f, 1f);

    /// <summary>The plate's alpha. The artwork is white, so the colour is used as picked.</summary>
    public float Opacity { get; set; } = 0.75f;
}
