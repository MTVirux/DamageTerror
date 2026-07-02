
namespace DamageTerror.Models;

public sealed class ThemePreset
{
    public string Name { get; set; } = "Untitled";
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsBuiltIn { get; set; }

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 20.0f;
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
    public int PercentDecimalPlaces { get; set; } = 1;
    public double AbbreviatedKThreshold { get; set; } = 10_000;
    public double AbbreviatedMThreshold { get; set; } = 1_000_000;

    public bool UsePerJobColors { get; set; } = true;
    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 LimitBreakColor { get; set; } = new(1.0f, 0.80f, 0.0f, 1.0f);
    public Vector4 DoHLColor { get; set; } = new(0.70f, 0.55f, 0.30f, 1.0f);
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
    public float WindowPaddingLeft { get; set; } = 0f;
    public float WindowPaddingRight { get; set; } = 0f;
    public float WindowPaddingTop { get; set; } = 0f;
    public float WindowPaddingBottom { get; set; } = 0f;

    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; }
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    public bool ShowMeterHeader { get; set; } = true;
    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 1.0f);
    public float HeaderHeight { get; set; } = 24.0f;
    public float HeaderFontSize { get; set; } = 14f;
    public bool HeaderSeparator { get; set; } = true;
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
    // Must match Configuration.StatusBarFontSize (single source of defaults; drift-asserted in DEBUG).
    public float StatusBarFontSize { get; set; } = 14.1f;
    public float StatusBarPadding { get; set; } = 4f;
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

    public Vector4 BuffFillColor { get; set; } = new(0.30f, 0.50f, 0.60f, 0.7f);
    public Vector4 DebuffFillColor { get; set; } = new(0.60f, 0.30f, 0.30f, 0.7f);
    public Vector4 BuffRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 BuffTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 BuffHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public float BuffRowHeight { get; set; } = 14f;
    public float BuffColumnPadding { get; set; } = 6f;
    public float BuffBarRounding { get; set; } = 0f;
    public float BuffFontSize { get; set; } = 14f;

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
    public bool GraphAutoScroll { get; set; } = false;
    public float GraphAutoScrollWindow { get; set; } = 60f;
    public float GraphAutoScrollSmoothing { get; set; } = 8f;
    public float GraphFontSize { get; set; } = 14f;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<MetricType, SkillMarkerConfig> DetailMarkers { get; set; } = new()
    {
        [MetricType.Dps] = new SkillMarkerConfig(),
        [MetricType.Hps] = new SkillMarkerConfig(),
        [MetricType.Dtps] = new SkillMarkerConfig(),
    };

    // Legacy-JSON migration shims: old preset files with flat DetailDpsMarkers etc.
    // keys route through these private setters into DetailMarkers above.
    [JsonProperty("DetailDpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? DetailDpsMarkersLegacy
    {
        get => null;
        set => DetailMarkers[MetricType.Dps] = value ?? new SkillMarkerConfig();
    }

    [JsonProperty("DetailHpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? DetailHpsMarkersLegacy
    {
        get => null;
        set => DetailMarkers[MetricType.Hps] = value ?? new SkillMarkerConfig();
    }

    [JsonProperty("DetailDtpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? DetailDtpsMarkersLegacy
    {
        get => null;
        set => DetailMarkers[MetricType.Dtps] = value ?? new SkillMarkerConfig();
    }

    public bool GraphViewAutoHeight { get; set; } = false;
    public float GraphViewHeight { get; set; } = 260f;
    public float GraphViewLineThickness { get; set; } = 2f;
    public Vector4 GraphViewBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.6f);
    public Vector4 GraphViewGridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.3f);
    public bool GraphViewShowLegend { get; set; } = true;
    public bool GraphViewShowGrid { get; set; } = true;
    public bool GraphViewShowXAxisLabels { get; set; } = false;
    public bool GraphViewShowYAxisLabels { get; set; } = true;
    public bool GraphViewHighlightSelf { get; set; } = true;
    public float GraphViewSelfLineThickness { get; set; } = 3.5f;
    public float GraphViewSmoothingWindow { get; set; } = 5f;
    public float GraphViewUpdateInterval { get; set; } = 0.25f;
    public bool GraphViewShowLabels { get; set; } = true;
    public float GraphViewLabelOffsetX { get; set; } = 21f;
    public float GraphViewLabelOffsetY { get; set; } = 0f;
    public float GraphViewFontSize { get; set; } = 14f;
    public float GraphViewXAxisPadding { get; set; } = 1.18f;
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

    [JsonProperty("GraphViewDpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? GraphViewDpsMarkersLegacy
    {
        get => null;
        set => GraphViewMarkers[MetricType.Dps] = value ?? new SkillMarkerConfig();
    }

    [JsonProperty("GraphViewHpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? GraphViewHpsMarkersLegacy
    {
        get => null;
        set => GraphViewMarkers[MetricType.Hps] = value ?? new SkillMarkerConfig();
    }

    [JsonProperty("GraphViewDtpsMarkers", NullValueHandling = NullValueHandling.Ignore,
                  DefaultValueHandling = DefaultValueHandling.Ignore)]
    private SkillMarkerConfig? GraphViewDtpsMarkersLegacy
    {
        get => null;
        set => GraphViewMarkers[MetricType.Dtps] = value ?? new SkillMarkerConfig();
    }

    public bool ShowJobIcons { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowJobAbbrevOnBar { get; set; } = true;
    public bool ShowRankNumber { get; set; }

    public Vector4 DetailBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.80f);
    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontSize { get; set; } = 14f;

    public bool ShowTooltip { get; set; } = true;
    public float TooltipDelay { get; set; } = 0.3f;

    public Vector4 TooltipBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.95f);
    public Vector4 TooltipTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 TooltipLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 1f);
    public float TooltipFontSize { get; set; } = 14f;
    public float TooltipRounding { get; set; } = 4f;
    public float TooltipPadding { get; set; } = 6f;

    public bool ShowTabBar { get; set; } = true;
    public Vector4 TabButtonColor { get; set; } = new(0.18f, 0.18f, 0.18f, 1.0f);
    public Vector4 TabButtonHoveredColor { get; set; } = new(0.28f, 0.31f, 0.36f, 0.22f);
    public Vector4 TabButtonActiveColor { get; set; } = new(0.64f, 0.19f, 0.19f, 1.0f);
    public Vector4 TabButtonTextColor { get; set; } = new(0.85f, 0.85f, 0.85f, 1.0f);
    public Vector4 TabButtonActiveTextColor { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);
    public float TabButtonHeight { get; set; } = 19f;
    public float TabButtonSpacing { get; set; } = 6f;
    public float TabButtonRounding { get; set; } = 4f;
    public float TabButtonFontSize { get; set; } = 14f;
    public float TabButtonWidth { get; set; } = 84f;
    public bool TabButtonStretchToFit { get; set; } = true;

    public void ApplyTo(Configuration config)
        => ThemePropertyMirror.ApplyTo(this, config);

    public static ThemePreset CreateFromConfig(Configuration config, string name, string description = "")
    {
        var preset = new ThemePreset { Name = name, Description = description };
        ThemePropertyMirror.CaptureFrom(preset, config);
        return preset;
    }
}
