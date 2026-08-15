namespace DamageTerror.Enums;

/// <summary>The stats that can be drawn after a party list name, in the order they appear.</summary>
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
