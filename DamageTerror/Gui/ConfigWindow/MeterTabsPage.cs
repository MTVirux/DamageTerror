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

        if (ImGui.CollapsingHeader("Sort", ImGuiTreeNodeFlags.DefaultOpen))
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

        if (ImGui.CollapsingHeader("Meter/Graph Content", ImGuiTreeNodeFlags.DefaultOpen))
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
            changed |= DisplayTab.DrawBarColumns(tab.ColumnOrder,
                col => DisplayTab.GetTabColumnEnabled(tab, col),
                (col, v) => DisplayTab.SetTabColumnEnabled(tab, col, v),
                tab.ColumnHeaderLabels,
                tab.ColumnFormatOverrides);
        }

        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Status Bar Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var sbTimer = tab.ShowStatusBarTimer;
        if (ImGui.Checkbox("Show combat timer##sbtab", ref sbTimer))
        {
            tab.ShowStatusBarTimer = sbTimer;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Metrics");
        tab.StatusBarMetrics ??= new List<BarColumn> { BarColumn.Dps, BarColumn.RaidDps };
        changed |= DrawStatusBarMetrics(tab.StatusBarMetrics);
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Tooltip Content", ImGuiTreeNodeFlags.DefaultOpen))
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

            var fields = tab.TooltipFields;
            var allFields = Enum.GetValues<TooltipField>();
            var disabledFields = allFields.Where(f => !fields.Contains(f)).ToList();
            disabledFields.Sort((a, b) =>
                string.Compare(
                    AppearanceTab.TooltipFieldLabels.GetValueOrDefault(a, a.ToString()),
                    AppearanceTab.TooltipFieldLabels.GetValueOrDefault(b, b.ToString()),
                    StringComparison.OrdinalIgnoreCase));

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var label = AppearanceTab.TooltipFieldLabels.GetValueOrDefault(field, field.ToString());

                ImGui.PushID($"ttf_{i}");

                var canUp = i > 0;
                if (!canUp) ImGui.BeginDisabled();
                if (ImGui.ArrowButton("##up", ImGuiDir.Up))
                {
                    (fields[i - 1], fields[i]) = (fields[i], fields[i - 1]);
                    changed = true;
                }
                if (!canUp) ImGui.EndDisabled();

                ImGui.SameLine();

                var canDown = i < fields.Count - 1;
                if (!canDown) ImGui.BeginDisabled();
                if (ImGui.ArrowButton("##down", ImGuiDir.Down))
                {
                    (fields[i], fields[i + 1]) = (fields[i + 1], fields[i]);
                    changed = true;
                }
                if (!canDown) ImGui.EndDisabled();

                ImGui.SameLine();

                var enabled = true;
                if (ImGui.Checkbox(label, ref enabled))
                {
                    fields.RemoveAt(i);
                    changed = true;
                    ImGui.PopID();
                    i--;
                    continue;
                }

                ImGui.PopID();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("Disabled");
            ImGui.Spacing();

            if (disabledFields.Count > 0 && ImGui.BeginTabBar("##disabledTooltipFields"))
            {
                foreach (var (catName, catFields) in AppearanceTab.DisabledTooltipCategories)
                {
                    var catDisabled = catFields.Where(f => disabledFields.Contains(f)).ToList();

                    if (catDisabled.Count == 0)
                        continue;

                    if (ImGui.BeginTabItem(catName))
                    {
                        catDisabled.Sort((a, b) =>
                            string.Compare(
                                AppearanceTab.TooltipFieldLabels.GetValueOrDefault(a, a.ToString()),
                                AppearanceTab.TooltipFieldLabels.GetValueOrDefault(b, b.ToString()),
                                StringComparison.OrdinalIgnoreCase));

                        foreach (var field in catDisabled)
                        {
                            var label = AppearanceTab.TooltipFieldLabels.GetValueOrDefault(field, field.ToString());
                            ImGui.PushID($"disabled_tt_{field}");

                            var off = false;
                            if (ImGui.Checkbox(label, ref off))
                            {
                                fields.Add(field);
                                changed = true;
                            }

                            ImGui.PopID();
                        }

                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("Details Panel Content", ImGuiTreeNodeFlags.DefaultOpen))
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

            ImGui.Spacing();

            if (ImGui.Button("Enable All##detailVis"))
            {
                tab.DetailVisibleColumns = new HashSet<BarColumn>(Enum.GetValues<BarColumn>());
                changed = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Disable All##detailVis"))
            {
                tab.DetailVisibleColumns.Clear();
                changed = true;
            }

            ImGui.Spacing();

            foreach (var (catName, catColumns) in AppearanceTab.DetailCategories)
            {
                if (ImGui.TreeNodeEx(catName + "##detailVis", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (!tab.DetailSectionOrder.TryGetValue(catName, out var sectionOrder) || sectionOrder.Count == 0)
                    {
                        sectionOrder = new List<BarColumn>(catColumns);
                        tab.DetailSectionOrder[catName] = sectionOrder;
                    }

                    foreach (var col in catColumns)
                    {
                        if (!sectionOrder.Contains(col))
                            sectionOrder.Add(col);
                    }

                    // Remove columns no longer in the default
                    var validCols = new HashSet<BarColumn>(catColumns);
                    sectionOrder.RemoveAll(c => !validCols.Contains(c));

                    // Render ordered list with visibility toggle + arrow button reorder
                    for (var i = 0; i < sectionOrder.Count; i++)
                    {
                        var col = sectionOrder[i];
                        var label = DisplayTab.ColumnLabels.GetValueOrDefault(col, col.ToString());
                        var enabled = tab.DetailVisibleColumns.Contains(col);

                        ImGui.PushID($"detailOrd_{catName}_{i}");

                        var canUp = i > 0;
                        if (!canUp) ImGui.BeginDisabled();
                        if (ImGui.ArrowButton("##up", ImGuiDir.Up))
                        {
                            (sectionOrder[i], sectionOrder[i - 1]) = (sectionOrder[i - 1], sectionOrder[i]);
                            changed = true;
                        }
                        if (!canUp) ImGui.EndDisabled();

                        ImGui.SameLine();

                        var canDown = i < sectionOrder.Count - 1;
                        if (!canDown) ImGui.BeginDisabled();
                        if (ImGui.ArrowButton("##down", ImGuiDir.Down))
                        {
                            (sectionOrder[i], sectionOrder[i + 1]) = (sectionOrder[i + 1], sectionOrder[i]);
                            changed = true;
                        }
                        if (!canDown) ImGui.EndDisabled();

                        ImGui.SameLine();

                        if (ImGui.Checkbox(label, ref enabled))
                        {
                            if (enabled)
                                tab.DetailVisibleColumns.Add(col);
                            else
                                tab.DetailVisibleColumns.Remove(col);
                            changed = true;
                        }

                        ImGui.PopID();
                    }

                    if (ImGui.Button($"Reset Order##{catName}"))
                    {
                        tab.DetailSectionOrder[catName] = new List<BarColumn>(catColumns);
                        changed = true;
                    }

                    ImGui.TreePop();
                }
            }

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

    private static readonly (string Name, BarColumn[] Columns)[] StatusBarCategories =
    {
        ("Dmg", new[] { BarColumn.Dps, BarColumn.Damage, BarColumn.InstantDps, BarColumn.PeakDps, BarColumn.MaxHitValue, BarColumn.DamageShield, BarColumn.RaidDps }),
        ("Heal", new[] { BarColumn.Hps, BarColumn.Healed, BarColumn.InstantHps, BarColumn.MaxHealValue, BarColumn.OverhealAmount, BarColumn.RaidHps }),
        ("D%", new[] { BarColumn.DamagePercent, BarColumn.DirectHit, BarColumn.Crit, BarColumn.CritDirectHit }),
        ("H%", new[] { BarColumn.HealPercent, BarColumn.Overheal, BarColumn.CritHealPct }),
        ("Taken", new[] { BarColumn.DamageTaken, BarColumn.DamageTakenPercent, BarColumn.HealsTaken }),
        ("Counts", new[] { BarColumn.Swings, BarColumn.Hits, BarColumn.Misses, BarColumn.HitRate, BarColumn.Deaths, BarColumn.Kills }),
        ("Other", new[] { BarColumn.CombatantDuration, BarColumn.HealCount, BarColumn.BlockPct, BarColumn.ParryPct }),
    };

    private static bool DrawStatusBarMetrics(List<BarColumn> metrics)
    {
        var changed = false;

        if (ImGui.BeginTabBar("##sb_cats"))
        {
            foreach (var (name, columns) in StatusBarCategories)
            {
                if (ImGui.BeginTabItem(name))
                {
                    foreach (var col in columns)
                    {
                        var label = DisplayTab.ColumnLabels.GetValueOrDefault(col, col.ToString());
                        var enabled = metrics.Contains(col);
                        if (ImGui.Checkbox($"{label}##sb_{col}", ref enabled))
                        {
                            if (enabled)
                                metrics.Add(col);
                            else
                                metrics.Remove(col);
                            changed = true;
                        }
                    }
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }

        if (metrics.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Order (drag to reorder)");
            for (var i = 0; i < metrics.Count; i++)
            {
                var col = metrics[i];
                var label = DisplayTab.ColumnLabels.GetValueOrDefault(col, col.ToString());
                ImGui.Selectable($"{label}##sbord_{i}");
                if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                {
                    var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Y;
                    if (delta < -ImGui.GetTextLineHeightWithSpacing() * 0.5f && i > 0)
                    {
                        (metrics[i], metrics[i - 1]) = (metrics[i - 1], metrics[i]);
                        ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
                        changed = true;
                    }
                    else if (delta > ImGui.GetTextLineHeightWithSpacing() * 0.5f && i < metrics.Count - 1)
                    {
                        (metrics[i], metrics[i + 1]) = (metrics[i + 1], metrics[i]);
                        ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
                        changed = true;
                    }
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
