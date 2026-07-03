namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ValueDisplayFormat
{
    Abbreviated = 0,

    Commas = 1,

    Raw = 2,
}
