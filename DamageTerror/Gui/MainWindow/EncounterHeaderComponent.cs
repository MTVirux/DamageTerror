using Dalamud.Interface;
using ECommons.Automation;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public sealed class EncounterHeaderComponent
{
    private readonly DataService dataService;
    private readonly Configuration config;
    private int selectedIndex = -1;
    private string searchFilter = string.Empty;
    private bool comboWasOpen;

    // -1 = nothing pending; -2 = active encounter pending; >=0 = history index pending.
    private int pendingRemoveIndex = -1;
    private bool removeConfirmOpenedThisFrame;

    public EncounterHeaderComponent(DataService dataService, Configuration config)
    {
        this.dataService = dataService;
        this.config = config;
        dataService.OnNewEncounter += ResetToLive;
        dataService.Store.OnSampleDataLoaded += ResetToLive;
    }

    public void ResetToLive() => selectedIndex = -1;

    private string FormatEncounterLabel(CombatEncounter enc, string playerName = "", string idSuffix = "", DateTime? timestamp = null)
    {
        var icon = enc.IsActive ? "●" : "○";
        var title = !string.IsNullOrEmpty(enc.Title) ? $" — {enc.Title}" : "";
        var time = timestamp.HasValue ? $"  [{timestamp.Value.ToLocalTime():yyyy-MM-dd HH:mm}]" : "";
        var formattedPlayer = !string.IsNullOrEmpty(playerName)
            ? $"  ({NameFormatHelper.GetDisplayName(playerName, "", true, config)})"
            : "";
        var replayBadge = dataService.Store.IsReplayActive
            && ReferenceEquals(enc, dataService.Store.ActiveEncounter?.Encounter)
            ? "  [REPLAY]"
            : "";
        return $"{icon} {enc.ZoneName}{title}{time}  |  {enc.Duration}  |  {ValueFormatter.Format(enc.EncDps, ValueDisplayFormat.Raw, 0)} eDPS{formattedPlayer}{idSuffix}{replayBadge}";
    }

    public EncounterSnapshot? SelectedEncounter
        => selectedIndex == -1 ? dataService.Store.ActiveEncounter : dataService.Store.GetByIndex(selectedIndex);

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

    public void Render()
    {
        if (!config.ShowEncounterPicker)
            return;

        var encounter = SelectedEncounter;

        var previewLabel = encounter != null
            ? FormatEncounterLabel(encounter.Encounter, encounter.PlayerName, timestamp: encounter.Timestamp)
            : dataService.ConnectionStatus;

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
                    if (EncounterSearchHelper.MatchesFilter(active, filter))
                    {
                        var activeLabel = FormatEncounterLabel(aEnc, active.PlayerName ?? "", "##active", active.Timestamp);
                        if (ImGui.Selectable(activeLabel, selectedIndex == -1))
                        {
                            selectedIndex = -1;
                            if (dataService.Store.IsSampleSimulating || dataService.Store.IsReplayActive)
                                dataService.Store.ClearSampleData();
                        }

                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                        if (ImGui.BeginPopupContextItem("##enc_remove_active"))
                        {
                            ImGui.PushFont(UiBuilder.IconFont);
                            ImGui.Text(FontAwesomeIcon.TrashAlt.ToIconString());
                            ImGui.PopFont();
                            ImGui.SameLine();
                            if (ImGui.Selectable("Remove"))
                                pendingRemoveIndex = -2;
                            ImGui.EndPopup();
                        }
                        ImGui.PopStyleVar();
                    }
                }

                for (var i = history.Count - 1; i >= 0; i--)
                {
                    var h = history[i];
                    var hEnc = h.Encounter;
                    if (!EncounterSearchHelper.MatchesFilter(h, filter))
                        continue;
                    var label = FormatEncounterLabel(hEnc, h.PlayerName ?? "", $"##{i}", h.Timestamp);
                    if (ImGui.Selectable(label, selectedIndex == i))
                    {
                        selectedIndex = i;
                        if (dataService.Store.IsSampleSimulating)
                            dataService.Store.ClearSampleData();
                    }

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                    if (ImGui.BeginPopupContextItem($"##enc_remove_{i}"))
                    {
                        var encDuration = DurationHelper.ParseDuration(hEnc.Duration, 0f);
                        var canReplay = h.HasTimeline && encDuration > 0.5f;

                        ImGui.BeginDisabled(!canReplay);
                        if (ImGui.Selectable("Replay##rpyMenu"))
                        {
                            dataService.Store.LoadReplay(h);
                            selectedIndex = -1;
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndDisabled();
                        if (!canReplay && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            ImGui.SetTooltip("No timeline data available — replay unavailable for this encounter.");

                        ImGui.Separator();

                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(FontAwesomeIcon.TrashAlt.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        if (ImGui.Selectable("Remove"))
                            pendingRemoveIndex = i;
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

        // Pending-remove confirmation. Owned by this component so the popup
        // outlives the combo / context menu that triggered it.
        if (pendingRemoveIndex != -1 && !removeConfirmOpenedThisFrame)
        {
            ImGui.OpenPopup("##confirmRemoveEncounter");
            removeConfirmOpenedThisFrame = true;
        }

        var modalOpen = true;
        if (ImGui.BeginPopupModal("##confirmRemoveEncounter", ref modalOpen,
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
        {
            ImGui.TextUnformatted("Remove this encounter?");
            ImGui.TextDisabled("This cannot be undone.");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.65f, 0.2f, 0.2f, 1f));
            if (ImGui.Button("Remove", new Vector2(100, 0)))
            {
                if (pendingRemoveIndex == -2)
                {
                    dataService.Store.RemoveActive();
                    selectedIndex = -1;
                }
                else if (pendingRemoveIndex >= 0)
                {
                    dataService.Store.RemoveHistory(pendingRemoveIndex);
                    if (selectedIndex == pendingRemoveIndex) selectedIndex = -1;
                    else if (selectedIndex > pendingRemoveIndex) selectedIndex--;
                }
                pendingRemoveIndex = -1;
                removeConfirmOpenedThisFrame = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                pendingRemoveIndex = -1;
                removeConfirmOpenedThisFrame = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
        else if (!modalOpen)
        {
            // User dismissed via the X — reset pending state.
            pendingRemoveIndex = -1;
            removeConfirmOpenedThisFrame = false;
        }

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

    public void ResetSelection() => selectedIndex = -1;
}
