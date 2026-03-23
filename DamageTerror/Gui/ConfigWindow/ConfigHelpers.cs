using Dalamud.Bindings.ImGui;
using DamageTerror.Helpers;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Shared helper methods for config window drawing.
/// </summary>
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
                    : JobColorHelper.GetDefaultJobColor(job);

                var fullName = JobNameHelper.GetFullName(job);
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
}
