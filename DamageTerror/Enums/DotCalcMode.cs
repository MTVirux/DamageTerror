using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DotCalcMode
{
    Plugin = 0,
    Iinact = 1,
    Refined = 2,
}
