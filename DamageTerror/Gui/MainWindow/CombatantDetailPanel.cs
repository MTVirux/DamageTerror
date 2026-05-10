using Dalamud.Bindings.ImGui;
using DamageTerror.Gui.MainWindow.Detail;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public sealed class CombatantDetailPanel
{
    private readonly Configuration config;
    private readonly DetailPanelState state = new();
    private readonly DetailsTabRenderer detailsTab;
    private readonly SkillsTabRenderer skillsTab;
    private readonly GraphTabRenderer graphTab;
    private readonly BuffsTabRenderer buffsTab;
    private readonly ItemsTabRenderer itemsTab;

    public CombatantDetailPanel(Configuration config, GraphDataTracker graphTracker,
        SkillTracker skillTracker, StatusTracker statusTracker)
    {
        this.config = config;
        detailsTab = new DetailsTabRenderer(config);
        skillsTab  = new SkillsTabRenderer(config, state);
        graphTab   = new GraphTabRenderer(config, graphTracker, skillTracker, state);
        buffsTab   = new BuffsTabRenderer(config, statusTracker);
        itemsTab   = new ItemsTabRenderer(config, skillTracker, state);
    }

    public void Toggle(string name) => state.Toggle(name);
    public bool IsExpanded(string name) => state.ExpandedName == name;
    public void CollapseAll() => state.CollapseAll();

    public void Render(RenderContext ctx, CombatantEntry combatant)
        => Render(combatant, ctx.Encounter, ctx.IsLive, ctx.ActiveTab);

    public void Render(CombatantEntry combatant, EncounterSnapshot? snapshot, bool isLive, MeterTab? activeTab = null)
    {
        if (state.ExpandedName != combatant.Name)
            return;

        var rctx = new DetailRenderContext
        {
            Combatant = combatant,
            Index = combatant.Name,
            Snapshot = snapshot,
            IsLive = isLive,
            ActiveTab = activeTab,
        };

        var panelStart = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);

        ImGui.Indent(config.DetailIndent);

        using var detailFont = FontScope.Push(config.GetFontScale(config.DetailFontSize));

        var showDetailsTab = activeTab?.DetailShowDetailsTab ?? config.DetailShowDetailsTab;
        var showSkillsTab = activeTab?.DetailShowSkillsTab ?? config.DetailShowSkillsTab;
        var showGraphTab = activeTab?.DetailShowGraphTab ?? config.DetailShowGraphTab;
        var showBuffsTab = activeTab?.DetailShowBuffsTab ?? config.DetailShowBuffsTab;
        var showItemTab = activeTab?.DetailShowItemTab ?? config.DetailShowItemTab;

        if (ImGui.BeginTabBar("##detailTabs", ImGuiTabBarFlags.Reorderable))
        {
            if (showDetailsTab && ImGui.BeginTabItem("Details##detail"))
            {
                detailsTab.Render(rctx);
                ImGui.EndTabItem();
            }

            if (showSkillsTab && ImGui.BeginTabItem("Skills##detail"))
            {
                skillsTab.Render(rctx);
                ImGui.EndTabItem();
            }

            if (showGraphTab && ImGui.BeginTabItem("Graph##detail"))
            {
                graphTab.Render(rctx);
                ImGui.EndTabItem();
            }

            if (showBuffsTab && ImGui.BeginTabItem("Buffs/Debuffs##detail"))
            {
                buffsTab.Render(rctx);
                ImGui.EndTabItem();
            }

            if (showItemTab && ImGui.BeginTabItem("Items##detail"))
            {
                itemsTab.Render(rctx);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        detailFont.Dispose();

        ImGui.Unindent(config.DetailIndent);

        var panelEnd = new Vector2(panelStart.X + ImGui.GetContentRegionAvail().X + config.DetailIndent, ImGui.GetCursorScreenPos().Y);
        drawList.ChannelsSetCurrent(0);
        drawList.AddRectFilled(panelStart, panelEnd, ImGui.ColorConvertFloat4ToU32(config.DetailBackgroundColor));
        drawList.ChannelsMerge();

        ImGui.Spacing();
    }
}
