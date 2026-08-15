using System.Runtime.Serialization;
using Dalamud.Configuration;

namespace DamageTerror;

public enum BackgroundImageScaleMode
{
    Stretch,
    Fit,
    Fill,
    Tile,
}

file static class FontDefaults
{
    public const float BaseSizePt = 14f;
}

public sealed class Configuration : IPluginConfiguration
{
    [JsonIgnore]
    public Action? Save { get; set; }

    public int Version { get; set; } = 1;

    public string WebSocketUrl { get; set; } = "ws://127.0.0.1:10501/ws";
    public bool PreferIpc { get; set; } = true;

    public bool ShowOnStart { get; set; } = true;

    public bool HasCompletedSetup { get; set; } = false;

    public bool HasCompletedCustomizationWizard { get; set; } = false;

    public bool HasCompletedColumnWizard { get; set; } = false;

    public bool HideDebugFeatures { get; set; } = false;

    public bool CaptureRawFrames { get; set; } = false;

    /// <summary>Draw encounter DPS into the game's native party list via Atk nodes.</summary>
    public bool ShowPartyListDps { get; set; } = false;

    public PartyListOverlaySettings PartyList { get; set; } = new();

    public bool EnableInOverworld { get; set; } = true;
    public bool EnableInDungeons { get; set; } = true;
    public bool EnableInTrials { get; set; } = true;
    public bool EnableInRaids { get; set; } = true;
    public bool EnableInAllianceRaids { get; set; } = true;
    public bool EnableInDeepDungeons { get; set; } = true;
    public bool EnableInFieldOperations { get; set; } = true;
    public bool EnableInFieldRaids { get; set; } = true;
    public bool EnableInCriterion { get; set; } = true;
    public bool EnableInVariant { get; set; } = true;
    public bool EnableInPvP { get; set; } = true;

    public bool ShowJobIcons { get; set; } = true;
    public JobIconStyle JobIconStyle { get; set; } = JobIconStyle.Plain;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<string, uint> CustomJobIcons { get; set; } = new();

    public bool ShowTabBar { get; set; } = true;
    public int SelectedMeterTab { get; set; } = 0;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<MeterTab> MeterTabs { get; set; } = new()
    {
        new MeterTab("DPS", TabFilterMode.All, SortField.EncDps, true)
        {
            StatusBarMetrics = new() { BarColumn.DpsRank, BarColumn.Dps, BarColumn.EncDps },
            VisibleColumns = new()
            {
                BarColumn.Damage, BarColumn.Dps, BarColumn.DirectHit, BarColumn.Crit,
                BarColumn.CritDirectHit, BarColumn.DamagePercent, BarColumn.MaxHit,
            },
            ColumnOrder = new()
            {
                BarColumn.Damage, BarColumn.Dps, BarColumn.DirectHit, BarColumn.Crit, BarColumn.CritDirectHit, BarColumn.DamagePercent,
                BarColumn.MaxHit, BarColumn.EncDps, BarColumn.SkillIssue, BarColumn.Hps, BarColumn.Healed, BarColumn.HealPercent,
                BarColumn.Deaths, BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.MaxHitValue,
                BarColumn.PeakDps, BarColumn.MaxHeal, BarColumn.MaxHealValue, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses,
                BarColumn.HitRate, BarColumn.CritHitCount, BarColumn.DirectHitCount, BarColumn.CritDirectHitCount, BarColumn.BlockPct, BarColumn.ParryPct,
                BarColumn.HealsTaken, BarColumn.AbsorbHeal, BarColumn.Kills, BarColumn.InstantDps, BarColumn.InstantHps, BarColumn.CritHealPct,
                BarColumn.HealCount, BarColumn.CombatantDuration, BarColumn.DamageShield, BarColumn.MaxHealWard, BarColumn.PowerDrain, BarColumn.PowerHeal,
                BarColumn.LegsSweeped, BarColumn.DamageDown, BarColumn.Positionals, BarColumn.PositionalHits, BarColumn.PositionalMisses, BarColumn.PositionalPct,
                BarColumn.EncHps, BarColumn.DpsRank, BarColumn.HpsRank, BarColumn.GroupDps, BarColumn.GroupHps, BarColumn.GroupDamage,
                BarColumn.GroupHealed, BarColumn.GroupDamageTaken, BarColumn.GroupDeaths, BarColumn.GroupOverheal, BarColumn.GroupInstantDps, BarColumn.GroupInstantHps,
                BarColumn.GroupSkillIssue, BarColumn.GroupDamageDown, BarColumn.GroupAvgDps, BarColumn.GroupAvgHps, BarColumn.GroupAvgCrit, BarColumn.GroupAvgDirectHit,
                BarColumn.GroupAvgCritDirectHit, BarColumn.GroupAvgOverhealPct, BarColumn.GroupAvgCritHealPct, BarColumn.GroupAvgHitRate, BarColumn.GroupPeakDps, BarColumn.GroupMaxHitValue,
                BarColumn.GroupMaxHealValue,
            },
            DetailSectionOrder = new()
            {
                ["Damage"] = new() { BarColumn.Dps, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.Damage, BarColumn.DamagePercent, BarColumn.MaxHit, BarColumn.MaxHitValue, BarColumn.DamageShield, BarColumn.EncDps },
                ["Healing"] = new() { BarColumn.Hps, BarColumn.InstantHps, BarColumn.Healed, BarColumn.HealPercent, BarColumn.Overheal, BarColumn.OverhealAmount, BarColumn.CritHealPct, BarColumn.MaxHeal, BarColumn.MaxHealValue, BarColumn.HealCount, BarColumn.EncHps },
            },
        },
        new MeterTab("Healing", TabFilterMode.All, SortField.EncHps, true)
        {
            GraphShowDpsLine = false,
            GraphShowHpsLine = true,
            VisibleColumns = new() { BarColumn.Hps, BarColumn.Overheal },
            ColumnOrder = new()
            {
                BarColumn.Hps, BarColumn.Overheal, BarColumn.Dps, BarColumn.Damage, BarColumn.DirectHit, BarColumn.Crit,
                BarColumn.CritDirectHit, BarColumn.Healed, BarColumn.HealPercent, BarColumn.DamagePercent, BarColumn.Deaths, BarColumn.DamageTaken,
                BarColumn.DamageTakenPercent, BarColumn.OverhealAmount, BarColumn.MaxHit, BarColumn.MaxHitValue, BarColumn.PeakDps, BarColumn.MaxHeal,
                BarColumn.MaxHealValue, BarColumn.Swings, BarColumn.Hits, BarColumn.Misses, BarColumn.HitRate, BarColumn.CritHitCount,
                BarColumn.DirectHitCount, BarColumn.CritDirectHitCount, BarColumn.BlockPct, BarColumn.ParryPct, BarColumn.HealsTaken, BarColumn.AbsorbHeal,
                BarColumn.Kills, BarColumn.InstantDps, BarColumn.InstantHps, BarColumn.CritHealPct, BarColumn.HealCount, BarColumn.CombatantDuration,
                BarColumn.DamageShield, BarColumn.MaxHealWard, BarColumn.PowerDrain, BarColumn.PowerHeal, BarColumn.LegsSweeped, BarColumn.SkillIssue,
                BarColumn.DamageDown, BarColumn.Positionals, BarColumn.PositionalHits, BarColumn.PositionalMisses, BarColumn.PositionalPct, BarColumn.EncDps,
                BarColumn.EncHps, BarColumn.DpsRank, BarColumn.HpsRank, BarColumn.GroupDps, BarColumn.GroupHps, BarColumn.GroupDamage,
                BarColumn.GroupHealed, BarColumn.GroupDamageTaken, BarColumn.GroupDeaths, BarColumn.GroupOverheal, BarColumn.GroupInstantDps, BarColumn.GroupInstantHps,
                BarColumn.GroupSkillIssue, BarColumn.GroupDamageDown, BarColumn.GroupAvgDps, BarColumn.GroupAvgHps, BarColumn.GroupAvgCrit, BarColumn.GroupAvgDirectHit,
                BarColumn.GroupAvgCritDirectHit, BarColumn.GroupAvgOverhealPct, BarColumn.GroupAvgCritHealPct, BarColumn.GroupAvgHitRate, BarColumn.GroupPeakDps, BarColumn.GroupMaxHitValue,
                BarColumn.GroupMaxHealValue,
            },
        },
        new MeterTab("Solo", TabFilterMode.All, SortField.EncDps, true)
        {
            GroupFilter = GroupFilter.Solo,
            // Matches enum declaration order: this tab's column order was never manually reordered.
            ColumnOrder = new(Enum.GetValues<BarColumn>()),
        },
    };

    public Vector4 TabButtonColor { get; set; } = new(0.18f, 0.18f, 0.18f, 1.0f);
    public Vector4 TabButtonHoveredColor { get; set; } = new(0.28f, 0.31f, 0.36f, 0.22f);
    public Vector4 TabButtonActiveColor { get; set; } = new(0.64f, 0.19f, 0.19f, 1.0f);
    public Vector4 TabButtonTextColor { get; set; } = new(0.85f, 0.85f, 0.85f, 1.0f);
    public Vector4 TabButtonActiveTextColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float TabButtonHeight { get; set; } = 19f;
    public float TabButtonSpacing { get; set; } = 6f;
    public float TabButtonRounding { get; set; } = 4f;
    public float TabButtonWidth { get; set; } = 84f;
    public float TabButtonFontSize { get; set; } = FontDefaults.BaseSizePt;
    public bool TabButtonStretchToFit { get; set; } = true;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<LayoutElement> Layout { get; set; } = new()
    {
        LayoutElement.EncounterSelect,
        LayoutElement.ReplayBar,
        LayoutElement.CombatantBars,
        LayoutElement.MeterTabs,
        LayoutElement.StatusBar,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<LayoutElement> CtrlShiftOnlyElements { get; set; } = new();

    public bool ReplayBarPinned { get; set; } = false;

    public bool EnableReplays { get; set; } = true;

    public ModifierCombo ModifierKeyCombo { get; set; } = ModifierCombo.CtrlShift;
    public ModifierMode ModifierKeyMode { get; set; } = ModifierMode.Hold;

    public bool HideOutOfCombat { get; set; } = false;
    public float HideOutOfCombatDelay { get; set; } = 5f;

    public bool SkipZeroEdpsEncounters { get; set; } = true;

    public DotCalcMode DotCalcMode { get; set; } = DotCalcMode.Refined;

    public EndEncounterMode EndEncounterMode { get; set; } = EndEncounterMode.Echo;

    public HistoryLimitMode HistoryLimitMode { get; set; } = HistoryLimitMode.Count;
    public int MaxEncounterHistory { get; set; } = 50;
    public int MaxEncounterHistoryDays { get; set; } = 30;

    public HistoryLimitMode TimelineRetentionMode { get; set; } = HistoryLimitMode.Count;
    public int MaxTimelineCount { get; set; } = 20;
    public int MaxTimelineDays { get; set; } = 7;

    public bool IgnoreEscClose { get; set; } = true;
    public bool HideWindowHeader { get; set; } = false;
    public float BarAlpha { get; set; } = 0.7f;

    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 LimitBreakColor { get; set; } = new(1.0f, 0.80f, 0.0f, 1.0f);
    public Vector4 DoHLColor { get; set; } = new(0.70f, 0.55f, 0.30f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    public bool UsePerJobColors { get; set; } = true;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<string, Vector4> JobColors { get; set; } = new();
    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public string? BackgroundImagePath { get; set; }
    public float BackgroundImageOpacity { get; set; } = 1.0f;
    public Vector4 BackgroundImageTint { get; set; } = new(1f, 1f, 1f, 1f);
    public BackgroundImageScaleMode BackgroundImageScale { get; set; } = BackgroundImageScaleMode.Stretch;
    public float WindowPaddingLeft { get; set; } = 0f;
    public float WindowPaddingRight { get; set; } = 0f;
    public float WindowPaddingTop { get; set; } = 0f;
    public float WindowPaddingBottom { get; set; } = 0f;

    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; } = 0.0f;
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    public float HeaderHeight { get; set; } = 24.0f;
    public float HeaderFontSize { get; set; } = FontDefaults.BaseSizePt;
    public bool HeaderSeparator { get; set; } = true;
    public Vector4 HeaderSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    public bool EnableCustomFont { get; set; } = false;
    public string? CustomFontPath { get; set; }
    public int CustomFontIndex { get; set; }
    public float CustomFontSizePt { get; set; } = 14f;
    public string? CustomFontDisplayName { get; set; }
    public string? CustomFontSpecJson { get; set; }

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 20.0f;
    public float BarFontSize { get; set; } = FontDefaults.BaseSizePt;
    public float BarLeftPadding { get; set; } = 4.0f;
    public float BarRightPadding { get; set; } = 6.0f;
    public float BarColumnSpacing { get; set; } = 6.0f;
    public float IconTextPadding { get; set; } = 4.0f;

    public bool SelfBarHighlight { get; set; } = false;
    public Vector4 SelfBarHighlightColor { get; set; } = new(1.0f, 0.85f, 0.3f, 0.9f);
    public bool UseSelfNameColor { get; set; } = false;

    public Vector4 SelfNameColor { get; set; } = new(1.0f, 0.9f, 0.4f, 1.0f);

    public ValueDisplayFormat ValueDisplayFormat { get; set; } = ValueDisplayFormat.Abbreviated;
    public int AbbreviatedDecimalPlaces { get; set; } = 1;
    public int RawDecimalPlaces { get; set; } = 1;
    public int PercentDecimalPlaces { get; set; } = 1;
    public double AbbreviatedKThreshold { get; set; } = 10_000;
    public double AbbreviatedMThreshold { get; set; } = 1_000_000;

    public bool ShowMeterHeader { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowYouOnBar { get; set; } = false;
    public NameDisplayFormat SelfNameFormat { get; set; } = NameDisplayFormat.FullName;
    public NameDisplayFormat OthersNameFormat { get; set; } = NameDisplayFormat.FullName;
    public int NameTruncateLength { get; set; } = 12;
    public bool ShowJobAbbrevOnBar { get; set; } = true;
    public bool ShowRankNumber { get; set; } = false;

    public int MaxHitSkillNameLength { get; set; } = 0;
    public bool TruncateSkillNames { get; set; } = false;

    public Vector4 DetailBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.80f);
    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = FontDefaults.BaseSizePt;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<BarColumn> DetailVisibleColumns { get; set; } = new(Enum.GetValues<BarColumn>());
    public bool DetailShowDetailsTab { get; set; } = true;
    public bool DetailShowSkillsTab { get; set; } = true;
    public bool DetailShowGraphTab { get; set; } = true;
    public bool DetailShowBuffsTab { get; set; } = true;
    public bool DetailShowItemTab { get; set; } = true;
    public bool DetailShowSkillBreakdown { get; set; } = true;
    public int MaxSkillBreakdownCount { get; set; } = 0;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<BarColumn> DetailNewLineColumns { get; set; } = new();
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<string> DetailExpandedSections { get; set; } = new();

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<MetricType, SkillMarkerConfig> DetailMarkers { get; set; } = new()
    {
        [MetricType.Dps] = new SkillMarkerConfig(),
        [MetricType.Hps] = new SkillMarkerConfig(),
        [MetricType.Dtps] = new SkillMarkerConfig(),
    };

    // Legacy-JSON migration: old configs stored flat DetailDpsMarkers /
    // GraphViewDpsMarkers etc. keys. Capture unknown keys and route any of
    // them into the marker dictionaries after deserialization.
    [JsonExtensionData]
    private Dictionary<string, JToken>? _extensionData;

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
        => MarkerMigration.Apply(ref _extensionData, DetailMarkers, GraphViewMarkers);

    public bool ShowTooltip { get; set; } = true;
    public float TooltipDelay { get; set; } = 0.3f;

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

    public int TooltipTopSkillCount { get; set; } = 3;

    public Vector4 TooltipBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.95f);
    public Vector4 TooltipTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 TooltipLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 1f);
    public float TooltipFontSize { get; set; } = FontDefaults.BaseSizePt;
    public float TooltipRounding { get; set; } = 4f;
    public float TooltipPadding { get; set; } = 6f;

    public float GraphHeight { get; set; } = 140f;
    public float GraphLineThickness { get; set; } = 2f;
    public Vector4 GraphDpsColor { get; set; } = new(0.9f, 0.4f, 0.4f, 1f);
    public Vector4 GraphHpsColor { get; set; } = new(0.4f, 0.85f, 0.4f, 1f);
    public Vector4 GraphDtpsColor { get; set; } = new(0.4f, 0.55f, 0.9f, 1f);
    public Vector4 GraphBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 1.0f);
    public Vector4 GraphGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.3f);
    public bool GraphShowLegend { get; set; } = true;
    public bool GraphShowGrid { get; set; } = true;
    public bool GraphShowXAxisLabels { get; set; } = false;
    public bool GraphShowYAxisLabels { get; set; } = true;
    public bool GraphShowDps { get; set; } = true;
    public bool GraphShowHps { get; set; } = true;
    public bool GraphShowDtps { get; set; } = true;
    public float GraphSmoothingWindow { get; set; } = 5f;
    public float GraphUpdateInterval { get; set; } = 0.25f;
    public bool GraphShowLabels { get; set; } = true;
    public float GraphLabelOffsetX { get; set; } = 18f;
    public float GraphLabelOffsetY { get; set; } = 0f;
    public float GraphMouseTextOpacity { get; set; } = 0.6f;
    public float GraphYAxisHeadroom { get; set; } = 1.1f;
    public int GraphYAxisTickCount { get; set; } = 8;
    public float GraphXAxisPadding { get; set; } = 1.25f;
    public bool GraphXAxisMinSec { get; set; } = false;
    public bool GraphAutoScroll { get; set; } = false;
    public float GraphAutoScrollWindow { get; set; } = 60f;
    public float GraphAutoScrollSmoothing { get; set; } = 8f;
    public float GraphFontSize { get; set; } = 14f;

    public bool GraphViewAutoHeight { get; set; } = false;
    public float GraphViewHeight { get; set; } = 260f;
    public float GraphViewLineThickness { get; set; } = 2f;
    public Vector4 GraphViewBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.6f);
    public Vector4 GraphViewGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.3f);
    public float GraphViewSmoothingWindow { get; set; } = 5f;
    public float GraphViewUpdateInterval { get; set; } = 0.25f;
    public bool GraphViewShowLegend { get; set; } = true;
    public bool GraphViewShowGrid { get; set; } = true;
    public bool GraphViewShowXAxisLabels { get; set; } = false;
    public bool GraphViewShowYAxisLabels { get; set; } = true;
    public bool GraphViewHighlightSelf { get; set; } = true;
    public float GraphViewSelfLineThickness { get; set; } = 3.5f;
    public bool GraphViewShowLabels { get; set; } = true;
    public float GraphViewLabelOffsetX { get; set; } = 21f;
    public float GraphViewLabelOffsetY { get; set; } = 0f;
    public float GraphViewFontSize { get; set; } = 14f;
    public float GraphViewXAxisPadding { get; set; } = 1.18f;
    public bool GraphViewXAxisMinSec { get; set; } = false;
    public bool GraphViewAutoScroll { get; set; } = true;
    public float GraphViewAutoScrollWindow { get; set; } = 15f;
    public float GraphViewAutoScrollSmoothing { get; set; } = 8f;
    public float GraphViewYAxisHeadroom { get; set; } = 1.0f;
    public int GraphViewYAxisTickCount { get; set; } = 14;
    public float GraphViewMouseTextOpacity { get; set; } = 0.6f;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<MetricType, SkillMarkerConfig> GraphViewMarkers { get; set; } = new()
    {
        [MetricType.Dps] = new SkillMarkerConfig(),
        [MetricType.Hps] = new SkillMarkerConfig(),
        [MetricType.Dtps] = new SkillMarkerConfig(),
    };

    public Vector4 SkillDamageFillColor { get; set; } = new(0.35f, 0.35f, 0.55f, 0.7f);
    public Vector4 SkillPhysicalFillColor { get; set; } = new(0.55f, 0.30f, 0.25f, 0.7f);
    public Vector4 SkillMagicFillColor { get; set; } = new(0.30f, 0.30f, 0.65f, 0.7f);
    public Vector4 SkillHealingFillColor { get; set; } = new(0.25f, 0.50f, 0.30f, 0.7f);
    public Vector4 SkillRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 SkillTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 SkillHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    public float SkillRowHeight { get; set; } = 14f;
    public float SkillColumnPadding { get; set; } = 6f;
    public float SkillBarRounding { get; set; } = 0f;
    public float SkillFontSize { get; set; } = FontDefaults.BaseSizePt;

    public Vector4 BuffFillColor { get; set; } = new(0.30f, 0.50f, 0.60f, 0.7f);
    public Vector4 DebuffFillColor { get; set; } = new(0.60f, 0.30f, 0.30f, 0.7f);
    public Vector4 BuffRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 BuffTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 BuffHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public float BuffRowHeight { get; set; } = 14f;
    public float BuffColumnPadding { get; set; } = 6f;
    public float BuffBarRounding { get; set; } = 0f;
    public float BuffFontSize { get; set; } = FontDefaults.BaseSizePt;

    public bool ShowStatusBar { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<BarColumn> StatusBarMetrics { get; set; } = new() { BarColumn.Dps, BarColumn.EncDps };
    public float StatusBarFontSize { get; set; } = 14.1f;
    public float StatusBarHeight { get; set; } = 20f;
    public float StatusBarPadding { get; set; } = 4f;
    public bool ShowStatusBarSeparator { get; set; } = true;
    public Vector4 StatusBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);
    public Vector4 StatusBarBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.9f);
    public Vector4 StatusBarActiveColor { get; set; } = new(1.0f, 0.6f, 0.0f, 1.0f);
    public Vector4 StatusBarInactiveColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<Guid> PopoutTabIds { get; set; } = new();
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<Guid, PopoutWindowPin> PopoutWindowPins { get; set; } = new();

    public bool PinMainWindow { get; set; } = false;
    public Vector2 MainWindowPos { get; set; } = new Vector2(100, 100);
    public Vector2 MainWindowSize { get; set; } = new Vector2(350, 400);
    public bool PinConfigWindow { get; set; } = false;
    public Vector2 ConfigWindowPos { get; set; } = new Vector2(100, 100);
    public Vector2 ConfigWindowSize { get; set; } = new Vector2(400, 350);
    public float ConfigSidebarWidth { get; set; } = 170f;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public HashSet<LogChannel> DisabledLogChannels { get; set; } = new();

    [JsonIgnore]
    public float BaseFontSizePt => EnableCustomFont && CustomFontSizePt > 0 ? CustomFontSizePt : FontDefaults.BaseSizePt;

    public float GetFontScale(float desiredSizePt) => desiredSizePt / BaseFontSizePt;

    public void ResetGraph()
    {
        GraphHeight = 120f;
        GraphLineThickness = 2f;
        GraphDpsColor = new Vector4(0.9f, 0.4f, 0.4f, 1f);
        GraphHpsColor = new Vector4(0.4f, 0.85f, 0.4f, 1f);
        GraphDtpsColor = new Vector4(0.4f, 0.55f, 0.9f, 1f);
        GraphBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
        GraphGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
        GraphShowLegend = true;
        GraphShowGrid = true;
        GraphShowXAxisLabels = true;
        GraphShowYAxisLabels = true;
        GraphShowDps = true;
        GraphShowHps = true;
        GraphSmoothingWindow = 5f;
        GraphUpdateInterval = 0.25f;
        GraphShowDtps = true;
        GraphShowLabels = true;
        GraphLabelOffsetX = 8f;
        GraphLabelOffsetY = 0f;
        GraphAutoScroll = false;
        GraphAutoScrollWindow = 60f;
        GraphAutoScrollSmoothing = 8f;
        GraphXAxisPadding = 1.25f;
        GraphYAxisHeadroom = 1.1f;
        GraphYAxisTickCount = 8;
        GraphMouseTextOpacity = 0.6f;
        GraphFontSize = 14f;
    }

    public void ResetGraphView()
    {
        GraphViewAutoHeight = true;
        GraphViewHeight = 300f;
        GraphViewLineThickness = 2f;
        GraphViewSmoothingWindow = 5f;
        GraphViewUpdateInterval = 0.25f;
        GraphViewBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
        GraphViewGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
        GraphViewShowLegend = true;
        GraphViewShowGrid = true;
        GraphViewShowXAxisLabels = true;
        GraphViewShowYAxisLabels = true;
        GraphViewHighlightSelf = true;
        GraphViewSelfLineThickness = 3.5f;
        GraphViewShowLabels = true;
        GraphViewLabelOffsetX = 8f;
        GraphViewLabelOffsetY = 0f;
        GraphViewFontSize = 14f;
        GraphViewAutoScroll = false;
        GraphViewAutoScrollWindow = 60f;
        GraphViewAutoScrollSmoothing = 8f;
        GraphViewXAxisPadding = 1.25f;
        GraphViewYAxisHeadroom = 1.1f;
        GraphViewYAxisTickCount = 8;
        GraphViewMouseTextOpacity = 0.6f;
        GraphViewMarkers[MetricType.Dps] = new SkillMarkerConfig();
        GraphViewMarkers[MetricType.Hps] = new SkillMarkerConfig();
        GraphViewMarkers[MetricType.Dtps] = new SkillMarkerConfig();
    }
}
