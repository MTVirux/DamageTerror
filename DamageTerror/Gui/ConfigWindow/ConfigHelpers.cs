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

    public static bool CheckboxProp(string label, bool value, Action<bool> setter)
    {
        var v = value;
        if (ImGui.Checkbox(label, ref v))
        {
            setter(v);
            return true;
        }
        return false;
    }

    public static bool SliderFloatProp(string label, float value, float min, float max, string format, Action<float> setter, float width = 0f)
    {
        var v = value;
        if (width > 0f) ImGui.SetNextItemWidth(width);
        if (ImGui.SliderFloat(label, ref v, min, max, format))
        {
            setter(v);
            return true;
        }
        return false;
    }

    public static bool SliderIntProp(string label, int value, int min, int max, Action<int> setter, float width = 0f)
    {
        var v = value;
        if (width > 0f) ImGui.SetNextItemWidth(width);
        if (ImGui.SliderInt(label, ref v, min, max))
        {
            setter(v);
            return true;
        }
        return false;
    }

    public static bool ComboProp(string label, int value, string[] items, Action<int> setter, float width = 0f)
    {
        var v = value;
        if (width > 0f) ImGui.SetNextItemWidth(width);
        if (ImGui.Combo(label, ref v, items, items.Length))
        {
            setter(v);
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
                    : JobRegistry.GetDefaultColor(job);

                var fullName = JobRegistry.GetFullName(job);
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
            changed |= CheckboxProp($"Show skill markers##sec_{id}", mc.ShowMarkers, v => mc.ShowMarkers = v);

            changed |= ColorEditProp($"Marker color##sec_{id}", mc.MarkerColor, v => mc.MarkerColor = v);

            changed |= SliderFloatProp($"Marker size##sec_{id}", mc.MarkerSize, 1f, 10f, "%.1f", v => mc.MarkerSize = v, 150);

            changed |= CheckboxProp($"Color by crit/DH##sec_{id}", mc.ShowCritMarkers, v => mc.ShowCritMarkers = v);

            if (mc.ShowCritMarkers)
            {
                changed |= ColorEditProp($"Crit ! color##sec_{id}", mc.CritMarkerColor, v => mc.CritMarkerColor = v);
                changed |= ColorEditProp($"Direct Hit !! color##sec_{id}", mc.DirectHitMarkerColor, v => mc.DirectHitMarkerColor = v);
                changed |= ColorEditProp($"Crit Direct Hit !!! color##sec_{id}", mc.CritDirectHitMarkerColor, v => mc.CritDirectHitMarkerColor = v);
            }

            ImGui.Spacing();
            ImGui.TextDisabled("DoT / HoT Markers");

            changed |= CheckboxProp($"Show DoT/HoT tick markers##sec_{id}", mc.ShowDoTTickMarkers, v => mc.ShowDoTTickMarkers = v);

            if (mc.ShowDoTTickMarkers)
            {
                changed |= ColorEditProp($"Tick color##sec_{id}", mc.DoTTickColor, v => mc.DoTTickColor = v);
                changed |= SliderFloatProp($"Tick size##sec_{id}", mc.DoTTickMarkerSize, 1f, 10f, "%.1f", v => mc.DoTTickMarkerSize = v, 150);
            }

            changed |= CheckboxProp($"Show DoT/HoT application markers##sec_{id}", mc.ShowDoTApplicationMarkers, v => mc.ShowDoTApplicationMarkers = v);

            if (mc.ShowDoTApplicationMarkers)
            {
                changed |= ColorEditProp($"Application color##sec_{id}", mc.DoTApplicationColor, v => mc.DoTApplicationColor = v);
                changed |= SliderFloatProp($"Application size##sec_{id}", mc.DoTApplicationMarkerSize, 1f, 10f, "%.1f", v => mc.DoTApplicationMarkerSize = v, 150);
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

    /// <summary>
    /// Draws up/down reorder arrows for <paramref name="list"/>[<paramref name="index"/>], disabled at the edges,
    /// followed by a trailing SameLine. Returns true if the item was moved. Ids must be scoped by an outer PushID.
    /// </summary>
    internal static bool ReorderArrows<T>(IList<T> list, int index)
    {
        var changed = false;

        var canUp = index > 0;
        if (!canUp) ImGui.BeginDisabled();
        if (ImGui.ArrowButton("##up", ImGuiDir.Up))
        {
            (list[index - 1], list[index]) = (list[index], list[index - 1]);
            changed = true;
        }
        if (!canUp) ImGui.EndDisabled();

        ImGui.SameLine();

        var canDown = index < list.Count - 1;
        if (!canDown) ImGui.BeginDisabled();
        if (ImGui.ArrowButton("##down", ImGuiDir.Down))
        {
            (list[index], list[index + 1]) = (list[index + 1], list[index]);
            changed = true;
        }
        if (!canDown) ImGui.EndDisabled();

        ImGui.SameLine();

        return changed;
    }
}
