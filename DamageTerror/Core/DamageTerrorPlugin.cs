using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using ECommons;

namespace DamageTerror.Core;

public class DamageTerrorPlugin : IDalamudPlugin, IDisposable
{
    public static DamageTerrorPlugin Instance { get; private set; } = null!;

    public IDalamudPluginInterface PluginInterface { get; init; }

    public Configuration Config { get; private set; } = new Configuration();

    public DataService DataService { get; private set; } = null!;

    public FontService FontService { get; private set; } = null!;

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

        ECommonsMain.Init(pluginInterface, this);
        ServiceManager.Initialize(pluginInterface, playerState, dataManager, pluginLog, textureProvider);

        var cfg = this.PluginInterface.GetPluginConfig() as Configuration;
        if (cfg == null)
        {
            cfg = new Configuration();
            this.PluginInterface.SavePluginConfig(cfg);
        }

        this.Config = cfg;
        Gui.ConfigWindow.LayoutPage.EnsureLayoutComplete(cfg);

        this.DataService = new DataService(pluginInterface, pluginLog, this.Config);

        this.FontService = new FontService(this.Config, pluginLog);
        if (this.Config.EnableCustomFont)
            this.FontService.Initialize(pluginInterface.UiBuilder);

        var presetManager = new PresetManager(
            pluginInterface.ConfigDirectory.FullName, pluginLog);

        this.mainWindow = new Gui.MainWindow.MainWindow(this, textureProvider);
        this.configWindow = new Gui.ConfigWindow.ConfigWindow(this, presetManager);

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);

        this.PluginInterface.UiBuilder.Draw += this.DrawUi;
        this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        this.PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;

        this.commandManager.AddHandler("/dt", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Toggle the Damage Terror meter window.",
        });

        this.mainWindow.IsOpen = this.Config.ShowOnStart;

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
            this.PluginInterface.SavePluginConfig(this.Config);
            this.DataService.Dispose();
            this.FontService.Dispose();

            this.windowSystem.RemoveAllWindows();
            this.mainWindow.Dispose();
            this.configWindow.Dispose();

            this.PluginInterface.UiBuilder.Draw -= this.DrawUi;
            this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            this.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi;

            this.commandManager.RemoveHandler("/dt");

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
