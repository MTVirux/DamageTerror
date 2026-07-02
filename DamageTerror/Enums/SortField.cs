using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum SortField
{
    EncDps = 0,
    EncHps = 1,
    Damage = 2,
    Healed = 3,
    CritPct = 4,
    Deaths = 5,
    DamageTaken = 6,
}
