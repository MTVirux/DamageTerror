using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabSectionHelpers
{
    public static bool DrawColumnColorPopup(BarColumn col, Dictionary<BarColumn, Vector4> colors)
    {
        var changed = false;
        var hasColor = colors.TryGetValue(col, out var current);

        if (!hasColor)
        {
            current = new Vector4(1f, 1f, 1f, 1f);
        }

        var useCustom = hasColor;
        if (ImGui.Checkbox("Use custom color", ref useCustom))
        {
            if (useCustom)
            {
                colors[col] = current;
            }
            else
            {
                colors.Remove(col);
            }
            changed = true;
        }

        if (useCustom)
        {
            if (ImGui.ColorEdit4($"##colClr_{col}", ref current, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
            {
                colors[col] = current;
                changed = true;
            }
        }

        return changed;
    }
}
