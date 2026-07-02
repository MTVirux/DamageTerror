using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private readonly GeneralTab generalTab;
    private readonly AppearanceTab appearanceTab;
    private readonly EncounterHistoryTab historyTab;
    private readonly SampleDataPage sampleDataPage;

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
        SampleData,
#if DEBUG
        Debug,
#endif
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
        (ConfigPage.SampleData,  "Sample Data",            null,           FontAwesomeIcon.Flask),
#if DEBUG
        (ConfigPage.Debug,       "Debug",                  null,           FontAwesomeIcon.Bug),
#endif
    };

    public ConfigWindow(DamageTerrorPlugin plugin, PresetManager presetManager)
        : base("Damage Terror — Settings###DamageTerrorConfig")
    {
        this.plugin = plugin;
        this.generalTab = new GeneralTab(plugin);
        this.appearanceTab = new AppearanceTab(presetManager);
        this.historyTab = new EncounterHistoryTab(plugin);
        this.sampleDataPage = new SampleDataPage(plugin);
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
                changed |= AppearanceGeneralSection.Draw(config);
                break;

            case ConfigPage.Bars:
                changed |= BarsSection.Draw(config);
                break;

            case ConfigPage.NameFormat:
                changed |= NameFormatSection.Draw(config);
                break;

            case ConfigPage.Formatting:
                changed |= FormattingSection.Draw(config);
                break;

            case ConfigPage.TabButtons:
                changed |= MeterTabsPage.DrawButtonAppearance(config);
                break;

            case ConfigPage.SelectionBar:
                changed |= SelectionBarSection.Draw(config);
                break;

            case ConfigPage.Colors:
                changed |= ColorsSection.Draw(config);
                break;

            case ConfigPage.StatusBar:
                changed |= StatusBarSection.Draw(config);
                break;

            case ConfigPage.Tooltip:
                changed |= TooltipSection.Draw(config);
                break;

            case ConfigPage.Details:
                changed |= DetailsSection.Draw(config);
                break;

            case ConfigPage.GraphView:
                changed |= GraphViewSection.Draw(config);
                break;

            case ConfigPage.Font:
                changed |= FontSection.Draw(config, plugin.FontService, plugin.PluginInterface.UiBuilder);
                break;

            case ConfigPage.History:
                historyTab.Draw();
                break;

            case ConfigPage.SampleData:
                sampleDataPage.Draw();
                break;

#if DEBUG
            case ConfigPage.Debug:
                changed |= DebugSection.Draw(config);
                break;
#endif
        }

        return changed;
    }
}
