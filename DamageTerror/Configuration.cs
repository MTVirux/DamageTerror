using Dalamud.Configuration;
using Newtonsoft.Json;

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
        new MeterTab("DPS", TabFilterMode.All, SortField.EncDps, true),
        new MeterTab("Healing", TabFilterMode.All, SortField.EncHps, true)
        {
            VisibleColumns = new() { BarColumn.Hps },
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
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
    public List<LayoutElement> Layout { get; set; } = new()
    {
        LayoutElement.EncounterSelect,
        LayoutElement.MeterTabs,
        LayoutElement.StatusBar,
        LayoutElement.CombatantBars,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
    public HashSet<LayoutElement> CtrlShiftOnlyElements { get; set; } = new();

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

    public bool IgnoreEscClose { get; set; } = true;
    public bool HideWindowHeader { get; set; } = true;
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

    public static readonly Dictionary<BarColumn, string> DefaultHeaderLabels = new()
    {
        { BarColumn.Dps, "DPS" },
        { BarColumn.Hps, "HPS" },
        { BarColumn.Damage, "Dmg" },
        { BarColumn.Healed, "Heal" },
        { BarColumn.DamagePercent, "D%" },
        { BarColumn.HealPercent, "H%" },
        { BarColumn.DirectHit, "!" },
        { BarColumn.Crit, "!!" },
        { BarColumn.CritDirectHit, "!!!" },
        { BarColumn.Deaths, "D" },
        { BarColumn.DamageTaken, "Taken" },
        { BarColumn.DamageTakenPercent, "T%" },
        { BarColumn.Overheal, "OH%" },
        { BarColumn.OverhealAmount, "OH" },
        { BarColumn.MaxHit, "Max" },
        { BarColumn.MaxHitValue, "MaxV" },
        { BarColumn.PeakDps, "Peak" },
        { BarColumn.MaxHeal, "MH" },
        { BarColumn.MaxHealValue, "MHV" },
        { BarColumn.Swings, "Sw" },
        { BarColumn.Hits, "Hits" },
        { BarColumn.Misses, "Miss" },
        { BarColumn.HitRate, "Acc" },
        { BarColumn.CritHitCount, "C#" },
        { BarColumn.DirectHitCount, "D#" },
        { BarColumn.CritDirectHitCount, "CD#" },
        { BarColumn.BlockPct, "Blk" },
        { BarColumn.ParryPct, "Par" },
        { BarColumn.HealsTaken, "HT" },
        { BarColumn.AbsorbHeal, "Abs" },
        { BarColumn.Kills, "K" },
        { BarColumn.InstantDps, "iDPS" },
        { BarColumn.InstantHps, "iHPS" },
        { BarColumn.CritHealPct, "CH%" },
        { BarColumn.HealCount, "HC" },
        { BarColumn.CombatantDuration, "Dur" },
        { BarColumn.DamageShield, "Shld" },
        { BarColumn.MaxHealWard, "MHW" },
        { BarColumn.PowerDrain, "MPD" },
        { BarColumn.PowerHeal, "MPR" },
        { BarColumn.LegsSweeped, "LS" },
        { BarColumn.SkillIssue, "SI" },
        { BarColumn.DamageDown, "DD" },
        { BarColumn.Positionals, "Pos" },
        { BarColumn.PositionalHits, "PosH" },
        { BarColumn.PositionalMisses, "PosM" },
        { BarColumn.PositionalPct, "Pos%" },
        { BarColumn.EncDps, "eDPS" },
        { BarColumn.EncHps, "eHPS" },
        { BarColumn.DpsRank, "Rank" },
        { BarColumn.HpsRank, "Rank" },
        { BarColumn.GroupDps, "gΣDPS" },
        { BarColumn.GroupHps, "gΣHPS" },
        { BarColumn.GroupDamage, "gΣDmg" },
        { BarColumn.GroupHealed, "gΣHeal" },
        { BarColumn.GroupDamageTaken, "gΣTkn" },
        { BarColumn.GroupDeaths, "gΣD" },
        { BarColumn.GroupOverheal, "gΣOH" },
        { BarColumn.GroupSkillIssue, "gΣSI" },
        { BarColumn.GroupDamageDown, "gΣDD" },
        { BarColumn.GroupInstantDps, "gΣiDPS" },
        { BarColumn.GroupInstantHps, "gΣiHPS" },
        { BarColumn.GroupAvgDps, "gx̄DPS" },
        { BarColumn.GroupAvgHps, "gx̄HPS" },
        { BarColumn.GroupAvgCrit, "gx̄!!" },
        { BarColumn.GroupAvgDirectHit, "gx̄!" },
        { BarColumn.GroupAvgCritDirectHit, "gx̄!!!" },
        { BarColumn.GroupAvgOverhealPct, "gx̄OH%" },
        { BarColumn.GroupAvgCritHealPct, "gx̄CH%" },
        { BarColumn.GroupAvgHitRate, "gx̄Acc" },
        { BarColumn.GroupPeakDps, "gPeak" },
        { BarColumn.GroupMaxHitValue, "gMaxV" },
        { BarColumn.GroupMaxHealValue, "gMHV" },
    };

    public static readonly Dictionary<TooltipField, string> DefaultTooltipFieldLabels = new()
    {
        { TooltipField.Name, "Name" },
        { TooltipField.Job, "Job" },
        { TooltipField.Dps, "DPS" },
        { TooltipField.Hps, "HPS" },
        { TooltipField.Damage, "Damage" },
        { TooltipField.Healed, "Healed" },
        { TooltipField.DamagePercent, "Damage %" },
        { TooltipField.HealPercent, "Heal %" },
        { TooltipField.Crit, "Crit %" },
        { TooltipField.DirectHit, "Direct Hit %" },
        { TooltipField.CritDirectHit, "Crit DH %" },
        { TooltipField.Deaths, "Deaths" },
        { TooltipField.DamageTaken, "Damage Taken" },
        { TooltipField.Overheal, "Overheal %" },
        { TooltipField.OverhealAmount, "Overheal" },
        { TooltipField.MaxHit, "Max Hit" },
        { TooltipField.MaxHitValue, "Max Hit Value" },
        { TooltipField.MaxHeal, "Max Heal" },
        { TooltipField.MaxHealValue, "Max Heal Value" },
        { TooltipField.PeakDps, "Peak DPS" },
        { TooltipField.Swings, "Swings" },
        { TooltipField.Hits, "Hits" },
        { TooltipField.Misses, "Misses" },
        { TooltipField.HitRate, "Hit Rate" },
        { TooltipField.Kills, "Kills" },
        { TooltipField.CombatantDuration, "Duration" },
        { TooltipField.HealsTaken, "Heals Taken" },
        { TooltipField.InstantDps, "Instant DPS" },
        { TooltipField.InstantHps, "Instant HPS" },
        { TooltipField.CritHealPct, "Crit Heal %" },
        { TooltipField.HealCount, "Heal Count" },
        { TooltipField.DamageShield, "Damage Shield" },
        { TooltipField.MaxHealWard, "Max Heal Ward" },
        { TooltipField.LegsSweeped, "Legs Sweeped" },
        { TooltipField.SkillIssue, "Skill Issue" },
        { TooltipField.DamageDown, "Damage Down" },
        { TooltipField.Positionals, "Positionals" },
        { TooltipField.PositionalHits, "Positional Hits" },
        { TooltipField.PositionalMisses, "Positional Misses" },
        { TooltipField.PositionalPct, "Positional %" },
        { TooltipField.EncDps, "Encounter DPS" },
        { TooltipField.EncHps, "Encounter HPS" },
        { TooltipField.TopDamageSkills, "Top Damage Skills" },
        { TooltipField.TopHealingSkills, "Top Healing Skills" },
    };

    public static readonly Dictionary<BarColumn, string> DefaultDetailColumnLabels = new()
    {
        { BarColumn.Dps, "DPS" },
        { BarColumn.InstantDps, "iDPS" },
        { BarColumn.PeakDps, "Peak" },
        { BarColumn.Damage, "Total" },
        { BarColumn.DamagePercent, "Dmg %" },
        { BarColumn.MaxHit, "Max Hit" },
        { BarColumn.MaxHitValue, "Max Hit Value" },
        { BarColumn.DamageShield, "Shield" },
        { BarColumn.EncDps, "Encounter DPS" },
        { BarColumn.Hps, "HPS" },
        { BarColumn.InstantHps, "iHPS" },
        { BarColumn.Healed, "Total" },
        { BarColumn.HealPercent, "Heal %" },
        { BarColumn.Overheal, "Overheal" },
        { BarColumn.OverhealAmount, "OH Amt" },
        { BarColumn.CritHealPct, "Crit Heal" },
        { BarColumn.MaxHeal, "Max Heal" },
        { BarColumn.MaxHealValue, "Max Heal Value" },
        { BarColumn.HealCount, "Heals" },
        { BarColumn.EncHps, "Encounter HPS" },
        { BarColumn.Crit, "Crit" },
        { BarColumn.DirectHit, "DH" },
        { BarColumn.CritDirectHit, "CDH" },
        { BarColumn.CritHitCount, "Crit#" },
        { BarColumn.DirectHitCount, "DH#" },
        { BarColumn.CritDirectHitCount, "CDH#" },
        { BarColumn.HitRate, "Hit Rate" },
        { BarColumn.Swings, "Swings" },
        { BarColumn.Hits, "Hits" },
        { BarColumn.Misses, "Misses" },
        { BarColumn.DamageTaken, "Taken" },
        { BarColumn.DamageTakenPercent, "Taken %" },
        { BarColumn.BlockPct, "Block" },
        { BarColumn.ParryPct, "Parry" },
        { BarColumn.HealsTaken, "Heals Taken" },
        { BarColumn.Deaths, "Deaths" },
        { BarColumn.Kills, "Kills" },
        { BarColumn.CombatantDuration, "Duration" },
        { BarColumn.PowerHeal, "MP Recovery" },
        { BarColumn.PowerDrain, "MP Drain" },
        { BarColumn.AbsorbHeal, "Absorb" },
        { BarColumn.MaxHealWard, "Max Ward" },
        { BarColumn.LegsSweeped, "LS" },
        { BarColumn.SkillIssue, "Skill Issue" },
        { BarColumn.DamageDown, "Damage Down" },
        { BarColumn.Positionals, "Positionals" },
        { BarColumn.PositionalHits, "Positional Hits" },
        { BarColumn.PositionalMisses, "Positional Misses" },
        { BarColumn.PositionalPct, "Positional %" },
    };

    public static readonly Dictionary<BarColumn, string> FullColumnNames = new()
    {
        { BarColumn.Dps, "Damage Per Second" },
        { BarColumn.Hps, "Healing Per Second" },
        { BarColumn.Damage, "Total Damage" },
        { BarColumn.Healed, "Total Healed" },
        { BarColumn.DamagePercent, "Damage %" },
        { BarColumn.HealPercent, "Healing %" },
        { BarColumn.DirectHit, "Direct Hit %" },
        { BarColumn.Crit, "Critical Hit %" },
        { BarColumn.CritDirectHit, "Critical Direct Hit %" },
        { BarColumn.Deaths, "Deaths" },
        { BarColumn.DamageTaken, "Damage Taken" },
        { BarColumn.DamageTakenPercent, "Damage Taken %" },
        { BarColumn.Overheal, "Overheal %" },
        { BarColumn.OverhealAmount, "Overheal Amount" },
        { BarColumn.MaxHit, "Max Hit" },
        { BarColumn.MaxHitValue, "Max Hit Value" },
        { BarColumn.PeakDps, "Peak DPS" },
        { BarColumn.MaxHeal, "Max Heal" },
        { BarColumn.MaxHealValue, "Max Heal Value" },
        { BarColumn.Swings, "Swings" },
        { BarColumn.Hits, "Hits" },
        { BarColumn.Misses, "Misses" },
        { BarColumn.HitRate, "Hit Rate / Accuracy" },
        { BarColumn.CritHitCount, "Critical Hit Count" },
        { BarColumn.DirectHitCount, "Direct Hit Count" },
        { BarColumn.CritDirectHitCount, "Critical Direct Hit Count" },
        { BarColumn.BlockPct, "Block %" },
        { BarColumn.ParryPct, "Parry %" },
        { BarColumn.HealsTaken, "Heals Taken" },
        { BarColumn.AbsorbHeal, "Absorb / Shield Heal" },
        { BarColumn.Kills, "Kills" },
        { BarColumn.InstantDps, "Instant DPS" },
        { BarColumn.InstantHps, "Instant HPS" },
        { BarColumn.CritHealPct, "Critical Heal %" },
        { BarColumn.HealCount, "Heal Count" },
        { BarColumn.CombatantDuration, "Combatant Duration" },
        { BarColumn.DamageShield, "Damage Shield" },
        { BarColumn.MaxHealWard, "Max Heal Ward" },
        { BarColumn.PowerDrain, "MP Drain" },
        { BarColumn.PowerHeal, "MP Restore" },
        { BarColumn.LegsSweeped, "Legs Sweeped" },
        { BarColumn.SkillIssue, "Skill Issue" },
        { BarColumn.DamageDown, "Damage Down" },
        { BarColumn.Positionals, "Positionals" },
        { BarColumn.PositionalHits, "Positional Hits" },
        { BarColumn.PositionalMisses, "Positional Misses" },
        { BarColumn.PositionalPct, "Positional %" },
        { BarColumn.EncDps, "Encounter DPS" },
        { BarColumn.EncHps, "Encounter HPS" },
        { BarColumn.DpsRank, "DPS Rank" },
        { BarColumn.HpsRank, "HPS Rank" },
    };

    public Vector4 DetailBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.80f);
    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = FontDefaults.BaseSizePt;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
    public HashSet<BarColumn> DetailVisibleColumns { get; set; } = new(Enum.GetValues<BarColumn>());
    public bool DetailShowDetailsTab { get; set; } = true;
    public bool DetailShowSkillsTab { get; set; } = true;
    public bool DetailShowGraphTab { get; set; } = true;
    public bool DetailShowBuffsTab { get; set; } = true;
    public bool DetailShowItemTab { get; set; } = true;
    public bool DetailShowSkillBreakdown { get; set; } = true;
    public int MaxSkillBreakdownCount { get; set; } = 0;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
    public HashSet<BarColumn> DetailNewLineColumns { get; set; } = new();
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<string> DetailExpandedSections { get; set; } = new();

    public SkillMarkerConfig DetailDpsMarkers { get; set; } = new();
    public SkillMarkerConfig DetailHpsMarkers { get; set; } = new();
    public SkillMarkerConfig DetailDtpsMarkers { get; set; } = new();

    public bool ShowTooltip { get; set; } = true;
    public float TooltipDelay { get; set; } = 0.3f;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
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

    public SkillMarkerConfig GraphViewDpsMarkers { get; set; } = new();
    public SkillMarkerConfig GraphViewHpsMarkers { get; set; } = new();
    public SkillMarkerConfig GraphViewDtpsMarkers { get; set; } = new();

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
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
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

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumCollectionConverter))]
    public HashSet<LogChannel> DisabledLogChannels { get; set; } = new();

    [JsonIgnore]
    public float BaseFontSizePt => EnableCustomFont && CustomFontSizePt > 0 ? CustomFontSizePt : FontDefaults.BaseSizePt;

    public float GetFontScale(float desiredSizePt) => desiredSizePt / BaseFontSizePt;
}
