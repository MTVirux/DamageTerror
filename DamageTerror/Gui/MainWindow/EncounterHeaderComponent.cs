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

        // Build the preview label for the combo box
        string previewLabel;
        if (encounter != null)
        {
            var enc = encounter.Encounter;
            var statusIcon = enc.IsActive ? "●" : "○";
            var primaryValue = dataService.Config.ShowHps
                ? $"{enc.EncHps:F1} rHPS"
                : $"{enc.EncDps:F1} rDPS";
            previewLabel = $"{statusIcon} {enc.ZoneName}  |  {enc.Duration}  |  {primaryValue}";
        }
        else
        {
            previewLabel = dataService.ConnectionStatus;
        }

        // Encounter combo box
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##enc_combo", previewLabel))
        {
            var history = dataService.Store.History;
            var active = dataService.Store.ActiveEncounter;

            // History entries (oldest first)
            for (var i = 0; i < history.Count; i++)
            {
                var h = history[i];
                var hEnc = h.Encounter;
                var hValue = dataService.Config.ShowHps
                    ? $"{hEnc.EncHps:F1} rHPS"
                    : $"{hEnc.EncDps:F1} rDPS";
                var label = $"○ {hEnc.ZoneName}  |  {hEnc.Duration}  |  {hValue}##{i}";
                if (ImGui.Selectable(label, selectedIndex == i))
                    selectedIndex = i;
            }

            // Active encounter
            if (active != null)
            {
                var aEnc = active.Encounter;
                var aValue = dataService.Config.ShowHps
                    ? $"{aEnc.EncHps:F1} rHPS"
                    : $"{aEnc.EncDps:F1} rDPS";
                var activeLabel = $"● {aEnc.ZoneName}  |  {aEnc.Duration}  |  {aValue}##active";
                if (ImGui.Selectable(activeLabel, selectedIndex == -1))
                    selectedIndex = -1;
            }

            ImGui.EndCombo();
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
