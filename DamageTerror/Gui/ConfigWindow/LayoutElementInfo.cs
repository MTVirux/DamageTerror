namespace DamageTerror.Gui.ConfigWindow;

public static class LayoutElementInfo
{
    public static readonly Dictionary<LayoutElement, string> Labels = new()
    {
        { LayoutElement.EncounterSelect, "Encounter Select" },
        { LayoutElement.ReplayBar, "Replay Bar" },
        { LayoutElement.MeterTabs, "Meter Tabs" },
        { LayoutElement.StatusBar, "Status Bar" },
        { LayoutElement.CombatantBars, "Combatant Bars" },
    };

    public static readonly Dictionary<LayoutElement, string> Descriptions = new()
    {
        { LayoutElement.EncounterSelect, "The encounter picker and sort controls." },
        { LayoutElement.ReplayBar, "Playback controls shown only during encounter replay." },
        { LayoutElement.MeterTabs, "Filter tabs (DPS, Heal, Tank, etc.) when enabled." },
        { LayoutElement.StatusBar, "Combat timer, personal DPS, and raid DPS summary." },
        { LayoutElement.CombatantBars, "The main combatant list with bars and details." },
    };

    public static string Label(LayoutElement element) => Labels.GetValueOrDefault(element, element.ToString());
}
