namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum JobIconStyle
{
    Framed = 0,
    Plain = 1,
    Custom = 2,
}
