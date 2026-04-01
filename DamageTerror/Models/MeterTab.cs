using Newtonsoft.Json;

namespace DamageTerror.Models;

public class MeterTab
{
    public string Name { get; set; } = "DPS";

    public TabFilterMode FilterMode { get; set; } = TabFilterMode.All;

    public SortField SortBy { get; set; } = SortField.EncDps;

    public bool SortDescending { get; set; } = true;

    public bool ShowDpsOnBar { get; set; } = true;
    public bool ShowHpsOnBar { get; set; } = false;
    public bool ShowDamageOnBar { get; set; } = false;
    public bool ShowHealedOnBar { get; set; } = false;
    public bool ShowDamagePercentOnBar { get; set; } = false;
    public bool ShowDirectHitOnBar { get; set; } = false;
    public bool ShowCritOnBar { get; set; } = false;
    public bool ShowCritDirectHitOnBar { get; set; } = false;
    public bool ShowDeathsOnBar { get; set; } = false;
    public bool ShowDamageTakenOnBar { get; set; } = false;
    public bool ShowOverhealOnBar { get; set; } = false;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<BarColumn> ColumnOrder { get; set; } = new()
    {
        BarColumn.DamagePercent,
        BarColumn.CritDirectHit,
        BarColumn.Crit,
        BarColumn.DirectHit,
        BarColumn.Deaths,
        BarColumn.Healed,
        BarColumn.Damage,
        BarColumn.Hps,
        BarColumn.Dps,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<BarColumn, string> ColumnHeaderLabels { get; set; } = new();

    public string GetHeaderLabel(BarColumn col)
    {
        if (ColumnHeaderLabels.TryGetValue(col, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
    }

    public List<string> CustomJobFilter { get; set; } = new();

    public MeterTab() { }

    public MeterTab(string name, TabFilterMode filterMode = TabFilterMode.All,
        SortField sortBy = SortField.EncDps, bool sortDescending = true)
    {
        Name = name;
        FilterMode = filterMode;
        SortBy = sortBy;
        SortDescending = sortDescending;
    }

    public MeterTab Clone()
    {
        return new MeterTab
        {
            Name = Name,
            FilterMode = FilterMode,
            SortBy = SortBy,
            SortDescending = SortDescending,
            ShowDpsOnBar = ShowDpsOnBar,
            ShowHpsOnBar = ShowHpsOnBar,
            ShowDamageOnBar = ShowDamageOnBar,
            ShowHealedOnBar = ShowHealedOnBar,
            ShowDamagePercentOnBar = ShowDamagePercentOnBar,
            ShowDirectHitOnBar = ShowDirectHitOnBar,
            ShowCritOnBar = ShowCritOnBar,
            ShowCritDirectHitOnBar = ShowCritDirectHitOnBar,
            ShowDeathsOnBar = ShowDeathsOnBar,
            ShowDamageTakenOnBar = ShowDamageTakenOnBar,
            ShowOverhealOnBar = ShowOverhealOnBar,
            ColumnOrder = new List<BarColumn>(ColumnOrder),
            ColumnHeaderLabels = new Dictionary<BarColumn, string>(ColumnHeaderLabels),
            CustomJobFilter = new List<string>(CustomJobFilter),
        };
    }

    public bool PassesFilter(CombatantEntry combatant)
    {
        if (FilterMode == TabFilterMode.All)
            return true;

        if (FilterMode == TabFilterMode.Deaths)
            return combatant.Deaths > 0;

        if (FilterMode == TabFilterMode.Custom)
        {
            if (CustomJobFilter.Count == 0)
                return true;
            return CustomJobFilter.Any(j => string.Equals(j, combatant.Job, StringComparison.OrdinalIgnoreCase));
        }

        var role = JobColorHelper.GetRole(combatant.Job);
        return FilterMode switch
        {
            TabFilterMode.Tanks => role == JobRole.Tank,
            TabFilterMode.Healers => role == JobRole.Healer,
            TabFilterMode.DPS => role is JobRole.MeleeDps or JobRole.RangedDps or JobRole.CasterDps,
            TabFilterMode.MeleeDPS => role == JobRole.MeleeDps,
            TabFilterMode.RangedDPS => role == JobRole.RangedDps,
            TabFilterMode.CasterDPS => role == JobRole.CasterDps,
            _ => true,
        };
    }
}
