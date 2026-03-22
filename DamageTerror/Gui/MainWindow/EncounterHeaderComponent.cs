using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using Dalamud.Bindings.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Renders the encounter header bar: zone name, duration, rDPS, and history navigation.
/// </summary>
public class EncounterHeaderComponent : IUIComponent
{
    private readonly DataService dataService;
    private int selectedIndex = -1; // -1 = active encounter (latest)

    public EncounterHeaderComponent(DataService dataService)
    {
        this.dataService = dataService;
    }

    /// <summary>
    /// The currently selected encounter snapshot to display.
    /// Returns null if no data is available.
    /// </summary>
    public EncounterSnapshot? SelectedEncounter
    {
        get
        {
            if (selectedIndex == -1)
                return dataService.Store.ActiveEncounter;

            return dataService.Store.GetByIndex(selectedIndex);
        }
    }

    public void Render()
    {
        var totalCount = dataService.Store.TotalCount;
        var encounter = SelectedEncounter;

        var windowWidth = ImGui.GetContentRegionAvail().X;

        // Left arrow
        if (totalCount > 1)
        {
            var canGoLeft = selectedIndex == -1 ? totalCount > 1 : selectedIndex > 0;
            if (!canGoLeft) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##enc_left", ImGuiDir.Left))
            {
                if (selectedIndex == -1)
                    selectedIndex = totalCount - 2; // Go to last history item
                else
                    selectedIndex--;
            }
            if (!canGoLeft) ImGui.EndDisabled();
            ImGui.SameLine();
        }

        // Encounter info
        if (encounter != null)
        {
            var enc = encounter.Encounter;
            var statusIcon = enc.IsActive ? "●" : "○";
            var primaryValue = dataService.Config.ShowHps
                ? $"{enc.EncHps:F1} rHPS"
                : $"{enc.EncDps:F1} rDPS";

            var text = $"{statusIcon} {enc.ZoneName}  |  {enc.Duration}  |  {primaryValue}";
            ImGui.TextUnformatted(text);
        }
        else
        {
            var status = dataService.ConnectionStatus;
            ImGui.TextDisabled(status);
        }

        // Right arrow
        if (totalCount > 1)
        {
            ImGui.SameLine(windowWidth - 22);
            var canGoRight = selectedIndex != -1;
            if (!canGoRight) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##enc_right", ImGuiDir.Right))
            {
                if (selectedIndex >= totalCount - 2)
                    selectedIndex = -1; // Back to active
                else
                    selectedIndex++;
            }
            if (!canGoRight) ImGui.EndDisabled();
        }

        ImGui.Separator();
    }

    /// <summary>
    /// Reset selection to follow the active encounter.
    /// </summary>
    public void ResetSelection()
    {
        selectedIndex = -1;
    }
}
