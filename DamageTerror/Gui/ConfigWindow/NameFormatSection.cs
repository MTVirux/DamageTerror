using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class NameFormatSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        var showName = config.ShowNameOnBar;
        if (ImGui.Checkbox("Show player name on bars", ref showName))
        {
            config.ShowNameOnBar = showName;
            changed = true;
        }
        ConfigHelpers.HelpMarker("These settings apply everywhere player names are displayed.");

        var showYou = config.ShowYouOnBar;
        if (ImGui.Checkbox("Show \"YOU\" instead of character name", ref showYou))
        {
            config.ShowYouOnBar = showYou;
            changed = true;
        }

        ImGui.Spacing();

        var nameFormatLabels = new[]
        {
            "Full Name",
            "First Name Only",
            "Last Name Only",
            "Initials (F. L.)",
            "Job Abbreviation",
            "Job Full Name",
            "Truncated (Name...)",
        };

        var selfFmt = (int)config.SelfNameFormat;
        if (ImGui.Combo("Your name", ref selfFmt, nameFormatLabels, nameFormatLabels.Length))
        {
            config.SelfNameFormat = (NameDisplayFormat)selfFmt;
            changed = true;
        }

        var othersFmt = (int)config.OthersNameFormat;
        if (ImGui.Combo("Others' names", ref othersFmt, nameFormatLabels, nameFormatLabels.Length))
        {
            config.OthersNameFormat = (NameDisplayFormat)othersFmt;
            changed = true;
        }

        if (config.SelfNameFormat == NameDisplayFormat.Truncated
            || config.OthersNameFormat == NameDisplayFormat.Truncated)
        {
            var truncLen = config.NameTruncateLength;
            if (ImGui.SliderInt("Max name length", ref truncLen, 3, 30))
            {
                config.NameTruncateLength = truncLen;
                changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Self name color.");

        var useSelfNameColor = config.UseSelfNameColor;
        if (ImGui.Checkbox("Custom name color for local player", ref useSelfNameColor))
        {
            config.UseSelfNameColor = useSelfNameColor;
            changed = true;
        }

        if (config.UseSelfNameColor)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Self name color", config.SelfNameColor, v => config.SelfNameColor = v);
            ImGui.Unindent();
        }

        return changed;
    }
}
