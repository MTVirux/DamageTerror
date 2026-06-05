using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public sealed class PopoutTabWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private readonly Guid tabId;
    private readonly EncounterHeaderComponent headerComponent;
    private readonly CombatantBarComponent barComponent;
    private readonly GraphViewComponent graphViewComponent;
    private readonly CombatantDetailPanel detailPanel;
    private readonly StatusBarComponent statusBarComponent;
    private TitleBarButton? lockButton;
    private MeterVisibilityState visibilityState;
    private bool wasDrawnLastFrame = true;
    private bool suppressClose;

    public Guid TabId => tabId;

    public PopoutTabWindow(DamageTerrorPlugin plugin, ITextureProvider textureProvider, MeterTab tab)
        : base($"DT \u2014 {tab.Name}##dtPopout_{tab.Id}")
    {
        this.plugin = plugin;
        this.tabId = tab.Id;

        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 150),
            MaximumSize = new Vector2(2000, 2000),
        };

        this.headerComponent = new EncounterHeaderComponent(plugin.DataService, plugin.Config);
        this.barComponent = new CombatantBarComponent(plugin.Config, textureProvider);
        this.graphViewComponent = new GraphViewComponent(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker);
        this.detailPanel = new CombatantDetailPanel(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker, plugin.DataService.StatusTracker);
        this.statusBarComponent = new StatusBarComponent(plugin.Config);

        var pinned = GetPin()?.Pinned ?? false;
        lockButton = new TitleBarButton
        {
            Icon = pinned ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
            IconOffset = new Vector2(3, 2),
            ShowTooltip = () => ImGui.SetTooltip("Lock window position and size"),
        };
        lockButton.Click = (m) =>
        {
            if (m == ImGuiMouseButton.Left)
            {
                var pin = GetOrCreatePin();
                if (!pin.Pinned)
                {
                    pin.Pos = ImGui.GetWindowPos();
                    pin.Size = ImGui.GetWindowSize();
                }

                pin.Pinned = !pin.Pinned;
                plugin.SaveConfig();
                lockButton!.Icon = pin.Pinned ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }
        };
        TitleBarButtons.Add(lockButton);
    }

    public void Dispose()
    {
    }

    private MeterTab? FindTab() => plugin.Config.MeterTabs.FirstOrDefault(t => t.Id == tabId);

    public override bool DrawConditions()
    {
        var ok = MeterWindowHelper.ShouldDraw(plugin.Config, ref visibilityState);
        if (!ok)
            wasDrawnLastFrame = false;
        return ok;
    }

    public bool WasDrawnLastFrame => wasDrawnLastFrame;

    public void RequestVisibilityOverride()
    {
        visibilityState.UserOverride = true;
        visibilityState.ObservedCombatSinceOverride = false;
    }

    private PopoutWindowPin? GetPin()
    {
        plugin.Config.PopoutWindowPins.TryGetValue(tabId, out var pin);
        return pin;
    }

    private PopoutWindowPin GetOrCreatePin()
    {
        if (!plugin.Config.PopoutWindowPins.TryGetValue(tabId, out var pin))
        {
            pin = new PopoutWindowPin();
            plugin.Config.PopoutWindowPins[tabId] = pin;
        }
        return pin;
    }

    public override void PreDraw()
    {
        RespectCloseHotkey = !plugin.Config.IgnoreEscClose;
        wasDrawnLastFrame = false;

        var forceShowHeader = MeterWindowHelper.IsModifierActive(plugin.Config);

        if (plugin.Config.HideWindowHeader && !forceShowHeader)
            Flags |= ImGuiWindowFlags.NoTitleBar;
        else
            Flags &= ~ImGuiWindowFlags.NoTitleBar;

        var pin = GetPin();
        if (pin is { Pinned: true })
        {
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

            if (pin.Pos.X > 1f && pin.Pos.Y > 1f && pin.Size.X > 1f && pin.Size.Y > 1f)
            {
                ImGui.SetNextWindowPos(pin.Pos);
                ImGui.SetNextWindowSize(pin.Size);
            }
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoResize;
        }

        if (lockButton != null)
            lockButton.Icon = (pin?.Pinned ?? false) ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;

        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, plugin.Config.WindowBackgroundColor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    public override void Draw()
    {
        wasDrawnLastFrame = true;

        var padLeft = plugin.Config.WindowPaddingLeft;
        var padRight = plugin.Config.WindowPaddingRight;
        var padTop = plugin.Config.WindowPaddingTop;
        var padBottom = plugin.Config.WindowPaddingBottom;

        var modifierActiveEarly = MeterWindowHelper.IsModifierActive(plugin.Config);
        LayoutElement? lastVisibleEl = null;
        foreach (var el in plugin.Config.Layout)
        {
            if (el == LayoutElement.MeterTabs) continue;
            if (plugin.Config.CtrlShiftOnlyElements.Contains(el) && !modifierActiveEarly)
                continue;
            lastVisibleEl = el;
        }
        var effectivePadBottom = lastVisibleEl == LayoutElement.StatusBar ? 0f : padBottom;

        ImGui.SetCursorPos(new Vector2(padLeft, ImGui.GetCursorPosY() + padTop));
        var avail = ImGui.GetContentRegionAvail();
        if (!ImGui.BeginChild("##paddedContent", new Vector2(avail.X - padRight, avail.Y - effectivePadBottom), false))
        {
            ImGui.EndChild();
            return;
        }

        using var fontScope = plugin.Config.EnableCustomFont ? plugin.FontService?.PushFont() : null;

        var config = plugin.Config;
        var tab = FindTab();
        if (tab == null)
        {
            IsOpen = false;
            ImGui.EndChild();
            return;
        }

        var encounter = headerComponent.SelectedEncounter;
        var currentPlayerName = !string.IsNullOrEmpty(encounter?.PlayerName)
            ? encounter.PlayerName
            : plugin.DataService.PlayerName;

        var sortBy = tab.SortBy;
        var sortDesc = tab.SortDescending;

        HashSet<string>? partyNames = null;
        HashSet<string>? allianceNames = null;
        if (tab.GroupFilter is GroupFilter.Party or GroupFilter.Alliance)
        {
            partyNames = plugin.PartyService.GetPartyMemberNames();
            allianceNames = plugin.PartyService.GetAllianceMemberNames();
        }

        List<CombatantEntry>? combatants = null;
        double maxVal = 0;

        if (encounter != null)
        {
            combatants = MainWindow.GetSortedCombatants(encounter, sortBy, sortDesc, tab, partyNames, allianceNames);
            if (combatants.Count > 0)
                maxVal = combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy));
        }

        var skipElements = new HashSet<LayoutElement> { LayoutElement.MeterTabs };
        var afterBarsHeight = MeterWindowHelper.CalculateAfterBarsHeight(
            config, statusBarComponent.GetHeight, headerComponent.GetHeight,
            encounter != null, false, skipElements: skipElements);

        if (!plugin.DataService.IsConnected && encounter == null)
        {
            if (!plugin.DataService.DisconnectNoticeDismissed)
            {
                ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
                if (ImGui.Button("Reconnect##disconnect-notice-popout"))
                    Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
                ImGui.SameLine();
                if (ImGui.Button("Dismiss##disconnect-notice-popout"))
                    plugin.DataService.DismissDisconnectNotice();
            }
            ImGui.EndChild();
            return;
        }

        var ctx = new MeterWindowContext
        {
            Config = config,
            Encounter = encounter,
            ActiveTab = tab,
            SortBy = sortBy,
            SortDescending = sortDesc,
            CurrentPlayerName = currentPlayerName,
            Combatants = combatants,
            MaxVal = maxVal,
            GroupAggregates = null,
            AfterBarsHeight = afterBarsHeight,
            UseTabBar = false,
            IsViewingLive = headerComponent.IsViewingLive,
            ChildId = "##popoutCombatants",
            DrawReplayBar = null,
            DrawMeterTabButtons = null,
            HeaderComponent = headerComponent,
            BarComponent = barComponent,
            GraphViewComponent = graphViewComponent,
            DetailPanel = detailPanel,
            StatusBarComponent = statusBarComponent,
            IsConnected = plugin.DataService.IsConnected,
            DisconnectNoticeDismissed = plugin.DataService.DisconnectNoticeDismissed,
            SpawnReconnect = () => Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false)),
            DismissDisconnectNotice = () => plugin.DataService.DismissDisconnectNotice(),
            ReconnectButtonIdSuffix = "-popout",
        };

        var earlyReturn = false;
        MeterWindowHelper.RenderLayoutElements(ref ctx, ref earlyReturn);
        if (earlyReturn)
        {
            ImGui.EndChild();
            return;
        }

        ImGui.EndChild();
    }

    public void SetVisible(bool visible)
    {
        if (!visible)
            suppressClose = true;
        IsOpen = visible;
    }

    public override void OnClose()
    {
        if (suppressClose)
        {
            suppressClose = false;
            return;
        }
        plugin.ClosePopoutTab(tabId);
    }
}
