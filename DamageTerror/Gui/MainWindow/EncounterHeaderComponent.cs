using Dalamud.Interface;
using Dalamud.Plugin.Services;
using ECommons.Automation;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using Dalamud.Bindings.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class EncounterHeaderComponent : IUIComponent
{
    private readonly DataService dataService;
    private readonly Action saveConfig;
    private int selectedIndex = -1; // -1 = active encounter (latest)
    private string searchFilter = string.Empty;
    private bool comboWasOpen;



    public EncounterHeaderComponent(DataService dataService, Action saveConfig)
    {
        this.dataService = dataService;
        this.saveConfig = saveConfig;
    }

    public EncounterSnapshot? SelectedEncounter
    {
        get
        {
            if (selectedIndex == -1)
                return dataService.Store.ActiveEncounter;

            return dataService.Store.GetByIndex(selectedIndex);
        }
    }

    public float GetHeight()
    {
        if (!dataService.Config.ShowSelectionBar)
            return 0f;

        var pad = dataService.Config.SelectionBarHeight;
        var frameH = ImGui.GetFrameHeight() + pad * 2;
        var sepH = dataService.Config.ShowSelectionBarSeparator ? ImGui.GetStyle().ItemSpacing.Y + 1f : 0f;
        return frameH + sepH;
    }

    public void Render()
    {
        if (!dataService.Config.ShowSelectionBar)
            return;

        var totalCount = dataService.Store.TotalCount;
        var encounter = SelectedEncounter;

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

        var comboWidth = ImGui.GetContentRegionAvail().X;

        if (dataService.Config.ShowEncounterPicker)
        {
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("##enc_combo", previewLabel))
            {
                if (!comboWasOpen)
                {
                    searchFilter = string.Empty;
                    comboWasOpen = true;
                }

                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##enc_search", "Search by zone, title, player, or job...", ref searchFilter, 256);

                var history = dataService.Store.History;
                var active = dataService.Store.ActiveEncounter;
                var filter = searchFilter.Trim();

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
        }

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

    public void ResetSelection()
    {
        selectedIndex = -1;
    }
}
