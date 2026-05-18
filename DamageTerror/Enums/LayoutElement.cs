using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum LayoutElement
{
    EncounterSelect = 0,
    MeterTabs = 1,
    StatusBar = 2,
    CombatantBars = 3,
    ReplayBar = 4,
}
