using Dalamud.Configuration;
using Newtonsoft.Json;

namespace DamageTerror;

file static class FontDefaults
{
    public const float BaseSizePt = 14f;
}

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public string WebSocketUrl { get; set; } = "ws://127.0.0.1:10501/ws";
    public bool PreferIpc { get; set; } = true;

    public bool ShowOnStart { get; set; } = true;
    public int MaxEncounterHistory { get; set; } = 30;

    // Duty Type Filters
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

    public SortField SortBy { get; set; } = SortField.EncDps;
    public bool SortDescending { get; set; } = true;
    public bool ShowJobIcons { get; set; } = true;
    public JobIconStyle JobIconStyle { get; set; } = JobIconStyle.Framed;
    public Dictionary<string, uint> CustomJobIcons { get; set; } = new();


    // Meter Tabs
    public bool ShowTabBar { get; set; } = false;
    public List<MeterTab> MeterTabs { get; set; } = new();

    // Meter Tab Button Appearance
    public Vector4 TabButtonColor { get; set; } = new(0.20f, 0.22f, 0.27f, 1.0f);
    public Vector4 TabButtonHoveredColor { get; set; } = new(0.28f, 0.30f, 0.36f, 1.0f);
    public Vector4 TabButtonActiveColor { get; set; } = new(0.38f, 0.44f, 0.64f, 1.0f);
    public Vector4 TabButtonTextColor { get; set; } = new(0.85f, 0.85f, 0.85f, 1.0f);
    public Vector4 TabButtonActiveTextColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float TabButtonHeight { get; set; } = 24f;
    public float TabButtonSpacing { get; set; } = 2f;
    public float TabButtonRounding { get; set; } = 4f;
    public float TabButtonWidth { get; set; } = 80f;
    public float TabButtonFontSize { get; set; } = FontDefaults.BaseSizePt;
    public bool TabButtonStretchToFit { get; set; } = true;

    // Layout Order
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<LayoutElement> Layout { get; set; } = new()
    {
        LayoutElement.EncounterSelect,
        LayoutElement.MeterTabs,
        LayoutElement.StatusBar,
        LayoutElement.CombatantBars,
    };

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public HashSet<LayoutElement> CtrlShiftOnlyElements { get; set; } = new();

    public bool HideOutOfCombat { get; set; } = false;
    public float HideOutOfCombatDelay { get; set; } = 5f;
    public bool IgnoreEscClose { get; set; } = true;
    public bool HideWindowHeader { get; set; } = false;
    public float BarAlpha { get; set; } = 0.7f;

    // Role Colors

    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 LimitBreakColor { get; set; } = new(1.0f, 0.5f, 0.0f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    public bool UsePerJobColors { get; set; } = false;
    public Dictionary<string, Vector4> JobColors { get; set; } = new();
    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public float WindowRounding { get; set; } = 0f;

    // Selection Bar
    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; } = 0.0f;
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // Header Row
    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float HeaderHeight { get; set; } = 22.0f;
    public float HeaderFontSize { get; set; } = FontDefaults.BaseSizePt;
    public bool HeaderSeparator { get; set; } = false;
    public Vector4 HeaderSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // Font Settings
    public bool EnableCustomFont { get; set; } = false;
    public string? CustomFontPath { get; set; }
    public int CustomFontIndex { get; set; }
    public float CustomFontSizePt { get; set; } = 14f;
    public string? CustomFontDisplayName { get; set; }
    public string? CustomFontSpecJson { get; set; }

    // Bar Appearance

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 16.0f;
    public float BarFontSize { get; set; } = FontDefaults.BaseSizePt;
    public float BarLeftPadding { get; set; } = 4.0f;
    public float BarRightPadding { get; set; } = 6.0f;
    public float BarColumnSpacing { get; set; } = 6.0f;
    public float IconTextPadding { get; set; } = 4.0f;

    // Self Highlighting
    public bool SelfBarHighlight { get; set; } = false;
    public Vector4 SelfBarHighlightColor { get; set; } = new(1.0f, 0.85f, 0.3f, 0.9f);
    public bool UseSelfNameColor { get; set; } = false;

    public Vector4 SelfNameColor { get; set; } = new(1.0f, 0.9f, 0.4f, 1.0f);

    public ValueDisplayFormat ValueDisplayFormat { get; set; } = ValueDisplayFormat.Abbreviated;

    // Bar Info
    public bool ShowMeterHeader { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowYouOnBar { get; set; } = true;
    public NameDisplayFormat SelfNameFormat { get; set; } = NameDisplayFormat.FullName;
    public NameDisplayFormat OthersNameFormat { get; set; } = NameDisplayFormat.FullName;
    public bool ShowDpsOnBar { get; set; } = true;
    public bool ShowHpsOnBar { get; set; } = false;
    public bool ShowDamageOnBar { get; set; } = false;
    public bool ShowHealedOnBar { get; set; } = false;
    public bool ShowDamagePercentOnBar { get; set; } = false;
    public bool ShowJobAbbrevOnBar { get; set; } = false;
    public bool ShowRankNumber { get; set; } = false;
    public bool ShowDirectHitOnBar { get; set; } = false;
    public bool ShowCritOnBar { get; set; } = false;
    public bool ShowCritDirectHitOnBar { get; set; } = false;
    public bool ShowDeathsOnBar { get; set; } = false;

    // Column Order (left-to-right rendering)
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

    // Custom column header labels
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<BarColumn, string> ColumnHeaderLabels { get; set; } = new();

    public static readonly Dictionary<BarColumn, string> DefaultHeaderLabels = new()
    {
        { BarColumn.Dps, "DPS" },
        { BarColumn.Hps, "HPS" },
        { BarColumn.Damage, "Dmg" },
        { BarColumn.Healed, "Heal" },
        { BarColumn.DamagePercent, "%" },
        { BarColumn.DirectHit, "!" },
        { BarColumn.Crit, "!!" },
        { BarColumn.CritDirectHit, "!!!" },
        { BarColumn.Deaths, "D" },
    };

    public string GetHeaderLabel(BarColumn col)
    {
        if (ColumnHeaderLabels.TryGetValue(col, out var custom) && !string.IsNullOrEmpty(custom))
            return custom;
        return DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
    }

    // Detail Panel
    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public Vector4 DetailDeathColor { get; set; } = new(1f, 0.3f, 0.3f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = FontDefaults.BaseSizePt;
    public bool DetailShowDamage { get; set; } = true;
    public bool DetailShowCritDhStats { get; set; } = true;
    public bool DetailShowDeaths { get; set; } = true;
    public bool DetailShowOverheal { get; set; } = true;
    public bool DetailShowMaxHit { get; set; } = true;
    public bool DetailShowDpsTrend { get; set; } = true;
    public bool DetailShowSkillBreakdown { get; set; } = true;
    public int MaxSkillBreakdownCount { get; set; } = 0;

    // Skill Breakdown Colors
    public Vector4 SkillDamageFillColor { get; set; } = new(0.35f, 0.35f, 0.55f, 0.7f);
    public Vector4 SkillPhysicalFillColor { get; set; } = new(0.55f, 0.30f, 0.25f, 0.7f);
    public Vector4 SkillMagicFillColor { get; set; } = new(0.30f, 0.30f, 0.65f, 0.7f);
    public Vector4 SkillHealingFillColor { get; set; } = new(0.25f, 0.50f, 0.30f, 0.7f);
    public Vector4 SkillRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 SkillTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 SkillHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    // Skill Breakdown Appearance
    public float SkillRowHeight { get; set; } = 14f;
    public float SkillColumnPadding { get; set; } = 6f;
    public float SkillBarRounding { get; set; } = 0f;
    public float SkillFontSize { get; set; } = FontDefaults.BaseSizePt;

    // Status Bar
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
    public bool ShowStatusBarPersonalDps { get; set; } = true;
    public bool ShowStatusBarRaidDps { get; set; } = true;
    public float StatusBarFontSize { get; set; } = FontDefaults.BaseSizePt;
    public float StatusBarHeight { get; set; } = 20f;
    public float StatusBarPadding { get; set; } = 6f;
    public bool ShowStatusBarSeparator { get; set; } = true;
    public Vector4 StatusBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);
    public Vector4 StatusBarBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.9f);
    public Vector4 StatusBarActiveColor { get; set; } = new(1.0f, 0.6f, 0.0f, 1.0f);
    public Vector4 StatusBarInactiveColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    // Window Pinning
    public bool PinMainWindow { get; set; } = false;
    public Vector2 MainWindowPos { get; set; } = new Vector2(100, 100);
    public Vector2 MainWindowSize { get; set; } = new Vector2(350, 400);
    public bool PinConfigWindow { get; set; } = false;
    public Vector2 ConfigWindowPos { get; set; } = new Vector2(100, 100);
    public Vector2 ConfigWindowSize { get; set; } = new Vector2(400, 350);

    // Helpers
    [JsonIgnore]
    public float BaseFontSizePt => EnableCustomFont && CustomFontSizePt > 0 ? CustomFontSizePt : FontDefaults.BaseSizePt;

    public float GetFontScale(float desiredSizePt) => desiredSizePt / BaseFontSizePt;
}
