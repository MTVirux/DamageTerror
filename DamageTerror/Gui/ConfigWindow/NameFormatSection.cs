using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class NameFormatSection
{
    public static bool Draw(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.CheckboxProp("Show player name on bars", config.ShowNameOnBar, v => config.ShowNameOnBar = v);
        ConfigHelpers.HelpMarker("These settings apply everywhere player names are displayed.");

        changed |= ConfigHelpers.CheckboxProp("Show \"YOU\" instead of character name", config.ShowYouOnBar, v => config.ShowYouOnBar = v);

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

        changed |= ConfigHelpers.ComboProp("Your name", (int)config.SelfNameFormat, nameFormatLabels, v => config.SelfNameFormat = (NameDisplayFormat)v);

        changed |= ConfigHelpers.ComboProp("Others' names", (int)config.OthersNameFormat, nameFormatLabels, v => config.OthersNameFormat = (NameDisplayFormat)v);

        if (config.SelfNameFormat == NameDisplayFormat.Truncated
            || config.OthersNameFormat == NameDisplayFormat.Truncated)
        {
            changed |= ConfigHelpers.SliderIntProp("Max name length", config.NameTruncateLength, 3, 30, v => config.NameTruncateLength = v);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Self name color.");

        changed |= ConfigHelpers.CheckboxProp("Custom name color for local player", config.UseSelfNameColor, v => config.UseSelfNameColor = v);

        if (config.UseSelfNameColor)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Self name color", config.SelfNameColor, v => config.SelfNameColor = v);
            ImGui.Unindent();
        }

        return changed;
    }
}
