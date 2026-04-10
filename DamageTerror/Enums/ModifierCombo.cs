using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ModifierCombo
{
    CtrlShift = 0,
    CtrlAlt = 1,
    ShiftAlt = 2,
    Ctrl = 3,
    Shift = 4,
    Alt = 5,
}
