using Newtonsoft.Json;

namespace DamageTerror.Models;

public class ThemePreset
{
    public string Name { get; set; } = "Untitled";
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsBuiltIn { get; set; }

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 16.0f;
    public float BarAlpha { get; set; } = 0.7f;
    public float BarFontSize { get; set; } = 14f;
    public float BarLeftPadding { get; set; } = 4.0f;
    public float BarRightPadding { get; set; } = 6.0f;
    public float BarColumnSpacing { get; set; } = 6.0f;
    public float IconTextPadding { get; set; } = 4.0f;

    public bool SelfBarHighlight { get; set; }
    public Vector4 SelfBarHighlightColor { get; set; } = new(1.0f, 0.85f, 0.3f, 0.9f);
    public bool UseSelfNameColor { get; set; }
    public Vector4 SelfNameColor { get; set; } = new(1.0f, 0.9f, 0.4f, 1.0f);

    public ValueDisplayFormat ValueDisplayFormat { get; set; } = ValueDisplayFormat.Abbreviated;
    public int AbbreviatedDecimalPlaces { get; set; } = 1;
    public int RawDecimalPlaces { get; set; } = 1;
    public int PercentDecimalPlaces { get; set; } = 0;
    public double AbbreviatedKThreshold { get; set; } = 10_000;
    public double AbbreviatedMThreshold { get; set; } = 1_000_000;

    public bool UsePerJobColors { get; set; }
    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);


    public Dictionary<string, Vector4>? JobColors { get; set; }

    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public float WindowRounding { get; set; } = 0f;

    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; }
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    public bool ShowMeterHeader { get; set; } = true;
    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float HeaderHeight { get; set; } = 22.0f;
    public float HeaderFontSize { get; set; } = 14f;
    public bool HeaderSeparator { get; set; }
    public Vector4 HeaderSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    public bool EnableCustomFont { get; set; } = false;
    public string? CustomFontPath { get; set; }
    public int CustomFontIndex { get; set; }
    public float CustomFontSizePt { get; set; } = 14f;
    public string? CustomFontDisplayName { get; set; }

    public bool ShowStatusBar { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
    public bool ShowStatusBarPersonalDps { get; set; } = true;
    public bool ShowStatusBarRaidDps { get; set; } = true;
    public float StatusBarHeight { get; set; } = 20f;
    public float StatusBarFontSize { get; set; } = 14f;
    public float StatusBarPadding { get; set; } = 6f;
    public bool ShowStatusBarSeparator { get; set; } = true;
    public Vector4 StatusBarBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.9f);
    public Vector4 StatusBarActiveColor { get; set; } = new(1.0f, 0.6f, 0.0f, 1.0f);
    public Vector4 StatusBarInactiveColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

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
    public float SkillFontSize { get; set; } = 14f;

    public bool ShowJobIcons { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowDpsOnBar { get; set; } = true;
    public bool ShowHpsOnBar { get; set; }
    public bool ShowDamageOnBar { get; set; }
    public bool ShowHealedOnBar { get; set; }
    public bool ShowDamagePercentOnBar { get; set; }
    public bool ShowJobAbbrevOnBar { get; set; }
    public bool ShowRankNumber { get; set; }
    public bool ShowDirectHitOnBar { get; set; }
    public bool ShowCritOnBar { get; set; }
    public bool ShowCritDirectHitOnBar { get; set; }
    public bool ShowDeathsOnBar { get; set; }
    public bool ShowDamageTakenOnBar { get; set; }
    public bool ShowOverhealOnBar { get; set; }

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

    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public Vector4 DetailDeathColor { get; set; } = new(1f, 0.3f, 0.3f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = 14f;

    // Tooltip
    public bool ShowTooltip { get; set; } = true;
    public float TooltipDelay { get; set; } = 0.3f;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
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

    public Vector4 TooltipBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.95f);
    public Vector4 TooltipTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 TooltipLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 1f);
    public float TooltipFontSize { get; set; } = 14f;
    public float TooltipRounding { get; set; } = 4f;
    public float TooltipPadding { get; set; } = 6f;

    // Tab Definitions — when non-null, applying this preset replaces config tabs entirely
    public List<MeterTab>? Tabs { get; set; }

    // Tab Button Styling
    public bool ShowTabBar { get; set; } = true;
    public Vector4 TabButtonColor { get; set; } = new(0.20f, 0.22f, 0.27f, 1.0f);
    public Vector4 TabButtonHoveredColor { get; set; } = new(0.28f, 0.30f, 0.36f, 1.0f);
    public Vector4 TabButtonActiveColor { get; set; } = new(0.38f, 0.44f, 0.64f, 1.0f);
    public Vector4 TabButtonTextColor { get; set; } = new(0.85f, 0.85f, 0.85f, 1.0f);
    public Vector4 TabButtonActiveTextColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float TabButtonHeight { get; set; } = 24f;
    public float TabButtonSpacing { get; set; } = 2f;
    public float TabButtonRounding { get; set; } = 4f;
    public float TabButtonFontSize { get; set; } = 14f;
    public bool TabButtonStretchToFit { get; set; } = true;

    public void ApplyTo(Configuration config)
    {
        config.BarHeight = BarHeight;
        config.BarSpacing = BarSpacing;
        config.BarRounding = BarRounding;
        config.IconSize = IconSize;
        config.BarAlpha = BarAlpha;
        config.BarFontSize = BarFontSize;
        config.BarLeftPadding = BarLeftPadding;
        config.BarRightPadding = BarRightPadding;
        config.BarColumnSpacing = BarColumnSpacing;
        config.IconTextPadding = IconTextPadding;

        config.SelfBarHighlight = SelfBarHighlight;
        config.SelfBarHighlightColor = SelfBarHighlightColor;
        config.UseSelfNameColor = UseSelfNameColor;
        config.SelfNameColor = SelfNameColor;

        config.ValueDisplayFormat = ValueDisplayFormat;
        config.AbbreviatedDecimalPlaces = AbbreviatedDecimalPlaces;
        config.RawDecimalPlaces = RawDecimalPlaces;
        config.PercentDecimalPlaces = PercentDecimalPlaces;
        config.AbbreviatedKThreshold = AbbreviatedKThreshold;
        config.AbbreviatedMThreshold = AbbreviatedMThreshold;

        config.UsePerJobColors = UsePerJobColors;
        config.TankColor = TankColor;
        config.HealerColor = HealerColor;
        config.MeleeDpsColor = MeleeDpsColor;
        config.RangedDpsColor = RangedDpsColor;
        config.CasterDpsColor = CasterDpsColor;
        config.DefaultJobColor = DefaultJobColor;

        config.JobColors = JobColors != null ? new Dictionary<string, Vector4>(JobColors) : new();

        config.BarBackgroundColor = BarBackgroundColor;
        config.NameTextColor = NameTextColor;
        config.ValueTextColor = ValueTextColor;
        config.WindowBackgroundColor = WindowBackgroundColor;
        config.WindowRounding = WindowRounding;

        config.SelectionBarTextColor = SelectionBarTextColor;
        config.SelectionBarBackgroundColor = SelectionBarBackgroundColor;
        config.SelectionBarHeight = SelectionBarHeight;
        config.ShowEncounterPicker = ShowEncounterPicker;
        config.ShowSelectionBarSeparator = ShowSelectionBarSeparator;
        config.SelectionBarSeparatorColor = SelectionBarSeparatorColor;

        config.ShowMeterHeader = ShowMeterHeader;
        config.HeaderTextColor = HeaderTextColor;
        config.HeaderBackgroundColor = HeaderBackgroundColor;
        config.HeaderHeight = HeaderHeight;
        config.HeaderFontSize = HeaderFontSize;
        config.HeaderSeparator = HeaderSeparator;
        config.HeaderSeparatorColor = HeaderSeparatorColor;

        config.EnableCustomFont = EnableCustomFont;
        config.CustomFontPath = CustomFontPath;
        config.CustomFontIndex = CustomFontIndex;
        config.CustomFontSizePt = CustomFontSizePt;
        config.CustomFontDisplayName = CustomFontDisplayName;

        config.ShowStatusBar = ShowStatusBar;
        config.ShowStatusBarTimer = ShowStatusBarTimer;
        config.ShowStatusBarPersonalDps = ShowStatusBarPersonalDps;
        config.ShowStatusBarRaidDps = ShowStatusBarRaidDps;
        config.StatusBarHeight = StatusBarHeight;
        config.StatusBarFontSize = StatusBarFontSize;
        config.StatusBarPadding = StatusBarPadding;
        config.ShowStatusBarSeparator = ShowStatusBarSeparator;
        config.StatusBarBackgroundColor = StatusBarBackgroundColor;
        config.StatusBarActiveColor = StatusBarActiveColor;
        config.StatusBarInactiveColor = StatusBarInactiveColor;
        config.StatusBarLabelColor = StatusBarLabelColor;
        config.StatusBarSeparatorColor = StatusBarSeparatorColor;

        config.SkillDamageFillColor = SkillDamageFillColor;
        config.SkillPhysicalFillColor = SkillPhysicalFillColor;
        config.SkillMagicFillColor = SkillMagicFillColor;
        config.SkillHealingFillColor = SkillHealingFillColor;
        config.SkillRowBackgroundColor = SkillRowBackgroundColor;
        config.SkillTextColor = SkillTextColor;
        config.SkillHeaderTextColor = SkillHeaderTextColor;
        config.SkillRowHeight = SkillRowHeight;
        config.SkillColumnPadding = SkillColumnPadding;
        config.SkillBarRounding = SkillBarRounding;
        config.SkillFontSize = SkillFontSize;

        config.ShowJobIcons = ShowJobIcons;
        config.ShowNameOnBar = ShowNameOnBar;
        config.ShowJobAbbrevOnBar = ShowJobAbbrevOnBar;
        config.ShowRankNumber = ShowRankNumber;

        // Tab button styling
        config.ShowTabBar = ShowTabBar;
        config.TabButtonColor = TabButtonColor;
        config.TabButtonHoveredColor = TabButtonHoveredColor;
        config.TabButtonActiveColor = TabButtonActiveColor;
        config.TabButtonTextColor = TabButtonTextColor;
        config.TabButtonActiveTextColor = TabButtonActiveTextColor;
        config.TabButtonHeight = TabButtonHeight;
        config.TabButtonSpacing = TabButtonSpacing;
        config.TabButtonRounding = TabButtonRounding;
        config.TabButtonFontSize = TabButtonFontSize;
        config.TabButtonStretchToFit = TabButtonStretchToFit;

        // Tab definitions — replace tabs entirely when preset defines them
        if (Tabs is { Count: > 0 })
        {
            config.MeterTabs = Tabs.Select(t => t.Clone()).ToList();
        }
        else
        {
            // Legacy path: apply column visibility from top-level flags to all existing tabs
            foreach (var tab in config.MeterTabs)
            {
                tab.ShowDpsOnBar = ShowDpsOnBar;
                tab.ShowHpsOnBar = ShowHpsOnBar;
                tab.ShowDamageOnBar = ShowDamageOnBar;
                tab.ShowHealedOnBar = ShowHealedOnBar;
                tab.ShowDamagePercentOnBar = ShowDamagePercentOnBar;
                tab.ShowDirectHitOnBar = ShowDirectHitOnBar;
                tab.ShowCritOnBar = ShowCritOnBar;
                tab.ShowCritDirectHitOnBar = ShowCritDirectHitOnBar;
                tab.ShowDeathsOnBar = ShowDeathsOnBar;
                tab.ShowDamageTakenOnBar = ShowDamageTakenOnBar;
                tab.ShowOverhealOnBar = ShowOverhealOnBar;
                tab.ColumnOrder = new List<BarColumn>(ColumnOrder);
            }
        }

        config.DetailLabelColor = DetailLabelColor;
        config.DetailDeathColor = DetailDeathColor;
        config.DetailIndent = DetailIndent;
        config.DetailFontSize = DetailFontSize;

        // Tooltip
        config.ShowTooltip = ShowTooltip;
        config.TooltipDelay = TooltipDelay;
        config.TooltipFields = new List<TooltipField>(TooltipFields);
        config.TooltipBackgroundColor = TooltipBackgroundColor;
        config.TooltipTextColor = TooltipTextColor;
        config.TooltipLabelColor = TooltipLabelColor;
        config.TooltipFontSize = TooltipFontSize;
        config.TooltipRounding = TooltipRounding;
        config.TooltipPadding = TooltipPadding;
    }

    public static ThemePreset CreateFromConfig(Configuration config, string name, string description = "")
    {
        var firstTab = config.MeterTabs.Count > 0 ? config.MeterTabs[0] : null;
        return new ThemePreset
        {
            Name = name,
            Description = description,

            BarHeight = config.BarHeight,
            BarSpacing = config.BarSpacing,
            BarRounding = config.BarRounding,
            IconSize = config.IconSize,
            BarAlpha = config.BarAlpha,
            BarFontSize = config.BarFontSize,
            BarLeftPadding = config.BarLeftPadding,
            BarRightPadding = config.BarRightPadding,
            BarColumnSpacing = config.BarColumnSpacing,
            IconTextPadding = config.IconTextPadding,

            SelfBarHighlight = config.SelfBarHighlight,
            SelfBarHighlightColor = config.SelfBarHighlightColor,
            UseSelfNameColor = config.UseSelfNameColor,
            SelfNameColor = config.SelfNameColor,

            ValueDisplayFormat = config.ValueDisplayFormat,
            AbbreviatedDecimalPlaces = config.AbbreviatedDecimalPlaces,
            RawDecimalPlaces = config.RawDecimalPlaces,
            PercentDecimalPlaces = config.PercentDecimalPlaces,
            AbbreviatedKThreshold = config.AbbreviatedKThreshold,
            AbbreviatedMThreshold = config.AbbreviatedMThreshold,

            UsePerJobColors = config.UsePerJobColors,
            TankColor = config.TankColor,
            HealerColor = config.HealerColor,
            MeleeDpsColor = config.MeleeDpsColor,
            RangedDpsColor = config.RangedDpsColor,
            CasterDpsColor = config.CasterDpsColor,
            DefaultJobColor = config.DefaultJobColor,

            JobColors = config.JobColors.Count > 0
                ? new Dictionary<string, Vector4>(config.JobColors)
                : null,

            BarBackgroundColor = config.BarBackgroundColor,
            NameTextColor = config.NameTextColor,
            ValueTextColor = config.ValueTextColor,
            WindowBackgroundColor = config.WindowBackgroundColor,
            WindowRounding = config.WindowRounding,

            SelectionBarTextColor = config.SelectionBarTextColor,
            SelectionBarBackgroundColor = config.SelectionBarBackgroundColor,
            SelectionBarHeight = config.SelectionBarHeight,
            ShowEncounterPicker = config.ShowEncounterPicker,
            ShowSelectionBarSeparator = config.ShowSelectionBarSeparator,
            SelectionBarSeparatorColor = config.SelectionBarSeparatorColor,

            ShowMeterHeader = config.ShowMeterHeader,
            HeaderTextColor = config.HeaderTextColor,
            HeaderBackgroundColor = config.HeaderBackgroundColor,
            HeaderHeight = config.HeaderHeight,
            HeaderFontSize = config.HeaderFontSize,
            HeaderSeparator = config.HeaderSeparator,
            HeaderSeparatorColor = config.HeaderSeparatorColor,

            EnableCustomFont = config.EnableCustomFont,
            CustomFontPath = config.CustomFontPath,
            CustomFontIndex = config.CustomFontIndex,
            CustomFontSizePt = config.CustomFontSizePt,
            CustomFontDisplayName = config.CustomFontDisplayName,

            ShowStatusBar = config.ShowStatusBar,
            ShowStatusBarTimer = config.ShowStatusBarTimer,
            ShowStatusBarPersonalDps = config.ShowStatusBarPersonalDps,
            ShowStatusBarRaidDps = config.ShowStatusBarRaidDps,
            StatusBarHeight = config.StatusBarHeight,
            StatusBarFontSize = config.StatusBarFontSize,
            StatusBarPadding = config.StatusBarPadding,
            ShowStatusBarSeparator = config.ShowStatusBarSeparator,
            StatusBarBackgroundColor = config.StatusBarBackgroundColor,
            StatusBarActiveColor = config.StatusBarActiveColor,
            StatusBarInactiveColor = config.StatusBarInactiveColor,
            StatusBarLabelColor = config.StatusBarLabelColor,
            StatusBarSeparatorColor = config.StatusBarSeparatorColor,

            SkillDamageFillColor = config.SkillDamageFillColor,
            SkillPhysicalFillColor = config.SkillPhysicalFillColor,
            SkillMagicFillColor = config.SkillMagicFillColor,
            SkillHealingFillColor = config.SkillHealingFillColor,
            SkillRowBackgroundColor = config.SkillRowBackgroundColor,
            SkillTextColor = config.SkillTextColor,
            SkillHeaderTextColor = config.SkillHeaderTextColor,
            SkillRowHeight = config.SkillRowHeight,
            SkillColumnPadding = config.SkillColumnPadding,
            SkillBarRounding = config.SkillBarRounding,
            SkillFontSize = config.SkillFontSize,

            ShowJobIcons = config.ShowJobIcons,
            ShowNameOnBar = config.ShowNameOnBar,
            ShowJobAbbrevOnBar = config.ShowJobAbbrevOnBar,
            ShowRankNumber = config.ShowRankNumber,
            ShowDpsOnBar = firstTab?.ShowDpsOnBar ?? true,
            ShowHpsOnBar = firstTab?.ShowHpsOnBar ?? false,
            ShowDamageOnBar = firstTab?.ShowDamageOnBar ?? false,
            ShowHealedOnBar = firstTab?.ShowHealedOnBar ?? false,
            ShowDamagePercentOnBar = firstTab?.ShowDamagePercentOnBar ?? false,
            ShowDirectHitOnBar = firstTab?.ShowDirectHitOnBar ?? false,
            ShowCritOnBar = firstTab?.ShowCritOnBar ?? false,
            ShowCritDirectHitOnBar = firstTab?.ShowCritDirectHitOnBar ?? false,
            ShowDeathsOnBar = firstTab?.ShowDeathsOnBar ?? false,
            ShowDamageTakenOnBar = firstTab?.ShowDamageTakenOnBar ?? false,
            ShowOverhealOnBar = firstTab?.ShowOverhealOnBar ?? false,
            ColumnOrder = firstTab != null ? new List<BarColumn>(firstTab.ColumnOrder) : new List<BarColumn>(),

            Tabs = config.MeterTabs.Select(t => t.Clone()).ToList(),

            ShowTabBar = config.ShowTabBar,
            TabButtonColor = config.TabButtonColor,
            TabButtonHoveredColor = config.TabButtonHoveredColor,
            TabButtonActiveColor = config.TabButtonActiveColor,
            TabButtonTextColor = config.TabButtonTextColor,
            TabButtonActiveTextColor = config.TabButtonActiveTextColor,
            TabButtonHeight = config.TabButtonHeight,
            TabButtonSpacing = config.TabButtonSpacing,
            TabButtonRounding = config.TabButtonRounding,
            TabButtonFontSize = config.TabButtonFontSize,
            TabButtonStretchToFit = config.TabButtonStretchToFit,

            DetailLabelColor = config.DetailLabelColor,
            DetailDeathColor = config.DetailDeathColor,
            DetailIndent = config.DetailIndent,
            DetailFontSize = config.DetailFontSize,

            // Tooltip
            ShowTooltip = config.ShowTooltip,
            TooltipDelay = config.TooltipDelay,
            TooltipFields = new List<TooltipField>(config.TooltipFields),
            TooltipBackgroundColor = config.TooltipBackgroundColor,
            TooltipTextColor = config.TooltipTextColor,
            TooltipLabelColor = config.TooltipLabelColor,
            TooltipFontSize = config.TooltipFontSize,
            TooltipRounding = config.TooltipRounding,
            TooltipPadding = config.TooltipPadding,
        };
    }
}
