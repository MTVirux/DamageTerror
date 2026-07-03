using System.Runtime.Serialization;

namespace DamageTerror.Models;

/// <summary>Per-line skill marker appearance settings.</summary>
public sealed record class SkillMarkerConfig
{
    public bool ShowMarkers { get; set; } = true;
    public Vector4 MarkerColor { get; set; } = new(1f, 0.85f, 0.3f, 0.9f);
    public float MarkerSize { get; set; } = 4f;
    public bool ShowCritMarkers { get; set; } = true;
    public Vector4 CritMarkerColor { get; set; } = new(1f, 0.6f, 0.2f, 0.95f);
    public Vector4 DirectHitMarkerColor { get; set; } = new(0.3f, 0.85f, 1f, 0.95f);
    public Vector4 CritDirectHitMarkerColor { get; set; } = new(1f, 0.4f, 0.8f, 0.95f);

    public bool ShowDoTTickMarkers { get; set; } = true;
    public Vector4 DoTTickColor { get; set; } = new(0.6f, 0.2f, 0.8f, 0.9f);
    public float DoTTickMarkerSize { get; set; } = 3f;

    public bool ShowDoTApplicationMarkers { get; set; } = true;
    public Vector4 DoTApplicationColor { get; set; } = new(0.9f, 0.3f, 0.9f, 0.95f);
    public float DoTApplicationMarkerSize { get; set; } = 5f;
}

public sealed class MeterTab
{
    #region Tab Identity & Filtering

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "DPS";

    public string Group { get; set; } = "";

    public TabFilterMode FilterMode { get; set; } = TabFilterMode.All;

    public SortField SortBy { get; set; } = SortField.EncDps;

    public bool SortDescending { get; set; } = true;

    public bool IsHidden { get; set; } = false;

    public ViewMode ViewMode { get; set; } = ViewMode.Bars;

    public GroupFilter GroupFilter { get; set; } = GroupFilter.All;

    #endregion

    #region Status Bar

    // Per-tab status bar content visibility
    public bool ShowStatusBarTimer { get; set; } = true;

    /// <summary>When true, per-column value colors override the active-encounter color in the status bar.</summary>
    public bool StatusBarColorOverridesActive { get; set; } = true;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<BarColumn> StatusBarMetrics { get; set; } = new() { BarColumn.Dps, BarColumn.EncDps };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<BarColumn, string> StatusBarMetricLabels { get; set; } = new();

    #endregion

    #region Graph Settings

    /// <summary>Whether DPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowDpsLine { get; set; } = true;

    /// <summary>Whether HPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowHpsLine { get; set; } = false;

    /// <summary>Whether DTPS graph line should be shown (independent of bar columns).</summary>
    public bool GraphShowDtpsLine { get; set; } = false;

    #endregion

    #region Column Settings

    /// <summary>Set of visible bar columns for this tab.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<BarColumn> VisibleColumns { get; set; } = new()
    {
        BarColumn.Dps, BarColumn.Damage, BarColumn.DamagePercent,
        BarColumn.DirectHit, BarColumn.Crit, BarColumn.CritDirectHit,
        BarColumn.SkillIssue,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<BarColumn> ColumnOrder { get; set; } = new()
    {
        BarColumn.Dps,
        BarColumn.Damage,
        BarColumn.Hps,
        BarColumn.DirectHit,
        BarColumn.Crit,
        BarColumn.CritDirectHit,
        BarColumn.Healed,
        BarColumn.HealPercent,
        BarColumn.DamagePercent,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, string> ColumnHeaderLabels { get; set; } = new();

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, ColumnFormatOverride> ColumnFormatOverrides { get; set; } = new();

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, Vector4> ColumnValueColors { get; set; } = new();

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, float> ColumnWidthOverrides { get; set; } = new();

    public Vector4? GetColumnValueColor(BarColumn col)
        => ColumnValueColors.TryGetValue(col, out var color) ? color : null;

    public float? GetColumnWidth(BarColumn col)
        => ColumnWidthOverrides.TryGetValue(col, out var width) ? width : null;

    public string GetHeaderLabel(BarColumn col)
    {
        if (ColumnHeaderLabels.TryGetValue(col, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
    }

    public string GetStatusBarLabel(BarColumn col)
    {
        if (StatusBarMetricLabels.TryGetValue(col, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
    }

    public string GetTooltipFieldLabel(TooltipField field)
    {
        if (TooltipFieldLabels.TryGetValue(field, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return ColumnLabels.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString());
    }

    public string GetDetailColumnLabel(BarColumn col)
    {
        if (DetailColumnLabels.TryGetValue(col, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return ColumnLabels.DefaultDetailColumnLabels.GetValueOrDefault(col, col.ToString());
    }

    public bool IsColumnVisible(BarColumn col) => VisibleColumns.Contains(col);

    public List<string> CustomJobFilter { get; set; } = new();

    #endregion

    #region Tooltip Settings

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<TooltipField> TooltipFields { get; set; } = new()
    {
        TooltipField.Name,
        TooltipField.Job,
        TooltipField.Dps,
        TooltipField.Damage,
        TooltipField.DamagePercent,
        TooltipField.Crit,
        TooltipField.DirectHit,
        TooltipField.CritDirectHit,
        TooltipField.MaxHit,
        TooltipField.Deaths,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<TooltipField, string> TooltipFieldLabels { get; set; } = new();

    public int TooltipTopSkillCount { get; set; } = 3;

    #endregion

    #region Detail Panel

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<BarColumn, string> DetailColumnLabels { get; set; } = new();

    /// <summary>Set of visible columns in the expanded detail panel for this tab.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<BarColumn> DetailVisibleColumns { get; set; } = new(Enum.GetValues<BarColumn>());

    /// <summary>Whether to show the Details tab in the detail panel.</summary>
    public bool DetailShowDetailsTab { get; set; } = true;

    /// <summary>Whether to show the Skills tab in the detail panel.</summary>
    public bool DetailShowSkillsTab { get; set; } = true;

    /// <summary>Whether to show the Graph tab in the detail panel.</summary>
    public bool DetailShowGraphTab { get; set; } = true;

    /// <summary>Whether to show the Buffs/Debuffs tab in the detail panel.</summary>
    public bool DetailShowBuffsTab { get; set; } = true;

    /// <summary>Whether to show the Item tab in the detail panel.</summary>
    public bool DetailShowItemTab { get; set; } = true;

    /// <summary>Whether to show the skill breakdown section in the detail panel.</summary>
    public bool DetailShowSkillBreakdown { get; set; } = true;

    /// <summary>Maximum skills shown in skill breakdown (0 = all).</summary>
    public int MaxSkillBreakdownCount { get; set; } = 0;

    /// <summary>Set of columns that start a new line in the detail panel.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<BarColumn> DetailNewLineColumns { get; set; } = new();

    /// <summary>Per-section column order for the detail panel. Key is section name, value is ordered column list.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<string, List<BarColumn>> DetailSectionOrder { get; set; } = new();

    #endregion

    #region Legacy Migration

    [JsonExtensionData]
    private Dictionary<string, JToken>? _extensionData;

    private static readonly Dictionary<string, BarColumn> LegacyColumnMap = new()
    {
        { "ShowDpsColumn", BarColumn.Dps },
        { "ShowHpsColumn", BarColumn.Hps },
        { "ShowDamageColumn", BarColumn.Damage },
        { "ShowHealedColumn", BarColumn.Healed },
        { "ShowDamagePercentColumn", BarColumn.DamagePercent },
        { "ShowHealPercentColumn", BarColumn.HealPercent },
        { "ShowDirectHitColumn", BarColumn.DirectHit },
        { "ShowCritColumn", BarColumn.Crit },
        { "ShowCritDirectHitColumn", BarColumn.CritDirectHit },
        { "ShowDeathsColumn", BarColumn.Deaths },
        { "ShowDamageTakenColumn", BarColumn.DamageTaken },
        { "ShowDamageTakenPercentColumn", BarColumn.DamageTakenPercent },
        { "ShowOverhealColumn", BarColumn.Overheal },
        { "ShowOverhealAmountColumn", BarColumn.OverhealAmount },
        { "ShowMaxHitColumn", BarColumn.MaxHit },
        { "ShowPeakDpsColumn", BarColumn.PeakDps },
        { "ShowMaxHealColumn", BarColumn.MaxHeal },
        { "ShowSwingsColumn", BarColumn.Swings },
        { "ShowHitsColumn", BarColumn.Hits },
        { "ShowMissesColumn", BarColumn.Misses },
        { "ShowHitRateColumn", BarColumn.HitRate },
        { "ShowCritHitCountColumn", BarColumn.CritHitCount },
        { "ShowDirectHitCountColumn", BarColumn.DirectHitCount },
        { "ShowCritDirectHitCountColumn", BarColumn.CritDirectHitCount },
        { "ShowBlockPctColumn", BarColumn.BlockPct },
        { "ShowParryPctColumn", BarColumn.ParryPct },
        { "ShowHealsTakenColumn", BarColumn.HealsTaken },
        { "ShowAbsorbHealColumn", BarColumn.AbsorbHeal },
        { "ShowKillsColumn", BarColumn.Kills },
        { "ShowInstantDpsColumn", BarColumn.InstantDps },
        { "ShowInstantHpsColumn", BarColumn.InstantHps },
        { "ShowCritHealPctColumn", BarColumn.CritHealPct },
        { "ShowHealCountColumn", BarColumn.HealCount },
        { "ShowCombatantDurationColumn", BarColumn.CombatantDuration },
        { "ShowDamageShieldColumn", BarColumn.DamageShield },
        { "ShowMaxHealWardColumn", BarColumn.MaxHealWard },
        { "ShowPowerDrainColumn", BarColumn.PowerDrain },
        { "ShowPowerHealColumn", BarColumn.PowerHeal },
    };

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (_extensionData == null || _extensionData.Count == 0)
            return;

        var migrated = new HashSet<BarColumn>();
        var foundLegacy = false;

        foreach (var (key, col) in LegacyColumnMap)
        {
            if (_extensionData.TryGetValue(key, out var val))
            {
                foundLegacy = true;
                if (val.Value<bool>())
                    migrated.Add(col);
            }
        }

        if (foundLegacy)
            VisibleColumns = migrated;

        _extensionData = null;
    }

    #endregion

    #region Construction & Methods

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
        var json = JsonConvert.SerializeObject(this, MeterTabCloneJsonSettings);
        return JsonConvert.DeserializeObject<MeterTab>(json, MeterTabCloneJsonSettings)!;
    }

    private static readonly JsonSerializerSettings MeterTabCloneJsonSettings = new()
    {
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    public bool PassesFilter(CombatantEntry combatant, HashSet<string>? partyNames = null, HashSet<string>? allianceNames = null)
    {
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

        var role = JobRegistry.GetRole(combatant.Job);
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

    #endregion
}
