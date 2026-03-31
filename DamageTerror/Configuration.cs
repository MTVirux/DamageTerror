using Dalamud.Configuration;

namespace DamageTerror;

/// <summary>
/// Plugin configuration persisted by Dalamud's config system.
/// </summary>
public class Configuration : IPluginConfiguration
{
    /// <inheritdoc />
    public int Version { get; set; } = 1;

    // ===== Connection Settings =====

    /// <summary>
    /// WebSocket URL for the IINACT OverlayPlugin server (fallback when IPC is unavailable).
    /// </summary>
    public string WebSocketUrl { get; set; } = "ws://127.0.0.1:10501/ws";

    /// <summary>
    /// If true, prefer IPC (Dalamud inter-plugin communication) over WebSocket.
    /// IPC is faster and zero-copy since both plugins run in the same process.
    /// </summary>
    public bool PreferIpc { get; set; } = true;

    // ===== Window Settings =====

    /// <summary>
    /// If true, the main window is opened on plugin start.
    /// </summary>
    public bool ShowOnStart { get; set; } = true;

    /// <summary>
    /// Maximum number of past encounters to keep in history.
    /// </summary>
    public int MaxEncounterHistory { get; set; } = 30;

    // ===== Duty Type Filters =====

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

    /// <summary>
    /// Which field to sort the combatant list by.
    /// </summary>
    public SortField SortBy { get; set; } = SortField.EncDps;

    /// <summary>
    /// If true, sort combatants in descending order (highest first).
    /// </summary>
    public bool SortDescending { get; set; } = true;

    /// <summary>
    /// If true, display job icons next to combatant names.
    /// </summary>
    public bool ShowJobIcons { get; set; } = true;

    /// <summary>
    /// If true, show HPS instead of DPS as the primary metric.
    /// </summary>
    public bool ShowHps { get; set; } = false;

    /// <summary>
    /// If true, the main window is hidden when the player is not in combat.
    /// </summary>
    public bool HideOutOfCombat { get; set; } = false;

    /// <summary>
    /// Seconds to wait after leaving combat before hiding the window (0 = immediate).
    /// </summary>
    public float HideOutOfCombatDelay { get; set; } = 5f;

    /// <summary>
    /// If true, pressing ESC will not close the main meter window.
    /// </summary>
    public bool IgnoreEscClose { get; set; } = true;

    /// <summary>
    /// If true, the ImGui window title bar / header is hidden on the main window.
    /// </summary>
    public bool HideWindowHeader { get; set; } = false;

    /// <summary>
    /// Alpha (opacity) for the colored DPS/HPS bars. 0.0 = transparent, 1.0 = opaque.
    /// </summary>
    public float BarAlpha { get; set; } = 0.7f;

    // ===== Customization — Role Colors =====

    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    // ===== Customization — Per-Job Colors =====

    /// <summary>
    /// If true, use individual per-job colors instead of role-based colors.
    /// </summary>
    public bool UsePerJobColors { get; set; } = false;

    /// <summary>
    /// Per-job color overrides keyed by job abbreviation (e.g. "Pld", "Whm").
    /// Only used when <see cref="UsePerJobColors"/> is true.
    /// </summary>
    public Dictionary<string, Vector4> JobColors { get; set; } = new();
    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 WindowBackgroundColor { get; set; } = new(0.06f, 0.06f, 0.06f, 0.94f);
    public float WindowRounding { get; set; } = 0f;

    // ===== Customization — Selection Bar =====

    public bool ShowSelectionBar { get; set; } = true;
    public Vector4 SelectionBarTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 SelectionBarBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float SelectionBarHeight { get; set; } = 0.0f;
    public bool ShowEncounterPicker { get; set; } = true;
    public bool ShowSortDropdown { get; set; } = true;
    public bool ShowSelectionBarSeparator { get; set; } = true;
    public Vector4 SelectionBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);
    public bool HideSelectionBarWhenPinned { get; set; } = false;
    public bool SelectionBarShowOnCtrlShift { get; set; } = true;

    // ===== Customization — Header Row =====

    public Vector4 HeaderTextColor { get; set; } = new(0.7f, 0.7f, 0.7f, 0.9f);
    public Vector4 HeaderBackgroundColor { get; set; } = new(0.0f, 0.0f, 0.0f, 0.0f);
    public float HeaderHeight { get; set; } = 22.0f;
    public float HeaderFontScale { get; set; } = 1.0f;
    public bool HeaderSeparator { get; set; } = false;
    public Vector4 HeaderSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);

    // ===== Customization — Font Settings =====

    /// <summary>Enable custom font loading. Disabled by default for stability.</summary>
    public bool EnableCustomFont { get; set; } = false;

    /// <summary>
    /// Master font scale multiplier applied to all UI elements.
    /// Individual component scales are multiplied by this value.
    /// </summary>
    public float GlobalFontScale { get; set; } = 1.0f;

    /// <summary>Path to the custom font file (.ttf/.otf). Null = Dalamud default.</summary>
    public string? CustomFontPath { get; set; }

    /// <summary>Font index within a collection file (.ttc).</summary>
    public int CustomFontIndex { get; set; }

    /// <summary>Custom font size in points.</summary>
    public float CustomFontSizePt { get; set; } = 14f;

    /// <summary>Display name of the selected font (for UI only).</summary>
    public string? CustomFontDisplayName { get; set; }

    /// <summary>Serialized SingleFontSpec JSON. Used to rebuild the font handle for any font type.</summary>
    public string? CustomFontSpecJson { get; set; }

    // ===== Customization — Bar Appearance =====

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 16.0f;
    public float BarFontScale { get; set; } = 1.0f;
    public float BarLeftPadding { get; set; } = 4.0f;
    public float BarRightPadding { get; set; } = 6.0f;
    public float BarColumnSpacing { get; set; } = 6.0f;
    public float IconTextPadding { get; set; } = 4.0f;

    // ===== Customization — Self Highlighting =====

    /// <summary>
    /// If true, draw an accent strip on the local player's bar for visibility.
    /// </summary>
    public bool SelfBarHighlight { get; set; } = false;

    /// <summary>
    /// Color of the self-highlight accent strip drawn on the left edge of the player's bar.
    /// </summary>
    public Vector4 SelfBarHighlightColor { get; set; } = new(1.0f, 0.85f, 0.3f, 0.9f);

    /// <summary>
    /// If true, use a distinct color for the local player's name text.
    /// </summary>
    public bool UseSelfNameColor { get; set; } = false;

    /// <summary>
    /// Override color for the local player's name. Only used when <see cref="UseSelfNameColor"/> is true.
    /// </summary>
    public Vector4 SelfNameColor { get; set; } = new(1.0f, 0.9f, 0.4f, 1.0f);

    // ===== Customization — Value Formatting =====

    /// <summary>
    /// Controls how DPS/HPS/damage numbers are formatted on bars and the status bar.
    /// </summary>
    public ValueDisplayFormat ValueDisplayFormat { get; set; } = ValueDisplayFormat.Abbreviated;

    // ===== Customization — Bar Info =====

    public bool ShowMeterHeader { get; set; } = true;
    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowYouOnBar { get; set; } = true;
    public NameDisplayFormat SelfNameFormat { get; set; } = NameDisplayFormat.FullName;
    public NameDisplayFormat OthersNameFormat { get; set; } = NameDisplayFormat.FullName;
    public bool ShowValueOnBar { get; set; } = true;
    public bool ShowDamagePercentOnBar { get; set; } = false;
    public bool ShowJobAbbrevOnBar { get; set; } = false;
    public bool ShowRankNumber { get; set; } = false;
    public bool ShowDirectHitOnBar { get; set; } = false;
    public bool ShowCritOnBar { get; set; } = false;
    public bool ShowCritDirectHitOnBar { get; set; } = false;

    // ===== Customization — Detail Panel =====

    public Vector4 DetailLabelColor { get; set; } = new(0.7f, 0.7f, 0.7f, 1f);
    public Vector4 DetailDeathColor { get; set; } = new(1f, 0.3f, 0.3f, 1f);
    public float DetailIndent { get; set; } = 8.0f;
    public float DetailFontScale { get; set; } = 1.0f;
    public bool DetailShowDamage { get; set; } = true;
    public bool DetailShowCritDhStats { get; set; } = true;
    public bool DetailShowDeaths { get; set; } = true;
    public bool DetailShowOverheal { get; set; } = true;
    public bool DetailShowMaxHit { get; set; } = true;
    public bool DetailShowDpsTrend { get; set; } = true;
    public bool DetailShowSkillBreakdown { get; set; } = true;

    /// <summary>
    /// Maximum number of skills to show in each breakdown (0 = show all).
    /// </summary>
    public int MaxSkillBreakdownCount { get; set; } = 0;

    // ===== Customization — Skill Breakdown Colors =====

    public Vector4 SkillDamageFillColor { get; set; } = new(0.35f, 0.35f, 0.55f, 0.7f);
    public Vector4 SkillHealingFillColor { get; set; } = new(0.25f, 0.50f, 0.30f, 0.7f);
    public Vector4 SkillRowBackgroundColor { get; set; } = new(0.12f, 0.12f, 0.12f, 0.6f);
    public Vector4 SkillTextColor { get; set; } = new(1f, 1f, 1f, 0.9f);
    public Vector4 SkillHeaderTextColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    // ===== Customization — Skill Breakdown Appearance =====

    public float SkillRowHeight { get; set; } = 14f;
    public float SkillColumnPadding { get; set; } = 6f;
    public float SkillBarRounding { get; set; } = 0f;
    public float SkillFontScale { get; set; } = 1.0f;

    // ===== Customization — Status Bar =====

    public bool ShowStatusBar { get; set; } = true;
    public bool StatusBarAbove { get; set; } = true;
    public bool ShowStatusBarTimer { get; set; } = true;
    public bool ShowStatusBarPersonalDps { get; set; } = true;
    public bool ShowStatusBarRaidDps { get; set; } = true;
    public float StatusBarFontScale { get; set; } = 1.0f;
    public float StatusBarHeight { get; set; } = 20f;
    public float StatusBarPadding { get; set; } = 6f;
    public bool ShowStatusBarSeparator { get; set; } = true;
    public Vector4 StatusBarSeparatorColor { get; set; } = new(0.4f, 0.4f, 0.4f, 0.5f);
    public Vector4 StatusBarBackgroundColor { get; set; } = new(0.08f, 0.08f, 0.08f, 0.9f);
    public Vector4 StatusBarActiveColor { get; set; } = new(1.0f, 0.6f, 0.0f, 1.0f);
    public Vector4 StatusBarInactiveColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);
    public Vector4 StatusBarLabelColor { get; set; } = new(0.6f, 0.6f, 0.6f, 0.9f);

    // ===== Window Settings =====

    /// <summary>
    /// If true, the main window position and size are locked.
    /// </summary>
    public bool PinMainWindow { get; set; } = false;

    /// <summary>
    /// Saved position for the main window when pinned.
    /// </summary>
    public Vector2 MainWindowPos { get; set; } = new Vector2(100, 100);

    /// <summary>
    /// Saved size for the main window when pinned.
    /// </summary>
    public Vector2 MainWindowSize { get; set; } = new Vector2(350, 400);

    /// <summary>
    /// If true, the config window position and size are locked.
    /// </summary>
    public bool PinConfigWindow { get; set; } = false;

    /// <summary>
    /// Saved position for the config window when pinned.
    /// </summary>
    public Vector2 ConfigWindowPos { get; set; } = new Vector2(100, 100);

    /// <summary>
    /// Saved size for the config window when pinned.
    /// </summary>
    public Vector2 ConfigWindowSize { get; set; } = new Vector2(400, 350);
}
