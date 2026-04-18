using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum EndEncounterMode
{
    Echo = 0,
    Endenc = 1,
}
