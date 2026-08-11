using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using ECommons;
using KamiToolKit;

namespace DamageTerror.Core;

public sealed class DamageTerrorPlugin : IDalamudPlugin, IDisposable
{
    public static DamageTerrorPlugin Instance { get; private set; } = null!;

    public IDalamudPluginInterface PluginInterface { get; init; }

    public Configuration Config { get; private set; } = new();

    public DataService DataService { get; private set; } = null!;

    public PartyMembershipService PartyService { get; private set; } = null!;

    public FontService FontService { get; private set; } = null!;

    public ConfigBackupService ConfigBackup { get; private set; } = null!;

    private const string CommandName = "/dt";

    private readonly WindowSystem windowSystem = new(typeof(DamageTerrorPlugin).AssemblyQualifiedName);
    private readonly Gui.MainWindow.MainWindow mainWindow;
    private readonly Gui.ConfigWindow.ConfigWindow configWindow;
    private readonly Gui.SetupWizard.FirstRunWindow firstRunWindow;
    private readonly Gui.SetupWizard.CustomizationWizardWindow customizationWizardWindow;
    private readonly Gui.SetupWizard.ColumnWizardWindow columnWizardWindow;
    private readonly ICommandManager commandManager;
    private readonly IPluginLog pluginLog;
    private readonly ITextureProvider textureProvider;
    private readonly Dictionary<Guid, Gui.MainWindow.PopoutTabWindow> popoutWindows = new();
    private readonly PartyListDpsOverlay partyListOverlay;
    private readonly Gui.PartyListConfigWindow partyListConfigWindow;
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

        this.ConfigBackup = new ConfigBackupService(pluginInterface.ConfigFile.FullName, pluginLog);

        Configuration? cfg = null;
        try
        {
            cfg = this.PluginInterface.GetPluginConfig() as Configuration;
        }
        catch (Exception ex)
        {
            ServiceManager.LogError(LogChannel.Plugin, ex, "Failed to load plugin config (possibly outdated enum values).");

            // Snapshot the broken file for forensics, then try .bak before
            // falling back to defaults. The user only loses customisation
            // when both the live config AND the backup are unreadable.
            var brokenPath = this.ConfigBackup.SaveBrokenSnapshot();
            if (brokenPath != null)
                ServiceManager.LogWarning(LogChannel.Plugin, $"Saved broken config to {brokenPath}");

            if (this.ConfigBackup.HasBackup() && this.ConfigBackup.RestoreFromFile(this.ConfigBackup.BackupPath))
            {
                try
                {
                    cfg = this.PluginInterface.GetPluginConfig() as Configuration;
                    if (cfg != null)
                        ServiceManager.LogWarning(LogChannel.Plugin, "Recovered config from automatic backup.");
                }
                catch (Exception bex)
                {
                    ServiceManager.LogError(LogChannel.Plugin, bex, "Backup config also failed to load. Falling back to defaults.");
                    cfg = null;
                }
            }
            else
            {
                ServiceManager.LogWarning(LogChannel.Plugin, "No automatic backup available; falling back to defaults.");
            }
        }

        var isFreshInstall = cfg == null;
        if (cfg == null)
        {
            cfg = new();
            this.PluginInterface.SavePluginConfig(cfg);
        }

        this.Config = cfg;
        this.Config.Save = this.SaveConfig;
        ServiceManager.Config = this.Config;
        Gui.ConfigWindow.LayoutPage.EnsureLayoutComplete(cfg);

        // Seed an initial .bak so a first-launch user with no prior backup
        // still has a recovery point if their next save somehow corrupts.
        this.ConfigBackup.WriteBackupFromLiveConfig(force: true);

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

        if (cfg.Version < 4)
        {
            // Installs that predate the setup wizard should never be nagged by it.
            if (!isFreshInstall)
                cfg.HasCompletedSetup = true;
            cfg.Version = 4;
        }

        if (cfg.Version < 5)
        {
            // Party list integration is off for everyone while it's still being
            // worked on, including installs that already had it on.
            cfg.ShowPartyListDps = false;
            cfg.Version = 5;
        }

        if (cfg.Version != loadedVersion)
            this.PluginInterface.SavePluginConfig(cfg);

        this.DataService = new DataService(pluginInterface, pluginLog, this.Config);

        this.PartyService = new PartyMembershipService();

        KamiToolKitLibrary.Initialize(pluginInterface);
        this.partyListOverlay = new PartyListDpsOverlay(this.DataService, this.Config);

        // Plugin construction runs on a task, not the framework thread, and KamiToolKit
        // asserts the framework thread when it touches the addon. Command handlers are
        // already on it, so this is the only caller that needs marshalling.
        if (this.Config.ShowPartyListDps)
            framework.RunOnFrameworkThread(() => this.partyListOverlay.SetEnabled(true));

        this.FontService = new FontService(this.Config, pluginLog);
        if (this.Config.EnableCustomFont)
            this.FontService.Initialize(pluginInterface.UiBuilder);

        var presetManager = new PresetManager(
            pluginInterface.ConfigDirectory.FullName, pluginLog);

#if DEBUG
        ThemePropertyMirror.SelfCheckOrThrow(BuiltInPresets.Default(), pluginLog);
        ThemePropertyMirror.CheckDefaultsMatchConfigOrThrow(pluginLog);
#endif

        this.mainWindow = new Gui.MainWindow.MainWindow(this, textureProvider);
        this.configWindow = new Gui.ConfigWindow.ConfigWindow(this, presetManager);
        this.firstRunWindow = new Gui.SetupWizard.FirstRunWindow(this, presetManager);
        this.customizationWizardWindow = new Gui.SetupWizard.CustomizationWizardWindow(this);
        this.columnWizardWindow = new Gui.SetupWizard.ColumnWizardWindow(this);

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);
        this.windowSystem.AddWindow(this.firstRunWindow);
        this.windowSystem.AddWindow(this.customizationWizardWindow);
        this.windowSystem.AddWindow(this.columnWizardWindow);

        this.partyListConfigWindow = new Gui.PartyListConfigWindow(this);
        this.windowSystem.AddWindow(this.partyListConfigWindow);

        this.PluginInterface.UiBuilder.Draw += this.DrawUi;
        this.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        this.PluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;

        Svc.ClientState.TerritoryChanged += this.OnTerritoryChanged;

        this.commandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Toggle the meter window. Subcommands: config, toggle <group>, partylist, partylist config",
        });

        this.mainWindow.IsOpen = this.Config.ShowOnStart;
        this.firstRunWindow.IsOpen = !this.Config.HasCompletedSetup;

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
                ServiceManager.LogError(LogChannel.Plugin, $"Failed to start data service: {ex.Message}");
            }
        });
    }

    public static string Name => "Damage Terror";

    public void OpenMainUi()
    {
        this.mainWindow.IsOpen = true;
        this.mainWindow.RequestVisibilityOverride();
    }

    public void OpenConfigUi() => this.configWindow.IsOpen = true;

    public void OpenSetupWizard(bool takeOverSampleData = false)
    {
        this.firstRunWindow.Restart();
        if (takeOverSampleData)
            this.firstRunWindow.AdoptSampleOwnership();
        this.firstRunWindow.IsOpen = true;
    }

    public void OpenCustomizationWizard(bool takeOverSampleData = false)
    {
        this.customizationWizardWindow.Restart();
        if (takeOverSampleData)
            this.customizationWizardWindow.AdoptSampleOwnership();
        this.customizationWizardWindow.IsOpen = true;
    }

    public void OpenColumnWizard(bool takeOverSampleData = false)
    {
        this.columnWizardWindow.Restart();
        if (takeOverSampleData)
            this.columnWizardWindow.AdoptSampleOwnership();
        this.columnWizardWindow.IsOpen = true;
    }

    public void SelectMeterTab(int index) => this.mainWindow.SelectTab(index);

    /// <summary>Called from the config window, which draws on the framework thread.</summary>
    public void SetPartyListOverlayEnabled(bool value) => this.partyListOverlay.SetEnabled(value);

    public void ResyncPartyListNames() => this.partyListOverlay.ResyncNameText();

    public void SaveConfig()
    {
        this.PluginInterface.SavePluginConfig(this.Config);
        // Throttled inside the service — color picker drags etc. won't actually
        // hit disk for the .bak more than once per cooldown window.
        this.ConfigBackup.WriteBackupFromLiveConfig();
    }

    public void Dispose()
    {
        if (this.disposed) return;
        this.disposed = true;

        // Detach UI/event handlers FIRST so no Draw or callback can fire after
        // we start tearing down the services they reach into. The previous
        // order disposed DataService before unhooking Draw, leaving a window
        // where a Draw tick could dereference a disposed service.
        SafeDispose(() => this.PluginInterface.UiBuilder.Draw -= this.DrawUi);
        SafeDispose(() => this.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi);
        SafeDispose(() => this.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi);
        SafeDispose(() => Svc.ClientState.TerritoryChanged -= this.OnTerritoryChanged);
        SafeDispose(() => this.commandManager.RemoveHandler(CommandName));

        SafeDispose(() => this.windowSystem.RemoveAllWindows());
        SafeDispose(() => this.firstRunWindow.CleanupOwnedSampleData());
        SafeDispose(() => this.customizationWizardWindow.CleanupOwnedSampleData());
        SafeDispose(() => this.columnWizardWindow.CleanupOwnedSampleData());

        foreach (var popout in this.popoutWindows.Values)
            SafeDispose(() => popout.Dispose());
        this.popoutWindows.Clear();

        // Native nodes must go while the party list addon is still alive, and the
        // library must go after every node it owns.
        SafeDispose(() => this.partyListOverlay.Dispose());
        SafeDispose(KamiToolKitLibrary.Dispose);

        SafeDispose(() => this.mainWindow.Dispose());
        SafeDispose(() => this.configWindow.Dispose());
        SafeDispose(() => this.FontService.Dispose());
        SafeDispose(() => this.DataService.Dispose());

        // Save last so a config write can't race a Draw tick that reads from it.
        SafeDispose(() => this.PluginInterface.SavePluginConfig(this.Config));

        ECommonsMain.Dispose();
    }

    private void SafeDispose(Action action)
    {
        try { action(); }
        catch (Exception ex) { ServiceManager.LogError(LogChannel.Plugin, $"Error during disposal: {ex.Message}"); }
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

    private void OnTerritoryChanged(uint territoryId)
    {
        var contentType = Content.ContentType;
        var contentName = Content.ContentName ?? "Unknown";
        ServiceManager.LogDebug(LogChannel.Plugin, $"Territory changed: {contentName} (ID: {territoryId}, Type: {contentType})");
    }

    private void OnCommand(string command, string arguments)
    {
        var args = arguments.Trim();
        if (string.IsNullOrWhiteSpace(args))
        {
            var visible = this.mainWindow.IsOpen && this.mainWindow.WasDrawnLastFrame;
            if (visible)
            {
                this.mainWindow.IsOpen = false;
                foreach (var popout in this.popoutWindows.Values)
                    popout.SetVisible(false);
            }
            else
            {
                this.mainWindow.IsOpen = true;
                this.mainWindow.RequestVisibilityOverride();
                foreach (var popout in this.popoutWindows.Values)
                {
                    popout.SetVisible(true);
                    popout.RequestVisibilityOverride();
                }
            }
        }
        else if (args.Equals("config", StringComparison.OrdinalIgnoreCase))
            this.configWindow.IsOpen = !this.configWindow.IsOpen;
        else if (args.Equals("partylist config", StringComparison.OrdinalIgnoreCase))
            this.partyListConfigWindow.IsOpen = !this.partyListConfigWindow.IsOpen;
        else if (args.Equals("partylist", StringComparison.OrdinalIgnoreCase))
        {
            this.Config.ShowPartyListDps = !this.Config.ShowPartyListDps;
            this.partyListOverlay.SetEnabled(this.Config.ShowPartyListDps);
            this.SaveConfig();
            Svc.Chat.Print($"[Damage Terror] Party list DPS {(this.Config.ShowPartyListDps ? "enabled" : "disabled")}.");
        }
        else if (args.StartsWith("toggle ", StringComparison.OrdinalIgnoreCase))
        {
            var groupName = args[7..].Trim();
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
            ServiceManager.LogWarning(LogChannel.Plugin, $"No popout group found matching '{groupName}'.");
            return;
        }

        var anyVisible = matching.Any(w => w.IsOpen);
        foreach (var window in matching)
        {
            window.SetVisible(!anyVisible);
            if (!anyVisible)
                window.RequestVisibilityOverride();
        }
    }
}
