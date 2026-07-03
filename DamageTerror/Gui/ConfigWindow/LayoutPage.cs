namespace DamageTerror.Gui.ConfigWindow;

public static class LayoutPage
{
    private static readonly Dictionary<LayoutElement, string> ElementLabels = new()
    {
        { LayoutElement.EncounterSelect, "Encounter Select" },
        { LayoutElement.ReplayBar, "Replay Bar" },
        { LayoutElement.MeterTabs, "Meter Tabs" },
        { LayoutElement.StatusBar, "Status Bar" },
        { LayoutElement.CombatantBars, "Combatant Bars" },
    };

    private static readonly Dictionary<LayoutElement, string> ElementDescriptions = new()
    {
        { LayoutElement.EncounterSelect, "The encounter picker and sort controls." },
        { LayoutElement.ReplayBar, "Playback controls shown only during encounter replay." },
        { LayoutElement.MeterTabs, "Filter tabs (DPS, Heal, Tank, etc.) when enabled." },
        { LayoutElement.StatusBar, "Combat timer, personal DPS, and raid DPS summary." },
        { LayoutElement.CombatantBars, "The main combatant list with bars and details." },
    };

    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Element Order", ImGuiTreeNodeFlags.DefaultOpen))
        {
        ConfigHelpers.HelpMarker("Drag the components to change their rendering order in the meter window.\nUse the arrow buttons to move items up or down.");
        ImGui.Spacing();

        EnsureLayoutComplete(config);

        for (var i = 0; i < config.Layout.Count; i++)
        {
            var element = config.Layout[i];
            var label = ElementLabels.GetValueOrDefault(element, element.ToString());

            ImGui.PushID(i);

            changed |= ConfigHelpers.ReorderArrows(config.Layout, i);

            var ctrlShiftOnly = config.CtrlShiftOnlyElements.Contains(element);
            if (ImGui.Checkbox($"##ctrlShift{i}", ref ctrlShiftOnly))
            {
                if (ctrlShiftOnly)
                    config.CtrlShiftOnlyElements.Add(element);
                else
                    config.CtrlShiftOnlyElements.Remove(element);
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Only show this component while the modifier key is active.");

            ImGui.SameLine();

            ImGui.Text($"{i + 1}.  {label}");

            if (ImGui.IsItemHovered() && ElementDescriptions.TryGetValue(element, out var desc))
                ImGui.SetTooltip(desc);

            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        changed |= ConfigHelpers.CheckboxProp("Hide window header##layout", config.HideWindowHeader, v => config.HideWindowHeader = v);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset to Default"))
        {
            config.Layout = new List<LayoutElement>
            {
                LayoutElement.EncounterSelect,
                LayoutElement.ReplayBar,
                LayoutElement.MeterTabs,
                LayoutElement.StatusBar,
                LayoutElement.CombatantBars,
            };
            changed = true;
        }
        }

        return changed;
    }

    public static void EnsureLayoutComplete(Configuration config)
    {
        var allElements = Enum.GetValues<LayoutElement>();

        foreach (var el in allElements)
        {
            if (config.Layout.Contains(el)) continue;
            if (el == LayoutElement.ReplayBar)
            {
                var insertAt = Math.Min(1, config.Layout.Count);
                config.Layout.Insert(insertAt, el);
            }
            else
            {
                config.Layout.Add(el);
            }
        }

        var seen = new HashSet<LayoutElement>();
        config.Layout.RemoveAll(el => !seen.Add(el));
    }
}
