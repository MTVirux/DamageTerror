using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public static class ConfigHelpers
{
    public static bool ColorEditProp(string label, Vector4 color, Action<Vector4> setter)
    {
        var c = color;
        if (ImGui.ColorEdit4(label, ref c, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
        {
            setter(c);
            return true;
        }
        return false;
    }

    public static bool DrawPerJobColorGroup(string groupLabel, string[] jobs, Configuration config)
    {
        var changed = false;
        if (ImGui.TreeNodeEx(groupLabel, ImGuiTreeNodeFlags.None))
        {
            foreach (var job in jobs)
            {
                var current = config.JobColors.TryGetValue(job, out var custom)
                    ? custom
                    : JobDataTable.GetDefaultColor(job);

                var fullName = JobDataTable.GetFullName(job);
                var label = $"{fullName} ({job})";

                var c = current;
                if (ImGui.ColorEdit4(label, ref c, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
                {
                    config.JobColors[job] = c;
                    changed = true;
                }
            }

            ImGui.TreePop();
        }

        return changed;
    }

    public static bool DrawSkillMarkerSection(string id, string label, SkillMarkerConfig mc)
    {
        var changed = false;

        if (ImGui.TreeNodeEx($"{label}##markers_{id}", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showMarkers = mc.ShowMarkers;
            if (ImGui.Checkbox($"Show skill markers##sec_{id}", ref showMarkers))
            {
                mc.ShowMarkers = showMarkers;
                changed = true;
            }

            changed |= ColorEditProp($"Marker color##sec_{id}", mc.MarkerColor, v => mc.MarkerColor = v);

            var markerSize = mc.MarkerSize;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat($"Marker size##sec_{id}", ref markerSize, 1f, 10f, "%.1f"))
            {
                mc.MarkerSize = markerSize;
                changed = true;
            }

            var showCrit = mc.ShowCritMarkers;
            if (ImGui.Checkbox($"Color by crit/DH##sec_{id}", ref showCrit))
            {
                mc.ShowCritMarkers = showCrit;
                changed = true;
            }

            if (mc.ShowCritMarkers)
            {
                changed |= ColorEditProp($"Crit ! color##sec_{id}", mc.CritMarkerColor, v => mc.CritMarkerColor = v);
                changed |= ColorEditProp($"Direct Hit !! color##sec_{id}", mc.DirectHitMarkerColor, v => mc.DirectHitMarkerColor = v);
                changed |= ColorEditProp($"Crit Direct Hit !!! color##sec_{id}", mc.CritDirectHitMarkerColor, v => mc.CritDirectHitMarkerColor = v);
            }

            ImGui.Spacing();
            ImGui.TextDisabled("DoT / HoT Markers");

            var showDotTick = mc.ShowDoTTickMarkers;
            if (ImGui.Checkbox($"Show DoT/HoT tick markers##sec_{id}", ref showDotTick))
            {
                mc.ShowDoTTickMarkers = showDotTick;
                changed = true;
            }

            if (mc.ShowDoTTickMarkers)
            {
                changed |= ColorEditProp($"Tick color##sec_{id}", mc.DoTTickColor, v => mc.DoTTickColor = v);
                var dotTickSize = mc.DoTTickMarkerSize;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat($"Tick size##sec_{id}", ref dotTickSize, 1f, 10f, "%.1f"))
                {
                    mc.DoTTickMarkerSize = dotTickSize;
                    changed = true;
                }
            }

            var showDotApp = mc.ShowDoTApplicationMarkers;
            if (ImGui.Checkbox($"Show DoT/HoT application markers##sec_{id}", ref showDotApp))
            {
                mc.ShowDoTApplicationMarkers = showDotApp;
                changed = true;
            }

            if (mc.ShowDoTApplicationMarkers)
            {
                changed |= ColorEditProp($"Application color##sec_{id}", mc.DoTApplicationColor, v => mc.DoTApplicationColor = v);
                var dotAppSize = mc.DoTApplicationMarkerSize;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat($"Application size##sec_{id}", ref dotAppSize, 1f, 10f, "%.1f"))
                {
                    mc.DoTApplicationMarkerSize = dotAppSize;
                    changed = true;
                }
            }

            ImGui.TreePop();
        }

        return changed;
    }

    public static bool ShiftResetButton(string label)
    {
        var shiftHeld = ImGui.GetIO().KeyShift;
        if (!shiftHeld) ImGui.BeginDisabled();
        var pressed = ImGui.Button(label);
        if (!shiftHeld) ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip("Click while holding SHIFT to reset");
        return pressed;
    }

    public static void HelpMarker(string description)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(description);
    }
}
