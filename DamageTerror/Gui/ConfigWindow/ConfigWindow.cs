using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class ConfigWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private readonly GeneralTab generalTab;
    private readonly AppearanceTab appearanceTab;
    private readonly EncounterHistoryTab historyTab;

    private ConfigPage selectedPage = ConfigPage.General;

    private enum ConfigPage
    {
        General,
        Tabs,
        Layout,
        Presets,
        AppearanceGeneral,
        Bars,
        NameFormat,
        TabButtons,
        SelectionBar,
        Colors,
        StatusBar,
        Tooltip,
        Details,
        GraphView,
        Font,
        Formatting,
        History,
    }

    private static readonly (ConfigPage Page, string Label, string? Group, FontAwesomeIcon Icon)[] PageEntries =
    {
        (ConfigPage.General,      "General",                null,           FontAwesomeIcon.Cog),
        (ConfigPage.Tabs,         "Tabs",                   null,           FontAwesomeIcon.Columns),
        (ConfigPage.Layout,       "Layout",                 null,           FontAwesomeIcon.ThLarge),
        (ConfigPage.Presets,      "Presets",                "Appearance",   FontAwesomeIcon.Palette),
        (ConfigPage.AppearanceGeneral, "General",             "Appearance",   FontAwesomeIcon.SlidersH),
        (ConfigPage.Bars,         "Meter Bars",             "Appearance",   FontAwesomeIcon.GripLines),
        (ConfigPage.NameFormat,   "Name Format",            "Appearance",   FontAwesomeIcon.IdCard),
        (ConfigPage.Formatting,   "Value Formatting",       "Appearance",   FontAwesomeIcon.SortNumericDown),
        (ConfigPage.TabButtons,   "Tab Buttons",            "Appearance",   FontAwesomeIcon.HandPointer),
        (ConfigPage.SelectionBar, "Encounter Select",       "Appearance",   FontAwesomeIcon.ArrowsAltH),
        (ConfigPage.Colors,       "Job/Role Colors",        "Appearance",   FontAwesomeIcon.FillDrip),
        (ConfigPage.StatusBar,    "Encounter Status Bar",   "Appearance",   FontAwesomeIcon.InfoCircle),
        (ConfigPage.Tooltip,      "Tooltips",               "Appearance",   FontAwesomeIcon.Comment),
        (ConfigPage.Details,      "Details Panel",          "Appearance",   FontAwesomeIcon.ChartBar),
        (ConfigPage.GraphView,    "Graph View",             "Appearance",   FontAwesomeIcon.ChartLine),
        (ConfigPage.Font,         "Fonts",                  "Appearance",   FontAwesomeIcon.Font),
        (ConfigPage.History,      "History",                null,           FontAwesomeIcon.History),
    };

    public ConfigWindow(DamageTerrorPlugin plugin, PresetManager presetManager)
        : base("Damage Terror — Settings")
    {
        this.plugin = plugin;
        this.generalTab = new GeneralTab(plugin);
        this.appearanceTab = new AppearanceTab(presetManager);
        this.historyTab = new EncounterHistoryTab(plugin);
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(620, 480),
            MaximumSize = new Vector2(1100, 900),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var config = plugin.Config;
        var changed = false;

        var sidebarWidth = 170f * ImGui.GetIO().FontGlobalScale;
        var avail = ImGui.GetContentRegionAvail();

        if (ImGui.BeginChild("##sidebar", new Vector2(sidebarWidth, avail.Y), true))
        {
            DrawSidebar();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        if (ImGui.BeginChild("##content", new Vector2(0, avail.Y), true))
        {
            changed |= DrawContentPage(config);
        }
        ImGui.EndChild();

        if (changed)
        {
            plugin.SaveConfig();
        }

        AppearanceTab.FileDialogManager.Draw();
    }

    private void DrawSidebar()
    {
        string? lastGroup = null;
        var iconWidth = ImGui.CalcTextSize("W").X + 4f;

        foreach (var (page, label, group, icon) in PageEntries)
        {
            if (group != lastGroup)
            {
                if (lastGroup != null)
                    ImGui.Spacing();

                if (group != null)
                {
                    ImGui.Spacing();
                    ImGui.TextDisabled(group);
                    ImGui.Separator();
                }
                else if (lastGroup != null)
                {
                    ImGui.Separator();
                }

                lastGroup = group;
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = icon.ToIconString();
            var iconTextSize = ImGui.CalcTextSize(iconStr);
            ImGui.PopFont();

            var isSelected = selectedPage == page;
            var cursorPos = ImGui.GetCursorPos();
            var selectableWidth = ImGui.GetContentRegionAvail().X;

            if (ImGui.Selectable($"##page_{page}", isSelected, ImGuiSelectableFlags.None, new Vector2(selectableWidth, ImGui.GetFrameHeight())))
            {
                selectedPage = page;
            }

            var afterCursor = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cursorPos.X + 4f, cursorPos.Y + (ImGui.GetFrameHeight() - iconTextSize.Y) * 0.5f));
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextUnformatted(iconStr);
            ImGui.PopFont();

            ImGui.SetCursorPos(new Vector2(cursorPos.X + 4f + iconWidth + 6f, cursorPos.Y + (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) * 0.5f));
            ImGui.TextUnformatted(label);

            ImGui.SetCursorPos(afterCursor);
        }
    }

    private bool DrawContentPage(Configuration config)
    {
        var changed = false;

        switch (selectedPage)
        {
            case ConfigPage.General:
                changed |= generalTab.Draw(config);
                break;

            case ConfigPage.Tabs:
                changed |= MeterTabsPage.Draw(config);
                break;

            case ConfigPage.Layout:
                changed |= LayoutPage.Draw(config);
                break;

            case ConfigPage.Presets:
                changed |= appearanceTab.DrawPresetsPage(config);
                break;

            case ConfigPage.AppearanceGeneral:
                changed |= AppearanceTab.DrawAppearanceGeneralPage(config);
                break;

            case ConfigPage.Bars:
                changed |= AppearanceTab.DrawBarsPage(config);
                break;

            case ConfigPage.NameFormat:
                changed |= AppearanceTab.DrawNameFormatPage(config);
                break;

            case ConfigPage.Formatting:
                changed |= AppearanceTab.DrawFormattingPage(config);
                break;

            case ConfigPage.TabButtons:
                changed |= MeterTabsPage.DrawButtonAppearance(config);
                break;

            case ConfigPage.SelectionBar:
                changed |= AppearanceTab.DrawSelectionBarPage(config);
                break;

            case ConfigPage.Colors:
                changed |= AppearanceTab.DrawColorsPage(config);
                break;

            case ConfigPage.StatusBar:
                changed |= AppearanceTab.DrawStatusBarPage(config);
                break;

            case ConfigPage.Tooltip:
                changed |= AppearanceTab.DrawTooltipPage(config);
                break;

            case ConfigPage.Details:
                changed |= AppearanceTab.DrawDetailsPage(config);
                break;

            case ConfigPage.GraphView:
                changed |= AppearanceTab.DrawGraphViewPage(config);
                break;

            case ConfigPage.Font:
                changed |= AppearanceTab.DrawFontPage(config, plugin.FontService, plugin.PluginInterface.UiBuilder);
                break;

            case ConfigPage.History:
                historyTab.Draw();
                break;
        }

        return changed;
    }
}
