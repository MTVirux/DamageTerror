using Dalamud.Bindings.ImGui;
using DamageTerror.Gui.MainWindow;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public static class MeterTabsPage
{
    private static int selectedTabIndex = -1;
    private static string renameBuffer = string.Empty;

    public static bool Draw(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.CheckboxProp("Enable meter tabs", config.ShowTabBar, v => config.ShowTabBar = v);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, adds a tab bar to the main meter window.\nEach tab can filter and sort combatants independently.");

        if (!config.ShowTabBar)
        {
            ImGui.TextDisabled("Enable meter tabs above to configure them.");
            return changed;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var panelStartY = ImGui.GetCursorPosY();

        if (config.MeterTabs.Count == 0)
        {
            config.MeterTabs.Add(new MeterTab("DPS"));
            changed = true;
        }

        var avail = ImGui.GetContentRegionAvail();
        var listWidth = 160f;

        if (ImGui.BeginChild("##tabList", new Vector2(listWidth, avail.Y - ImGui.GetFrameHeightWithSpacing()), true))
        {
            for (var i = 0; i < config.MeterTabs.Count; i++)
            {
                var tab = config.MeterTabs[i];
                var isSelected = selectedTabIndex == i;
                var label = tab.IsHidden ? $"{tab.Name} (Hidden)" : tab.Name;
                if (tab.IsHidden)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
                if (ImGui.Selectable($"{label}##tab{i}", isSelected))
                {
                    selectedTabIndex = i;
                    renameBuffer = tab.Name;
                }
                if (tab.IsHidden)
                    ImGui.PopStyleColor();
            }
        }
        ImGui.EndChild();

        if (ImGui.Button("+##addTab"))
        {
            config.MeterTabs.Add(new MeterTab($"Tab {config.MeterTabs.Count + 1}"));
            selectedTabIndex = config.MeterTabs.Count - 1;
            renameBuffer = config.MeterTabs[selectedTabIndex].Name;
            changed = true;
        }

        ImGui.SameLine();
        var canRemove = config.MeterTabs.Count > 1;
        if (!canRemove) ImGui.BeginDisabled();
        if (ImGui.Button("-##removeTab") && selectedTabIndex >= 0 && selectedTabIndex < config.MeterTabs.Count)
        {            // Close popout window if this tab was popped out
            var removedTab = config.MeterTabs[selectedTabIndex];
            if (DamageTerrorPlugin.Instance.IsTabPoppedOut(removedTab.Id))
                DamageTerrorPlugin.Instance.ClosePopoutTab(removedTab.Id);
            config.MeterTabs.RemoveAt(selectedTabIndex);
            if (selectedTabIndex >= config.MeterTabs.Count)
                selectedTabIndex = config.MeterTabs.Count - 1;
            if (selectedTabIndex >= 0)
                renameBuffer = config.MeterTabs[selectedTabIndex].Name;
            changed = true;
        }
        if (!canRemove) ImGui.EndDisabled();

        ImGui.SameLine();
        var canDuplicate = selectedTabIndex >= 0 && selectedTabIndex < config.MeterTabs.Count;
        if (!canDuplicate) ImGui.BeginDisabled();
        if (ImGui.Button("D##dupTab"))
        {
            var clone = config.MeterTabs[selectedTabIndex].Clone();
            clone.Name += " (Copy)";
            config.MeterTabs.Insert(selectedTabIndex + 1, clone);
            selectedTabIndex++;
            renameBuffer = clone.Name;
            changed = true;
        }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Duplicate tab");
        if (!canDuplicate) ImGui.EndDisabled();

        ImGui.SameLine();
        var canMoveUp = selectedTabIndex > 0;
        if (!canMoveUp) ImGui.BeginDisabled();
        if (ImGui.Button("^##moveUp"))
        {
            var tmp = config.MeterTabs[selectedTabIndex];
            config.MeterTabs[selectedTabIndex] = config.MeterTabs[selectedTabIndex - 1];
            config.MeterTabs[selectedTabIndex - 1] = tmp;
            selectedTabIndex--;
            changed = true;
        }
        if (!canMoveUp) ImGui.EndDisabled();

        ImGui.SameLine();
        var canMoveDown = selectedTabIndex >= 0 && selectedTabIndex < config.MeterTabs.Count - 1;
        if (!canMoveDown) ImGui.BeginDisabled();
        if (ImGui.Button("v##moveDown"))
        {
            var tmp = config.MeterTabs[selectedTabIndex];
            config.MeterTabs[selectedTabIndex] = config.MeterTabs[selectedTabIndex + 1];
            config.MeterTabs[selectedTabIndex + 1] = tmp;
            selectedTabIndex++;
            changed = true;
        }
        if (!canMoveDown) ImGui.EndDisabled();

        ImGui.SameLine();
        var rightStart = ImGui.GetCursorPosX();
        ImGui.SetCursorPos(new Vector2(listWidth + ImGui.GetStyle().ItemSpacing.X, panelStartY));

        var rightWidth = avail.X - listWidth - ImGui.GetStyle().ItemSpacing.X;
        if (ImGui.BeginChild("##tabDetails", new Vector2(rightWidth, avail.Y), true))
        {
            if (selectedTabIndex >= 0 && selectedTabIndex < config.MeterTabs.Count)
            {
                changed |= DrawTabSettings(config.MeterTabs[selectedTabIndex]);
            }
            else
            {
                ImGui.TextDisabled("Select a tab from the list to configure it.");
            }
        }
        ImGui.EndChild();

        return changed;
    }

    private static bool DrawTabSettings(MeterTab tab)
    {
        var changed = false;
        changed |= MeterTabBasicsSection.Draw(tab, ref renameBuffer);
        ImGui.Separator();
        changed |= MeterTabSortSection.Draw(tab);
        ImGui.Separator();
        changed |= MeterTabContentSection.Draw(tab);
        ImGui.Separator();
        changed |= MeterTabStatusBarSection.Draw(tab);
        ImGui.Separator();
        changed |= MeterTabTooltipSection.Draw(tab);
        ImGui.Separator();
        changed |= MeterTabDetailsSection.Draw(tab);
        return changed;
    }

    public static bool DrawButtonAppearance(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Button Color", config.TabButtonColor, v => config.TabButtonColor = v);
        changed |= ConfigHelpers.ColorEditProp("Hovered Color", config.TabButtonHoveredColor, v => config.TabButtonHoveredColor = v);
        changed |= ConfigHelpers.ColorEditProp("Active Color", config.TabButtonActiveColor, v => config.TabButtonActiveColor = v);
        changed |= ConfigHelpers.ColorEditProp("Text Color", config.TabButtonTextColor, v => config.TabButtonTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Active Text Color", config.TabButtonActiveTextColor, v => config.TabButtonActiveTextColor = v);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.SliderFloatProp("Button Height", config.TabButtonHeight, 14f, 48f, "%.0f", v => config.TabButtonHeight = v, 150);
        changed |= ConfigHelpers.SliderFloatProp("Button Spacing", config.TabButtonSpacing, 0f, 16f, "%.0f", v => config.TabButtonSpacing = v, 150);
        changed |= ConfigHelpers.SliderFloatProp("Button Rounding", config.TabButtonRounding, 0f, 16f, "%.1f", v => config.TabButtonRounding = v, 150);
        changed |= ConfigHelpers.SliderFloatProp("Font Size", config.TabButtonFontSize, 6f, 40f, "%.1fpt", v => config.TabButtonFontSize = v, 150);

        ImGui.Spacing();

        changed |= ConfigHelpers.CheckboxProp("Stretch buttons to fill width", config.TabButtonStretchToFit, v => config.TabButtonStretchToFit = v);

        if (!config.TabButtonStretchToFit)
        {
            changed |= ConfigHelpers.SliderFloatProp("Button Width", config.TabButtonWidth, 20f, 300f, "%.0f", v => config.TabButtonWidth = v, 150);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Fixed width for each tab button.\nSet to 0 to auto-size based on text.");
        }
        }

        return changed;
    }
}
