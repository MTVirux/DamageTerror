namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DotCalcMode
{
    Refined = 0,
    Iinact = 1,
}
