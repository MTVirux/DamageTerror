using Dalamud.Interface;
using Dalamud.Plugin.Services;
using DamageTerror.Gui;
using ECommons.Automation;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using Dalamud.Bindings.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class EncounterHeaderComponent
{
    private readonly DataService dataService;
    private readonly Configuration config;
    private int selectedIndex = -1;
    private string searchFilter = string.Empty;
    private bool comboWasOpen;

    public EncounterHeaderComponent(DataService dataService, Configuration config)
    {
        this.dataService = dataService;
        this.config = config;
        dataService.OnNewEncounter += ResetToLive;
    }

    public void ResetToLive() => selectedIndex = -1;

    private string FormatEncounterLabel(CombatEncounter enc, string playerName = "", string suffix = "", DateTime? timestamp = null)
    {
        var icon = enc.IsActive ? "●" : "○";
        var title = !string.IsNullOrEmpty(enc.Title) ? $" — {enc.Title}" : "";
        var time = timestamp.HasValue ? $"  [{timestamp.Value.ToLocalTime():yyyy-MM-dd HH:mm}]" : "";
        var formattedPlayer = !string.IsNullOrEmpty(playerName)
            ? $"  ({NameFormatHelper.GetDisplayName(playerName, "", true, config)})"
            : "";
        return $"{icon} {enc.ZoneName}{title}{time}  |  {enc.Duration}  |  {ValueFormatter.Format(enc.EncDps, ValueDisplayFormat.Raw, 1)} rDPS{formattedPlayer}{suffix}";
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

    public bool IsViewingLive => selectedIndex == -1;

    public bool IsComboOpen => comboWasOpen;
    public bool RequestContextMenu { get; private set; }

    public float GetHeight()
    {
        if (!config.ShowEncounterPicker)
            return 0f;

        var pad = config.SelectionBarHeight;
        var frameH = ImGui.GetFrameHeight() + pad * 2;
        var sepH = config.ShowSelectionBarSeparator ? ImGui.GetStyle().ItemSpacing.Y + 1f : 0f;
        return frameH + sepH;
    }

    public void Render(RenderContext ctx) => Render();

    public void Render()
    {
        if (!config.ShowEncounterPicker)
            return;

        var totalCount = dataService.Store.TotalCount;
        var encounter = SelectedEncounter;

        string previewLabel;
        if (encounter != null)
        {
            var enc = encounter.Encounter;
            previewLabel = FormatEncounterLabel(enc, encounter.PlayerName, timestamp: encounter.Timestamp);
        }
        else
        {
            previewLabel = dataService.ConnectionStatus;
        }

        var selBarBg = config.SelectionBarBackgroundColor;
        var selBarPad = config.SelectionBarHeight;
        var selBarTextCol = config.SelectionBarTextColor;
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

                if (active != null)
                {
                    var aEnc = active.Encounter;
                    if (filter.Length == 0
                        || aEnc.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || (aEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (active.PlayerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                        || active.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                            || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                    {
                        var activeLabel = FormatEncounterLabel(aEnc, active.PlayerName ?? "", "##active", active.Timestamp);
                        if (ImGui.Selectable(activeLabel, selectedIndex == -1))
                            selectedIndex = -1;

                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                        if (ImGui.BeginPopupContextItem("##enc_remove_active"))
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Text(FontAwesomeIcon.TrashAlt.ToIconString());
                            ImGui.PopFont();
                            ImGui.SameLine();
                            if (ImGui.Selectable("Remove"))
                            {
                                dataService.Store.RemoveActive();
                                selectedIndex = -1;
                            }
                            ImGui.EndPopup();
                        }
                        ImGui.PopStyleVar();
                    }
                }

                for (var i = history.Count - 1; i >= 0; i--)
                {
                    var h = history[i];
                    var hEnc = h.Encounter;
                    if (filter.Length > 0
                        && !hEnc.ZoneName.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        && !(hEnc.Title?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                        && !(h.PlayerName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false)
                        && !h.Combatants.Any(c => c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                            || c.Job.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    var label = FormatEncounterLabel(hEnc, h.PlayerName ?? "", $"##{i}", h.Timestamp);
                    if (ImGui.Selectable(label, selectedIndex == i))
                        selectedIndex = i;

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                    if (ImGui.BeginPopupContextItem($"##enc_remove_{i}"))
                    {
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.TrashAlt.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
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
                    ImGui.PopStyleVar();
                }

                ImGui.EndCombo();
            }
            else
            {
                comboWasOpen = false;
            }

        RequestContextMenu = ImGui.IsItemClicked(ImGuiMouseButton.Right);

        if (selBarPad > 0f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + selBarPad);

        ImGui.PopStyleColor();

        if (config.ShowSelectionBarSeparator)
        {
            var drawList = ImGui.GetWindowDrawList();
            var sepPos = ImGui.GetCursorScreenPos();
            var sepW = ImGui.GetContentRegionAvail().X;
            drawList.AddLine(sepPos, new Vector2(sepPos.X + sepW, sepPos.Y), ImGui.ColorConvertFloat4ToU32(config.SelectionBarSeparatorColor));
            ImGui.Spacing();
        }
    }

    public void ResetSelection()
    {
        selectedIndex = -1;
    }
}
