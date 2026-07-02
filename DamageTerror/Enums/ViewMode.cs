using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ViewMode
{
    Bars = 0,
    LineGraph = 1,
}
