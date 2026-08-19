namespace DamageTerror.Models;

/// <summary>
/// One piece of the shield drawn over a party list HP bar - the fill layered inside the bar,
/// or the overflow bar above it. Each piece is several sibling nodes rather than one, so the
/// position, scale and colour from <see cref="RowPartStyle"/> are applied to every node of
/// the piece.
/// </summary>
public sealed class ShieldStyle : RowPartStyle
{
    public ShieldStyle()
    {
        Enabled = false;
        OffsetY = 0f;
    }

    /// <summary>Hides the piece outright, independently of whether it is being moved.</summary>
    public bool Hidden { get; set; } = false;

    /// <summary>Multiplied over the alpha the game gives the artwork, so 1 leaves it alone.</summary>
    public float Opacity { get; set; } = 1f;
}
