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

    public PartyMembershipService PartyService { get; private set; } = null!;

    public FontService FontService { get; private set; } = null!;

    private readonly WindowSystem windowSystem = new(typeof(DamageTerrorPlugin).AssemblyQualifiedName);
    private readonly Gui.MainWindow.MainWindow mainWindow;
    private readonly Gui.ConfigWindow.ConfigWindow configWindow;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog pluginLog;
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<Guid, Gui.MainWindow.PopoutTabWindow> popoutWindows = new();
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
        this.textureProvider = textureProvider;

        ECommonsMain.Init(pluginInterface, this);
        ServiceManager.Initialize(pluginInterface, playerState, dataManager, pluginLog, textureProvider);

        Configuration? cfg = null;
        try
        {
            cfg = this.PluginInterface.GetPluginConfig() as Configuration;
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "Failed to load plugin config (possibly outdated enum values). Creating fresh config.");
        }

        if (cfg == null)
        {
            cfg = new Configuration();
            this.PluginInterface.SavePluginConfig(cfg);
        }

        this.Config = cfg;
        this.Config.Save = this.SaveConfig;
        Gui.ConfigWindow.LayoutPage.EnsureLayoutComplete(cfg);

        foreach (var tab in cfg.MeterTabs)
        {
            if (tab.Id == Guid.Empty)
                tab.Id = Guid.NewGuid();
        }

        var loadedVersion = cfg.Version;

        if (cfg.Version < 2)
        {
            foreach (var tab in cfg.MeterTabs)
            {
                tab.GraphShowDpsLine = tab.VisibleColumns.Contains(BarColumn.Dps) || tab.VisibleColumns.Contains(BarColumn.InstantDps) || tab.VisibleColumns.Contains(BarColumn.PeakDps);
                tab.GraphShowHpsLine = tab.VisibleColumns.Contains(BarColumn.Hps) || tab.VisibleColumns.Contains(BarColumn.InstantHps);
                tab.GraphShowDtpsLine = tab.VisibleColumns.Contains(BarColumn.DamageTaken) || tab.VisibleColumns.Contains(BarColumn.DamageTakenPercent);
            }
            cfg.Version = 2;
        }

        if (cfg.Version < 3)
        {
            cfg.DetailVisibleColumns.Add(BarColumn.PositionalPct);
            cfg.Version = 3;
        }

        if (cfg.Version != loadedVersion)
            this.PluginInterface.SavePluginConfig(cfg);

        this.DataService = new DataService(pluginInterface, pluginLog, this.Config);

        this.PartyService = new PartyMembershipService();

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

        Svc.ClientState.TerritoryChanged += this.OnTerritoryChanged;

        this.commandManager.AddHandler("/dt", new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Toggle the meter window. Subcommands: config, toggle <group>",
        });

        this.mainWindow.IsOpen = this.Config.ShowOnStart;

        foreach (var tabId in this.Config.PopoutTabIds.ToList())
        {
            var tab = this.Config.MeterTabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
                OpenPopoutTabInternal(tab);
            else
                this.Config.PopoutTabIds.Remove(tabId);
        }

        Task.Run(async () =>
        {
            try
            {
                await this.DataService.StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                pluginLog.Error($"Failed to start data service: {ex.Message}");
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
            SafeDispose(() => this.PluginInterface.SavePluginConfig(this.Config));
            SafeDispose(() => this.DataService.Dispose());

            foreach (var popout in this.popoutWindows.Values)
                SafeDispose(() => popout.Dispose());
            this.popoutWindows.Clear();

            this.windowSystem.RemoveAllWindows();
            SafeDispose(() => this.mainWindow.Dispose());
            SafeDispose(() => this.configWindow.Dispose());
            SafeDispose(() => this.FontService.Dispose());

            Svc.ClientState.TerritoryChanged -= this.OnTerritoryChanged;

            this.PluginInterface.UiBuilder.Draw -= this.DrawUi;
            this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            this.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi;

            this.commandManager.RemoveHandler("/dt");

            ECommonsMain.Dispose();
        }

        this.disposed = true;
    }

    private void SafeDispose(Action action)
    {
        try { action(); }
        catch (Exception ex) { this.pluginLog.Error($"Error during disposal: {ex.Message}"); }
    }

    public void OpenPopoutTab(Guid tabId)
    {
        if (popoutWindows.ContainsKey(tabId))
            return;

        var tab = Config.MeterTabs.FirstOrDefault(t => t.Id == tabId);
        if (tab == null)
            return;

        OpenPopoutTabInternal(tab);

        if (!Config.PopoutTabIds.Contains(tabId))
        {
            Config.PopoutTabIds.Add(tabId);
            SaveConfig();
        }
    }

    public void ClosePopoutTab(Guid tabId)
    {
        if (!popoutWindows.TryGetValue(tabId, out var window))
            return;

        window.IsOpen = false;
        this.windowSystem.RemoveWindow(window);
        window.Dispose();
        popoutWindows.Remove(tabId);

        if (Config.PopoutTabIds.Remove(tabId))
            SaveConfig();
    }

    public bool IsTabPoppedOut(Guid tabId) => popoutWindows.ContainsKey(tabId);

    private void OpenPopoutTabInternal(MeterTab tab)
    {
        var window = new Gui.MainWindow.PopoutTabWindow(this, textureProvider, tab);
        this.windowSystem.AddWindow(window);
        window.IsOpen = true;
        popoutWindows[tab.Id] = window;
    }

    private void DrawUi() => this.windowSystem.Draw();

    private void OnTerritoryChanged(ushort territoryId)
    {
        var contentType = Content.ContentType;
        var contentName = Content.ContentName ?? "Unknown";
        this.pluginLog.Information($"Territory changed: {contentName} (ID: {territoryId}, Type: {contentType})");
    }

    private void OnCommand(string command, string arguments)
    {
        var args = arguments.Trim();
        if (string.IsNullOrWhiteSpace(args))
        {
            var newState = !this.mainWindow.IsOpen;
            this.mainWindow.IsOpen = newState;
            foreach (var popout in this.popoutWindows.Values)
                popout.SetVisible(newState);
        }
        else if (args.Equals("config", StringComparison.OrdinalIgnoreCase))
            this.configWindow.IsOpen = !this.configWindow.IsOpen;
        else if (args.StartsWith("toggle ", StringComparison.OrdinalIgnoreCase))
        {
            var groupName = args.Substring(7).Trim();
            if (groupName.Length > 0)
                TogglePopoutGroup(groupName);
        }
    }

    private void TogglePopoutGroup(string groupName)
    {
        var matching = new List<Gui.MainWindow.PopoutTabWindow>();
        foreach (var (tabId, window) in this.popoutWindows)
        {
            var tab = this.Config.MeterTabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null && !string.IsNullOrEmpty(tab.Group) &&
                tab.Group.Equals(groupName, StringComparison.OrdinalIgnoreCase))
            {
                matching.Add(window);
            }
        }

        if (matching.Count == 0)
        {
            this.pluginLog.Warning($"No popout group found matching '{groupName}'.");
            return;
        }

        var anyVisible = matching.Any(w => w.IsOpen);
        foreach (var window in matching)
            window.SetVisible(!anyVisible);
    }
}
