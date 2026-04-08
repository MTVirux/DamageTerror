using Dalamud.Bindings.ImGui;
using DamageTerror.Enums;
using DamageTerror.Gui.MainWindow;
using DamageTerror.Helpers;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public static class MeterTabsPage
{
    private static int selectedTabIndex = -1;
    private static string renameBuffer = string.Empty;

    private static readonly string[] FilterModeLabels =
    {
        "All",
        "Tanks",
        "Healers",
        "DPS (All)",
        "Melee DPS",
        "Ranged DPS",
        "Caster DPS",
        "Deaths Only",
        "Custom Jobs",
    };

    private static readonly string[] GroupFilterLabels =
    {
        "All",
        "Solo",
        "Party Only",
        "Alliance",
    };

    public static bool Draw(Configuration config)
    {
        var changed = false;

        var showTabBar = config.ShowTabBar;
        if (ImGui.Checkbox("Enable meter tabs", ref showTabBar))
        {
            config.ShowTabBar = showTabBar;
            changed = true;
        }

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

        if (ImGui.CollapsingHeader("Tab Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
        ImGui.Spacing();

        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Name", ref renameBuffer, 64))
        {
            tab.Name = renameBuffer;
            changed = true;
        }

        var groupBuffer = tab.Group ?? "";
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("Group", ref groupBuffer, 64))
        {
            tab.Group = groupBuffer;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Assign this tab to a group.\nTabs with the same group string are grouped together.");

        var isHidden = tab.IsHidden;
        if (ImGui.Checkbox("Hidden", ref isHidden))
        {
            tab.IsHidden = isHidden;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hide this tab from the tab bar.\nThe tab still exists and can be used for popout windows.");

        ImGui.Spacing();

        var filterIdx = (int)tab.FilterMode;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Role Filter", ref filterIdx, FilterModeLabels, FilterModeLabels.Length))
        {
            tab.FilterMode = (TabFilterMode)filterIdx;
            changed = true;
        }

        if (tab.FilterMode == TabFilterMode.Custom)
        {
            ImGui.Spacing();
            changed |= DrawCustomJobFilter(tab);
        }

        var groupFilterIdx = (int)tab.GroupFilter;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Group Filter", ref groupFilterIdx, GroupFilterLabels, GroupFilterLabels.Length))
        {
            tab.GroupFilter = (GroupFilter)groupFilterIdx;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Filter by party membership.\nSolo = only you.\nParty Only = your party members.\nAlliance = all alliance members.\nCombines with Role Filter above.");

        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Sort", ImGuiTreeNodeFlags.None))
        {
        var sortOptions = Enum.GetNames(typeof(SortField));
        var currentSort = (int)tab.SortBy;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Sort by", ref currentSort, sortOptions, sortOptions.Length))
        {
            tab.SortBy = (SortField)currentSort;
            changed = true;
        }

        var sortDesc = tab.SortDescending;
        if (ImGui.Checkbox("Descending (highest first)", ref sortDesc))
        {
            tab.SortDescending = sortDesc;
            changed = true;
        }

        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Meter/Graph Content", ImGuiTreeNodeFlags.None))
        {
        var viewModeLabels = new[] { "Bars", "Line Graph" };
        var viewModeIdx = (int)tab.ViewMode;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("View Mode", ref viewModeIdx, viewModeLabels, viewModeLabels.Length))
        {
            tab.ViewMode = (ViewMode)viewModeIdx;
            changed = true;
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Switch between traditional bars and a line graph overlay.\nGraph mode plots metrics over time for all combatants.");

        if (tab.ViewMode == ViewMode.LineGraph)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Graph Lines");

            var showDps = tab.GraphShowDpsLine;
            if (ImGui.Checkbox("Show DPS Line", ref showDps))
            {
                tab.GraphShowDpsLine = showDps;
                changed = true;
            }

            var showHps = tab.GraphShowHpsLine;
            if (ImGui.Checkbox("Show HPS Line", ref showHps))
            {
                tab.GraphShowHpsLine = showHps;
                changed = true;
            }

            var showDtps = tab.GraphShowDtpsLine;
            if (ImGui.Checkbox("Show DTPS Line", ref showDtps))
            {
                tab.GraphShowDtpsLine = showDtps;
                changed = true;
            }
        }

        if (tab.ViewMode != ViewMode.LineGraph)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Columns");

            tab.ColumnOrder ??= new List<BarColumn>();
            CombatantBarComponent.EnsureColumnOrderComplete(tab.ColumnOrder);

            var enabledCols = tab.ColumnOrder.Where(c => tab.IsColumnVisible(c)).ToList();

            Func<BarColumn, bool> barColExtras = col =>
            {
                var extChanged = false;

                ImGui.SameLine();
                var defaultLabel = Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
                tab.ColumnHeaderLabels.TryGetValue(col, out var currentHeader);
                currentHeader ??= "";
                ImGui.SetNextItemWidth(60);
                if (ImGui.InputTextWithHint($"##hdr_{col}", defaultLabel, ref currentHeader, 32))
                {
                    if (string.IsNullOrEmpty(currentHeader))
                        tab.ColumnHeaderLabels.Remove(col);
                    else
                        tab.ColumnHeaderLabels[col] = currentHeader;
                    extChanged = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(MetricPicker.GetBarColumnLabel(col));

                if (tab.ColumnFormatOverrides != null && ColumnFormatOverride.SupportsFormatting(col))
                {
                    ImGui.SameLine();
                    var hasOverride = tab.ColumnFormatOverrides.ContainsKey(col);
                    if (hasOverride)
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.8f, 1.0f, 1.0f));
                    if (ImGui.SmallButton($"F##fmt_{col}"))
                        ImGui.OpenPopup($"##fmtPopup_{col}");
                    if (hasOverride)
                        ImGui.PopStyleColor();
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(hasOverride ? "Custom format (click to edit)" : "Set custom format");

                    if (ImGui.BeginPopup($"##fmtPopup_{col}"))
                    {
                        extChanged |= DisplayTab.DrawColumnFormatPopup(col, tab.ColumnFormatOverrides);
                        ImGui.EndPopup();
                    }
                }

                ImGui.SameLine();
                var hasColor = tab.ColumnValueColors.ContainsKey(col);
                if (hasColor)
                    ImGui.PushStyleColor(ImGuiCol.Text, tab.ColumnValueColors[col]);
                if (ImGui.SmallButton($"C##clr_{col}"))
                    ImGui.OpenPopup($"##clrPopup_{col}");
                if (hasColor)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasColor ? "Custom value color (click to edit)" : "Set custom value color");
                if (ImGui.BeginPopup($"##clrPopup_{col}"))
                {
                    extChanged |= DrawColumnColorPopup(col, tab.ColumnValueColors);
                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                var hasWidth = tab.ColumnWidthOverrides.ContainsKey(col);
                if (hasWidth)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
                if (ImGui.SmallButton($"W##wid_{col}"))
                    ImGui.OpenPopup($"##widPopup_{col}");
                if (hasWidth)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasWidth ? "Custom width (click to edit)" : "Set custom column width");
                if (ImGui.BeginPopup($"##widPopup_{col}"))
                {
                    extChanged |= DrawColumnWidthPopup(col, tab.ColumnWidthOverrides);
                    ImGui.EndPopup();
                }

                return extChanged;
            };

            if (MetricPicker.Draw("barCols", enabledCols,
                MetricPicker.GetBarColumnLabel,
                MetricPicker.BarColumnCategories,
                barColExtras,
                c => MetricPicker.BarColumnDescriptions.GetValueOrDefault(c)))
            {
                var newEnabledSet = new HashSet<BarColumn>(enabledCols);
                var disabledOrder = tab.ColumnOrder.Where(c => !newEnabledSet.Contains(c)).ToList();
                tab.ColumnOrder.Clear();
                tab.ColumnOrder.AddRange(enabledCols);
                tab.ColumnOrder.AddRange(disabledOrder);
                CombatantBarComponent.EnsureColumnOrderComplete(tab.ColumnOrder);
                tab.VisibleColumns = newEnabledSet;
                changed = true;
            }
        }

        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Status Bar Content", ImGuiTreeNodeFlags.None))
        {
        var sbTimer = tab.ShowStatusBarTimer;
        if (ImGui.Checkbox("Show combat timer##sbtab", ref sbTimer))
        {
            tab.ShowStatusBarTimer = sbTimer;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Metrics");
        tab.StatusBarMetrics ??= new List<BarColumn> { BarColumn.Dps, BarColumn.EncDps };
        Func<BarColumn, bool> sbExtras = col =>
        {
            var extChanged = false;
            ImGui.SameLine();
            var defaultLabel = Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            tab.StatusBarMetricLabels.TryGetValue(col, out var current);
            current ??= "";
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputTextWithHint($"##sbLbl_{col}", defaultLabel, ref current, 32))
            {
                if (string.IsNullOrEmpty(current))
                    tab.StatusBarMetricLabels.Remove(col);
                else
                    tab.StatusBarMetricLabels[col] = current;
                extChanged = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(MetricPicker.GetBarColumnLabel(col));
            ImGui.SameLine();
            var hasColor = tab.ColumnValueColors.ContainsKey(col);
            if (hasColor)
                ImGui.PushStyleColor(ImGuiCol.Text, tab.ColumnValueColors[col]);
            if (ImGui.SmallButton($"C##sbClr_{col}"))
                ImGui.OpenPopup($"##sbClrPopup_{col}");
            if (hasColor)
                ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(hasColor ? "Custom value color (click to edit)" : "Set custom value color");
            if (ImGui.BeginPopup($"##sbClrPopup_{col}"))
            {
                extChanged |= DrawColumnColorPopup(col, tab.ColumnValueColors);
                ImGui.EndPopup();
            }
            return extChanged;
        };
        changed |= MetricPicker.Draw("statusBar", tab.StatusBarMetrics,
            MetricPicker.GetBarColumnLabel,
            MetricPicker.BarColumnCategories,
            sbExtras,
            c => MetricPicker.BarColumnDescriptions.GetValueOrDefault(c));
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Tooltip Content", ImGuiTreeNodeFlags.None))
        {
            ImGui.TextDisabled("Choose which fields to show in the tooltip and their order.");
            ImGui.Spacing();

            var skillCount = tab.TooltipTopSkillCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Top skills to show", ref skillCount, 1, 10))
            {
                tab.TooltipTopSkillCount = skillCount;
                changed = true;
            }
            ImGui.Spacing();

            Func<TooltipField, bool> tooltipExtras = field =>
            {
                var extChanged = false;
                ImGui.SameLine();
                var defaultLabel = Configuration.DefaultTooltipFieldLabels.GetValueOrDefault(field, field.ToString());
                tab.TooltipFieldLabels.TryGetValue(field, out var current);
                current ??= "";
                ImGui.SetNextItemWidth(60);
                if (ImGui.InputTextWithHint($"##ttLbl_{field}", defaultLabel, ref current, 32))
                {
                    if (string.IsNullOrEmpty(current))
                        tab.TooltipFieldLabels.Remove(field);
                    else
                        tab.TooltipFieldLabels[field] = current;
                    extChanged = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(MetricPicker.GetTooltipFieldLabel(field));
                return extChanged;
            };
            changed |= MetricPicker.Draw("tooltip", tab.TooltipFields,
                MetricPicker.GetTooltipFieldLabel,
                MetricPicker.TooltipFieldCategories,
                tooltipExtras,
                f => MetricPicker.TooltipFieldDescriptions.GetValueOrDefault(f));
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Details Panel Content", ImGuiTreeNodeFlags.None))
        {
            ImGui.TextDisabled("Choose what to show in the expanded detail view.");
            ImGui.Spacing();

            ImGui.TextDisabled("Tabs");

            var showDetails = tab.DetailShowDetailsTab;
            if (ImGui.Checkbox("Details##detailTab", ref showDetails))
            {
                tab.DetailShowDetailsTab = showDetails;
                changed = true;
            }

            ImGui.SameLine();
            var showSkillsTab = tab.DetailShowSkillsTab;
            if (ImGui.Checkbox("Skills##detailTab", ref showSkillsTab))
            {
                tab.DetailShowSkillsTab = showSkillsTab;
                changed = true;
            }

            ImGui.SameLine();
            var showGraphTab = tab.DetailShowGraphTab;
            if (ImGui.Checkbox("Graph##detailTab", ref showGraphTab))
            {
                tab.DetailShowGraphTab = showGraphTab;
                changed = true;
            }

            ImGui.SameLine();
            var showBuffsTab = tab.DetailShowBuffsTab;
            if (ImGui.Checkbox("Buffs##detailTab", ref showBuffsTab))
            {
                tab.DetailShowBuffsTab = showBuffsTab;
                changed = true;
            }

            ImGui.Spacing();

            if (ImGui.Button("Enable All##detailVis"))
            {
                foreach (var (_, items) in MetricPicker.BarColumnCategories)
                    foreach (var col in items)
                        tab.DetailVisibleColumns.Add(col);
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Disable All##detailVis"))
            {
                tab.DetailVisibleColumns.Clear();
                changed = true;
            }

            ImGui.Spacing();

            Func<BarColumn, bool> detailExtras = col =>
            {
                var extChanged = false;
                ImGui.SameLine();
                var defaultLabel = Configuration.DefaultDetailColumnLabels.GetValueOrDefault(col, col.ToString());
                tab.DetailColumnLabels.TryGetValue(col, out var current);
                current ??= "";
                ImGui.SetNextItemWidth(60);
                if (ImGui.InputTextWithHint($"##dtLbl_{col}", defaultLabel, ref current, 32))
                {
                    if (string.IsNullOrEmpty(current))
                        tab.DetailColumnLabels.Remove(col);
                    else
                        tab.DetailColumnLabels[col] = current;
                    extChanged = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(MetricPicker.GetBarColumnLabel(col));

                ImGui.SameLine();
                var hasColor = tab.ColumnValueColors.ContainsKey(col);
                if (hasColor)
                    ImGui.PushStyleColor(ImGuiCol.Text, tab.ColumnValueColors[col]);
                if (ImGui.SmallButton($"C##dtClr_{col}"))
                    ImGui.OpenPopup($"##dtClrPopup_{col}");
                if (hasColor)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(hasColor ? "Custom value color (click to edit)" : "Set custom value color");
                if (ImGui.BeginPopup($"##dtClrPopup_{col}"))
                {
                    extChanged |= DrawColumnColorPopup(col, tab.ColumnValueColors);
                    ImGui.EndPopup();
                }

                ImGui.SameLine();
                var hasNewLine = tab.DetailNewLineColumns.Contains(col);
                if (hasNewLine)
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.85f, 1f, 1f));
                if (ImGui.SmallButton($"NL##dtNl_{col}"))
                {
                    if (hasNewLine)
                        tab.DetailNewLineColumns.Remove(col);
                    else
                        tab.DetailNewLineColumns.Add(col);
                    extChanged = true;
                }
                if (hasNewLine)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Starts a new line with this metric");

                return extChanged;
            };
            changed |= MetricPicker.DrawCategorized("detailVis", tab.DetailVisibleColumns,
                MetricPicker.GetBarColumnLabel,
                MetricPicker.BarColumnCategories,
                tab.DetailSectionOrder,
                detailExtras,
                c => MetricPicker.BarColumnDescriptions.GetValueOrDefault(c));

            ImGui.Spacing();

            ImGui.TextDisabled("Skill breakdown");

            var showSkills = tab.DetailShowSkillBreakdown;
            if (ImGui.Checkbox("Show skill breakdown##detailVis", ref showSkills))
            {
                tab.DetailShowSkillBreakdown = showSkills;
                changed = true;
            }

            if (tab.DetailShowSkillBreakdown)
            {
                var maxSkills = tab.MaxSkillBreakdownCount;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("Max skills shown (0 = all)##detailVis", ref maxSkills, 0, 30))
                {
                    tab.MaxSkillBreakdownCount = maxSkills;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool DrawCustomJobFilter(MeterTab tab)
    {
        var changed = false;

        ImGui.TextDisabled("Select which jobs to include:");
        ImGui.Indent();

        DrawJobGroup("Tanks", JobColorHelper.TankJobs, tab, ref changed);
        DrawJobGroup("Healers", JobColorHelper.HealerJobs, tab, ref changed);
        DrawJobGroup("Melee DPS", JobColorHelper.MeleeDpsJobs, tab, ref changed);
        DrawJobGroup("Ranged DPS", JobColorHelper.RangedDpsJobs, tab, ref changed);
        DrawJobGroup("Caster DPS", JobColorHelper.CasterDpsJobs, tab, ref changed);

        ImGui.Unindent();
        return changed;
    }

    private static void DrawJobGroup(string groupLabel, string[] jobs, MeterTab tab, ref bool changed)
    {
        if (ImGui.TreeNodeEx(groupLabel, ImGuiTreeNodeFlags.None))
        {
            foreach (var job in jobs)
            {
                var isChecked = tab.CustomJobFilter.Contains(job, StringComparer.OrdinalIgnoreCase);
                var fullName = JobNameHelper.GetFullName(job);
                if (ImGui.Checkbox($"{fullName} ({job})##custom_{job}", ref isChecked))
                {
                    if (isChecked)
                    {
                        if (!tab.CustomJobFilter.Contains(job, StringComparer.OrdinalIgnoreCase))
                            tab.CustomJobFilter.Add(job);
                    }
                    else
                    {
                        tab.CustomJobFilter.RemoveAll(j => string.Equals(j, job, StringComparison.OrdinalIgnoreCase));
                    }
                    changed = true;
                }
            }
            ImGui.TreePop();
        }
    }

    private static bool DrawColumnColorPopup(BarColumn col, Dictionary<BarColumn, Vector4> colors)
    {
        var changed = false;
        var hasColor = colors.TryGetValue(col, out var current);

        if (!hasColor)
        {
            current = new Vector4(1f, 1f, 1f, 1f);
        }

        var useCustom = hasColor;
        if (ImGui.Checkbox("Use custom color", ref useCustom))
        {
            if (useCustom)
            {
                colors[col] = current;
            }
            else
            {
                colors.Remove(col);
            }
            changed = true;
        }

        if (useCustom)
        {
            if (ImGui.ColorEdit4($"##colClr_{col}", ref current, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
            {
                colors[col] = current;
                changed = true;
            }
        }

        return changed;
    }

    private static bool DrawColumnWidthPopup(BarColumn col, Dictionary<BarColumn, float> widths)
    {
        var changed = false;
        var hasWidth = widths.TryGetValue(col, out var current);

        if (!hasWidth)
        {
            current = 50f;
        }

        var useCustom = hasWidth;
        if (ImGui.Checkbox("Use custom width", ref useCustom))
        {
            if (useCustom)
            {
                widths[col] = current;
            }
            else
            {
                widths.Remove(col);
            }
            changed = true;
        }

        if (useCustom)
        {
            ImGui.SetNextItemWidth(150);
            if (ImGui.DragFloat($"##colWid_{col}", ref current, 1f, 20f, 300f, "%.0f px"))
            {
                widths[col] = current;
                changed = true;
            }
        }

        return changed;
    }

    public static bool DrawButtonAppearance(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var btnColor = config.TabButtonColor;
        if (ImGui.ColorEdit4("Button Color", ref btnColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            config.TabButtonColor = btnColor;
            changed = true;
        }

        var btnHovered = config.TabButtonHoveredColor;
        if (ImGui.ColorEdit4("Hovered Color", ref btnHovered, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            config.TabButtonHoveredColor = btnHovered;
            changed = true;
        }

        var btnActive = config.TabButtonActiveColor;
        if (ImGui.ColorEdit4("Active Color", ref btnActive, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            config.TabButtonActiveColor = btnActive;
            changed = true;
        }

        var btnText = config.TabButtonTextColor;
        if (ImGui.ColorEdit4("Text Color", ref btnText, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            config.TabButtonTextColor = btnText;
            changed = true;
        }

        var btnActiveText = config.TabButtonActiveTextColor;
        if (ImGui.ColorEdit4("Active Text Color", ref btnActiveText, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            config.TabButtonActiveTextColor = btnActiveText;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var btnHeight = config.TabButtonHeight;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Button Height", ref btnHeight, 14f, 48f, "%.0f"))
        {
            config.TabButtonHeight = btnHeight;
            changed = true;
        }

        var btnSpacing = config.TabButtonSpacing;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Button Spacing", ref btnSpacing, 0f, 16f, "%.0f"))
        {
            config.TabButtonSpacing = btnSpacing;
            changed = true;
        }

        var btnRounding = config.TabButtonRounding;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Button Rounding", ref btnRounding, 0f, 16f, "%.1f"))
        {
            config.TabButtonRounding = btnRounding;
            changed = true;
        }

        var btnFontSize = config.TabButtonFontSize;
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Font Size", ref btnFontSize, 6f, 40f, "%.1fpt"))
        {
            config.TabButtonFontSize = btnFontSize;
            changed = true;
        }

        ImGui.Spacing();

        var btnStretch = config.TabButtonStretchToFit;
        if (ImGui.Checkbox("Stretch buttons to fill width", ref btnStretch))
        {
            config.TabButtonStretchToFit = btnStretch;
            changed = true;
        }

        if (!config.TabButtonStretchToFit)
        {
            var btnWidth = config.TabButtonWidth;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Button Width", ref btnWidth, 20f, 300f, "%.0f"))
            {
                config.TabButtonWidth = btnWidth;
                changed = true;
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Fixed width for each tab button.\nSet to 0 to auto-size based on text.");
        }
        }

        return changed;
    }
}
