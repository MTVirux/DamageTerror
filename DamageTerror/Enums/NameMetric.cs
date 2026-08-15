namespace DamageTerror.Enums;

/// <summary>
/// The fixed set of stats a party list name could be given before it took the meter's own
/// columns. Read only to migrate configs written back then.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum NameMetric
{
    Dps = 0,
    Damage = 1,
    Crit = 2,
    DirectHit = 3,
    CritDirectHit = 4,
    DamagePercent = 5,
}
