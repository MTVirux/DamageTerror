using Dalamud.Interface.Windowing;

namespace DamageTerror.Gui.MainWindow;

public abstract class MeterWindowBase : Window
{
    protected readonly DamageTerrorPlugin plugin;
    protected readonly EncounterHeaderComponent headerComponent;
    protected readonly CombatantBarComponent barComponent;
    protected readonly GraphViewComponent graphViewComponent;
    protected readonly CombatantDetailPanel detailPanel;
    protected readonly StatusBarComponent statusBarComponent;
    protected TitleBarButton? lockButton;
    private MeterVisibilityState visibilityState;
    protected bool wasDrawnLastFrame = true;

    protected MeterWindowBase(DamageTerrorPlugin plugin, ITextureProvider textureProvider, string name)
        : base(name)
    {
        this.plugin = plugin;
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
    }

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

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    protected bool BeginPaddedContent(bool honorReplayBarPin, HashSet<LayoutElement>? layoutSkip = null)
    {
        var config = plugin.Config;
        var modifierActive = MeterWindowHelper.IsModifierActive(config);
        LayoutElement? lastVisibleEl = null;
        foreach (var el in config.Layout)
        {
            if (layoutSkip?.Contains(el) == true) continue;
            if (config.CtrlShiftOnlyElements.Contains(el) && !modifierActive
                && !(honorReplayBarPin && el == LayoutElement.ReplayBar && config.ReplayBarPinned))
                continue;
            lastVisibleEl = el;
        }

        var padLeft = config.WindowPaddingLeft;
        var padRight = config.WindowPaddingRight;
        var padTop = config.WindowPaddingTop;
        var padBottom = config.WindowPaddingBottom;
        var effectivePadBottom = lastVisibleEl == LayoutElement.StatusBar ? 0f : padBottom;

        ImGui.SetCursorPos(new Vector2(padLeft, ImGui.GetCursorPosY() + padTop));
        var avail = ImGui.GetContentRegionAvail();
        return ImGui.BeginChild("##paddedContent", new Vector2(avail.X - padRight, avail.Y - effectivePadBottom), false);
    }

    protected bool DrawDisconnectNoticeIfNeeded(EncounterSnapshot? encounter, string idSuffix, Action spawnReconnect)
    {
        if (plugin.DataService.IsConnected || encounter != null)
            return false;

        if (!plugin.DataService.DisconnectNoticeDismissed)
            MeterWindowHelper.DrawDisconnectNotice(idSuffix, spawnReconnect, plugin.DataService.DismissDisconnectNotice);
        return true;
    }
}
