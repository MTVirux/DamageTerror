using Newtonsoft.Json;

namespace DamageTerror.Models;

/// <summary>Per-line skill marker appearance settings.</summary>
public class SkillMarkerConfig
{
    public bool ShowMarkers { get; set; } = true;
    public Vector4 MarkerColor { get; set; } = new(1f, 0.85f, 0.3f, 0.9f);
    public float MarkerSize { get; set; } = 4f;
    public bool ShowCritMarkers { get; set; } = true;
    public Vector4 CritMarkerColor { get; set; } = new(1f, 0.6f, 0.2f, 0.95f);
    public Vector4 DirectHitMarkerColor { get; set; } = new(0.3f, 0.85f, 1f, 0.95f);
    public Vector4 CritDirectHitMarkerColor { get; set; } = new(1f, 0.4f, 0.8f, 0.95f);

    // DoT/HoT tick markers
    public bool ShowDoTTickMarkers { get; set; } = true;
    public Vector4 DoTTickColor { get; set; } = new(0.6f, 0.2f, 0.8f, 0.9f);
    public float DoTTickMarkerSize { get; set; } = 3f;

    // DoT/HoT application markers
    public bool ShowDoTApplicationMarkers { get; set; } = true;
    public Vector4 DoTApplicationColor { get; set; } = new(0.9f, 0.3f, 0.9f, 0.95f);
    public float DoTApplicationMarkerSize { get; set; } = 5f;

    public SkillMarkerConfig Clone() => new()
    {
        ShowMarkers = ShowMarkers,
        MarkerColor = MarkerColor,
        MarkerSize = MarkerSize,
        ShowCritMarkers = ShowCritMarkers,
        CritMarkerColor = CritMarkerColor,
        DirectHitMarkerColor = DirectHitMarkerColor,
        CritDirectHitMarkerColor = CritDirectHitMarkerColor,
        ShowDoTTickMarkers = ShowDoTTickMarkers,
        DoTTickColor = DoTTickColor,
        DoTTickMarkerSize = DoTTickMarkerSize,
        ShowDoTApplicationMarkers = ShowDoTApplicationMarkers,
        DoTApplicationColor = DoTApplicationColor,
        DoTApplicationMarkerSize = DoTApplicationMarkerSize,
    };
}

public class MeterTab
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "DPS";

    public string Group { get; set; } = "";

    public TabFilterMode FilterMode { get; set; } = TabFilterMode.All;

    public SortField SortBy { get; set; } = SortField.EncDps;

    public bool SortDescending { get; set; } = true;

    public bool IsHidden { get; set; } = false;

    public ViewMode ViewMode { get; set; } = ViewMode.Bars;

    public GroupFilter GroupFilter { get; set; } = GroupFilter.All;

    /// <summary>Whether DPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowDpsLine { get; set; } = true;

    /// <summary>Whether HPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowHpsLine { get; set; } = false;

    /// <summary>Whether DTPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowDtpsLine { get; set; } = false;

    /// <summary>Skill marker settings for the DPS line.</summary>
    public SkillMarkerConfig DpsMarkers { get; set; } = new();

    /// <summary>Skill marker settings for the HPS line.</summary>
    public SkillMarkerConfig HpsMarkers { get; set; } = new();

    /// <summary>Skill marker settings for the DTPS line.</summary>
    public SkillMarkerConfig DtpsMarkers { get; set; } = new();

    public bool ShowDpsColumn { get; set; } = true;
    public bool ShowHpsColumn { get; set; } = false;
    public bool ShowDamageColumn { get; set; } = false;
    public bool ShowHealedColumn { get; set; } = false;
    public bool ShowDamagePercentColumn { get; set; } = false;
    public bool ShowHealPercentColumn { get; set; } = false;
    public bool ShowDirectHitColumn { get; set; } = false;
    public bool ShowCritColumn { get; set; } = false;
    public bool ShowCritDirectHitColumn { get; set; } = false;
    public bool ShowDeathsColumn { get; set; } = false;
    public bool ShowDamageTakenColumn { get; set; } = false;
    public bool ShowDamageTakenPercentColumn { get; set; } = false;
    public bool ShowOverhealColumn { get; set; } = false;
    public bool ShowOverhealAmountColumn { get; set; } = false;
    public bool ShowMaxHitColumn { get; set; } = false;
    public bool ShowPeakDpsColumn { get; set; } = false;
    public bool ShowMaxHealColumn { get; set; } = false;
    public bool ShowSwingsColumn { get; set; } = false;
    public bool ShowHitsColumn { get; set; } = false;
    public bool ShowMissesColumn { get; set; } = false;
    public bool ShowHitRateColumn { get; set; } = false;
    public bool ShowCritHitCountColumn { get; set; } = false;
    public bool ShowDirectHitCountColumn { get; set; } = false;
    public bool ShowCritDirectHitCountColumn { get; set; } = false;
    public bool ShowBlockPctColumn { get; set; } = false;
    public bool ShowParryPctColumn { get; set; } = false;
    public bool ShowHealsTakenColumn { get; set; } = false;
    public bool ShowAbsorbHealColumn { get; set; } = false;
    public bool ShowKillsColumn { get; set; } = false;
    public bool ShowInstantDpsColumn { get; set; } = false;
    public bool ShowInstantHpsColumn { get; set; } = false;
    public bool ShowCritHealPctColumn { get; set; } = false;
    public bool ShowHealCountColumn { get; set; } = false;
    public bool ShowCombatantDurationColumn { get; set; } = false;
    public bool ShowDamageShieldColumn { get; set; } = false;
    public bool ShowMaxHealWardColumn { get; set; } = false;
    public bool ShowPowerDrainColumn { get; set; } = false;
    public bool ShowPowerHealColumn { get; set; } = false;

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

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<BarColumn, ColumnFormatOverride> ColumnFormatOverrides { get; set; } = new();

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
            // New GUID for cloned tab — do not copy Id
            Name = Name,
            Group = Group,
            IsHidden = IsHidden,
            FilterMode = FilterMode,
            GroupFilter = GroupFilter,
            SortBy = SortBy,
            SortDescending = SortDescending,
            ViewMode = ViewMode,
            GraphShowDpsLine = GraphShowDpsLine,
            GraphShowHpsLine = GraphShowHpsLine,
            GraphShowDtpsLine = GraphShowDtpsLine,
            DpsMarkers = DpsMarkers.Clone(),
            HpsMarkers = HpsMarkers.Clone(),
            DtpsMarkers = DtpsMarkers.Clone(),
            ShowDpsColumn = ShowDpsColumn,
            ShowHpsColumn = ShowHpsColumn,
            ShowDamageColumn = ShowDamageColumn,
            ShowHealedColumn = ShowHealedColumn,
            ShowDamagePercentColumn = ShowDamagePercentColumn,
            ShowHealPercentColumn = ShowHealPercentColumn,
            ShowDirectHitColumn = ShowDirectHitColumn,
            ShowCritColumn = ShowCritColumn,
            ShowCritDirectHitColumn = ShowCritDirectHitColumn,
            ShowDeathsColumn = ShowDeathsColumn,
            ShowDamageTakenColumn = ShowDamageTakenColumn,
            ShowDamageTakenPercentColumn = ShowDamageTakenPercentColumn,
            ShowOverhealColumn = ShowOverhealColumn,
            ShowOverhealAmountColumn = ShowOverhealAmountColumn,
            ShowMaxHitColumn = ShowMaxHitColumn,
            ShowPeakDpsColumn = ShowPeakDpsColumn,
            ShowMaxHealColumn = ShowMaxHealColumn,
            ShowSwingsColumn = ShowSwingsColumn,
            ShowHitsColumn = ShowHitsColumn,
            ShowMissesColumn = ShowMissesColumn,
            ShowHitRateColumn = ShowHitRateColumn,
            ShowCritHitCountColumn = ShowCritHitCountColumn,
            ShowDirectHitCountColumn = ShowDirectHitCountColumn,
            ShowCritDirectHitCountColumn = ShowCritDirectHitCountColumn,
            ShowBlockPctColumn = ShowBlockPctColumn,
            ShowParryPctColumn = ShowParryPctColumn,
            ShowHealsTakenColumn = ShowHealsTakenColumn,
            ShowAbsorbHealColumn = ShowAbsorbHealColumn,
            ShowKillsColumn = ShowKillsColumn,
            ShowInstantDpsColumn = ShowInstantDpsColumn,
            ShowInstantHpsColumn = ShowInstantHpsColumn,
            ShowCritHealPctColumn = ShowCritHealPctColumn,
            ShowHealCountColumn = ShowHealCountColumn,
            ShowCombatantDurationColumn = ShowCombatantDurationColumn,
            ShowDamageShieldColumn = ShowDamageShieldColumn,
            ShowMaxHealWardColumn = ShowMaxHealWardColumn,
            ShowPowerDrainColumn = ShowPowerDrainColumn,
            ShowPowerHealColumn = ShowPowerHealColumn,
            ColumnOrder = new List<BarColumn>(ColumnOrder),
            ColumnHeaderLabels = new Dictionary<BarColumn, string>(ColumnHeaderLabels),
            ColumnFormatOverrides = ColumnFormatOverrides.ToDictionary(kv => kv.Key, kv => kv.Value.Clone()),
            CustomJobFilter = new List<string>(CustomJobFilter),
        };
    }

    public bool PassesFilter(CombatantEntry combatant, HashSet<string>? partyNames = null, HashSet<string>? allianceNames = null)
    {
        // Group filter (Solo/Party/Alliance) — applied first
        if (GroupFilter != GroupFilter.All)
        {
            var passes = GroupFilter switch
            {
                GroupFilter.Solo => combatant.IsLocalPlayer,
                GroupFilter.Party => combatant.IsLocalPlayer
                    || (partyNames != null && PartyMembershipService.MatchesName(partyNames, combatant.Name)),
                GroupFilter.Alliance => combatant.IsLocalPlayer
                    || (allianceNames != null && PartyMembershipService.MatchesName(allianceNames, combatant.Name)),
                _ => true,
            };
            if (!passes)
                return false;
        }

        // Role filter
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
