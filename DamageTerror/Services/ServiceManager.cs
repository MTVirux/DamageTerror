using Dalamud.Plugin;

namespace DamageTerror.Services;

public static class ServiceManager
{
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static IPluginLog PluginLog { get; private set; } = null!;
    public static IDataManager DataManager { get; private set; } = null!;
    public static ITextureProvider TextureProvider { get; private set; } = null!;
    public static IPlayerState PlayerState { get; private set; } = null!;
    public static Configuration? Config { get; set; }

    private static string? gameVersion;

    /// <summary>Client version the game is running, e.g. "2025.06.10.0000.0000". Null if Lumina can't tell us.</summary>
    public static string? GameVersion => gameVersion ??= ReadGameVersion();

    private static string? ReadGameVersion()
    {
        try
        {
            return DataManager.GameData.Repositories.TryGetValue("ffxiv", out var repo) ? repo.Version : null;
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to read the game version: {ex.Message}");
            return null;
        }
    }

    public static void Initialize(
        IDalamudPluginInterface pluginInterface,
        IPlayerState playerState,
        IDataManager dataManager,
        IPluginLog pluginLog,
        ITextureProvider textureProvider)
    {
        PluginInterface = pluginInterface;
        PlayerState = playerState;
        DataManager = dataManager;
        PluginLog = pluginLog;
        TextureProvider = textureProvider;
    }

#if DEBUG
    private static LogChannel? GetParent(LogChannel channel) => channel switch
    {
        LogChannel.PetDebug => LogChannel.SkillTracker,
        LogChannel.DoTDiag => LogChannel.SkillTracker,
        _ => null,
    };

    private static bool IsEnabled(LogChannel channel)
    {
        var disabled = Config?.DisabledLogChannels;
        if (disabled == null)
            return true;
        if (disabled.Contains(channel))
            return false;
        var parent = GetParent(channel);
        return parent == null || !disabled.Contains(parent.Value);
    }
#endif

    public static void LogDebug(LogChannel channel, string message)
    {
#if DEBUG
        if (IsEnabled(channel))
            PluginLog.Debug(message);
#endif
    }

    private static bool ShouldLog(LogChannel channel)
    {
#if DEBUG
        return IsEnabled(channel);
#else
        return true;
#endif
    }

    public static void LogInfo(LogChannel channel, string message)
    {
        if (ShouldLog(channel))
            PluginLog.Information(message);
    }

    public static void LogWarning(LogChannel channel, string message)
    {
        if (ShouldLog(channel))
            PluginLog.Warning(message);
    }

    public static void LogError(LogChannel channel, string message)
    {
        if (ShouldLog(channel))
            PluginLog.Error(message);
    }

    public static void LogError(LogChannel channel, Exception ex, string message)
    {
        if (ShouldLog(channel))
            PluginLog.Error(ex, message);
    }
}
