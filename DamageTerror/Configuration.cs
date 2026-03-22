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

    // ===== Display Settings =====

    /// <summary>
    /// If true, the main window is opened on plugin start.
    /// </summary>
    public bool ShowOnStart { get; set; } = true;

    /// <summary>
    /// Maximum number of past encounters to keep in history.
    /// </summary>
    public int MaxEncounterHistory { get; set; } = 30;

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
    public Vector4 BarBackgroundColor { get; set; } = new(0.15f, 0.15f, 0.15f, 1.0f);
    public Vector4 NameTextColor { get; set; } = new(1f, 1f, 1f, 1f);
    public Vector4 ValueTextColor { get; set; } = new(1f, 1f, 1f, 1f);

    // ===== Customization — Bar Appearance =====

    public float BarHeight { get; set; } = 22.0f;
    public float BarSpacing { get; set; } = 1.0f;
    public float BarRounding { get; set; } = 0.0f;
    public float IconSize { get; set; } = 16.0f;

    // ===== Customization — Bar Info =====

    public bool ShowNameOnBar { get; set; } = true;
    public bool ShowValueOnBar { get; set; } = true;
    public bool ShowDamagePercentOnBar { get; set; } = false;
    public bool ShowJobAbbrevOnBar { get; set; } = false;
    public bool ShowRankNumber { get; set; } = false;

    // ===== Customization — Detail Panel =====

    public bool DetailShowDamage { get; set; } = true;
    public bool DetailShowCritDhStats { get; set; } = true;
    public bool DetailShowDeaths { get; set; } = true;
    public bool DetailShowOverheal { get; set; } = true;
    public bool DetailShowMaxHit { get; set; } = true;
    public bool DetailShowDpsTrend { get; set; } = true;

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
