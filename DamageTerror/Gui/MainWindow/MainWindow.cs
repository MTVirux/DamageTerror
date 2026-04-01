using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public class MainWindow : Window, IDisposable
{
    private static string GetTitleWithVersion()
    {
        try
        {
            var ver = typeof(DamageTerrorPlugin).Assembly.GetName().Version?.ToString() ?? string.Empty;

#if DEBUG
            var title = string.IsNullOrEmpty(ver)
                ? "Damage Terror [TESTING]"
                : $"Damage Terror  -  v{ver} [TESTING]";
#else
            var title = string.IsNullOrEmpty(ver)
                ? "Damage Terror"
                : $"Damage Terror  -  v{ver}";
#endif
            return title;
        }
        catch
        {
            return "Damage Terror";
        }
    }

    private readonly DamageTerrorPlugin plugin;
    private readonly ITextureProvider textureProvider;
    private readonly EncounterHeaderComponent headerComponent;
    private readonly CombatantBarComponent barComponent;
    private readonly CombatantDetailPanel detailPanel;
    private readonly StatusBarComponent statusBarComponent;
    private TitleBarButton? lockButton;
    private DateTime? combatEndTime;
    private int selectedMeterTab;

    public MainWindow(DamageTerrorPlugin plugin, ITextureProvider textureProvider)
        : base(GetTitleWithVersion())
    {
        this.plugin = plugin;
        this.textureProvider = textureProvider;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(250, 150),
            MaximumSize = new Vector2(2000, 2000),
        };

        this.headerComponent = new EncounterHeaderComponent(plugin.DataService, plugin.SaveConfig);
        this.barComponent = new CombatantBarComponent(plugin.Config, textureProvider);
        this.detailPanel = new CombatantDetailPanel(plugin.Config);
        this.statusBarComponent = new StatusBarComponent(plugin.Config);

        TitleBarButtons.Add(new TitleBarButton
        {
            Click = (m) => { if (m == ImGuiMouseButton.Left) plugin.OpenConfigUi(); },
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip("Open settings"),
        });

        lockButton = new TitleBarButton
        {
            Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen,
            IconOffset = new Vector2(3, 2),
            ShowTooltip = () => ImGui.SetTooltip("Lock window position and size"),
        };
        lockButton.Click = (m) =>
        {
            if (m == ImGuiMouseButton.Left)
            {
                if (!plugin.Config.PinMainWindow)
                {
                    plugin.Config.MainWindowPos = ImGui.GetWindowPos();
                    plugin.Config.MainWindowSize = ImGui.GetWindowSize();
                }

                plugin.Config.PinMainWindow = !plugin.Config.PinMainWindow;
                plugin.SaveConfig();
                lockButton!.Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }
        };
        TitleBarButtons.Add(lockButton);
    }

    public void Dispose()
    {
    }

    public override bool DrawConditions()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return false;

        if (!IsDutyTypeEnabled())
            return false;

        if (!this.plugin.Config.HideOutOfCombat)
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
        return elapsed < this.plugin.Config.HideOutOfCombatDelay;
    }

    private bool IsDutyTypeEnabled()
    {
        var contentType = Content.ContentType;
        var config = this.plugin.Config;
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

    public override void PreDraw()
    {
        RespectCloseHotkey = !this.plugin.Config.IgnoreEscClose;

        var io = ImGui.GetIO();
        var forceShowHeader = io.KeyCtrl && io.KeyShift;

        if (this.plugin.Config.HideWindowHeader && !forceShowHeader)
            Flags |= ImGuiWindowFlags.NoTitleBar;
        else
            Flags &= ~ImGuiWindowFlags.NoTitleBar;

        if (this.plugin.Config.PinMainWindow)
        {
            Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

            var pos = this.plugin.Config.MainWindowPos;
            var size = this.plugin.Config.MainWindowSize;
            if (pos.X > 1f && pos.Y > 1f && size.X > 1f && size.Y > 1f)
            {
                ImGui.SetNextWindowPos(pos);
                ImGui.SetNextWindowSize(size);
            }
        }
        else
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
            Flags &= ~ImGuiWindowFlags.NoResize;
        }

        if (lockButton != null)
            lockButton.Icon = this.plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;

        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, this.plugin.Config.WindowBackgroundColor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, this.plugin.Config.WindowRounding);
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

        // --- Data resolution (always runs regardless of layout order) ---
        var encounter = headerComponent.SelectedEncounter;

        // Resolve active tab (needs encounter to exist for filtering)
        var useTabBar = config.ShowTabBar && config.MeterTabs.Count > 0;
        MeterTab? activeTab = null;

        if (useTabBar && encounter != null)
        {
            if (selectedMeterTab >= config.MeterTabs.Count)
                selectedMeterTab = 0;
            activeTab = config.MeterTabs[selectedMeterTab];
        }

        var sortBy = activeTab?.SortBy ?? config.SortBy;
        var sortDesc = activeTab?.SortDescending ?? config.SortDescending;

        List<CombatantEntry>? combatants = null;
        double maxVal = 0;

        if (encounter != null)
        {
            combatants = GetSortedCombatants(encounter, sortBy, sortDesc, activeTab);
            if (combatants.Count > 0)
                maxVal = combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy));
        }

        // Calculate height reserved for elements rendered after CombatantBars
        float afterBarsHeight = 0f;
        bool passedBars = false;
        var ctrlShiftHeldForHeight = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
        foreach (var el in config.Layout)
        {
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
                    case LayoutElement.MeterTabs when useTabBar && encounter != null:
                        afterBarsHeight += config.TabButtonHeight + ImGui.GetStyle().ItemSpacing.Y;
                        break;
                }
            }
            else if (el == LayoutElement.CombatantBars)
            {
                passedBars = true;
            }
        }

        // --- Render components in configured layout order ---
        var ctrlShiftHeld = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
        foreach (var element in config.Layout)
        {
            if (config.CtrlShiftOnlyElements.Contains(element) && !ctrlShiftHeld)
                continue;
            switch (element)
            {
                case LayoutElement.EncounterSelect:
                    headerComponent.Render();
                    if (encounter == null)
                    {
                        ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
                        if (ImGui.Button("Reconnect"))
                            Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
                        return;
                    }
                    break;

                case LayoutElement.MeterTabs:
                    if (encounter == null) break;
                    if (useTabBar)
                    {
                        DrawMeterTabButtons(config);

                        // Re-resolve if tab changed
                        var newTab = config.MeterTabs[selectedMeterTab];
                        if (newTab != activeTab)
                        {
                            activeTab = newTab;
                            sortBy = activeTab.SortBy;
                            sortDesc = activeTab.SortDescending;
                            combatants = GetSortedCombatants(encounter, sortBy, sortDesc, activeTab);
                            maxVal = combatants.Count > 0
                                ? combatants.Sum(c => CombatantBarComponent.GetSortValue(c, sortBy))
                                : 0;
                        }
                    }
                    break;

                case LayoutElement.StatusBar:
                    if (encounter != null)
                        statusBarComponent.Render(encounter);
                    break;

                case LayoutElement.CombatantBars:
                    if (encounter == null) break;
                    if (combatants == null || combatants.Count == 0)
                    {
                        ImGui.TextDisabled(useTabBar ? "No combatants match this tab's filter." : "No combatant data.");
                        break;
                    }
                    DrawCombatantBars(combatants, maxVal, sortBy, afterBarsHeight, activeTab);
                    break;
            }
        }
    }

    private void DrawMeterTabButtons(Configuration config)
    {
        var tabCount = config.MeterTabs.Count;
        if (tabCount == 0) return;

        var regionWidth = ImGui.GetContentRegionAvail().X;
        var spacing = config.TabButtonSpacing;
        var buttonHeight = config.TabButtonHeight;
        var rounding = config.TabButtonRounding;

        float buttonWidth;
        if (config.TabButtonStretchToFit)
        {
            buttonWidth = (regionWidth - spacing * (tabCount - 1)) / tabCount;
        }
        else
        {
            buttonWidth = config.TabButtonWidth;
        }

        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();

        var prevScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.TabButtonFontSize);
        ImGui.PushFont(ImGui.GetFont());

        for (var i = 0; i < tabCount; i++)
        {
            var tab = config.MeterTabs[i];
            var isActive = selectedMeterTab == i;

            var bgColor = isActive ? config.TabButtonActiveColor : config.TabButtonColor;
            var textColor = isActive ? config.TabButtonActiveTextColor : config.TabButtonTextColor;

            var textSize = ImGui.CalcTextSize(tab.Name);
            var w = buttonWidth > 0
                ? buttonWidth
                : textSize.X + ImGui.GetStyle().FramePadding.X * 2;

            var btnMin = cursor;
            var btnMax = new Vector2(cursor.X + w, cursor.Y + buttonHeight);

            // Hit test
            ImGui.SetCursorScreenPos(cursor);
            ImGui.InvisibleButton($"##mtBtn{i}", new Vector2(w, buttonHeight));

            if (ImGui.IsItemHovered())
                bgColor = isActive ? config.TabButtonActiveColor : config.TabButtonHoveredColor;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                selectedMeterTab = i;

            // Draw background
            drawList.AddRectFilled(btnMin, btnMax, ImGui.ColorConvertFloat4ToU32(bgColor), rounding);

            // Draw text centered
            var textPos = new Vector2(
                btnMin.X + (w - textSize.X) * 0.5f,
                btnMin.Y + (buttonHeight - textSize.Y) * 0.5f);
            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize(), textPos, ImGui.ColorConvertFloat4ToU32(textColor), tab.Name);

            // Advance cursor for next button
            cursor = new Vector2(cursor.X + w + spacing, cursor.Y);
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        // Move ImGui cursor past the button row
        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X, cursor.Y + buttonHeight));
    }

    private void DrawCombatantBars(List<CombatantEntry> combatants, double maxVal, SortField sortBy, float reservedHeight, MeterTab? activeTab)
    {
        var availY = ImGui.GetContentRegionAvail().Y;
        var childHeight = reservedHeight > 0 ? Math.Max(0f, availY - reservedHeight) : 0f;

        if (ImGui.BeginChild("##combatants", new Vector2(0, childHeight), false))
        {
            if (plugin.Config.ShowMeterHeader)
            {
                DrawMeterHeader(activeTab);
            }

            for (int i = 0; i < combatants.Count; i++)
            {
                var combatant = combatants[i];
                if (barComponent.Render(combatant, maxVal, i, sortBy, activeTab))
                {
                    detailPanel.Toggle(i);
                }

                detailPanel.Render(combatant, i);
            }
        }
        ImGui.EndChild();
    }

    private void DrawMeterHeader(MeterTab? activeTab)
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

        var vis = ColumnVisibility.Resolve(config, activeTab);

        var rightX = cursorPos.X + windowWidth - config.BarRightPadding;
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab?.ColumnOrder ?? config.ColumnOrder;
        CombatantBarComponent.EnsureColumnOrderComplete(columnOrder);

        // Measure column widths at bar font scale for exact alignment
        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.BarFontSize);
        ImGui.PushFont(ImGui.GetFont());
        var colWidths = new Dictionary<BarColumn, float>();
        foreach (var col in columnOrder)
        {
            colWidths[col] = col switch
            {
                BarColumn.DamagePercent => ImGui.CalcTextSize("00.0%").X,
                BarColumn.CritDirectHit => ImGui.CalcTextSize("100%").X,
                BarColumn.Crit => ImGui.CalcTextSize("100%").X,
                BarColumn.DirectHit => ImGui.CalcTextSize("100%").X,
                BarColumn.Deaths => ImGui.CalcTextSize("00").X,
                BarColumn.Healed => ImGui.CalcTextSize("000.0K").X,
                BarColumn.Damage => ImGui.CalcTextSize("000.0K").X,
                BarColumn.Hps => ImGui.CalcTextSize("000.0K").X,
                BarColumn.Dps => ImGui.CalcTextSize("000.0K").X,
                _ => 0f,
            };
        }
        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.HeaderFontSize);
        ImGui.PushFont(ImGui.GetFont());

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (!vis.IsVisible(col)) continue;

            var headerLabel = activeTab?.GetHeaderLabel(col) ?? config.GetHeaderLabel(col);
            var colW = colWidths[col];
            var lw = ImGui.CalcTextSize(headerLabel).X;
            rightX -= colW;
            var textPos = new Vector2(rightX + (colW - lw) * 0.5f, textY);
            drawList.AddText(textPos, headerColor, headerLabel);

            // Tooltip with original column name on hover
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

    private List<CombatantEntry> GetSortedCombatants(EncounterSnapshot encounter,
        SortField sortBy, bool desc, MeterTab? activeTab)
    {
        var combatants = new List<CombatantEntry>(encounter.Combatants);

        if (activeTab != null)
            combatants.RemoveAll(c => !activeTab.PassesFilter(c));

        combatants.Sort((a, b) =>
        {
            var cmp = sortBy switch
            {
                SortField.EncDps => a.EncDps.CompareTo(b.EncDps),
                SortField.EncHps => a.EncHps.CompareTo(b.EncHps),
                SortField.Damage => a.Damage.CompareTo(b.Damage),
                SortField.Healed => a.Healed.CompareTo(b.Healed),
                SortField.CritPct => a.CritPct.CompareTo(b.CritPct),
                SortField.Deaths => a.Deaths.CompareTo(b.Deaths),
                SortField.DamageTaken => a.DamageTaken.CompareTo(b.DamageTaken),
                _ => a.EncDps.CompareTo(b.EncDps),
            };
            return desc ? -cmp : cmp;
        });

        return combatants;
    }
}
