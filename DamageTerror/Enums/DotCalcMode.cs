using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum DotCalcMode
{
    Refined = 0,
    Iinact = 1,

    [Obsolete("Replaced by Refined. Kept for config deserialization backward compat.")]
    Plugin = 2,
}
