namespace DamageTerror.Enums;

/// <summary>
/// How heavy the outline around each glyph is drawn. The game has no outline width - only an
/// edge pass and a wider glare pass - so these are the three weights it can actually produce.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum PartyListOutlineThickness
{
    None = 0,
    Thin = 1,
    Thick = 2,
}
