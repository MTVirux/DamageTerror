using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public bool ShowDoTTickMarkers { get; set; } = true;
    public Vector4 DoTTickColor { get; set; } = new(0.6f, 0.2f, 0.8f, 0.9f);
    public float DoTTickMarkerSize { get; set; } = 3f;

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

    /// <summary>Set of visible bar columns for this tab.</summary>
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<BarColumn> VisibleColumns { get; set; } = new() { BarColumn.Dps };

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

    public bool IsColumnVisible(BarColumn col) => VisibleColumns.Contains(col);

    public List<string> CustomJobFilter { get; set; } = new();

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
            VisibleColumns = new HashSet<BarColumn>(VisibleColumns),
            ColumnOrder = new List<BarColumn>(ColumnOrder),
            ColumnHeaderLabels = new Dictionary<BarColumn, string>(ColumnHeaderLabels),
            ColumnFormatOverrides = ColumnFormatOverrides.ToDictionary(kv => kv.Key, kv => kv.Value.Clone()),
            CustomJobFilter = new List<string>(CustomJobFilter),
        };
    }

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
