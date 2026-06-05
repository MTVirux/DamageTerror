using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabDetailsSection
{
    public static bool Draw(MeterTab tab)
    {
        var changed = false;

        if (!ImGui.CollapsingHeader("Details Panel Content", ImGuiTreeNodeFlags.None))
            return changed;

        ImGui.TextDisabled("Tabs");

        changed |= ConfigHelpers.CheckboxProp("Details##detailTab", tab.DetailShowDetailsTab, v => tab.DetailShowDetailsTab = v);

        ImGui.SameLine();
        changed |= ConfigHelpers.CheckboxProp("Skills##detailTab", tab.DetailShowSkillsTab, v => tab.DetailShowSkillsTab = v);

        ImGui.SameLine();
        changed |= ConfigHelpers.CheckboxProp("Graph##detailTab", tab.DetailShowGraphTab, v => tab.DetailShowGraphTab = v);

        ImGui.SameLine();
        changed |= ConfigHelpers.CheckboxProp("Buffs##detailTab", tab.DetailShowBuffsTab, v => tab.DetailShowBuffsTab = v);

        ImGui.SameLine();
        changed |= ConfigHelpers.CheckboxProp("Items##detailTab", tab.DetailShowItemTab, v => tab.DetailShowItemTab = v);

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
                extChanged |= MeterTabSectionHelpers.DrawColumnColorPopup(col, tab.ColumnValueColors);
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

        changed |= ConfigHelpers.CheckboxProp("Show skill breakdown##detailVis", tab.DetailShowSkillBreakdown, v => tab.DetailShowSkillBreakdown = v);

        if (tab.DetailShowSkillBreakdown)
        {
            changed |= ConfigHelpers.SliderIntProp("Max skills shown (0 = all)##detailVis", tab.MaxSkillBreakdownCount, 0, 30, v => tab.MaxSkillBreakdownCount = v, 200);
        }

        return changed;
    }
}
