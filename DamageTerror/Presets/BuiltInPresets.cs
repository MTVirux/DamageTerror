namespace DamageTerror.Presets;

public static class BuiltInPresets
{
    public static ThemePreset[] All { get; } = { Default(), Kagerou(), Ember(), Horizoverlay(), MopiMopi(), Ikegami(), NextUI() };

    public static ThemePreset Default() => new()
    {
        Name = "Default",
        Description = "Stock DamageTerror appearance. Use this to reset to factory settings.",
        IsBuiltIn = true,
    };


    public static ThemePreset Kagerou()
    {
        var p = Default();
        p.Name = "Kagerou";
        p.Description = "Classic MiniParse style — sharp bars, dark background, compact layout.";

        // Bar Styling
        p.BarHeight = 20f;
        p.IconSize = 14f;
        p.BarAlpha = 0.75f;
        p.BarFontSize = 12.9f;
        p.BarLeftPadding = 3f;
        p.BarRightPadding = 4f;
        p.BarColumnSpacing = 5f;
        p.IconTextPadding = 3f;

        // Self Highlighting
        p.SelfBarHighlight = true;
        p.SelfBarHighlightColor = new(0.35f, 0.55f, 0.95f, 0.9f);

        // Value Formatting
        p.ValueDisplayFormat = ValueDisplayFormat.Commas;
        p.RawDecimalPlaces = 0;

        // Role Colors
        p.UsePerJobColors = false;
        p.TankColor = new(0.24f, 0.32f, 0.71f, 1.0f);
        p.HealerColor = new(0.30f, 0.64f, 0.31f, 1.0f);
        p.MeleeDpsColor = new(0.90f, 0.22f, 0.21f, 1.0f);
        p.RangedDpsColor = new(1.00f, 0.60f, 0.0f, 1.0f);
        p.CasterDpsColor = new(0.49f, 0.34f, 0.76f, 1.0f);
        p.LimitBreakColor = new(0.90f, 0.75f, 0.10f, 1.0f);
        p.DoHLColor = new(0.65f, 0.50f, 0.28f, 1.0f);
        p.DefaultJobColor = new(0.46f, 0.46f, 0.46f, 1.0f);

        // Background & Text
        p.BarBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.85f);
        p.WindowBackgroundColor = new(0.055f, 0.055f, 0.055f, 0.95f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.85f, 0.85f, 0.85f, 1f);
        p.SelectionBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.6f);
        p.SelectionBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.5f);

        // Header
        p.HeaderTextColor = new(0.6f, 0.6f, 0.6f, 0.8f);
        p.HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        p.HeaderHeight = 18f;
        p.HeaderFontSize = 12.3f;
        p.HeaderSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.4f);

        // Status Bar
        p.StatusBarHeight = 18f;
        p.StatusBarFontSize = 12.6f;
        p.StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.9f);
        p.StatusBarInactiveColor = new(0.5f, 0.5f, 0.5f, 0.8f);
        p.StatusBarLabelColor = new(0.5f, 0.5f, 0.5f, 0.8f);
        p.StatusBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.4f);

        // Skill Breakdown
        p.SkillDamageFillColor = new(0.30f, 0.30f, 0.50f, 0.7f);
        p.SkillPhysicalFillColor = new(0.50f, 0.25f, 0.20f, 0.7f);
        p.SkillMagicFillColor = new(0.25f, 0.25f, 0.55f, 0.7f);
        p.SkillHealingFillColor = new(0.20f, 0.45f, 0.25f, 0.7f);
        p.SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f);
        p.SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.9f);
        p.SkillRowHeight = 13f;
        p.SkillColumnPadding = 5f;
        p.SkillFontSize = 11.9f;

        // Buff/Debuff
        p.BuffFillColor = new(0.25f, 0.40f, 0.55f, 0.7f);
        p.DebuffFillColor = new(0.55f, 0.25f, 0.25f, 0.7f);
        p.BuffRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f);
        p.BuffHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.9f);
        p.BuffRowHeight = 13f;
        p.BuffColumnPadding = 5f;
        p.BuffFontSize = 11.9f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.85f);
        p.DetailLabelColor = new(0.6f, 0.6f, 0.6f, 1f);
        p.DetailIndent = 6f;
        p.DetailFontSize = 12.3f;

        // Display Flags
        p.ShowRankNumber = true;
        p.ShowJobAbbrevOnBar = false;

        // Tab Buttons
        p.TabButtonColor = new(0.10f, 0.10f, 0.10f, 0.85f);
        p.TabButtonHoveredColor = new(0.18f, 0.18f, 0.18f, 0.9f);
        p.TabButtonActiveColor = new(0.24f, 0.32f, 0.71f, 0.9f);
        p.TabButtonTextColor = new(0.6f, 0.6f, 0.6f, 0.8f);
        p.TabButtonHeight = 18f;
        p.TabButtonSpacing = 1f;
        p.TabButtonRounding = 0f;
        p.TabButtonFontSize = 12.3f;
        p.TabButtonWidth = 70f;

        // Tooltip
        p.TooltipBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.95f);
        p.TooltipLabelColor = new(0.55f, 0.55f, 0.55f, 1f);
        p.TooltipFontSize = 12.3f;
        p.TooltipRounding = 0f;
        p.TooltipPadding = 4f;

        // Detail Graph
        p.GraphHeight = 130f;
        p.GraphLineThickness = 1.5f;
        p.GraphDpsColor = new(0.85f, 0.30f, 0.30f, 1f);
        p.GraphHpsColor = new(0.30f, 0.80f, 0.30f, 1f);
        p.GraphDtpsColor = new(0.35f, 0.50f, 0.85f, 1f);
        p.GraphBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.95f);
        p.GraphGridColor = new(0.25f, 0.25f, 0.25f, 0.3f);
        p.GraphLabelOffsetX = 16f;
        p.GraphFontSize = 11.9f;

        // Graph View
        p.GraphViewHeight = 240f;
        p.GraphViewLineThickness = 1.5f;
        p.GraphViewBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.6f);
        p.GraphViewGridColor = new(0.25f, 0.25f, 0.25f, 0.3f);
        p.GraphViewSelfLineThickness = 3f;
        p.GraphViewLabelOffsetX = 18f;
        p.GraphViewFontSize = 11.9f;
        p.GraphViewXAxisPadding = 1.25f;
        p.GraphViewYAxisHeadroom = 1.1f;
        p.GraphViewYAxisTickCount = 8;

        return p;
    }


    public static ThemePreset Ember()
    {
        var p = Default();
        p.Name = "Ember Overlay";
        p.Description = "Modern compact bars with warm tones and slight rounding.";

        // Bar Styling
        p.BarHeight = 20f;
        p.BarSpacing = 2f;
        p.BarRounding = 3f;
        p.IconSize = 14f;
        p.BarAlpha = 0.80f;
        p.BarFontSize = 13.3f;
        p.BarLeftPadding = 5f;

        // Self Highlighting
        p.SelfBarHighlight = true;
        p.SelfBarHighlightColor = new(1.0f, 0.55f, 0.15f, 0.85f);
        p.SelfNameColor = new(1.0f, 0.85f, 0.3f, 1.0f);

        // Role Colors
        p.UsePerJobColors = false;
        p.TankColor = new(0.26f, 0.38f, 0.72f, 1.0f);
        p.HealerColor = new(0.30f, 0.60f, 0.32f, 1.0f);
        p.MeleeDpsColor = new(0.88f, 0.24f, 0.22f, 1.0f);
        p.RangedDpsColor = new(1.00f, 0.55f, 0.15f, 1.0f);
        p.CasterDpsColor = new(0.52f, 0.35f, 0.72f, 1.0f);
        p.LimitBreakColor = new(1.0f, 0.70f, 0.15f, 1.0f);
        p.DoHLColor = new(0.72f, 0.52f, 0.25f, 1.0f);
        p.DefaultJobColor = new(0.48f, 0.48f, 0.48f, 1.0f);

        // Background & Text
        p.BarBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.90f);
        p.ValueTextColor = new(1f, 1f, 1f, 0.95f);
        p.WindowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.92f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.9f, 0.9f, 0.9f, 1f);
        p.SelectionBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.5f);
        p.SelectionBarHeight = 2f;
        p.SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f);

        // Header
        p.HeaderTextColor = new(0.65f, 0.65f, 0.65f, 0.85f);
        p.HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        p.HeaderHeight = 20f;
        p.HeaderFontSize = 12.9f;
        p.HeaderSeparator = false;
        p.HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f);

        // Status Bar
        p.StatusBarFontSize = 13.3f;
        p.StatusBarPadding = 6f;
        p.StatusBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.85f);
        p.StatusBarActiveColor = new(1.0f, 0.55f, 0.10f, 1.0f);
        p.StatusBarInactiveColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.StatusBarLabelColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f);

        // Skill Breakdown
        p.SkillPhysicalFillColor = new(0.55f, 0.28f, 0.18f, 0.7f);
        p.SkillMagicFillColor = new(0.28f, 0.28f, 0.55f, 0.7f);
        p.SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f);
        p.SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.SkillBarRounding = 3f;
        p.SkillFontSize = 12.6f;

        // Buff/Debuff
        p.BuffFillColor = new(0.30f, 0.42f, 0.55f, 0.7f);
        p.DebuffFillColor = new(0.60f, 0.28f, 0.22f, 0.7f);
        p.BuffRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f);
        p.BuffHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.BuffBarRounding = 3f;
        p.BuffFontSize = 12.6f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.85f);
        p.DetailLabelColor = new(0.65f, 0.65f, 0.65f, 1f);
        p.DetailFontSize = 12.9f;

        // Display Flags
        p.ShowRankNumber = true;
        p.ShowJobAbbrevOnBar = false;

        // Tab Buttons
        p.TabButtonColor = new(0.12f, 0.12f, 0.12f, 0.9f);
        p.TabButtonHoveredColor = new(0.20f, 0.18f, 0.15f, 0.95f);
        p.TabButtonActiveColor = new(1.0f, 0.55f, 0.10f, 0.85f);
        p.TabButtonTextColor = new(0.65f, 0.65f, 0.65f, 0.85f);
        p.TabButtonHeight = 20f;
        p.TabButtonSpacing = 2f;
        p.TabButtonRounding = 3f;
        p.TabButtonFontSize = 13.3f;
        p.TabButtonWidth = 80f;

        // Tooltip
        p.TooltipBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.92f);
        p.TooltipLabelColor = new(0.55f, 0.55f, 0.55f, 1f);
        p.TooltipFontSize = 12.9f;
        p.TooltipRounding = 3f;

        // Detail Graph
        p.GraphDpsColor = new(0.95f, 0.45f, 0.20f, 1f);
        p.GraphHpsColor = new(0.35f, 0.80f, 0.35f, 1f);
        p.GraphBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.92f);
        p.GraphFontSize = 12.6f;

        // Graph View
        p.GraphViewFontSize = 12.6f;

        return p;
    }


    public static ThemePreset Horizoverlay()
    {
        var p = Default();
        p.Name = "Horizoverlay";
        p.Description = "Minimal horizontal bars — thin, rounded, highly transparent.";

        // Bar Styling
        p.BarHeight = 16f;
        p.BarRounding = 8f;
        p.IconSize = 12f;
        p.BarAlpha = 0.60f;
        p.BarFontSize = 11.9f;
        p.BarLeftPadding = 6f;
        p.BarColumnSpacing = 4f;
        p.IconTextPadding = 3f;

        // Self Highlighting
        p.SelfBarHighlightColor = new(0.9f, 0.6f, 0.1f, 0.8f);
        p.UseSelfNameColor = true;
        p.SelfNameColor = new(0.95f, 0.85f, 0.5f, 1.0f);

        // Value Formatting
        p.RawDecimalPlaces = 0;
        p.PercentDecimalPlaces = 0;

        // Role Colors
        p.UsePerJobColors = false;
        p.TankColor = new(0.30f, 0.45f, 0.75f, 1.0f);
        p.HealerColor = new(0.30f, 0.65f, 0.35f, 1.0f);
        p.MeleeDpsColor = new(0.75f, 0.25f, 0.25f, 1.0f);
        p.RangedDpsColor = new(0.85f, 0.50f, 0.20f, 1.0f);
        p.CasterDpsColor = new(0.55f, 0.35f, 0.70f, 1.0f);
        p.LimitBreakColor = new(0.90f, 0.75f, 0.20f, 1.0f);
        p.DoHLColor = new(0.68f, 0.48f, 0.22f, 1.0f);
        p.DefaultJobColor = new(0.45f, 0.45f, 0.45f, 1.0f);

        // Background & Text
        p.BarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.50f);
        p.NameTextColor = new(1f, 1f, 1f, 0.90f);
        p.ValueTextColor = new(1f, 1f, 1f, 0.90f);
        p.WindowBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.70f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.8f, 0.8f, 0.8f, 0.9f);
        p.ShowSelectionBarSeparator = false;
        p.SelectionBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f);

        // Header
        p.ShowMeterHeader = false;
        p.HeaderTextColor = new(0.6f, 0.6f, 0.6f, 0.8f);
        p.HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        p.HeaderHeight = 18f;
        p.HeaderFontSize = 11.9f;
        p.HeaderSeparator = false;
        p.HeaderSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f);

        // Status Bar
        p.ShowStatusBar = false;
        p.StatusBarHeight = 18f;
        p.StatusBarFontSize = 12.6f;
        p.ShowStatusBarSeparator = false;
        p.StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.7f);
        p.StatusBarActiveColor = new(0.9f, 0.6f, 0.1f, 1.0f);
        p.StatusBarInactiveColor = new(0.5f, 0.5f, 0.5f, 0.7f);
        p.StatusBarLabelColor = new(0.5f, 0.5f, 0.5f, 0.7f);
        p.StatusBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f);

        // Skill Breakdown
        p.SkillDamageFillColor = new(0.30f, 0.30f, 0.50f, 0.6f);
        p.SkillPhysicalFillColor = new(0.45f, 0.22f, 0.18f, 0.6f);
        p.SkillMagicFillColor = new(0.22f, 0.22f, 0.50f, 0.6f);
        p.SkillHealingFillColor = new(0.20f, 0.45f, 0.25f, 0.6f);
        p.SkillRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.5f);
        p.SkillTextColor = new(1f, 1f, 1f, 0.85f);
        p.SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.SkillRowHeight = 12f;
        p.SkillColumnPadding = 4f;
        p.SkillBarRounding = 6f;
        p.SkillFontSize = 11.2f;

        // Buff/Debuff
        p.BuffFillColor = new(0.25f, 0.38f, 0.50f, 0.6f);
        p.DebuffFillColor = new(0.50f, 0.22f, 0.22f, 0.6f);
        p.BuffRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.5f);
        p.BuffTextColor = new(1f, 1f, 1f, 0.85f);
        p.BuffHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.BuffRowHeight = 12f;
        p.BuffColumnPadding = 4f;
        p.BuffBarRounding = 6f;
        p.BuffFontSize = 11.2f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.75f);
        p.DetailLabelColor = new(0.6f, 0.6f, 0.6f, 1f);
        p.DetailIndent = 6f;
        p.DetailFontSize = 11.5f;

        // Display Flags
        p.ShowJobAbbrevOnBar = false;

        // Tab Buttons
        p.ShowTabBar = false;
        p.TabButtonColor = new(0.08f, 0.08f, 0.08f, 0.5f);
        p.TabButtonHoveredColor = new(0.15f, 0.15f, 0.15f, 0.6f);
        p.TabButtonActiveColor = new(0.9f, 0.6f, 0.1f, 0.7f);
        p.TabButtonTextColor = new(0.6f, 0.6f, 0.6f, 0.7f);
        p.TabButtonActiveTextColor = new(1.0f, 1.0f, 1.0f, 0.9f);
        p.TabButtonHeight = 16f;
        p.TabButtonSpacing = 1f;
        p.TabButtonRounding = 8f;
        p.TabButtonFontSize = 11.9f;
        p.TabButtonWidth = 60f;

        // Tooltip
        p.TooltipDelay = 0.2f;
        p.TooltipBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.85f);
        p.TooltipTextColor = new(1f, 1f, 1f, 0.9f);
        p.TooltipLabelColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.TooltipFontSize = 11.5f;
        p.TooltipRounding = 8f;

        // Detail Graph
        p.GraphHeight = 100f;
        p.GraphLineThickness = 1.5f;
        p.GraphDpsColor = new(0.80f, 0.35f, 0.35f, 0.9f);
        p.GraphHpsColor = new(0.35f, 0.75f, 0.35f, 0.9f);
        p.GraphDtpsColor = new(0.35f, 0.50f, 0.80f, 0.9f);
        p.GraphBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.70f);
        p.GraphGridColor = new(0.25f, 0.25f, 0.25f, 0.2f);
        p.GraphShowLegend = false;
        p.GraphLabelOffsetX = 14f;
        p.GraphMouseTextOpacity = 0.5f;
        p.GraphYAxisTickCount = 6;
        p.GraphFontSize = 11.2f;

        // Graph View
        p.GraphViewAutoHeight = true;
        p.GraphViewHeight = 200f;
        p.GraphViewLineThickness = 1.5f;
        p.GraphViewBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.5f);
        p.GraphViewGridColor = new(0.25f, 0.25f, 0.25f, 0.2f);
        p.GraphViewShowLegend = false;
        p.GraphViewSelfLineThickness = 3f;
        p.GraphViewLabelOffsetX = 16f;
        p.GraphViewFontSize = 11.2f;
        p.GraphViewXAxisPadding = 1.25f;
        p.GraphViewYAxisHeadroom = 1.1f;
        p.GraphViewYAxisTickCount = 6;
        p.GraphViewMouseTextOpacity = 0.5f;

        return p;
    }


    public static ThemePreset MopiMopi()
    {
        var p = Default();
        p.Name = "MopiMopi";
        p.Description = "Colorful rounded bars with unique per-job colors and vibrant styling.";

        // Bar Styling
        p.BarSpacing = 2f;
        p.BarRounding = 6f;
        p.IconSize = 16f;
        p.BarAlpha = 0.85f;
        p.BarLeftPadding = 5f;

        // Self Highlighting
        p.SelfBarHighlight = true;
        p.UseSelfNameColor = true;
        p.SelfNameColor = new(1.0f, 0.95f, 0.6f, 1.0f);

        // Role Colors
        p.TankColor = new(0.25f, 0.40f, 0.80f, 1.0f);
        p.HealerColor = new(0.25f, 0.70f, 0.35f, 1.0f);
        p.MeleeDpsColor = new(0.80f, 0.25f, 0.25f, 1.0f);
        p.RangedDpsColor = new(0.90f, 0.55f, 0.15f, 1.0f);
        p.JobColors = new Dictionary<string, Vector4>
        {
            { "Pld", new(0.45f, 0.60f, 0.95f, 1.0f) },
            { "War", new(0.75f, 0.20f, 0.20f, 1.0f) },
            { "Drk", new(0.55f, 0.20f, 0.65f, 1.0f) },
            { "Gnb", new(0.30f, 0.50f, 0.70f, 1.0f) },
            { "Whm", new(0.90f, 0.90f, 0.75f, 1.0f) },
            { "Sch", new(0.35f, 0.50f, 0.90f, 1.0f) },
            { "Ast", new(0.95f, 0.80f, 0.35f, 1.0f) },
            { "Sge", new(0.40f, 0.70f, 0.80f, 1.0f) },
            { "Mnk", new(0.90f, 0.70f, 0.20f, 1.0f) },
            { "Drg", new(0.30f, 0.45f, 0.90f, 1.0f) },
            { "Nin", new(0.75f, 0.25f, 0.40f, 1.0f) },
            { "Sam", new(0.95f, 0.60f, 0.25f, 1.0f) },
            { "Rpr", new(0.65f, 0.25f, 0.45f, 1.0f) },
            { "Vpr", new(0.50f, 0.75f, 0.35f, 1.0f) },
            { "Brd", new(0.60f, 0.85f, 0.35f, 1.0f) },
            { "Mch", new(0.50f, 0.80f, 0.85f, 1.0f) },
            { "Dnc", new(0.90f, 0.60f, 0.70f, 1.0f) },
            { "Blm", new(0.65f, 0.50f, 0.90f, 1.0f) },
            { "Smn", new(0.35f, 0.75f, 0.45f, 1.0f) },
            { "Rdm", new(0.90f, 0.40f, 0.50f, 1.0f) },
            { "Pct", new(0.80f, 0.60f, 0.85f, 1.0f) },
            { "Blu", new(0.35f, 0.60f, 0.95f, 1.0f) },
        };

        // Background & Text
        p.BarBackgroundColor = new(0.10f, 0.10f, 0.12f, 0.85f);
        p.WindowBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.92f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.9f, 0.9f, 0.9f, 1f);
        p.SelectionBarBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.4f);
        p.SelectionBarHeight = 2f;
        p.SelectionBarSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f);

        // Header
        p.HeaderTextColor = new(0.65f, 0.65f, 0.70f, 0.9f);
        p.HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        p.HeaderHeight = 20f;
        p.HeaderSeparator = false;
        p.HeaderSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f);

        // Status Bar
        p.StatusBarPadding = 6f;
        p.StatusBarBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.9f);
        p.StatusBarActiveColor = new(1.0f, 0.55f, 0.15f, 1.0f);
        p.StatusBarInactiveColor = new(0.55f, 0.55f, 0.60f, 0.9f);
        p.StatusBarLabelColor = new(0.55f, 0.55f, 0.60f, 0.9f);
        p.StatusBarSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f);

        // Skill Breakdown
        p.SkillDamageFillColor = new(0.40f, 0.35f, 0.60f, 0.7f);
        p.SkillPhysicalFillColor = new(0.55f, 0.28f, 0.22f, 0.7f);
        p.SkillMagicFillColor = new(0.28f, 0.28f, 0.58f, 0.7f);
        p.SkillHealingFillColor = new(0.25f, 0.55f, 0.30f, 0.7f);
        p.SkillRowBackgroundColor = new(0.10f, 0.10f, 0.12f, 0.6f);
        p.SkillHeaderTextColor = new(0.60f, 0.60f, 0.65f, 0.9f);
        p.SkillBarRounding = 4f;
        p.SkillFontSize = 12.9f;

        // Buff/Debuff
        p.BuffFillColor = new(0.30f, 0.45f, 0.58f, 0.7f);
        p.DebuffFillColor = new(0.58f, 0.28f, 0.28f, 0.7f);
        p.BuffRowBackgroundColor = new(0.10f, 0.10f, 0.12f, 0.6f);
        p.BuffHeaderTextColor = new(0.60f, 0.60f, 0.65f, 0.9f);
        p.BuffBarRounding = 4f;
        p.BuffFontSize = 12.9f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.85f);
        p.DetailLabelColor = new(0.65f, 0.65f, 0.70f, 1f);
        p.DetailFontSize = 13.3f;

        // Display Flags
        p.ShowRankNumber = true;
        p.ShowJobAbbrevOnBar = false;

        // Tab Buttons
        p.TabButtonColor = new(0.10f, 0.10f, 0.12f, 0.85f);
        p.TabButtonHoveredColor = new(0.20f, 0.20f, 0.25f, 0.9f);
        p.TabButtonActiveColor = new(0.60f, 0.30f, 0.80f, 0.85f);
        p.TabButtonTextColor = new(0.65f, 0.65f, 0.70f, 0.9f);
        p.TabButtonHeight = 22f;
        p.TabButtonSpacing = 2f;
        p.TabButtonRounding = 6f;

        // Tooltip
        p.TooltipBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.95f);
        p.TooltipLabelColor = new(0.55f, 0.55f, 0.60f, 1f);
        p.TooltipFontSize = 12.9f;
        p.TooltipRounding = 6f;

        // Detail Graph
        p.GraphHeight = 145f;
        p.GraphDpsColor = new(0.90f, 0.35f, 0.35f, 1f);
        p.GraphHpsColor = new(0.30f, 0.85f, 0.40f, 1f);
        p.GraphBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.92f);
        p.GraphGridColor = new(0.30f, 0.30f, 0.35f, 0.3f);
        p.GraphFontSize = 12.9f;

        // Graph View
        p.GraphViewHeight = 270f;
        p.GraphViewBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.6f);
        p.GraphViewGridColor = new(0.30f, 0.30f, 0.35f, 0.3f);
        p.GraphViewFontSize = 12.9f;

        return p;
    }


    public static ThemePreset Ikegami()
    {
        var p = Default();
        p.Name = "Ikegami";
        p.Description = "Data-dense table layout — all stats visible, compact rows, sharp edges.";

        // Bar Styling
        p.BarHeight = 18f;
        p.BarSpacing = 0f;
        p.IconSize = 14f;
        p.BarAlpha = 0.50f;
        p.BarFontSize = 12.3f;
        p.BarLeftPadding = 2f;
        p.BarRightPadding = 3f;
        p.BarColumnSpacing = 4f;
        p.IconTextPadding = 3f;

        // Self Highlighting
        p.SelfBarHighlight = true;
        p.SelfBarHighlightColor = new(0.95f, 0.75f, 0.15f, 0.7f);

        // Value Formatting
        p.ValueDisplayFormat = ValueDisplayFormat.Commas;
        p.AbbreviatedDecimalPlaces = 0;
        p.RawDecimalPlaces = 0;
        p.PercentDecimalPlaces = 0;

        // Role Colors
        p.UsePerJobColors = false;
        p.TankColor = new(0.25f, 0.38f, 0.72f, 1.0f);
        p.HealerColor = new(0.28f, 0.62f, 0.32f, 1.0f);
        p.MeleeDpsColor = new(0.72f, 0.24f, 0.24f, 1.0f);
        p.RangedDpsColor = new(0.82f, 0.50f, 0.18f, 1.0f);
        p.CasterDpsColor = new(0.50f, 0.32f, 0.70f, 1.0f);
        p.LimitBreakColor = new(0.90f, 0.72f, 0.10f, 1.0f);
        p.DoHLColor = new(0.66f, 0.50f, 0.26f, 1.0f);
        p.DefaultJobColor = new(0.42f, 0.42f, 0.42f, 1.0f);

        // Background & Text
        p.BarBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.75f);
        p.NameTextColor = new(1f, 1f, 1f, 0.95f);
        p.ValueTextColor = new(1f, 1f, 1f, 0.95f);
        p.WindowBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.95f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.8f, 0.8f, 0.8f, 1f);
        p.SelectionBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.5f);
        p.SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f);

        // Header
        p.HeaderTextColor = new(0.75f, 0.75f, 0.75f, 0.95f);
        p.HeaderBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.7f);
        p.HeaderHeight = 18f;
        p.HeaderFontSize = 11.9f;
        p.HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f);

        // Status Bar
        p.StatusBarHeight = 18f;
        p.StatusBarFontSize = 11.9f;
        p.StatusBarPadding = 3f;
        p.StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.9f);
        p.StatusBarActiveColor = new(0.95f, 0.60f, 0.10f, 1.0f);
        p.StatusBarInactiveColor = new(0.50f, 0.50f, 0.50f, 0.8f);
        p.StatusBarLabelColor = new(0.50f, 0.50f, 0.50f, 0.8f);
        p.StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f);

        // Skill Breakdown
        p.SkillDamageFillColor = new(0.30f, 0.30f, 0.48f, 0.65f);
        p.SkillPhysicalFillColor = new(0.48f, 0.24f, 0.18f, 0.65f);
        p.SkillMagicFillColor = new(0.22f, 0.22f, 0.52f, 0.65f);
        p.SkillHealingFillColor = new(0.22f, 0.45f, 0.28f, 0.65f);
        p.SkillRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.55f);
        p.SkillRowHeight = 13f;
        p.SkillColumnPadding = 5f;
        p.SkillFontSize = 11.5f;

        // Buff/Debuff
        p.BuffFillColor = new(0.25f, 0.38f, 0.52f, 0.65f);
        p.DebuffFillColor = new(0.52f, 0.25f, 0.25f, 0.65f);
        p.BuffRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.55f);
        p.BuffRowHeight = 13f;
        p.BuffColumnPadding = 5f;
        p.BuffFontSize = 11.5f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.85f);
        p.DetailLabelColor = new(0.65f, 0.65f, 0.65f, 1f);
        p.DetailIndent = 4f;
        p.DetailFontSize = 11.9f;

        // Display Flags
        p.ShowRankNumber = true;

        // Tab Buttons
        p.TabButtonColor = new(0.08f, 0.08f, 0.08f, 0.7f);
        p.TabButtonHoveredColor = new(0.15f, 0.15f, 0.15f, 0.8f);
        p.TabButtonActiveColor = new(0.25f, 0.38f, 0.72f, 0.85f);
        p.TabButtonTextColor = new(0.60f, 0.60f, 0.60f, 0.9f);
        p.TabButtonActiveTextColor = new(1.0f, 1.0f, 1.0f, 0.95f);
        p.TabButtonHeight = 18f;
        p.TabButtonSpacing = 0f;
        p.TabButtonRounding = 0f;
        p.TabButtonFontSize = 11.9f;
        p.TabButtonWidth = 65f;

        // Tooltip
        p.TooltipDelay = 0.2f;
        p.TooltipBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.95f);
        p.TooltipTextColor = new(1f, 1f, 1f, 0.95f);
        p.TooltipLabelColor = new(0.60f, 0.60f, 0.60f, 0.9f);
        p.TooltipFontSize = 11.5f;
        p.TooltipRounding = 0f;
        p.TooltipPadding = 4f;

        // Detail Graph
        p.GraphHeight = 120f;
        p.GraphLineThickness = 1.5f;
        p.GraphDpsColor = new(0.80f, 0.30f, 0.30f, 1f);
        p.GraphHpsColor = new(0.30f, 0.75f, 0.30f, 1f);
        p.GraphDtpsColor = new(0.35f, 0.50f, 0.80f, 1f);
        p.GraphBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.95f);
        p.GraphGridColor = new(0.25f, 0.25f, 0.25f, 0.3f);
        p.GraphLabelOffsetX = 15f;
        p.GraphFontSize = 11.5f;

        // Graph View
        p.GraphViewHeight = 230f;
        p.GraphViewLineThickness = 1.5f;
        p.GraphViewBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.6f);
        p.GraphViewGridColor = new(0.25f, 0.25f, 0.25f, 0.3f);
        p.GraphViewSelfLineThickness = 3f;
        p.GraphViewLabelOffsetX = 17f;
        p.GraphViewFontSize = 11.5f;
        p.GraphViewXAxisPadding = 1.25f;
        p.GraphViewYAxisHeadroom = 1.1f;
        p.GraphViewYAxisTickCount = 8;

        return p;
    }


    public static ThemePreset NextUI()
    {
        var p = Default();
        p.Name = "Next UI";
        p.Description = "Game-integrated look — desaturated per-job colors, subtle rounding, HUD-like feel.";

        // Bar Styling
        p.BarRounding = 2f;
        p.IconSize = 16f;
        p.BarAlpha = 0.72f;
        p.BarFontSize = 13.3f;
        p.BarRightPadding = 5f;
        p.BarColumnSpacing = 5f;

        // Self Highlighting
        p.SelfBarHighlight = true;
        p.SelfBarHighlightColor = new(0.7f, 0.55f, 0.25f, 0.75f);
        p.SelfNameColor = new(0.95f, 0.9f, 0.5f, 1.0f);

        // Role Colors
        p.TankColor = new(0.25f, 0.40f, 0.72f, 1.0f);
        p.HealerColor = new(0.28f, 0.60f, 0.35f, 1.0f);
        p.MeleeDpsColor = new(0.70f, 0.25f, 0.25f, 1.0f);
        p.RangedDpsColor = new(0.80f, 0.50f, 0.20f, 1.0f);
        p.CasterDpsColor = new(0.52f, 0.32f, 0.68f, 1.0f);
        p.LimitBreakColor = new(0.80f, 0.68f, 0.15f, 1.0f);
        p.DoHLColor = new(0.62f, 0.48f, 0.24f, 1.0f);
        p.DefaultJobColor = new(0.45f, 0.45f, 0.45f, 1.0f);
        p.JobColors = new Dictionary<string, Vector4>
        {
            { "Pld", new(0.38f, 0.52f, 0.82f, 1.0f) },
            { "War", new(0.55f, 0.22f, 0.22f, 1.0f) },
            { "Drk", new(0.45f, 0.22f, 0.52f, 1.0f) },
            { "Gnb", new(0.28f, 0.42f, 0.58f, 1.0f) },
            { "Whm", new(0.78f, 0.78f, 0.65f, 1.0f) },
            { "Sch", new(0.32f, 0.42f, 0.75f, 1.0f) },
            { "Ast", new(0.80f, 0.68f, 0.32f, 1.0f) },
            { "Sge", new(0.35f, 0.58f, 0.68f, 1.0f) },
            { "Mnk", new(0.78f, 0.60f, 0.18f, 1.0f) },
            { "Drg", new(0.28f, 0.38f, 0.78f, 1.0f) },
            { "Nin", new(0.62f, 0.22f, 0.35f, 1.0f) },
            { "Sam", new(0.82f, 0.50f, 0.22f, 1.0f) },
            { "Rpr", new(0.55f, 0.25f, 0.38f, 1.0f) },
            { "Vpr", new(0.42f, 0.62f, 0.30f, 1.0f) },
            { "Brd", new(0.52f, 0.72f, 0.30f, 1.0f) },
            { "Mch", new(0.42f, 0.68f, 0.72f, 1.0f) },
            { "Dnc", new(0.78f, 0.52f, 0.60f, 1.0f) },
            { "Blm", new(0.55f, 0.42f, 0.78f, 1.0f) },
            { "Smn", new(0.32f, 0.62f, 0.38f, 1.0f) },
            { "Rdm", new(0.78f, 0.35f, 0.42f, 1.0f) },
            { "Pct", new(0.68f, 0.50f, 0.72f, 1.0f) },
            { "Blu", new(0.30f, 0.50f, 0.82f, 1.0f) },
        };

        // Background & Text
        p.BarBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.80f);
        p.NameTextColor = new(0.95f, 0.95f, 0.95f, 1f);
        p.ValueTextColor = new(0.95f, 0.95f, 0.95f, 1f);
        p.WindowBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.92f);
        p.WindowPaddingLeft = 8f;
        p.WindowPaddingRight = 8f;
        p.WindowPaddingTop = 8f;
        p.WindowPaddingBottom = 8f;

        // Selection Bar
        p.SelectionBarTextColor = new(0.85f, 0.85f, 0.85f, 1f);
        p.SelectionBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.4f);
        p.SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f);

        // Header
        p.HeaderTextColor = new(0.65f, 0.65f, 0.65f, 0.85f);
        p.HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f);
        p.HeaderHeight = 20f;
        p.HeaderFontSize = 12.9f;
        p.HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f);

        // Status Bar
        p.StatusBarFontSize = 13.3f;
        p.StatusBarPadding = 5f;
        p.StatusBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.88f);
        p.StatusBarActiveColor = new(0.90f, 0.60f, 0.15f, 1.0f);
        p.StatusBarInactiveColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.StatusBarLabelColor = new(0.55f, 0.55f, 0.55f, 0.85f);
        p.StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f);

        // Skill Breakdown
        p.SkillDamageFillColor = new(0.32f, 0.32f, 0.52f, 0.65f);
        p.SkillPhysicalFillColor = new(0.50f, 0.26f, 0.20f, 0.65f);
        p.SkillMagicFillColor = new(0.25f, 0.25f, 0.52f, 0.65f);
        p.SkillHealingFillColor = new(0.22f, 0.48f, 0.28f, 0.65f);
        p.SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.58f);
        p.SkillTextColor = new(0.95f, 0.95f, 0.95f, 0.9f);
        p.SkillHeaderTextColor = new(0.58f, 0.58f, 0.58f, 0.88f);
        p.SkillBarRounding = 2f;
        p.SkillFontSize = 12.6f;

        // Buff/Debuff
        p.BuffFillColor = new(0.28f, 0.42f, 0.55f, 0.65f);
        p.DebuffFillColor = new(0.55f, 0.28f, 0.28f, 0.65f);
        p.BuffRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.58f);
        p.BuffTextColor = new(0.95f, 0.95f, 0.95f, 0.9f);
        p.BuffHeaderTextColor = new(0.58f, 0.58f, 0.58f, 0.88f);
        p.BuffBarRounding = 2f;
        p.BuffFontSize = 12.6f;

        // Detail Panel
        p.DetailBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.82f);
        p.DetailLabelColor = new(0.65f, 0.65f, 0.65f, 1f);
        p.DetailFontSize = 12.9f;

        // Display Flags
        p.ShowJobAbbrevOnBar = false;

        // Tab Buttons
        p.TabButtonColor = new(0.12f, 0.12f, 0.12f, 0.8f);
        p.TabButtonHoveredColor = new(0.20f, 0.20f, 0.20f, 0.85f);
        p.TabButtonActiveColor = new(0.25f, 0.40f, 0.72f, 0.8f);
        p.TabButtonTextColor = new(0.65f, 0.65f, 0.65f, 0.85f);
        p.TabButtonActiveTextColor = new(0.95f, 0.95f, 0.95f, 1.0f);
        p.TabButtonHeight = 20f;
        p.TabButtonSpacing = 1f;
        p.TabButtonRounding = 2f;
        p.TabButtonFontSize = 12.9f;
        p.TabButtonWidth = 80f;

        // Tooltip
        p.TooltipBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.92f);
        p.TooltipTextColor = new(0.95f, 0.95f, 0.95f, 1f);
        p.TooltipLabelColor = new(0.55f, 0.55f, 0.55f, 0.88f);
        p.TooltipFontSize = 12.6f;
        p.TooltipRounding = 2f;
        p.TooltipPadding = 5f;

        // Detail Graph
        p.GraphDpsColor = new(0.75f, 0.32f, 0.32f, 1f);
        p.GraphHpsColor = new(0.32f, 0.70f, 0.35f, 1f);
        p.GraphDtpsColor = new(0.35f, 0.48f, 0.78f, 1f);
        p.GraphBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.90f);
        p.GraphGridColor = new(0.28f, 0.28f, 0.28f, 0.3f);
        p.GraphFontSize = 12.6f;

        // Graph View
        p.GraphViewBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.6f);
        p.GraphViewGridColor = new(0.28f, 0.28f, 0.28f, 0.3f);
        p.GraphViewFontSize = 12.6f;

        return p;
    }
}
