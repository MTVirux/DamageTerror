using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class PopoutTabWindow : Window, IDisposable
{
    private readonly DamageTerrorPlugin plugin;
    private readonly Guid tabId;
    private readonly EncounterHeaderComponent headerComponent;
    private readonly CombatantBarComponent barComponent;
    private readonly GraphViewComponent graphViewComponent;
    private readonly CombatantDetailPanel detailPanel;
    private readonly StatusBarComponent statusBarComponent;
    private TitleBarButton? lockButton;
    private DateTime? combatEndTime;
    private bool suppressClose;

    public Guid TabId => tabId;

    public PopoutTabWindow(DamageTerrorPlugin plugin, ITextureProvider textureProvider, MeterTab tab)
        : base($"DT \u2014 {tab.Name}##dtPopout_{tab.Id}")
    {
        this.plugin = plugin;
        this.tabId = tab.Id;

        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 150),
            MaximumSize = new Vector2(2000, 2000),
        };

        this.headerComponent = new EncounterHeaderComponent(plugin.DataService, plugin.Config);
        this.barComponent = new CombatantBarComponent(plugin.Config, textureProvider);
        this.graphViewComponent = new GraphViewComponent(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker);
        this.detailPanel = new CombatantDetailPanel(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker);
        this.statusBarComponent = new StatusBarComponent(plugin.Config);

        var pinned = GetPin()?.Pinned ?? false;
        lockButton = new TitleBarButton
        {
            Icon = pinned ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
            IconOffset = new Vector2(3, 2),
            ShowTooltip = () => ImGui.SetTooltip("Lock window position and size"),
        };
        lockButton.Click = (m) =>
        {
            if (m == ImGuiMouseButton.Left)
            {
                var pin = GetOrCreatePin();
                if (!pin.Pinned)
                {
                    pin.Pos = ImGui.GetWindowPos();
                    pin.Size = ImGui.GetWindowSize();
                }

                pin.Pinned = !pin.Pinned;
                plugin.SaveConfig();
                lockButton!.Icon = pin.Pinned ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }
        };
        TitleBarButtons.Add(lockButton);
    }

    public void Dispose()
    {
    }

    private MeterTab? FindTab()
    {
        return plugin.Config.MeterTabs.FirstOrDefault(t => t.Id == tabId);
    }

    public override bool DrawConditions()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return false;

        if (!IsDutyTypeEnabled())
            return false;

        if (!plugin.Config.HideOutOfCombat)
        {
            combatEndTime = null;
            return true;
        }

        if (Svc.Condition[ConditionFlag.InCombat])
        {
            combatEndTime = null;
            return true;
        }

        combatEndTime ??= DateTime.UtcNow;
        var elapsed = (DateTime.UtcNow - combatEndTime.Value).TotalSeconds;
        return elapsed < plugin.Config.HideOutOfCombatDelay;
    }

    private bool IsDutyTypeEnabled()
    {
        var contentType = Content.ContentType;
        var config = plugin.Config;
        return contentType switch
        {
            ECommons.GameHelpers.ContentType.Dungeon => config.EnableInDungeons,
            ECommons.GameHelpers.ContentType.Trial => config.EnableInTrials,
            ECommons.GameHelpers.ContentType.Raid => config.EnableInRaids,
            ECommons.GameHelpers.ContentType.ARaid => config.EnableInAllianceRaids,
            ECommons.GameHelpers.ContentType.DeepDungeon => config.EnableInDeepDungeons,
            ECommons.GameHelpers.ContentType.FieldOperations => config.EnableInFieldOperations,
            ECommons.GameHelpers.ContentType.FieldRaid => config.EnableInFieldRaids,
            ECommons.GameHelpers.ContentType.Criterion => config.EnableInCriterion,
            ECommons.GameHelpers.ContentType.Variant => config.EnableInVariant,
            ECommons.GameHelpers.ContentType.PVP => config.EnableInPvP,
            ECommons.GameHelpers.ContentType.OverWorld => config.EnableInOverworld,
            _ => config.EnableInOverworld,
        };
    }

    private PopoutWindowPin? GetPin()
    {
        plugin.Config.PopoutWindowPins.TryGetValue(tabId, out var pin);
        return pin;
    }

    private PopoutWindowPin GetOrCreatePin()
    {
        if (!plugin.Config.PopoutWindowPins.TryGetValue(tabId, out var pin))
        {
            pin = new PopoutWindowPin();
            plugin.Config.PopoutWindowPins[tabId] = pin;
        }
        return pin;
    }

    public override void PreDraw()
    {
        RespectCloseHotkey = !plugin.Config.IgnoreEscClose;

        var io = ImGui.GetIO();
        var forceShowHeader = io.KeyCtrl && io.KeyShift;

        if (plugin.Config.HideWindowHeader && !forceShowHeader)
            Flags |= ImGuiWindowFlags.NoTitleBar;
        else
            Flags &= ~ImGuiWindowFlags.NoTitleBar;

        var pin = GetPin();
        if (pin is { Pinned: true })
        {
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

            if (pin.Pos.X > 1f && pin.Pos.Y > 1f && pin.Size.X > 1f && pin.Size.Y > 1f)
            {
                ImGui.SetNextWindowPos(pin.Pos);
                ImGui.SetNextWindowSize(pin.Size);
            }
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoResize;
        }

        if (lockButton != null)
            lockButton.Icon = (pin?.Pinned ?? false) ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;

        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, plugin.Config.WindowBackgroundColor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, plugin.Config.WindowRounding);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    public override void Draw()
    {
        using var fontScope = plugin.Config.EnableCustomFont ? plugin.FontService?.PushFont() : null;

        var config = plugin.Config;
        var tab = FindTab();
        if (tab == null)
        {
            IsOpen = false;
            return;
        }

        // --- Data resolution ---
        var encounter = headerComponent.SelectedEncounter;
        var currentPlayerName = !string.IsNullOrEmpty(encounter?.PlayerName)
            ? encounter.PlayerName
            : plugin.DataService.PlayerName;

        var sortBy = tab.SortBy;
        var sortDesc = tab.SortDescending;

        // Resolve party membership for group filtering
        HashSet<string>? partyNames = null;
        HashSet<string>? allianceNames = null;
        if (tab.GroupFilter is GroupFilter.Party or GroupFilter.Alliance)
        {
            partyNames = plugin.PartyService.GetPartyMemberNames();
            allianceNames = plugin.PartyService.GetAllianceMemberNames();
        }

        List<CombatantEntry>? combatants = null;
        double maxVal = 0;

        if (encounter != null)
        {
            combatants = MainWindow.GetSortedCombatants(encounter, sortBy, sortDesc, tab, partyNames, allianceNames);
            if (combatants.Count > 0)
                maxVal = combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy));
        }

        // Calculate height reserved for elements rendered after CombatantBars
        float afterBarsHeight = 0f;
        bool passedBars = false;
        var ctrlShiftHeldForHeight = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
        foreach (var el in config.Layout)
        {
            // Skip MeterTabs in popout — this window is already a single tab
            if (el == LayoutElement.MeterTabs)
                continue;
            if (config.CtrlShiftOnlyElements.Contains(el) && !ctrlShiftHeldForHeight)
                continue;
            if (passedBars)
            {
                switch (el)
                {
                    case LayoutElement.StatusBar when encounter != null:
                        afterBarsHeight += statusBarComponent.GetHeight();
                        break;
                    case LayoutElement.EncounterSelect:
                        afterBarsHeight += headerComponent.GetHeight();
                        break;
                }
            }
            else if (el == LayoutElement.CombatantBars)
            {
                passedBars = true;
            }
        }

        // --- Force disconnected message even if EncounterSelect is hidden ---
        if (!plugin.DataService.IsConnected && encounter == null)
        {
            ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
            if (ImGui.Button("Reconnect"))
                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
            return;
        }

        // --- Render components in configured layout order ---
        var ctrlShiftHeld = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
        foreach (var element in config.Layout)
        {
            // Skip MeterTabs — this popout is already a single tab
            if (element == LayoutElement.MeterTabs)
                continue;
            if (config.CtrlShiftOnlyElements.Contains(element) && !ctrlShiftHeld)
                continue;

            switch (element)
            {
                case LayoutElement.EncounterSelect:
                    headerComponent.Render();
                    if (encounter == null)
                    {
                        if (plugin.DataService.IsConnected)
                            ImGui.TextDisabled("No combat data, go hit something!");
                        else
                        {
                            ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
                            if (ImGui.Button("Reconnect"))
                                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
                        }
                        return;
                    }
                    break;

                case LayoutElement.StatusBar:
                    if (encounter != null)
                        statusBarComponent.Render(encounter, currentPlayerName);
                    break;

                case LayoutElement.CombatantBars:
                    if (encounter == null) break;
                    if (combatants == null || combatants.Count == 0)
                    {
                        ImGui.TextDisabled("No combatants match this tab's filter.");
                        break;
                    }
                    DrawCombatantBars(combatants, maxVal, sortBy, afterBarsHeight, tab, currentPlayerName, encounter, headerComponent.IsViewingLive);
                    break;
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (!visible)
            suppressClose = true;
        IsOpen = visible;
    }

    public override void OnClose()
    {
        if (suppressClose)
        {
            suppressClose = false;
            return;
        }
        plugin.ClosePopoutTab(tabId);
    }

    private void DrawCombatantBars(List<CombatantEntry> combatants, double maxVal, SortField sortBy, float reservedHeight, MeterTab activeTab, string currentPlayerName, EncounterSnapshot snapshot, bool isLive)
    {
        var availY = ImGui.GetContentRegionAvail().Y;
        var childHeight = reservedHeight > 0 ? Math.Max(0f, availY - reservedHeight) : 0f;

        if (ImGui.BeginChild("##popoutCombatants", new Vector2(0, childHeight), false))
        {
            if (activeTab.ViewMode == ViewMode.LineGraph)
            {
                graphViewComponent.Render(combatants, snapshot, isLive, activeTab, currentPlayerName);
            }
            else
            {
                if (plugin.Config.ShowMeterHeader)
                    DrawMeterHeader(activeTab);

                for (int i = 0; i < combatants.Count; i++)
                {
                    var combatant = combatants[i];
                    if (barComponent.Render(combatant, maxVal, i, sortBy, activeTab, currentPlayerName))
                        detailPanel.Toggle(i);

                    detailPanel.Render(combatant, i, snapshot, isLive);
                }
            }
        }
        ImGui.EndChild();
    }

    private void DrawMeterHeader(MeterTab activeTab)
    {
        var config = plugin.Config;
        var headerHeight = config.HeaderHeight;
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.HeaderTextColor);

        var headerBg = config.HeaderBackgroundColor;
        if (headerBg.W > 0f)
        {
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y + headerHeight),
                ImGui.ColorConvertFloat4ToU32(headerBg));
        }

        var prevHdrScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.HeaderFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var textY = cursorPos.Y + (headerHeight - ImGui.GetTextLineHeight()) * 0.5f;
        var textStartX = cursorPos.X + config.BarLeftPadding;

        if (config.ShowRankNumber)
        {
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "#");
            textStartX += ImGui.CalcTextSize("#. ").X;
        }

        if (config.ShowJobIcons)
            textStartX += config.IconSize + config.IconTextPadding;

        if (config.ShowJobAbbrevOnBar)
        {
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "Job");
            textStartX += ImGui.CalcTextSize("[WHM] ").X;
        }

        if (config.ShowNameOnBar)
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "Name");

        var rightX = cursorPos.X + windowWidth - config.BarRightPadding;
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab.ColumnOrder ?? new List<BarColumn>();
        CombatantBarComponent.EnsureColumnOrderComplete(columnOrder);

        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.BarFontSize);
        ImGui.PushFont(ImGui.GetFont());
        var colWidths = new Dictionary<BarColumn, float>();
        foreach (var col in columnOrder)
        {
            if (CombatantBarComponent.ColumnWidthTemplates.TryGetValue(col, out var template))
                colWidths[col] = ImGui.CalcTextSize(template).X;
            else
                colWidths[col] = 0f;
        }
        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.HeaderFontSize);
        ImGui.PushFont(ImGui.GetFont());

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (!activeTab.IsColumnVisible(col)) continue;

            var headerLabel = activeTab.GetHeaderLabel(col) ?? Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            var colW = colWidths[col];
            var lw = ImGui.CalcTextSize(headerLabel).X;
            rightX -= colW;
            var textPos = new Vector2(rightX + (colW - lw) * 0.5f, textY);
            drawList.AddText(textPos, headerColor, headerLabel);

            var hitMin = new Vector2(rightX, textY);
            var hitMax = new Vector2(rightX + colW, textY + ImGui.GetFontSize());
            if (ImGui.IsMouseHoveringRect(hitMin, hitMax))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString()));
                ImGui.EndTooltip();
            }

            rightX -= colSpacing;
        }

        ImGui.PopFont();
        ImGui.GetFont().Scale = prevHdrScale;

        if (config.HeaderSeparator)
        {
            var sepY = cursorPos.Y + headerHeight;
            drawList.AddLine(
                new Vector2(cursorPos.X, sepY),
                new Vector2(cursorPos.X + windowWidth, sepY),
                ImGui.ColorConvertFloat4ToU32(config.HeaderSeparatorColor));
        }

        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + headerHeight + config.BarSpacing));
    }
}
