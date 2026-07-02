using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum SkillDamageType : byte
{
    Unknown = 0,
    Physical = 1,
    Magic = 2,
}
