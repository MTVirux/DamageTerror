using Dalamud.Bindings.ImGui;
using DamageTerror.Gui.MainWindow;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabContentSection
{
    public static bool Draw(MeterTab tab)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Meter/Graph Content", ImGuiTreeNodeFlags.None))
            return changed;

        var viewModeLabels = new[] { "Bars", "Line Graph" };
        changed |= ConfigHelpers.ComboProp("View Mode", (int)tab.ViewMode, viewModeLabels, v => tab.ViewMode = (ViewMode)v, 200);

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Switch between traditional bars and a line graph overlay.\nGraph mode plots metrics over time for all combatants.\nYou can toggle the view mode from the titlebar or context menu.");

        if (tab.ViewMode == ViewMode.LineGraph)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Graph Lines");

            changed |= ConfigHelpers.CheckboxProp("Show DPS Line", tab.GraphShowDpsLine, v => tab.GraphShowDpsLine = v);
            changed |= ConfigHelpers.CheckboxProp("Show HPS Line", tab.GraphShowHpsLine, v => tab.GraphShowHpsLine = v);
            changed |= ConfigHelpers.CheckboxProp("Show DTPS Line", tab.GraphShowDtpsLine, v => tab.GraphShowDtpsLine = v);
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
                        extChanged |= ColumnFormatHelper.DrawColumnFormatPopup(col, tab.ColumnFormatOverrides);
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
                    extChanged |= MeterTabSectionHelpers.DrawColumnColorPopup(col, tab.ColumnValueColors);
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
}
