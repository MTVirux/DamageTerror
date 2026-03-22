using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace DamageTerror.Services;

/// <summary>
/// Simple static service locator for sharing Dalamud services across the plugin.
/// </summary>
public static class ServiceManager
{
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static IPluginLog PluginLog { get; private set; } = null!;
    public static IDataManager DataManager { get; private set; } = null!;
    public static ITextureProvider TextureProvider { get; private set; } = null!;
    public static IPlayerState PlayerState { get; private set; } = null!;

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
}
