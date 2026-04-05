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
    public Vector4 LimitBreakColor { get; set; } = new(1.0f, 0.5f, 0.0f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);


    public Dictionary<string, Vector4>? JobColors { get; set; }

    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public string? BackgroundImagePath { get; set; }
    public float BackgroundImageOpacity { get; set; } = 1.0f;
    public Vector4 BackgroundImageTint { get; set; } = new(1f, 1f, 1f, 1f);
    public BackgroundImageScaleMode BackgroundImageScale { get; set; } = BackgroundImageScaleMode.Stretch;
    public float WindowPaddingLeft { get; set; } = 8f;
    public float WindowPaddingRight { get; set; } = 8f;
    public float WindowPaddingTop { get; set; } = 8f;
    public float WindowPaddingBottom { get; set; } = 8f;

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
    public string? CustomFontSpecJson { get; set; }

    public bool ShowStatusBar { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
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

    // Buff/Debuff styling
    public Vector4 BuffFillColor { get; set; } = new(0.30f, 0.50f, 0.60f, 0.7f);
    public Vector4 DebuffFillColor { get; set; } = new(0.60f, 0.30f, 0.30f, 0.7f);
    public Vector4 BuffRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 BuffTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 BuffHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public float BuffRowHeight { get; set; } = 14f;
    public float BuffColumnPadding { get; set; } = 6f;
    public float BuffBarRounding { get; set; } = 0f;
    public float BuffFontSize { get; set; } = 14f;

    // Detail inline graph
    public float GraphHeight { get; set; } = 120f;
    public float GraphLineThickness { get; set; } = 2f;
    public Vector4 GraphDpsColor { get; set; } = new(0.9f, 0.4f, 0.4f, 1f);
    public Vector4 GraphHpsColor { get; set; } = new(0.4f, 0.85f, 0.4f, 1f);
    public Vector4 GraphDtpsColor { get; set; } = new(0.4f, 0.55f, 0.9f, 1f);
    public Vector4 GraphBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.6f);
    public Vector4 GraphGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.3f);
    public bool GraphShowLegend { get; set; } = true;
    public bool GraphShowGrid { get; set; } = true;
    public bool GraphShowXAxisLabels { get; set; } = true;
    public bool GraphShowYAxisLabels { get; set; } = true;
    public bool GraphShowDps { get; set; } = true;
    public bool GraphShowHps { get; set; } = true;
    public bool GraphShowDtps { get; set; } = true;
    public float GraphSmoothingWindow { get; set; } = 5f;
    public float GraphUpdateInterval { get; set; } = 0.25f;
    public bool GraphShowLabels { get; set; } = true;
    public float GraphLabelOffsetX { get; set; } = 8f;
    public float GraphLabelOffsetY { get; set; } = 0f;
    public float GraphMouseTextOpacity { get; set; } = 0.6f;
    public float GraphYAxisHeadroom { get; set; } = 1.1f;
    public int GraphYAxisTickCount { get; set; } = 8;
    public float GraphXAxisPadding { get; set; } = 1.25f;
    public bool GraphAutoScroll { get; set; } = false;
    public float GraphAutoScrollWindow { get; set; } = 60f;
    public float GraphAutoScrollSmoothing { get; set; } = 8f;
    public float GraphFontSize { get; set; } = 14f;

    public SkillMarkerConfig DetailDpsMarkers { get; set; } = new();
    public SkillMarkerConfig DetailHpsMarkers { get; set; } = new();
    public SkillMarkerConfig DetailDtpsMarkers { get; set; } = new();

    // Graph View (main window graph mode)
    public bool GraphViewAutoHeight { get; set; } = true;
    public float GraphViewHeight { get; set; } = 300f;
    public float GraphViewLineThickness { get; set; } = 2f;
    public Vector4 GraphViewBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.6f);
    public Vector4 GraphViewGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.3f);
    public bool GraphViewShowLegend { get; set; } = true;
    public bool GraphViewShowGrid { get; set; } = true;
    public bool GraphViewShowXAxisLabels { get; set; } = true;
    public bool GraphViewShowYAxisLabels { get; set; } = true;
    public bool GraphViewHighlightSelf { get; set; } = true;
    public float GraphViewSelfLineThickness { get; set; } = 3.5f;
    public float GraphViewSmoothingWindow { get; set; } = 5f;
    public float GraphViewUpdateInterval { get; set; } = 0.25f;
    public bool GraphViewShowLabels { get; set; } = true;
    public float GraphViewLabelOffsetX { get; set; } = 8f;
    public float GraphViewLabelOffsetY { get; set; } = 0f;
    public float GraphViewFontSize { get; set; } = 14f;
    public float GraphViewXAxisPadding { get; set; } = 1.25f;
    public bool GraphViewAutoScroll { get; set; } = false;
    public float GraphViewAutoScrollWindow { get; set; } = 60f;
    public float GraphViewAutoScrollSmoothing { get; set; } = 8f;
    public float GraphViewYAxisHeadroom { get; set; } = 1.1f;
    public int GraphViewYAxisTickCount { get; set; } = 8;
    public float GraphViewMouseTextOpacity { get; set; } = 0.6f;

    public SkillMarkerConfig GraphViewDpsMarkers { get; set; } = new();
    public SkillMarkerConfig GraphViewHpsMarkers { get; set; } = new();
    public SkillMarkerConfig GraphViewDtpsMarkers { get; set; } = new();

    public bool ShowJobIcons { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowJobAbbrevOnBar { get; set; }
    public bool ShowRankNumber { get; set; }

    public Vector4 DetailBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.6f);
    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public Vector4 DetailDeathColor { get; set; } = new(1f, 0.3f, 0.3f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = 14f;

    // Tooltip
    public bool ShowTooltip { get; set; } = true;
    public float TooltipDelay { get; set; } = 0.3f;

    public Vector4 TooltipBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.95f);
    public Vector4 TooltipTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 TooltipLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 1f);
    public float TooltipFontSize { get; set; } = 14f;
    public float TooltipRounding { get; set; } = 4f;
    public float TooltipPadding { get; set; } = 6f;

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
    public float TabButtonWidth { get; set; } = 80f;
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
        config.LimitBreakColor = LimitBreakColor;
        config.DefaultJobColor = DefaultJobColor;

        config.JobColors = JobColors != null ? new Dictionary<string, Vector4>(JobColors) : new();

        config.BarBackgroundColor = BarBackgroundColor;
        config.NameTextColor = NameTextColor;
        config.ValueTextColor = ValueTextColor;
        config.WindowBackgroundColor = WindowBackgroundColor;
        config.BackgroundImagePath = BackgroundImagePath;
        config.BackgroundImageOpacity = BackgroundImageOpacity;
        config.BackgroundImageTint = BackgroundImageTint;
        config.BackgroundImageScale = BackgroundImageScale;
        config.WindowPaddingLeft = WindowPaddingLeft;
        config.WindowPaddingRight = WindowPaddingRight;
        config.WindowPaddingTop = WindowPaddingTop;
        config.WindowPaddingBottom = WindowPaddingBottom;

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
        config.CustomFontSpecJson = CustomFontSpecJson;

        config.ShowStatusBar = ShowStatusBar;
        config.ShowStatusBarTimer = ShowStatusBarTimer;
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

        config.BuffFillColor = BuffFillColor;
        config.DebuffFillColor = DebuffFillColor;
        config.BuffRowBackgroundColor = BuffRowBackgroundColor;
        config.BuffTextColor = BuffTextColor;
        config.BuffHeaderTextColor = BuffHeaderTextColor;
        config.BuffRowHeight = BuffRowHeight;
        config.BuffColumnPadding = BuffColumnPadding;
        config.BuffBarRounding = BuffBarRounding;
        config.BuffFontSize = BuffFontSize;

        // Detail inline graph
        config.GraphHeight = GraphHeight;
        config.GraphLineThickness = GraphLineThickness;
        config.GraphDpsColor = GraphDpsColor;
        config.GraphHpsColor = GraphHpsColor;
        config.GraphDtpsColor = GraphDtpsColor;
        config.GraphBackgroundColor = GraphBackgroundColor;
        config.GraphGridColor = GraphGridColor;
        config.GraphShowLegend = GraphShowLegend;
        config.GraphShowGrid = GraphShowGrid;
        config.GraphShowXAxisLabels = GraphShowXAxisLabels;
        config.GraphShowYAxisLabels = GraphShowYAxisLabels;
        config.GraphShowDps = GraphShowDps;
        config.GraphShowHps = GraphShowHps;
        config.GraphShowDtps = GraphShowDtps;
        config.GraphSmoothingWindow = GraphSmoothingWindow;
        config.GraphUpdateInterval = GraphUpdateInterval;
        config.GraphShowLabels = GraphShowLabels;
        config.GraphLabelOffsetX = GraphLabelOffsetX;
        config.GraphLabelOffsetY = GraphLabelOffsetY;
        config.GraphMouseTextOpacity = GraphMouseTextOpacity;
        config.GraphYAxisHeadroom = GraphYAxisHeadroom;
        config.GraphYAxisTickCount = GraphYAxisTickCount;
        config.GraphXAxisPadding = GraphXAxisPadding;
        config.GraphAutoScroll = GraphAutoScroll;
        config.GraphAutoScrollWindow = GraphAutoScrollWindow;
        config.GraphAutoScrollSmoothing = GraphAutoScrollSmoothing;
        config.GraphFontSize = GraphFontSize;

        config.DetailDpsMarkers = DetailDpsMarkers.Clone();
        config.DetailHpsMarkers = DetailHpsMarkers.Clone();
        config.DetailDtpsMarkers = DetailDtpsMarkers.Clone();

        // Graph View
        config.GraphViewAutoHeight = GraphViewAutoHeight;
        config.GraphViewHeight = GraphViewHeight;
        config.GraphViewLineThickness = GraphViewLineThickness;
        config.GraphViewBackgroundColor = GraphViewBackgroundColor;
        config.GraphViewGridColor = GraphViewGridColor;
        config.GraphViewSmoothingWindow = GraphViewSmoothingWindow;
        config.GraphViewUpdateInterval = GraphViewUpdateInterval;
        config.GraphViewShowLegend = GraphViewShowLegend;
        config.GraphViewShowGrid = GraphViewShowGrid;
        config.GraphViewShowXAxisLabels = GraphViewShowXAxisLabels;
        config.GraphViewShowYAxisLabels = GraphViewShowYAxisLabels;
        config.GraphViewHighlightSelf = GraphViewHighlightSelf;
        config.GraphViewSelfLineThickness = GraphViewSelfLineThickness;
        config.GraphViewShowLabels = GraphViewShowLabels;
        config.GraphViewLabelOffsetX = GraphViewLabelOffsetX;
        config.GraphViewLabelOffsetY = GraphViewLabelOffsetY;
        config.GraphViewFontSize = GraphViewFontSize;
        config.GraphViewXAxisPadding = GraphViewXAxisPadding;
        config.GraphViewAutoScroll = GraphViewAutoScroll;
        config.GraphViewAutoScrollWindow = GraphViewAutoScrollWindow;
        config.GraphViewAutoScrollSmoothing = GraphViewAutoScrollSmoothing;
        config.GraphViewYAxisHeadroom = GraphViewYAxisHeadroom;
        config.GraphViewYAxisTickCount = GraphViewYAxisTickCount;
        config.GraphViewMouseTextOpacity = GraphViewMouseTextOpacity;

        config.GraphViewDpsMarkers = GraphViewDpsMarkers.Clone();
        config.GraphViewHpsMarkers = GraphViewHpsMarkers.Clone();
        config.GraphViewDtpsMarkers = GraphViewDtpsMarkers.Clone();

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
        config.TabButtonWidth = TabButtonWidth;
        config.TabButtonStretchToFit = TabButtonStretchToFit;

        config.DetailBackgroundColor = DetailBackgroundColor;
        config.DetailLabelColor = DetailLabelColor;
        config.DetailDeathColor = DetailDeathColor;
        config.DetailIndent = DetailIndent;
        config.DetailFontSize = DetailFontSize;

        // Tooltip
        config.ShowTooltip = ShowTooltip;
        config.TooltipDelay = TooltipDelay;
        config.TooltipBackgroundColor = TooltipBackgroundColor;
        config.TooltipTextColor = TooltipTextColor;
        config.TooltipLabelColor = TooltipLabelColor;
        config.TooltipFontSize = TooltipFontSize;
        config.TooltipRounding = TooltipRounding;
        config.TooltipPadding = TooltipPadding;
    }

    public static ThemePreset CreateFromConfig(Configuration config, string name, string description = "")
    {
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
            LimitBreakColor = config.LimitBreakColor,
            DefaultJobColor = config.DefaultJobColor,

            JobColors = config.JobColors.Count > 0
                ? new Dictionary<string, Vector4>(config.JobColors)
                : null,

            BarBackgroundColor = config.BarBackgroundColor,
            NameTextColor = config.NameTextColor,
            ValueTextColor = config.ValueTextColor,
            WindowBackgroundColor = config.WindowBackgroundColor,
            BackgroundImagePath = config.BackgroundImagePath,
            BackgroundImageOpacity = config.BackgroundImageOpacity,
            BackgroundImageTint = config.BackgroundImageTint,
            BackgroundImageScale = config.BackgroundImageScale,
            WindowPaddingLeft = config.WindowPaddingLeft,
            WindowPaddingRight = config.WindowPaddingRight,
            WindowPaddingTop = config.WindowPaddingTop,
            WindowPaddingBottom = config.WindowPaddingBottom,

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
            CustomFontSpecJson = config.CustomFontSpecJson,

            ShowStatusBar = config.ShowStatusBar,
            ShowStatusBarTimer = config.ShowStatusBarTimer,
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

            BuffFillColor = config.BuffFillColor,
            DebuffFillColor = config.DebuffFillColor,
            BuffRowBackgroundColor = config.BuffRowBackgroundColor,
            BuffTextColor = config.BuffTextColor,
            BuffHeaderTextColor = config.BuffHeaderTextColor,
            BuffRowHeight = config.BuffRowHeight,
            BuffColumnPadding = config.BuffColumnPadding,
            BuffBarRounding = config.BuffBarRounding,
            BuffFontSize = config.BuffFontSize,

            // Detail inline graph
            GraphHeight = config.GraphHeight,
            GraphLineThickness = config.GraphLineThickness,
            GraphDpsColor = config.GraphDpsColor,
            GraphHpsColor = config.GraphHpsColor,
            GraphDtpsColor = config.GraphDtpsColor,
            GraphBackgroundColor = config.GraphBackgroundColor,
            GraphGridColor = config.GraphGridColor,
            GraphShowLegend = config.GraphShowLegend,
            GraphShowGrid = config.GraphShowGrid,
            GraphShowXAxisLabels = config.GraphShowXAxisLabels,
            GraphShowYAxisLabels = config.GraphShowYAxisLabels,
            GraphShowDps = config.GraphShowDps,
            GraphShowHps = config.GraphShowHps,
            GraphShowDtps = config.GraphShowDtps,
            GraphSmoothingWindow = config.GraphSmoothingWindow,
            GraphUpdateInterval = config.GraphUpdateInterval,
            GraphShowLabels = config.GraphShowLabels,
            GraphLabelOffsetX = config.GraphLabelOffsetX,
            GraphLabelOffsetY = config.GraphLabelOffsetY,
            GraphMouseTextOpacity = config.GraphMouseTextOpacity,
            GraphYAxisHeadroom = config.GraphYAxisHeadroom,
            GraphYAxisTickCount = config.GraphYAxisTickCount,
            GraphXAxisPadding = config.GraphXAxisPadding,
            GraphAutoScroll = config.GraphAutoScroll,
            GraphAutoScrollWindow = config.GraphAutoScrollWindow,
            GraphAutoScrollSmoothing = config.GraphAutoScrollSmoothing,
            GraphFontSize = config.GraphFontSize,

            DetailDpsMarkers = config.DetailDpsMarkers.Clone(),
            DetailHpsMarkers = config.DetailHpsMarkers.Clone(),
            DetailDtpsMarkers = config.DetailDtpsMarkers.Clone(),

            // Graph View
            GraphViewAutoHeight = config.GraphViewAutoHeight,
            GraphViewHeight = config.GraphViewHeight,
            GraphViewLineThickness = config.GraphViewLineThickness,
            GraphViewBackgroundColor = config.GraphViewBackgroundColor,
            GraphViewGridColor = config.GraphViewGridColor,
            GraphViewShowLegend = config.GraphViewShowLegend,
            GraphViewShowGrid = config.GraphViewShowGrid,
            GraphViewShowXAxisLabels = config.GraphViewShowXAxisLabels,
            GraphViewShowYAxisLabels = config.GraphViewShowYAxisLabels,
            GraphViewHighlightSelf = config.GraphViewHighlightSelf,
            GraphViewSelfLineThickness = config.GraphViewSelfLineThickness,
            GraphViewSmoothingWindow = config.GraphViewSmoothingWindow,
            GraphViewUpdateInterval = config.GraphViewUpdateInterval,
            GraphViewShowLabels = config.GraphViewShowLabels,
            GraphViewLabelOffsetX = config.GraphViewLabelOffsetX,
            GraphViewLabelOffsetY = config.GraphViewLabelOffsetY,
            GraphViewFontSize = config.GraphViewFontSize,
            GraphViewXAxisPadding = config.GraphViewXAxisPadding,
            GraphViewAutoScroll = config.GraphViewAutoScroll,
            GraphViewAutoScrollWindow = config.GraphViewAutoScrollWindow,
            GraphViewAutoScrollSmoothing = config.GraphViewAutoScrollSmoothing,
            GraphViewYAxisHeadroom = config.GraphViewYAxisHeadroom,
            GraphViewYAxisTickCount = config.GraphViewYAxisTickCount,
            GraphViewMouseTextOpacity = config.GraphViewMouseTextOpacity,

            GraphViewDpsMarkers = config.GraphViewDpsMarkers.Clone(),
            GraphViewHpsMarkers = config.GraphViewHpsMarkers.Clone(),
            GraphViewDtpsMarkers = config.GraphViewDtpsMarkers.Clone(),

            ShowJobIcons = config.ShowJobIcons,
            ShowNameOnBar = config.ShowNameOnBar,
            ShowJobAbbrevOnBar = config.ShowJobAbbrevOnBar,
            ShowRankNumber = config.ShowRankNumber,

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
            TabButtonWidth = config.TabButtonWidth,
            TabButtonStretchToFit = config.TabButtonStretchToFit,

            DetailBackgroundColor = config.DetailBackgroundColor,
            DetailLabelColor = config.DetailLabelColor,
            DetailDeathColor = config.DetailDeathColor,
            DetailIndent = config.DetailIndent,
            DetailFontSize = config.DetailFontSize,

            // Tooltip
            ShowTooltip = config.ShowTooltip,
            TooltipDelay = config.TooltipDelay,
            TooltipBackgroundColor = config.TooltipBackgroundColor,
            TooltipTextColor = config.TooltipTextColor,
            TooltipLabelColor = config.TooltipLabelColor,
            TooltipFontSize = config.TooltipFontSize,
            TooltipRounding = config.TooltipRounding,
            TooltipPadding = config.TooltipPadding,
        };
    }
}
