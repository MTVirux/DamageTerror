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

                extChanged |= MeterTabSectionHelpers.DrawLabelOverride(col, "hdr_",
                    ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString()),
                    tab.ColumnHeaderLabels, MetricPicker.GetBarColumnLabel(col));

                if (tab.ColumnFormatOverrides != null && ColumnFormatOverride.SupportsFormatting(col))
                {
                    extChanged |= MeterTabSectionHelpers.DrawColumnButtonPopup(col, "F", "fmt",
                        tab.ColumnFormatOverrides.ContainsKey(col), new Vector4(0.4f, 0.8f, 1.0f, 1.0f),
                        "Custom format (click to edit)", "Set custom format",
                        () => ColumnFormatHelper.DrawColumnFormatPopup(col, tab.ColumnFormatOverrides));
                }

                extChanged |= MeterTabSectionHelpers.DrawColorButton(col, "clr", tab.ColumnValueColors);

                extChanged |= MeterTabSectionHelpers.DrawColumnButtonPopup(col, "W", "wid",
                    tab.ColumnWidthOverrides.ContainsKey(col), new Vector4(0.4f, 1.0f, 0.4f, 1.0f),
                    "Custom width (click to edit)", "Set custom column width",
                    () => DrawColumnWidthPopup(col, tab.ColumnWidthOverrides));

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
