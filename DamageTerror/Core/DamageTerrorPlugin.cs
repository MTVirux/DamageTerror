using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using ECommons;

namespace DamageTerror.Core;

/// <summary>
/// Entry point for the DamageTerror Dalamud plugin.
/// Native ImGui damage meter overlay powered by IINACT.
/// </summary>
public class DamageTerrorPlugin : IDalamudPlugin, IDisposable
{
    public static DamageTerrorPlugin Instance { get; private set; } = null!;

    public IDalamudPluginInterface PluginInterface { get; init; }

    public Configuration Config { get; private set; } = new Configuration();

    public DataService DataService { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new(typeof(DamageTerrorPlugin).AssemblyQualifiedName);
    private readonly Gui.MainWindow.MainWindow mainWindow;
    private readonly Gui.ConfigWindow.ConfigWindow configWindow;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog pluginLog;
    private bool disposed;

    public DamageTerrorPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IPlayerState playerState,
        IDataManager dataManager,
        IFramework framework,
        IPluginLog pluginLog,
        ITextureProvider textureProvider)
    {
        Instance = this;
        this.PluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.pluginLog = pluginLog;

        // Initialize ECommons
        ECommonsMain.Init(pluginInterface, this);

        // Initialize service locator
        ServiceManager.Initialize(pluginInterface, playerState, dataManager, pluginLog, textureProvider);

        // Load configuration
        var cfg = this.PluginInterface.GetPluginConfig() as Configuration;
        if (cfg == null)
        {
            cfg = new Configuration();
            this.PluginInterface.SavePluginConfig(cfg);
        }

        this.Config = cfg;

        // Initialize data service (connects to IINACT)
        this.DataService = new DataService(pluginInterface, pluginLog, this.Config);

        // Create UI windows
        this.mainWindow = new Gui.MainWindow.MainWindow(this, textureProvider);
        this.configWindow = new Gui.ConfigWindow.ConfigWindow(this);

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);

        // Register UI callbacks
        this.PluginInterface.UiBuilder.Draw += this.DrawUi;
        this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        this.PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;

        // Register slash command
        this.commandManager.AddHandler("/dt", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Toggle the Damage Terror meter window.",
        });

        // Open main window on start if configured
        this.mainWindow.IsOpen = this.Config.ShowOnStart;

        // Start data service (async, fire-and-forget with logging)
        Task.Run(async () =>
        {
            try
            {
                await this.DataService.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                pluginLog.Error($"[DamageTerror] Failed to start data service: {ex.Message}");
            }
        });
    }

    public static string Name => "Damage Terror";

    public void OpenMainUi() => this.mainWindow.IsOpen = true;

    public void OpenConfigUi() => this.configWindow.IsOpen = true;

    public void SaveConfig() => this.PluginInterface.SavePluginConfig(this.Config);

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed) return;

        if (disposing)
        {
            // Save config
            this.PluginInterface.SavePluginConfig(this.Config);

            // Tear down data service
            this.DataService.Dispose();

            // Tear down UI
            this.windowSystem.RemoveAllWindows();
            this.mainWindow.Dispose();
            this.configWindow.Dispose();

            this.PluginInterface.UiBuilder.Draw -= this.DrawUi;
            this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            this.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi;

            this.commandManager.RemoveHandler("/dt");

            // Dispose ECommons
            ECommonsMain.Dispose();
        }

        this.disposed = true;
    }

    private void DrawUi() => this.windowSystem.Draw();

    private void OnCommand(string command, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            this.mainWindow.IsOpen = !this.mainWindow.IsOpen;
        else if (arguments.Trim().Equals("config", StringComparison.OrdinalIgnoreCase))
            this.configWindow.IsOpen = !this.configWindow.IsOpen;
        else
            this.mainWindow.IsOpen = true;
    }
}
