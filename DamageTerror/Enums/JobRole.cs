using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum JobRole
{
    Tank = 0,
    Healer = 1,
    MeleeDps = 2,
    RangedDps = 3,
    CasterDps = 4,
    LimitBreak = 5,
    Default = 6,
    DoHL = 7,
}
