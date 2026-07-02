using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class DetailsSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Layout", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.SliderFloatProp("Indent", config.DetailIndent, 0.0f, 24.0f, "%.0f px", v => config.DetailIndent = v, 200);
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
        changed |= ConfigHelpers.SliderFloatProp("Row height", config.SkillRowHeight, 10.0f, 30.0f, "%.0f px", v => config.SkillRowHeight = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Column padding", config.SkillColumnPadding, 0.0f, 16.0f, "%.0f px", v => config.SkillColumnPadding = v, 200);
        changed |= ConfigHelpers.SliderFloatProp("Bar rounding##skills", config.SkillBarRounding, 0.0f, 12.0f, "%.1f", v => config.SkillBarRounding = v, 200);
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

            changed |= ConfigHelpers.CheckboxProp("Show iDPS##graph", config.GraphShowDps, v => config.GraphShowDps = v);
            changed |= ConfigHelpers.CheckboxProp("Show iHPS##graph", config.GraphShowHps, v => config.GraphShowHps = v);
            changed |= ConfigHelpers.CheckboxProp("Show iDTPS##graph", config.GraphShowDtps, v => config.GraphShowDtps = v);

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
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dps", "DPS Markers", config.DetailMarkers[MetricType.Dps]);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_hps", "HPS Markers", config.DetailMarkers[MetricType.Hps]);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dtps", "DTPS Markers", config.DetailMarkers[MetricType.Dtps]);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Buffs / Debuffs — Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.SliderFloatProp("Row height##buffs", config.BuffRowHeight, 10.0f, 30.0f, "%.0f px", v => config.BuffRowHeight = v, 200);
            changed |= ConfigHelpers.SliderFloatProp("Column padding##buffs", config.BuffColumnPadding, 0.0f, 16.0f, "%.0f px", v => config.BuffColumnPadding = v, 200);
            changed |= ConfigHelpers.SliderFloatProp("Bar rounding##buffs", config.BuffBarRounding, 0.0f, 12.0f, "%.1f", v => config.BuffBarRounding = v, 200);
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
