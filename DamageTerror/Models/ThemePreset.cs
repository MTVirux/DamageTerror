using Newtonsoft.Json;

namespace DamageTerror.Models;

/// <summary>
/// A serializable snapshot of all appearance-related configuration fields.
/// Can be applied to a <see cref="Configuration"/> to instantly restyle the meter.
/// </summary>
public class ThemePreset
{
    // ===== Metadata =====

    public string Name { get; set; } = "Untitled";
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsBuiltIn { get; set; }

    // ===== Bar Geometry =====

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 16.0f;
    public float BarAlpha { get; set; } = 0.7f;
    public float BarFontScale { get; set; } = 1.0f;
    public float BarLeftPadding { get; set; } = 4.0f;
    public float BarRightPadding { get; set; } = 6.0f;
    public float BarColumnSpacing { get; set; } = 6.0f;

    // ===== Role Colors =====

    public bool UsePerJobColors { get; set; }
    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    // ===== Per-Job Colors (nullable — only present when UsePerJobColors) =====

    public Dictionary<string, Vector4>? JobColors { get; set; }

    // ===== UI Colors =====

    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public float WindowRounding { get; set; } = 0f;

    // ===== Selection Bar =====

    public bool ShowSelectionBar { get; set; } = true;
    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; }
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSortDropdown { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // ===== Header =====

    public bool ShowMeterHeader { get; set; } = true;
    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float HeaderHeight { get; set; } = 22.0f;
    public float HeaderFontScale { get; set; } = 1.0f;
    public bool HeaderSeparator { get; set; }
    public Vector4 HeaderSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // ===== Status Bar =====

    public bool ShowStatusBar { get; set; } = true;
    public bool StatusBarAbove { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
    public float StatusBarHeight { get; set; } = 20f;
    public float StatusBarFontScale { get; set; } = 1.0f;
    public float StatusBarPadding { get; set; } = 6f;
    public bool ShowStatusBarSeparator { get; set; } = true;
    public Vector4 StatusBarBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.9f);
    public Vector4 StatusBarActiveColor { get; set; } = new(1.0f, 0.6f, 0.0f, 1.0f);
    public Vector4 StatusBarInactiveColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // ===== Skill Breakdown =====

    public Vector4 SkillDamageFillColor { get; set; } = new(0.35f, 0.35f, 0.55f, 0.7f);
    public Vector4 SkillHealingFillColor { get; set; } = new(0.25f, 0.50f, 0.30f, 0.7f);
    public Vector4 SkillRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 SkillTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 SkillHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public float SkillRowHeight { get; set; } = 14f;
    public float SkillColumnPadding { get; set; } = 6f;
    public float SkillBarRounding { get; set; } = 0f;

    // ===== Display Flags =====

    public bool ShowJobIcons { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowValueOnBar { get; set; } = true;
    public bool ShowDamagePercentOnBar { get; set; }
    public bool ShowJobAbbrevOnBar { get; set; }
    public bool ShowRankNumber { get; set; }
    public bool ShowDirectHitOnBar { get; set; }
    public bool ShowCritOnBar { get; set; }
    public bool ShowCritDirectHitOnBar { get; set; }

    // ===== Detail Panel =====

    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public Vector4 DetailDeathColor { get; set; } = new(1f, 0.3f, 0.3f, 1f);
    public float DetailIndent { get; set; } = 8.0f;

    /// <summary>
    /// Applies all preset fields onto the given configuration, overwriting appearance settings.
    /// </summary>
    public void ApplyTo(Configuration config)
    {
        // Bar geometry
        config.BarHeight = BarHeight;
        config.BarSpacing = BarSpacing;
        config.BarRounding = BarRounding;
        config.IconSize = IconSize;
        config.BarAlpha = BarAlpha;
        config.BarFontScale = BarFontScale;
        config.BarLeftPadding = BarLeftPadding;
        config.BarRightPadding = BarRightPadding;
        config.BarColumnSpacing = BarColumnSpacing;

        // Role colors
        config.UsePerJobColors = UsePerJobColors;
        config.TankColor = TankColor;
        config.HealerColor = HealerColor;
        config.MeleeDpsColor = MeleeDpsColor;
        config.RangedDpsColor = RangedDpsColor;
        config.CasterDpsColor = CasterDpsColor;
        config.DefaultJobColor = DefaultJobColor;

        // Per-job colors
        config.JobColors = JobColors != null ? new Dictionary<string, Vector4>(JobColors) : new();

        // UI colors
        config.BarBackgroundColor = BarBackgroundColor;
        config.NameTextColor = NameTextColor;
        config.ValueTextColor = ValueTextColor;
        config.WindowBackgroundColor = WindowBackgroundColor;
        config.WindowRounding = WindowRounding;

        // Selection bar
        config.ShowSelectionBar = ShowSelectionBar;
        config.SelectionBarTextColor = SelectionBarTextColor;
        config.SelectionBarBackgroundColor = SelectionBarBackgroundColor;
        config.SelectionBarHeight = SelectionBarHeight;
        config.ShowEncounterPicker = ShowEncounterPicker;
        config.ShowSortDropdown = ShowSortDropdown;
        config.ShowSelectionBarSeparator = ShowSelectionBarSeparator;
        config.SelectionBarSeparatorColor = SelectionBarSeparatorColor;

        // Header
        config.ShowMeterHeader = ShowMeterHeader;
        config.HeaderTextColor = HeaderTextColor;
        config.HeaderBackgroundColor = HeaderBackgroundColor;
        config.HeaderHeight = HeaderHeight;
        config.HeaderFontScale = HeaderFontScale;
        config.HeaderSeparator = HeaderSeparator;
        config.HeaderSeparatorColor = HeaderSeparatorColor;

        // Status bar
        config.ShowStatusBar = ShowStatusBar;
        config.StatusBarAbove = StatusBarAbove;
        config.ShowStatusBarTimer = ShowStatusBarTimer;
        config.StatusBarHeight = StatusBarHeight;
        config.StatusBarFontScale = StatusBarFontScale;
        config.StatusBarPadding = StatusBarPadding;
        config.ShowStatusBarSeparator = ShowStatusBarSeparator;
        config.StatusBarBackgroundColor = StatusBarBackgroundColor;
        config.StatusBarActiveColor = StatusBarActiveColor;
        config.StatusBarInactiveColor = StatusBarInactiveColor;
        config.StatusBarLabelColor = StatusBarLabelColor;
        config.StatusBarSeparatorColor = StatusBarSeparatorColor;

        // Skill breakdown
        config.SkillDamageFillColor = SkillDamageFillColor;
        config.SkillHealingFillColor = SkillHealingFillColor;
        config.SkillRowBackgroundColor = SkillRowBackgroundColor;
        config.SkillTextColor = SkillTextColor;
        config.SkillHeaderTextColor = SkillHeaderTextColor;
        config.SkillRowHeight = SkillRowHeight;
        config.SkillColumnPadding = SkillColumnPadding;
        config.SkillBarRounding = SkillBarRounding;

        // Display flags
        config.ShowJobIcons = ShowJobIcons;
        config.ShowNameOnBar = ShowNameOnBar;
        config.ShowValueOnBar = ShowValueOnBar;
        config.ShowDamagePercentOnBar = ShowDamagePercentOnBar;
        config.ShowJobAbbrevOnBar = ShowJobAbbrevOnBar;
        config.ShowRankNumber = ShowRankNumber;
        config.ShowDirectHitOnBar = ShowDirectHitOnBar;
        config.ShowCritOnBar = ShowCritOnBar;
        config.ShowCritDirectHitOnBar = ShowCritDirectHitOnBar;

        // Detail panel
        config.DetailLabelColor = DetailLabelColor;
        config.DetailDeathColor = DetailDeathColor;
        config.DetailIndent = DetailIndent;
    }

    /// <summary>
    /// Creates a new preset from the current configuration state.
    /// </summary>
    public static ThemePreset CreateFromConfig(Configuration config, string name, string description = "")
    {
        return new ThemePreset
        {
            Name = name,
            Description = description,

            // Bar geometry
            BarHeight = config.BarHeight,
            BarSpacing = config.BarSpacing,
            BarRounding = config.BarRounding,
            IconSize = config.IconSize,
            BarAlpha = config.BarAlpha,
            BarFontScale = config.BarFontScale,
            BarLeftPadding = config.BarLeftPadding,
            BarRightPadding = config.BarRightPadding,
            BarColumnSpacing = config.BarColumnSpacing,

            // Role colors
            UsePerJobColors = config.UsePerJobColors,
            TankColor = config.TankColor,
            HealerColor = config.HealerColor,
            MeleeDpsColor = config.MeleeDpsColor,
            RangedDpsColor = config.RangedDpsColor,
            CasterDpsColor = config.CasterDpsColor,
            DefaultJobColor = config.DefaultJobColor,

            // Per-job colors
            JobColors = config.JobColors.Count > 0
                ? new Dictionary<string, Vector4>(config.JobColors)
                : null,

            // UI colors
            BarBackgroundColor = config.BarBackgroundColor,
            NameTextColor = config.NameTextColor,
            ValueTextColor = config.ValueTextColor,
            WindowBackgroundColor = config.WindowBackgroundColor,
            WindowRounding = config.WindowRounding,

            // Selection bar
            ShowSelectionBar = config.ShowSelectionBar,
            SelectionBarTextColor = config.SelectionBarTextColor,
            SelectionBarBackgroundColor = config.SelectionBarBackgroundColor,
            SelectionBarHeight = config.SelectionBarHeight,
            ShowEncounterPicker = config.ShowEncounterPicker,
            ShowSortDropdown = config.ShowSortDropdown,
            ShowSelectionBarSeparator = config.ShowSelectionBarSeparator,
            SelectionBarSeparatorColor = config.SelectionBarSeparatorColor,

            // Header
            ShowMeterHeader = config.ShowMeterHeader,
            HeaderTextColor = config.HeaderTextColor,
            HeaderBackgroundColor = config.HeaderBackgroundColor,
            HeaderHeight = config.HeaderHeight,
            HeaderFontScale = config.HeaderFontScale,
            HeaderSeparator = config.HeaderSeparator,
            HeaderSeparatorColor = config.HeaderSeparatorColor,

            // Status bar
            ShowStatusBar = config.ShowStatusBar,
            StatusBarAbove = config.StatusBarAbove,
            ShowStatusBarTimer = config.ShowStatusBarTimer,
            StatusBarHeight = config.StatusBarHeight,
            StatusBarFontScale = config.StatusBarFontScale,
            StatusBarPadding = config.StatusBarPadding,
            ShowStatusBarSeparator = config.ShowStatusBarSeparator,
            StatusBarBackgroundColor = config.StatusBarBackgroundColor,
            StatusBarActiveColor = config.StatusBarActiveColor,
            StatusBarInactiveColor = config.StatusBarInactiveColor,
            StatusBarLabelColor = config.StatusBarLabelColor,
            StatusBarSeparatorColor = config.StatusBarSeparatorColor,

            // Skill breakdown
            SkillDamageFillColor = config.SkillDamageFillColor,
            SkillHealingFillColor = config.SkillHealingFillColor,
            SkillRowBackgroundColor = config.SkillRowBackgroundColor,
            SkillTextColor = config.SkillTextColor,
            SkillHeaderTextColor = config.SkillHeaderTextColor,
            SkillRowHeight = config.SkillRowHeight,
            SkillColumnPadding = config.SkillColumnPadding,
            SkillBarRounding = config.SkillBarRounding,

            // Display flags
            ShowJobIcons = config.ShowJobIcons,
            ShowNameOnBar = config.ShowNameOnBar,
            ShowValueOnBar = config.ShowValueOnBar,
            ShowDamagePercentOnBar = config.ShowDamagePercentOnBar,
            ShowJobAbbrevOnBar = config.ShowJobAbbrevOnBar,
            ShowRankNumber = config.ShowRankNumber,
            ShowDirectHitOnBar = config.ShowDirectHitOnBar,
            ShowCritOnBar = config.ShowCritOnBar,
            ShowCritDirectHitOnBar = config.ShowCritDirectHitOnBar,

            // Detail panel
            DetailLabelColor = config.DetailLabelColor,
            DetailDeathColor = config.DetailDeathColor,
            DetailIndent = config.DetailIndent,
        };
    }
}
