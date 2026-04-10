using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum GroupFilter
{
    All = 0,
    Solo = 1,
    Party = 2,
    Alliance = 3,
}
