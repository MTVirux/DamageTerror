using Dalamud.Interface.Windowing;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

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

    protected bool BeginPaddedContent(LayoutElement? lastVisibleEl)
    {
        var padLeft = plugin.Config.WindowPaddingLeft;
        var padRight = plugin.Config.WindowPaddingRight;
        var padTop = plugin.Config.WindowPaddingTop;
        var padBottom = plugin.Config.WindowPaddingBottom;
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
