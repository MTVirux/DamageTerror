using Dalamud.Game.ClientState.Conditions;

namespace DamageTerror.Gui.MainWindow;

internal struct MeterVisibilityState
{
    public DateTime? CombatEndTime;
    public bool UserOverride;
    public bool ObservedCombatSinceOverride;
}

internal struct MeterWindowContext
{
    public required Configuration Config;
    public required EncounterSnapshot? Encounter;
    public MeterTab? ActiveTab;
    public SortField SortBy;
    public bool SortDescending;
    public required string CurrentPlayerName;
    public List<CombatantEntry>? Combatants;
    public double MaxVal;
    public GroupAggregates? GroupAggregates;
    public required float AfterBarsHeight;
    public required bool UseTabBar;
    public required bool IsViewingLive;
    public required string ChildId;
    public Action? DrawReplayBar;
    public Action? DrawMeterTabButtons;
    public required EncounterHeaderComponent HeaderComponent;
    public required CombatantBarComponent BarComponent;
    public required GraphViewComponent GraphViewComponent;
    public required CombatantDetailPanel DetailPanel;
    public required StatusBarComponent StatusBarComponent;
    public required bool IsConnected;
    public required bool DisconnectNoticeDismissed;
    public required Action SpawnReconnect;
    public required Action DismissDisconnectNotice;
    public required string ReconnectButtonIdSuffix;
    public bool IsReplayActive;
}

internal static class MeterWindowHelper
{
    /// <summary>Height of the replay control row (frame height plus vertical padding).</summary>
    public static float ReplayBarRowHeight => ImGui.GetFrameHeight() + 6f;

    /// <summary>Draws a FontAwesome icon followed by a selectable label on the same line.</summary>
    public static bool IconMenuItem(string label, FontAwesomeIcon icon)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(icon.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        return ImGui.Selectable(label);
    }

    /// <summary>Persists across frames: current toggle state for modifier key mode.</summary>
    private static bool toggleState;
    /// <summary>Persists across frames: tracks if modifier was down last frame (for edge detection).</summary>
    private static bool wasModifierDown;

    /// <summary>
    /// Returns true when the configured modifier combo is active,
    /// respecting the hold/toggle mode setting.
    /// </summary>
    /// <summary>
    /// Raw physical state of the configured modifier combo, without the
    /// hold/toggle interpretation or its edge-detection side effects.
    /// </summary>
    public static bool IsModifierComboDown(Configuration config)
    {
        var io = ImGui.GetIO();
        return config.ModifierKeyCombo switch
        {
            ModifierCombo.CtrlShift => io.KeyCtrl && io.KeyShift,
            ModifierCombo.CtrlAlt   => io.KeyCtrl && io.KeyAlt,
            ModifierCombo.ShiftAlt  => io.KeyShift && io.KeyAlt,
            ModifierCombo.Ctrl      => io.KeyCtrl,
            ModifierCombo.Shift     => io.KeyShift,
            ModifierCombo.Alt       => io.KeyAlt,
            _ => io.KeyCtrl && io.KeyShift,
        };
    }

    public static string ModifierComboName(ModifierCombo combo) => combo switch
    {
        ModifierCombo.CtrlShift => "Ctrl + Shift",
        ModifierCombo.CtrlAlt   => "Ctrl + Alt",
        ModifierCombo.ShiftAlt  => "Shift + Alt",
        ModifierCombo.Ctrl      => "Ctrl",
        ModifierCombo.Shift     => "Shift",
        ModifierCombo.Alt       => "Alt",
        _ => "Ctrl + Shift",
    };

    public static bool IsModifierActive(Configuration config)
    {
        var down = IsModifierComboDown(config);

        if (config.ModifierKeyMode == ModifierMode.Hold)
            return down;

        // Toggle: flip state on rising edge
        if (down && !wasModifierDown)
            toggleState = !toggleState;
        wasModifierDown = down;
        return toggleState;
    }

    public static bool IsDutyTypeEnabled(Configuration config)
    {
        var contentType = Content.ContentType;
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

    /// <summary>
    /// When set, the setup wizard's combat simulator overrides the real in-game
    /// combat condition so the preview meter's hide/show follows the toggle
    /// instead of actual encounters. Null restores normal behaviour.
    /// </summary>
    public static bool? SimulatedCombat;

    /// <summary>
    /// When true, the preview meter draws a non-interactive demo replay bar even
    /// with no live replay running. The setup wizard sets this on the Layout step
    /// so the user can see and arrange the Replay Bar element.
    /// </summary>
    public static bool PreviewReplayBar;

    public static bool ShouldDraw(Configuration config, ref MeterVisibilityState state)
    {
        if (!Svc.ClientState.IsLoggedIn)
            return false;

        if (!IsDutyTypeEnabled(config))
            return false;

        if (!config.HideOutOfCombat)
        {
            state = default;
            return true;
        }

        var inCombat = SimulatedCombat ?? Svc.Condition[ConditionFlag.InCombat];

        if (inCombat)
        {
            state.CombatEndTime = null;
            if (state.UserOverride)
                state.ObservedCombatSinceOverride = true;
            return true;
        }

        state.CombatEndTime ??= DateTime.UtcNow;
        var elapsed = (DateTime.UtcNow - state.CombatEndTime.Value).TotalSeconds;
        var graceExpired = elapsed >= config.HideOutOfCombatDelay;

        // While simulating, the toggle is authoritative: ignore the user
        // visibility override so the preview hides exactly as configured.
        if (SimulatedCombat.HasValue)
            return !graceExpired;

        // Override is cleared once a full combat cycle has completed after it was set.
        if (graceExpired && state.UserOverride && state.ObservedCombatSinceOverride)
        {
            state.UserOverride = false;
            state.ObservedCombatSinceOverride = false;
        }

        if (state.UserOverride)
            return true;

        return !graceExpired;
    }

    public static void DrawMeterHeader(Configuration config, MeterTab? activeTab)
    {
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

        using var headerFont = FontScope.Push(config.GetFontScale(config.HeaderFontSize));

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

        var columnOrder = activeTab?.ColumnOrder ?? new List<BarColumn>();
        CombatantBarComponent.EnsureColumnOrderComplete(columnOrder);

        // Measure column widths at bar font size
        headerFont.Dispose();
        Dictionary<BarColumn, float> colWidths;
        using (var barFont = FontScope.Push(config.GetFontScale(config.BarFontSize)))
        {
            colWidths = new Dictionary<BarColumn, float>();
            foreach (var col in columnOrder)
            {
                if (activeTab?.GetColumnWidth(col) is { } customW)
                    colWidths[col] = customW;
                else if (CombatantBarComponent.ColumnWidthTemplates.TryGetValue(col, out var template))
                    colWidths[col] = ImGui.CalcTextSize(template).X;
                else
                    colWidths[col] = 0f;
            }
        }
        using var headerFont2 = FontScope.Push(config.GetFontScale(config.HeaderFontSize));

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (activeTab == null || !activeTab.IsColumnVisible(col)) continue;

            var headerLabel = activeTab?.GetHeaderLabel(col) ?? ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            var colW = colWidths[col];
            rightX -= colW;
            TableDrawHelper.DrawCentered(drawList, rightX, colW, headerLabel, headerColor, textY);

            var hitMin = new Vector2(rightX, textY);
            var hitMax = new Vector2(rightX + colW, textY + ImGui.GetFontSize());
            if (ImGui.IsMouseHoveringRect(hitMin, hitMax))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(ColumnLabels.FullColumnNames.GetValueOrDefault(col, col.ToString()));
                ImGui.EndTooltip();
            }

            rightX -= colSpacing;
        }

        headerFont2.Dispose();

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

    public static float CalculateAfterBarsHeight(
        Configuration config,
        Func<float> getStatusBarHeight,
        Func<float> getHeaderHeight,
        bool hasEncounter,
        bool useTabBar,
        bool isReplayActive = false,
        HashSet<LayoutElement>? skipElements = null)
    {
        float height = 0f;
        bool passedBars = false;
        var modifierHeld = IsModifierActive(config);
        foreach (var el in config.Layout)
        {
            if (skipElements?.Contains(el) == true) continue;
            if (config.CtrlShiftOnlyElements.Contains(el) && !modifierHeld
                && !(el == LayoutElement.ReplayBar && config.ReplayBarPinned))
                continue;
            if (passedBars)
            {
                switch (el)
                {
                    case LayoutElement.StatusBar when hasEncounter:
                        height += getStatusBarHeight();
                        break;
                    case LayoutElement.EncounterSelect:
                        height += getHeaderHeight();
                        break;
                    case LayoutElement.MeterTabs when useTabBar && hasEncounter:
                        height += config.TabButtonHeight;
                        break;
                    case LayoutElement.ReplayBar when isReplayActive:
                        height += ReplayBarRowHeight;
                        break;
                }
            }
            else if (el == LayoutElement.CombatantBars)
            {
                passedBars = true;
            }
        }
        return height;
    }

    public static (List<CombatantEntry> Combatants, double MaxVal, GroupAggregates? Aggregates) BuildCombatantData(
        EncounterSnapshot encounter,
        SortField sortBy,
        bool sortDesc,
        MeterTab? activeTab,
        HashSet<string>? partyNames,
        HashSet<string>? allianceNames,
        bool stampRanks,
        bool computeAggregates)
    {
        var combatants = MainWindow.GetSortedCombatants(encounter, sortBy, sortDesc, activeTab, partyNames, allianceNames);
        if (stampRanks)
            StampRanks(combatants);
        var aggregates = computeAggregates ? GroupAggregates.Compute(combatants) : null;
        var maxVal = combatants.Count > 0
            ? combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy))
            : 0d;
        return (combatants, maxVal, aggregates);
    }

    /// <summary>
    /// Writes each combatant's rank within the set it was given, so a rank column reads as
    /// a position among the combatants its consumer shows rather than the whole encounter.
    /// </summary>
    public static void StampRanks(List<CombatantEntry> combatants)
    {
        var total = combatants.Count;

        var byDps = combatants.OrderByDescending(c => c.EncDps).ToList();
        for (var i = 0; i < byDps.Count; i++)
        {
            byDps[i].DpsRank = i + 1;
            byDps[i].DpsRankTotal = total;
        }

        var byHps = combatants.OrderByDescending(c => c.EncHps).ToList();
        for (var i = 0; i < byHps.Count; i++)
        {
            byHps[i].HpsRank = i + 1;
            byHps[i].HpsRankTotal = total;
        }
    }

    public static void DrawDisconnectNotice(string idSuffix, Action spawnReconnect, Action dismissDisconnectNotice)
    {
        ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
        if (ImGui.Button($"Reconnect##{idSuffix}"))
            spawnReconnect();
        ImGui.SameLine();
        if (ImGui.Button($"Dismiss##{idSuffix}"))
            dismissDisconnectNotice();
    }

    public static void RenderCombatantBars(in MeterWindowContext ctx)
    {
        var availY = ImGui.GetContentRegionAvail().Y;
        var childHeight = ctx.AfterBarsHeight > 0 ? Math.Max(0f, availY - ctx.AfterBarsHeight) : 0f;
        if (!ImGui.BeginChild(ctx.ChildId, new Vector2(0, childHeight), false))
        {
            ImGui.EndChild();
            return;
        }

        var viewMode = ctx.ActiveTab?.ViewMode ?? ViewMode.Bars;

        if (viewMode == ViewMode.LineGraph)
        {
            ctx.GraphViewComponent.Render(ctx.Combatants!, ctx.Encounter, ctx.IsViewingLive, ctx.ActiveTab, ctx.CurrentPlayerName);
        }
        else
        {
            if (ctx.Config.ShowMeterHeader)
                DrawMeterHeader(ctx.Config, ctx.ActiveTab);

            for (int i = 0; i < ctx.Combatants!.Count; i++)
            {
                var combatant = ctx.Combatants[i];
                if (ctx.BarComponent.Render(combatant, ctx.MaxVal, i, ctx.SortBy, ctx.ActiveTab, ctx.CurrentPlayerName, ctx.GroupAggregates))
                    ctx.DetailPanel.Toggle(combatant.Name);

                ctx.DetailPanel.Render(combatant, ctx.Encounter, ctx.IsViewingLive, ctx.ActiveTab);
            }
        }
        ImGui.EndChild();
    }

    public static void RenderLayoutElements(ref MeterWindowContext ctx, ref bool earlyReturn)
    {
        var modifierActive = IsModifierActive(ctx.Config);
        foreach (var element in ctx.Config.Layout)
        {
            if (ctx.Config.CtrlShiftOnlyElements.Contains(element) && !modifierActive
                && !(element == LayoutElement.EncounterSelect && ctx.HeaderComponent.IsComboOpen)
                && !(element == LayoutElement.ReplayBar && ctx.Config.ReplayBarPinned))
                continue;

            switch (element)
            {
                case LayoutElement.EncounterSelect:
                    ctx.HeaderComponent.Render();
                    if (ctx.Encounter == null)
                    {
                        if (ctx.IsConnected)
                        {
                            ImGui.TextDisabled("No combat data, go hit something!");
                        }
                        else if (!ctx.DisconnectNoticeDismissed)
                        {
                            DrawDisconnectNotice($"disconnect-notice-encsel{ctx.ReconnectButtonIdSuffix}", ctx.SpawnReconnect, ctx.DismissDisconnectNotice);
                        }
                        earlyReturn = true;
                        return;
                    }
                    break;

                case LayoutElement.MeterTabs:
                    if (ctx.Encounter == null) break;
                    if (ctx.UseTabBar && ctx.DrawMeterTabButtons != null)
                    {
                        ctx.DrawMeterTabButtons();
                        // Caller's DrawMeterTabButtons callback updates ctx.ActiveTab/SortBy/Combatants/MaxVal/GroupAggregates
                        // when the user clicks a different tab; helper picks up the new values on the next iteration.
                    }
                    break;

                case LayoutElement.StatusBar:
                    if (ctx.Encounter != null)
                        ctx.StatusBarComponent.Render(ctx.Encounter, ctx.CurrentPlayerName, ctx.ActiveTab, ctx.GroupAggregates);
                    break;

                case LayoutElement.CombatantBars:
                    if (ctx.Encounter == null) break;
                    if (ctx.Combatants == null || ctx.Combatants.Count == 0)
                    {
                        ImGui.TextDisabled(ctx.UseTabBar ? "No combatants match this tab's filter." : "No combatant data.");
                    }
                    else
                    {
                        RenderCombatantBars(in ctx);
                    }
                    if (ctx.AfterBarsHeight > 0)
                    {
                        var contentMaxY = ImGui.GetWindowContentRegionMax().Y;
                        ImGui.SetCursorPosY(contentMaxY - ctx.AfterBarsHeight);
                    }
                    break;

                case LayoutElement.ReplayBar:
                    ctx.DrawReplayBar?.Invoke();
                    break;
            }
        }
    }
}
