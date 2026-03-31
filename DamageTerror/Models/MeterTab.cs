using Newtonsoft.Json;

namespace DamageTerror.Models;

public class MeterTab
{
    public string Name { get; set; } = "DPS";

    public TabFilterMode FilterMode { get; set; } = TabFilterMode.All;

    public SortField SortBy { get; set; } = SortField.EncDps;

    public bool SortDescending { get; set; } = true;

    public bool ShowHps { get; set; } = false;

    public List<string> CustomJobFilter { get; set; } = new();

    public MeterTab() { }

    public MeterTab(string name, TabFilterMode filterMode = TabFilterMode.All,
        SortField sortBy = SortField.EncDps, bool sortDescending = true, bool showHps = false)
    {
        Name = name;
        FilterMode = filterMode;
        SortBy = sortBy;
        SortDescending = sortDescending;
        ShowHps = showHps;
    }

    public MeterTab Clone()
    {
        return new MeterTab
        {
            Name = Name,
            FilterMode = FilterMode,
            SortBy = SortBy,
            SortDescending = SortDescending,
            ShowHps = ShowHps,
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
