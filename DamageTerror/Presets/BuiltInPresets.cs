namespace DamageTerror.Presets;

/// <summary>
/// Ships the built-in theme presets that simulate popular FFXIV overlays.
/// </summary>
public static class BuiltInPresets
{
    /// <summary>All built-in presets, in display order.</summary>
    public static ThemePreset[] All { get; } = { Default(), Kagerou(), Ember(), Horizoverlay(), MopiMopi(), Ikegami(), NextUI() };

    // ================================================================
    // Default — stock DamageTerror settings (factory reset)
    // ================================================================
    public static ThemePreset Default() => new()
    {
        Name = "Default",
        Description = "Stock DamageTerror appearance. Use this to reset to factory settings.",
        IsBuiltIn = true,
    };

    // ================================================================
    // Kagerou — classic MiniParse overlay
    // Sharp bars, role-based colors, compact dark theme
    // ================================================================
    public static ThemePreset Kagerou() => new()
    {
        Name = "Kagerou",
        Description = "Classic MiniParse style — sharp bars, dark background, compact layout.",
        IsBuiltIn = true,

        // Bar geometry
        BarHeight = 20f,
        BarSpacing = 1f,
        BarRounding = 0f,
        IconSize = 14f,
        BarAlpha = 0.75f,

        // Role colors — Kagerou's distinctive palette
        UsePerJobColors = false,
        TankColor = new(0.24f, 0.32f, 0.71f, 1.0f),
        HealerColor = new(0.30f, 0.64f, 0.31f, 1.0f),
        MeleeDpsColor = new(0.90f, 0.22f, 0.21f, 1.0f),
        RangedDpsColor = new(1.00f, 0.60f, 0.0f, 1.0f),
        CasterDpsColor = new(0.49f, 0.34f, 0.76f, 1.0f),
        DefaultJobColor = new(0.46f, 0.46f, 0.46f, 1.0f),

        // UI colors
        BarBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.85f),
        NameTextColor = new(1f, 1f, 1f, 1f),
        ValueTextColor = new(1f, 1f, 1f, 1f),
        WindowBackgroundColor = new(0.055f, 0.055f, 0.055f, 0.95f),

        // Selection bar
        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.85f, 0.85f, 0.85f, 1f),
        SelectionBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.6f),
        SelectionBarHeight = 0f,
        ShowEncounterPicker = true,
        ShowSortDropdown = true,
        ShowSelectionBarSeparator = true,
        SelectionBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.5f),

        // Header — compact
        ShowMeterHeader = true,
        HeaderTextColor = new(0.6f, 0.6f, 0.6f, 0.8f),
        HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        HeaderHeight = 18f,
        HeaderSeparator = true,
        HeaderSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.4f),

        // Status bar — minimal
        ShowStatusBar = true,
        StatusBarAbove = false,
        ShowStatusBarTimer = true,
        StatusBarHeight = 18f,
        StatusBarFontScale = 0.9f,
        ShowStatusBarSeparator = true,
        StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.9f),
        StatusBarActiveColor = new(1.0f, 0.6f, 0.0f, 1.0f),
        StatusBarInactiveColor = new(0.5f, 0.5f, 0.5f, 0.8f),
        StatusBarLabelColor = new(0.5f, 0.5f, 0.5f, 0.8f),
        StatusBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.4f),

        // Skills
        SkillDamageFillColor = new(0.30f, 0.30f, 0.50f, 0.7f),
        SkillHealingFillColor = new(0.20f, 0.45f, 0.25f, 0.7f),
        SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f),
        SkillTextColor = new(1f, 1f, 1f, 0.9f),
        SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.9f),
        SkillRowHeight = 13f,
        SkillColumnPadding = 5f,

        // Display flags — Kagerou shows name, value, %, rank; hides DH/Crit columns
        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = true,
        ShowJobAbbrevOnBar = false,
        ShowRankNumber = true,
        ShowDirectHitOnBar = false,
        ShowCritOnBar = false,
        ShowCritDirectHitOnBar = false,
    };

    // ================================================================
    // Ember Overlay — modern, compact with slight rounding
    // ================================================================
    public static ThemePreset Ember() => new()
    {
        Name = "Ember Overlay",
        Description = "Modern compact bars with warm tones and slight rounding.",
        IsBuiltIn = true,

        BarHeight = 20f,
        BarSpacing = 2f,
        BarRounding = 3f,
        IconSize = 14f,
        BarAlpha = 0.80f,

        UsePerJobColors = false,
        TankColor = new(0.28f, 0.44f, 0.82f, 1.0f),
        HealerColor = new(0.25f, 0.70f, 0.35f, 1.0f),
        MeleeDpsColor = new(0.85f, 0.28f, 0.22f, 1.0f),
        RangedDpsColor = new(0.95f, 0.55f, 0.15f, 1.0f),
        CasterDpsColor = new(0.55f, 0.30f, 0.78f, 1.0f),
        DefaultJobColor = new(0.50f, 0.50f, 0.50f, 1.0f),

        BarBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.90f),
        NameTextColor = new(1f, 1f, 1f, 1f),
        ValueTextColor = new(1f, 1f, 1f, 0.95f),
        WindowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.92f),

        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.9f, 0.9f, 0.9f, 1f),
        SelectionBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.5f),
        SelectionBarHeight = 2f,
        ShowEncounterPicker = true,
        ShowSortDropdown = true,
        ShowSelectionBarSeparator = true,
        SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f),

        ShowMeterHeader = true,
        HeaderTextColor = new(0.65f, 0.65f, 0.65f, 0.85f),
        HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        HeaderHeight = 20f,
        HeaderSeparator = false,
        HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f),

        ShowStatusBar = true,
        StatusBarAbove = false,
        ShowStatusBarTimer = true,
        StatusBarHeight = 20f,
        StatusBarFontScale = 0.95f,
        ShowStatusBarSeparator = true,
        StatusBarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.85f),
        StatusBarActiveColor = new(1.0f, 0.55f, 0.10f, 1.0f),
        StatusBarInactiveColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        StatusBarLabelColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.5f),

        SkillDamageFillColor = new(0.35f, 0.35f, 0.55f, 0.7f),
        SkillHealingFillColor = new(0.25f, 0.50f, 0.30f, 0.7f),
        SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.6f),
        SkillTextColor = new(1f, 1f, 1f, 0.9f),
        SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        SkillRowHeight = 14f,
        SkillColumnPadding = 6f,

        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = false,
        ShowJobAbbrevOnBar = false,
        ShowRankNumber = true,
        ShowDirectHitOnBar = false,
        ShowCritOnBar = false,
        ShowCritDirectHitOnBar = false,
    };

    // ================================================================
    // Horizoverlay — minimal horizontal bar style
    // Thin, rounded, transparent, name+value only
    // ================================================================
    public static ThemePreset Horizoverlay() => new()
    {
        Name = "Horizoverlay",
        Description = "Minimal horizontal bars — thin, rounded, highly transparent.",
        IsBuiltIn = true,

        BarHeight = 16f,
        BarSpacing = 1f,
        BarRounding = 8f,
        IconSize = 12f,
        BarAlpha = 0.60f,

        UsePerJobColors = false,
        TankColor = new(0.30f, 0.45f, 0.75f, 1.0f),
        HealerColor = new(0.30f, 0.65f, 0.35f, 1.0f),
        MeleeDpsColor = new(0.75f, 0.25f, 0.25f, 1.0f),
        RangedDpsColor = new(0.85f, 0.50f, 0.20f, 1.0f),
        CasterDpsColor = new(0.55f, 0.35f, 0.70f, 1.0f),
        DefaultJobColor = new(0.45f, 0.45f, 0.45f, 1.0f),

        BarBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.50f),
        NameTextColor = new(1f, 1f, 1f, 0.90f),
        ValueTextColor = new(1f, 1f, 1f, 0.90f),
        WindowBackgroundColor = new(0.04f, 0.04f, 0.04f, 0.70f),

        // Minimal selection bar
        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.8f, 0.8f, 0.8f, 0.9f),
        SelectionBarBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        SelectionBarHeight = 0f,
        ShowEncounterPicker = true,
        ShowSortDropdown = false,
        ShowSelectionBarSeparator = false,
        SelectionBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f),

        // No header
        ShowMeterHeader = false,
        HeaderTextColor = new(0.6f, 0.6f, 0.6f, 0.8f),
        HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        HeaderHeight = 18f,
        HeaderSeparator = false,
        HeaderSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f),

        // No status bar
        ShowStatusBar = false,
        StatusBarAbove = true,
        ShowStatusBarTimer = true,
        StatusBarHeight = 18f,
        StatusBarFontScale = 0.9f,
        ShowStatusBarSeparator = false,
        StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.7f),
        StatusBarActiveColor = new(0.9f, 0.6f, 0.1f, 1.0f),
        StatusBarInactiveColor = new(0.5f, 0.5f, 0.5f, 0.7f),
        StatusBarLabelColor = new(0.5f, 0.5f, 0.5f, 0.7f),
        StatusBarSeparatorColor = new(0.3f, 0.3f, 0.3f, 0.3f),

        SkillDamageFillColor = new(0.30f, 0.30f, 0.50f, 0.6f),
        SkillHealingFillColor = new(0.20f, 0.45f, 0.25f, 0.6f),
        SkillRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.5f),
        SkillTextColor = new(1f, 1f, 1f, 0.85f),
        SkillHeaderTextColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        SkillRowHeight = 12f,
        SkillColumnPadding = 4f,

        // Only name + value
        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = false,
        ShowJobAbbrevOnBar = false,
        ShowRankNumber = false,
        ShowDirectHitOnBar = false,
        ShowCritOnBar = false,
        ShowCritDirectHitOnBar = false,
    };

    // ================================================================
    // MopiMopi — colorful, rounded, per-job colors
    // ================================================================
    public static ThemePreset MopiMopi() => new()
    {
        Name = "MopiMopi",
        Description = "Colorful rounded bars with unique per-job colors and vibrant styling.",
        IsBuiltIn = true,

        BarHeight = 22f,
        BarSpacing = 2f,
        BarRounding = 6f,
        IconSize = 16f,
        BarAlpha = 0.85f,

        // Per-job colors — vibrant, saturated
        UsePerJobColors = true,
        TankColor = new(0.25f, 0.40f, 0.80f, 1.0f),
        HealerColor = new(0.25f, 0.70f, 0.35f, 1.0f),
        MeleeDpsColor = new(0.80f, 0.25f, 0.25f, 1.0f),
        RangedDpsColor = new(0.90f, 0.55f, 0.15f, 1.0f),
        CasterDpsColor = new(0.60f, 0.30f, 0.80f, 1.0f),
        DefaultJobColor = new(0.50f, 0.50f, 0.50f, 1.0f),
        JobColors = new Dictionary<string, Vector4>
        {
            // Tanks — vivid blues and purples
            { "Pld", new(0.45f, 0.60f, 0.95f, 1.0f) },
            { "War", new(0.75f, 0.20f, 0.20f, 1.0f) },
            { "Drk", new(0.55f, 0.20f, 0.65f, 1.0f) },
            { "Gnb", new(0.30f, 0.50f, 0.70f, 1.0f) },
            // Healers — greens, golds, cyans
            { "Whm", new(0.90f, 0.90f, 0.75f, 1.0f) },
            { "Sch", new(0.35f, 0.50f, 0.90f, 1.0f) },
            { "Ast", new(0.95f, 0.80f, 0.35f, 1.0f) },
            { "Sge", new(0.40f, 0.70f, 0.80f, 1.0f) },
            // Melee DPS — warm reds, oranges
            { "Mnk", new(0.90f, 0.70f, 0.20f, 1.0f) },
            { "Drg", new(0.30f, 0.45f, 0.90f, 1.0f) },
            { "Nin", new(0.75f, 0.25f, 0.40f, 1.0f) },
            { "Sam", new(0.95f, 0.60f, 0.25f, 1.0f) },
            { "Rpr", new(0.65f, 0.25f, 0.45f, 1.0f) },
            { "Vpr", new(0.50f, 0.75f, 0.35f, 1.0f) },
            // Ranged Physical DPS
            { "Brd", new(0.60f, 0.85f, 0.35f, 1.0f) },
            { "Mch", new(0.50f, 0.80f, 0.85f, 1.0f) },
            { "Dnc", new(0.90f, 0.60f, 0.70f, 1.0f) },
            // Casters
            { "Blm", new(0.65f, 0.50f, 0.90f, 1.0f) },
            { "Smn", new(0.35f, 0.75f, 0.45f, 1.0f) },
            { "Rdm", new(0.90f, 0.40f, 0.50f, 1.0f) },
            { "Pct", new(0.80f, 0.60f, 0.85f, 1.0f) },
            { "Blu", new(0.35f, 0.60f, 0.95f, 1.0f) },
        },

        BarBackgroundColor = new(0.10f, 0.10f, 0.12f, 0.85f),
        NameTextColor = new(1f, 1f, 1f, 1f),
        ValueTextColor = new(1f, 1f, 1f, 1f),
        WindowBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.92f),

        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.9f, 0.9f, 0.9f, 1f),
        SelectionBarBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.4f),
        SelectionBarHeight = 2f,
        ShowEncounterPicker = true,
        ShowSortDropdown = true,
        ShowSelectionBarSeparator = true,
        SelectionBarSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f),

        ShowMeterHeader = true,
        HeaderTextColor = new(0.65f, 0.65f, 0.70f, 0.9f),
        HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        HeaderHeight = 20f,
        HeaderSeparator = false,
        HeaderSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f),

        ShowStatusBar = true,
        StatusBarAbove = true,
        ShowStatusBarTimer = true,
        StatusBarHeight = 20f,
        StatusBarFontScale = 1.0f,
        ShowStatusBarSeparator = true,
        StatusBarBackgroundColor = new(0.06f, 0.06f, 0.08f, 0.9f),
        StatusBarActiveColor = new(1.0f, 0.55f, 0.15f, 1.0f),
        StatusBarInactiveColor = new(0.55f, 0.55f, 0.60f, 0.9f),
        StatusBarLabelColor = new(0.55f, 0.55f, 0.60f, 0.9f),
        StatusBarSeparatorColor = new(0.4f, 0.4f, 0.4f, 0.4f),

        SkillDamageFillColor = new(0.40f, 0.35f, 0.60f, 0.7f),
        SkillHealingFillColor = new(0.25f, 0.55f, 0.30f, 0.7f),
        SkillRowBackgroundColor = new(0.10f, 0.10f, 0.12f, 0.6f),
        SkillTextColor = new(1f, 1f, 1f, 0.9f),
        SkillHeaderTextColor = new(0.60f, 0.60f, 0.65f, 0.9f),
        SkillRowHeight = 14f,
        SkillColumnPadding = 6f,

        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = false,
        ShowJobAbbrevOnBar = false,
        ShowRankNumber = true,
        ShowDirectHitOnBar = false,
        ShowCritOnBar = false,
        ShowCritDirectHitOnBar = false,
    };

    // ================================================================
    // Ikegami — data-dense table style: all stats visible
    // ================================================================
    public static ThemePreset Ikegami() => new()
    {
        Name = "Ikegami",
        Description = "Data-dense table layout — all stats visible, compact rows, sharp edges.",
        IsBuiltIn = true,

        BarHeight = 18f,
        BarSpacing = 0f,
        BarRounding = 0f,
        IconSize = 14f,
        BarAlpha = 0.50f,

        UsePerJobColors = false,
        TankColor = new(0.25f, 0.38f, 0.72f, 1.0f),
        HealerColor = new(0.28f, 0.62f, 0.32f, 1.0f),
        MeleeDpsColor = new(0.72f, 0.24f, 0.24f, 1.0f),
        RangedDpsColor = new(0.82f, 0.50f, 0.18f, 1.0f),
        CasterDpsColor = new(0.50f, 0.32f, 0.70f, 1.0f),
        DefaultJobColor = new(0.42f, 0.42f, 0.42f, 1.0f),

        BarBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.75f),
        NameTextColor = new(1f, 1f, 1f, 0.95f),
        ValueTextColor = new(1f, 1f, 1f, 0.95f),
        WindowBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.95f),

        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.8f, 0.8f, 0.8f, 1f),
        SelectionBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.5f),
        SelectionBarHeight = 0f,
        ShowEncounterPicker = true,
        ShowSortDropdown = true,
        ShowSelectionBarSeparator = true,
        SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f),

        // Prominent header for column labels
        ShowMeterHeader = true,
        HeaderTextColor = new(0.75f, 0.75f, 0.75f, 0.95f),
        HeaderBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.7f),
        HeaderHeight = 18f,
        HeaderSeparator = true,
        HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f),

        ShowStatusBar = true,
        StatusBarAbove = false,
        ShowStatusBarTimer = true,
        StatusBarHeight = 18f,
        StatusBarFontScale = 0.85f,
        ShowStatusBarSeparator = true,
        StatusBarBackgroundColor = new(0.06f, 0.06f, 0.06f, 0.9f),
        StatusBarActiveColor = new(0.95f, 0.60f, 0.10f, 1.0f),
        StatusBarInactiveColor = new(0.50f, 0.50f, 0.50f, 0.8f),
        StatusBarLabelColor = new(0.50f, 0.50f, 0.50f, 0.8f),
        StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.6f),

        SkillDamageFillColor = new(0.30f, 0.30f, 0.48f, 0.65f),
        SkillHealingFillColor = new(0.22f, 0.45f, 0.28f, 0.65f),
        SkillRowBackgroundColor = new(0.08f, 0.08f, 0.08f, 0.55f),
        SkillTextColor = new(1f, 1f, 1f, 0.9f),
        SkillHeaderTextColor = new(0.60f, 0.60f, 0.60f, 0.9f),
        SkillRowHeight = 13f,
        SkillColumnPadding = 5f,

        // ALL stats visible — the defining feature of Ikegami
        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = true,
        ShowJobAbbrevOnBar = true,
        ShowRankNumber = true,
        ShowDirectHitOnBar = true,
        ShowCritOnBar = true,
        ShowCritDirectHitOnBar = true,
    };

    // ================================================================
    // Next UI — game-integrated feel, desaturated per-job colors
    // ================================================================
    public static ThemePreset NextUI() => new()
    {
        Name = "Next UI",
        Description = "Game-integrated look — desaturated per-job colors, subtle rounding, HUD-like feel.",
        IsBuiltIn = true,

        BarHeight = 22f,
        BarSpacing = 1f,
        BarRounding = 2f,
        IconSize = 16f,
        BarAlpha = 0.72f,

        // Desaturated per-job colors to match the game's HUD aesthetic
        UsePerJobColors = true,
        TankColor = new(0.25f, 0.40f, 0.72f, 1.0f),
        HealerColor = new(0.28f, 0.60f, 0.35f, 1.0f),
        MeleeDpsColor = new(0.70f, 0.25f, 0.25f, 1.0f),
        RangedDpsColor = new(0.80f, 0.50f, 0.20f, 1.0f),
        CasterDpsColor = new(0.52f, 0.32f, 0.68f, 1.0f),
        DefaultJobColor = new(0.45f, 0.45f, 0.45f, 1.0f),
        JobColors = new Dictionary<string, Vector4>
        {
            // Tanks — muted blues
            { "Pld", new(0.38f, 0.52f, 0.82f, 1.0f) },
            { "War", new(0.55f, 0.22f, 0.22f, 1.0f) },
            { "Drk", new(0.45f, 0.22f, 0.52f, 1.0f) },
            { "Gnb", new(0.28f, 0.42f, 0.58f, 1.0f) },
            // Healers — muted greens/golds
            { "Whm", new(0.78f, 0.78f, 0.65f, 1.0f) },
            { "Sch", new(0.32f, 0.42f, 0.75f, 1.0f) },
            { "Ast", new(0.80f, 0.68f, 0.32f, 1.0f) },
            { "Sge", new(0.35f, 0.58f, 0.68f, 1.0f) },
            // Melee — muted warms
            { "Mnk", new(0.78f, 0.60f, 0.18f, 1.0f) },
            { "Drg", new(0.28f, 0.38f, 0.78f, 1.0f) },
            { "Nin", new(0.62f, 0.22f, 0.35f, 1.0f) },
            { "Sam", new(0.82f, 0.50f, 0.22f, 1.0f) },
            { "Rpr", new(0.55f, 0.25f, 0.38f, 1.0f) },
            { "Vpr", new(0.42f, 0.62f, 0.30f, 1.0f) },
            // Ranged Physical
            { "Brd", new(0.52f, 0.72f, 0.30f, 1.0f) },
            { "Mch", new(0.42f, 0.68f, 0.72f, 1.0f) },
            { "Dnc", new(0.78f, 0.52f, 0.60f, 1.0f) },
            // Casters
            { "Blm", new(0.55f, 0.42f, 0.78f, 1.0f) },
            { "Smn", new(0.32f, 0.62f, 0.38f, 1.0f) },
            { "Rdm", new(0.78f, 0.35f, 0.42f, 1.0f) },
            { "Pct", new(0.68f, 0.50f, 0.72f, 1.0f) },
            { "Blu", new(0.30f, 0.50f, 0.82f, 1.0f) },
        },

        BarBackgroundColor = new(0.12f, 0.12f, 0.12f, 0.80f),
        NameTextColor = new(0.95f, 0.95f, 0.95f, 1f),
        ValueTextColor = new(0.95f, 0.95f, 0.95f, 1f),
        WindowBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.92f),

        ShowSelectionBar = true,
        SelectionBarTextColor = new(0.85f, 0.85f, 0.85f, 1f),
        SelectionBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.4f),
        SelectionBarHeight = 0f,
        ShowEncounterPicker = true,
        ShowSortDropdown = false,
        ShowSelectionBarSeparator = true,
        SelectionBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f),

        ShowMeterHeader = true,
        HeaderTextColor = new(0.65f, 0.65f, 0.65f, 0.85f),
        HeaderBackgroundColor = new(0.0f, 0.0f, 0.0f, 0.0f),
        HeaderHeight = 20f,
        HeaderSeparator = true,
        HeaderSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f),

        ShowStatusBar = true,
        StatusBarAbove = true,
        ShowStatusBarTimer = true,
        StatusBarHeight = 20f,
        StatusBarFontScale = 0.95f,
        ShowStatusBarSeparator = true,
        StatusBarBackgroundColor = new(0.07f, 0.07f, 0.07f, 0.88f),
        StatusBarActiveColor = new(0.90f, 0.60f, 0.15f, 1.0f),
        StatusBarInactiveColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        StatusBarLabelColor = new(0.55f, 0.55f, 0.55f, 0.85f),
        StatusBarSeparatorColor = new(0.35f, 0.35f, 0.35f, 0.45f),

        SkillDamageFillColor = new(0.32f, 0.32f, 0.52f, 0.65f),
        SkillHealingFillColor = new(0.22f, 0.48f, 0.28f, 0.65f),
        SkillRowBackgroundColor = new(0.10f, 0.10f, 0.10f, 0.58f),
        SkillTextColor = new(0.95f, 0.95f, 0.95f, 0.9f),
        SkillHeaderTextColor = new(0.58f, 0.58f, 0.58f, 0.88f),
        SkillRowHeight = 14f,
        SkillColumnPadding = 6f,

        ShowJobIcons = true,
        ShowNameOnBar = true,
        ShowValueOnBar = true,
        ShowDamagePercentOnBar = false,
        ShowJobAbbrevOnBar = false,
        ShowRankNumber = false,
        ShowDirectHitOnBar = false,
        ShowCritOnBar = false,
        ShowCritDirectHitOnBar = false,
    };
}
