using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum TabFilterMode
{
    All = 0,
    Tanks = 1,
    Healers = 2,
    DPS = 3,
    MeleeDPS = 4,
    RangedDPS = 5,
    CasterDPS = 6,
    Deaths = 7,
    Custom = 8,
}
