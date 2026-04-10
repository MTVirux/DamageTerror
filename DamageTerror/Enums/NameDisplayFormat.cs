using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DamageTerror.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum NameDisplayFormat
{
    FullName = 0,
    FirstNameOnly = 1,
    LastNameOnly = 2,
    Initials = 3,
    JobAbbreviation = 4,
    JobFullName = 5,
    Truncated = 6,
}
