namespace DamageTerror.Models;

public class ColumnFormatOverride
{
    public ValueDisplayFormat ValueDisplayFormat { get; set; } = ValueDisplayFormat.Abbreviated;
    public int AbbreviatedDecimalPlaces { get; set; } = 1;
    public int RawDecimalPlaces { get; set; } = 1;
    public int PercentDecimalPlaces { get; set; } = 0;
    public double AbbreviatedKThreshold { get; set; } = 10_000;
    public double AbbreviatedMThreshold { get; set; } = 1_000_000;

    public ColumnFormatOverride Clone() => new()
    {
        ValueDisplayFormat = ValueDisplayFormat,
        AbbreviatedDecimalPlaces = AbbreviatedDecimalPlaces,
        RawDecimalPlaces = RawDecimalPlaces,
        PercentDecimalPlaces = PercentDecimalPlaces,
        AbbreviatedKThreshold = AbbreviatedKThreshold,
        AbbreviatedMThreshold = AbbreviatedMThreshold,
    };

    /// <summary>Columns whose values go through ValueFormatter.Format (numeric values).</summary>
    public static readonly HashSet<BarColumn> ValueColumns = new()
    {
        BarColumn.Dps, BarColumn.Hps, BarColumn.Damage, BarColumn.Healed,
        BarColumn.DamageTaken, BarColumn.OverhealAmount, BarColumn.MaxHitValue,
        BarColumn.PeakDps, BarColumn.MaxHealValue, BarColumn.HealsTaken,
        BarColumn.AbsorbHeal, BarColumn.InstantDps, BarColumn.InstantHps,
        BarColumn.DamageShield, BarColumn.MaxHealWard, BarColumn.PowerDrain,
        BarColumn.PowerHeal, BarColumn.EncDps, BarColumn.EncHps,
        BarColumn.GroupDps, BarColumn.GroupHps, BarColumn.GroupDamage,
        BarColumn.GroupHealed, BarColumn.GroupDamageTaken, BarColumn.GroupOverheal,
        BarColumn.GroupInstantDps, BarColumn.GroupInstantHps,
        BarColumn.GroupAvgDps, BarColumn.GroupAvgHps,
        BarColumn.GroupPeakDps, BarColumn.GroupMaxHitValue, BarColumn.GroupMaxHealValue,
    };

    /// <summary>Columns whose values go through ValueFormatter.FormatPercent.</summary>
    public static readonly HashSet<BarColumn> PercentColumns = new()
    {
        BarColumn.CritDirectHit, BarColumn.Crit, BarColumn.DirectHit,
        BarColumn.Overheal, BarColumn.HitRate, BarColumn.BlockPct,
        BarColumn.ParryPct, BarColumn.CritHealPct,
        BarColumn.GroupAvgCrit, BarColumn.GroupAvgDirectHit, BarColumn.GroupAvgCritDirectHit,
        BarColumn.GroupAvgOverhealPct, BarColumn.GroupAvgCritHealPct, BarColumn.GroupAvgHitRate,
    };

    public static bool SupportsFormatting(BarColumn col)
        => ValueColumns.Contains(col) || PercentColumns.Contains(col);
}
