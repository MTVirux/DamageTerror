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

        ImGui.SameLine();
        var showItemTab = tab.DetailShowItemTab;
        if (ImGui.Checkbox("Items##detailTab", ref showItemTab))
        {
            tab.DetailShowItemTab = showItemTab;
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

        return changed;
    }
}
