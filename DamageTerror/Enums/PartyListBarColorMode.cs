namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum PartyListBarColorMode
{
    MatchMeter = 0,
    OwnPalette = 1,
    SingleColor = 2,
}
