using Dalamud.Interface;
using Dalamud.Plugin.Services;
using ECommons.Automation;
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
    private string searchFilter = string.Empty;
    private bool comboWasOpen;

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
            var titlePart = !string.IsNullOrEmpty(enc.Title) ? $" — {enc.Title}" : "";
            previewLabel = $"{statusIcon} {enc.ZoneName}{titlePart}  |  {enc.Duration}  |  {primaryValue}";
        }
        else
        {
            previewLabel = dataService.ConnectionStatus;
        }

        // Encounter combo box (right-click for context menu)
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##enc_combo", previewLabel))
        {
            // Reset search filter when combo first opens
            if (!comboWasOpen)
            {
                searchFilter = string.Empty;
                comboWasOpen = true;
            }

            // Search input at the top of the dropdown
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##enc_search", "Search encounters...", ref searchFilter, 256);

            var history = dataService.Store.History;
            var active = dataService.Store.ActiveEncounter;
            var filter = searchFilter.Trim();

            // History entries (oldest first)
            for (var i = 0; i < history.Count; i++)
            {
                var h = history[i];
                var hEnc = h.Encounter;
                if (filter.Length > 0
                    && !hEnc.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    && !(hEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;
                var hValue = dataService.Config.ShowHps
                    ? $"{hEnc.EncHps:F1} rHPS"
                    : $"{hEnc.EncDps:F1} rDPS";
                var hTitle = !string.IsNullOrEmpty(hEnc.Title) ? $" — {hEnc.Title}" : "";
                var hIcon = hEnc.IsActive ? "●" : "○";
                var label = $"{hIcon} {hEnc.ZoneName}{hTitle}  |  {hEnc.Duration}  |  {hValue}##{i}";
                if (ImGui.Selectable(label, selectedIndex == i))
                    selectedIndex = i;

                if (ImGui.BeginPopupContextItem($"##enc_remove_{i}"))
                {
                    if (ImGui.Selectable("Remove"))
                    {
                        dataService.Store.RemoveHistory(i);
                        if (selectedIndex == i)
                            selectedIndex = -1;
                        else if (selectedIndex > i)
                            selectedIndex--;
                    }
                    ImGui.EndPopup();
                }
            }

            // Active encounter
            if (active != null)
            {
                var aEnc = active.Encounter;
                if (filter.Length == 0
                    || aEnc.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || (aEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    var aValue = dataService.Config.ShowHps
                        ? $"{aEnc.EncHps:F1} rHPS"
                        : $"{aEnc.EncDps:F1} rDPS";
                    var aTitle = !string.IsNullOrEmpty(aEnc.Title) ? $" — {aEnc.Title}" : "";
                    var aIcon = aEnc.IsActive ? "●" : "○";
                    var activeLabel = $"{aIcon} {aEnc.ZoneName}{aTitle}  |  {aEnc.Duration}  |  {aValue}##active";
                    if (ImGui.Selectable(activeLabel, selectedIndex == -1))
                        selectedIndex = -1;
                }
            }

            ImGui.EndCombo();
        }
        else
        {
            comboWasOpen = false;
        }

        // Right-click context menu on the combo
        if (ImGui.BeginPopupContextItem("##enc_context"))
        {
            var scissorsIcon = FontAwesomeIcon.Cut.ToIconString();
            ImGui.PushFont(UiBuilder.IconFont);
            var iconSize = ImGui.CalcTextSize(scissorsIcon);
            ImGui.PopFont();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(scissorsIcon);
            ImGui.PopFont();
            ImGui.SameLine();
            if (ImGui.Selectable("Cut Encounter"))
            {
                Chat.SendMessage("/e end");
            }

            ImGui.EndPopup();
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
