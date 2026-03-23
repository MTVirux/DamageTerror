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
    private readonly Action saveConfig;
    private int selectedIndex = -1; // -1 = active encounter (latest)
    private string searchFilter = string.Empty;
    private bool comboWasOpen;

    private static readonly (SortField Field, string Label)[] SortOptions =
    [
        (SortField.EncDps, "DPS"),
        (SortField.EncHps, "HPS"),
        (SortField.Damage, "Damage"),
        (SortField.Healed, "Healed"),
        (SortField.CritPct, "Crit%"),
        (SortField.Deaths, "Deaths"),
    ];

    public EncounterHeaderComponent(DataService dataService, Action saveConfig)
    {
        this.dataService = dataService;
        this.saveConfig = saveConfig;
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
        if (!dataService.Config.ShowSelectionBar)
            return;

        // Hide when pinned (optionally show with Ctrl+Shift)
        if (dataService.Config.HideSelectionBarWhenPinned && dataService.Config.PinMainWindow)
        {
            if (!dataService.Config.SelectionBarShowOnCtrlShift)
                return;

            var io = ImGui.GetIO();
            if (!(io.KeyCtrl && io.KeyShift))
                return;
        }

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

        // Apply selection bar styling
        var selBarBg = dataService.Config.SelectionBarBackgroundColor;
        var selBarPad = dataService.Config.SelectionBarHeight;
        var selBarTextCol = dataService.Config.SelectionBarTextColor;
        var hasSelBarBg = selBarBg.W > 0f;

        if (hasSelBarBg)
        {
            var drawList = ImGui.GetWindowDrawList();
            var curPos = ImGui.GetCursorScreenPos();
            var regionW = ImGui.GetContentRegionAvail().X;
            var frameH = ImGui.GetFrameHeight() + selBarPad * 2;
            drawList.AddRectFilled(curPos, new Vector2(curPos.X + regionW, curPos.Y + frameH), ImGui.ColorConvertFloat4ToU32(selBarBg));
        }

        if (selBarPad > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + selBarPad);

        ImGui.PushStyleColor(ImGuiCol.Text, selBarTextCol);

        // Sort dropdown width
        var currentSort = dataService.Config.SortBy;
        var sortLabel = SortOptions.FirstOrDefault(o => o.Field == currentSort).Label ?? "DPS";
        var sortArrow = dataService.Config.SortDescending ? "\u25BC" : "\u25B2";
        var sortPreview = $"{sortLabel} {sortArrow}";
        var sortComboWidth = dataService.Config.ShowSortDropdown
            ? ImGui.CalcTextSize("Damage \u25BC").X + ImGui.GetStyle().FramePadding.X * 2 + 20
            : 0f;

        // Encounter combo box (right-click for context menu)
        var comboWidth = dataService.Config.ShowSortDropdown
            ? ImGui.GetContentRegionAvail().X - sortComboWidth - ImGui.GetStyle().ItemSpacing.X
            : ImGui.GetContentRegionAvail().X;

        if (!dataService.Config.ShowEncounterPicker)
            goto SkipEncounterPicker;

        ImGui.SetNextItemWidth(comboWidth);
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
            ImGui.InputTextWithHint("##enc_search", "Search by zone, title, player, or job...", ref searchFilter, 256);

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
                    && !(hEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    && !h.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase)))
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
                    || (aEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                    || active.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase)))
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

SkipEncounterPicker:

        // Sort dropdown
        if (!dataService.Config.ShowSortDropdown)
            goto SkipSortDropdown;

        if (dataService.Config.ShowEncounterPicker)
            ImGui.SameLine();
        ImGui.SetNextItemWidth(sortComboWidth);
        if (ImGui.BeginCombo("##sort_combo", sortPreview))
        {
            foreach (var (field, label) in SortOptions)
            {
                var isSelected = currentSort == field;
                if (ImGui.Selectable(label, isSelected))
                {
                    if (isSelected)
                    {
                        dataService.Config.SortDescending = !dataService.Config.SortDescending;
                    }
                    else
                    {
                        dataService.Config.SortBy = field;
                        dataService.Config.SortDescending = true;
                    }
                    saveConfig();
                }
            }
            ImGui.EndCombo();
        }

SkipSortDropdown:

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

        if (selBarPad > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + selBarPad);

        ImGui.PopStyleColor();

        if (dataService.Config.ShowSelectionBarSeparator)
        {
            var drawList = ImGui.GetWindowDrawList();
            var sepPos = ImGui.GetCursorScreenPos();
            var sepW = ImGui.GetContentRegionAvail().X;
            drawList.AddLine(sepPos, new Vector2(sepPos.X + sepW, sepPos.Y), ImGui.ColorConvertFloat4ToU32(dataService.Config.SelectionBarSeparatorColor));
            ImGui.Spacing();
        }
    }

    /// <summary>
    /// Reset selection to follow the active encounter.
    /// </summary>
    public void ResetSelection()
    {
        selectedIndex = -1;
    }
}
