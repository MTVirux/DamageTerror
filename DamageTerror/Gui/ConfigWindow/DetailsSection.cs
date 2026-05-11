using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class DetailsSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Layout", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var detailIndent = config.DetailIndent;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Indent", ref detailIndent, 0.0f, 24.0f, "%.0f px"))
        {
            config.DetailIndent = detailIndent;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##details", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Background", config.DetailBackgroundColor, v => config.DetailBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Label color", config.DetailLabelColor, v => config.DetailLabelColor = v);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Details"))
        {
            config.DetailBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
            config.DetailLabelColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            config.DetailIndent = 8.0f;
            config.DetailFontSize = 14f;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Breakdown — Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var skillRowHeight = config.SkillRowHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Row height", ref skillRowHeight, 10.0f, 30.0f, "%.0f px"))
        {
            config.SkillRowHeight = skillRowHeight;
            changed = true;
        }

        var skillColPad = config.SkillColumnPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Column padding", ref skillColPad, 0.0f, 16.0f, "%.0f px"))
        {
            config.SkillColumnPadding = skillColPad;
            changed = true;
        }

        var skillRounding = config.SkillBarRounding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar rounding##skills", ref skillRounding, 0.0f, 12.0f, "%.1f"))
        {
            config.SkillBarRounding = skillRounding;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Breakdown — Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Unknown damage fill", config.SkillDamageFillColor, v => config.SkillDamageFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Physical damage fill", config.SkillPhysicalFillColor, v => config.SkillPhysicalFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Magic damage fill", config.SkillMagicFillColor, v => config.SkillMagicFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Healing fill", config.SkillHealingFillColor, v => config.SkillHealingFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Row background", config.SkillRowBackgroundColor, v => config.SkillRowBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Skill text", config.SkillTextColor, v => config.SkillTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header text", config.SkillHeaderTextColor, v => config.SkillHeaderTextColor = v);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Skill Colors"))
        {
            config.SkillDamageFillColor = new Vector4(0.35f, 0.35f, 0.55f, 0.7f);
            config.SkillHealingFillColor = new Vector4(0.25f, 0.50f, 0.30f, 0.7f);
            config.SkillPhysicalFillColor = new Vector4(0.55f, 0.30f, 0.25f, 0.7f);
            config.SkillMagicFillColor = new Vector4(0.30f, 0.30f, 0.65f, 0.7f);
            config.SkillRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
            config.SkillTextColor = new Vector4(1f, 1f, 1f, 0.9f);
            config.SkillHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Graph", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= GraphConfigBlock.Draw(config, isGraphView: false);

            ImGui.Spacing();
            ImGui.TextDisabled("Series visibility");

            var showDps = config.GraphShowDps;
            if (ImGui.Checkbox("Show iDPS##graph", ref showDps))
            {
                config.GraphShowDps = showDps;
                changed = true;
            }

            var showHps = config.GraphShowHps;
            if (ImGui.Checkbox("Show iHPS##graph", ref showHps))
            {
                config.GraphShowHps = showHps;
                changed = true;
            }

            var showDtps = config.GraphShowDtps;
            if (ImGui.Checkbox("Show iDTPS##graph", ref showDtps))
            {
                config.GraphShowDtps = showDtps;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Series Colors");

            changed |= ConfigHelpers.ColorEditProp("iDPS line", config.GraphDpsColor, v => config.GraphDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("iHPS line", config.GraphHpsColor, v => config.GraphHpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("iDTPS line", config.GraphDtpsColor, v => config.GraphDtpsColor = v);

            ImGui.Spacing();

            if (ConfigHelpers.ShiftResetButton("Reset Graph"))
            {
                config.GraphHeight = 120f;
                config.GraphLineThickness = 2f;
                config.GraphDpsColor = new Vector4(0.9f, 0.4f, 0.4f, 1f);
                config.GraphHpsColor = new Vector4(0.4f, 0.85f, 0.4f, 1f);
                config.GraphDtpsColor = new Vector4(0.4f, 0.55f, 0.9f, 1f);
                config.GraphBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
                config.GraphGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
                config.GraphShowLegend = true;
                config.GraphShowGrid = true;
                config.GraphShowXAxisLabels = true;
                config.GraphShowYAxisLabels = true;
                config.GraphShowDps = true;
                config.GraphShowHps = true;
                config.GraphSmoothingWindow = 5f;
                config.GraphUpdateInterval = 0.25f;
                config.GraphShowDtps = true;
                config.GraphShowLabels = true;
                config.GraphLabelOffsetX = 8f;
                config.GraphLabelOffsetY = 0f;
                config.GraphAutoScroll = false;
                config.GraphAutoScrollWindow = 60f;
                config.GraphAutoScrollSmoothing = 8f;
                config.GraphXAxisPadding = 1.25f;
                config.GraphYAxisHeadroom = 1.1f;
                config.GraphYAxisTickCount = 8;
                config.GraphMouseTextOpacity = 0.6f;
                config.GraphFontSize = 14f;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Markers##details", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dps", "DPS Markers", config.DetailDpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_hps", "HPS Markers", config.DetailHpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dtps", "DTPS Markers", config.DetailDtpsMarkers);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Buffs / Debuffs — Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var buffRowHeight = config.BuffRowHeight;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Row height##buffs", ref buffRowHeight, 10.0f, 30.0f, "%.0f px"))
            {
                config.BuffRowHeight = buffRowHeight;
                changed = true;
            }

            var buffColPad = config.BuffColumnPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Column padding##buffs", ref buffColPad, 0.0f, 16.0f, "%.0f px"))
            {
                config.BuffColumnPadding = buffColPad;
                changed = true;
            }

            var buffRounding = config.BuffBarRounding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar rounding##buffs", ref buffRounding, 0.0f, 12.0f, "%.1f"))
            {
                config.BuffBarRounding = buffRounding;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Buffs / Debuffs — Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.ColorEditProp("Buff fill", config.BuffFillColor, v => config.BuffFillColor = v);
            changed |= ConfigHelpers.ColorEditProp("Debuff fill", config.DebuffFillColor, v => config.DebuffFillColor = v);
            changed |= ConfigHelpers.ColorEditProp("Row background##buffs", config.BuffRowBackgroundColor, v => config.BuffRowBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Text##buffs", config.BuffTextColor, v => config.BuffTextColor = v);
            changed |= ConfigHelpers.ColorEditProp("Header text##buffs", config.BuffHeaderTextColor, v => config.BuffHeaderTextColor = v);

            ImGui.Spacing();

            if (ConfigHelpers.ShiftResetButton("Reset Buff Colors"))
            {
                config.BuffFillColor = new Vector4(0.30f, 0.50f, 0.60f, 0.7f);
                config.DebuffFillColor = new Vector4(0.60f, 0.30f, 0.30f, 0.7f);
                config.BuffRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
                config.BuffTextColor = new Vector4(1f, 1f, 1f, 0.9f);
                config.BuffHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
                changed = true;
            }
        }

        return changed;
    }
}
