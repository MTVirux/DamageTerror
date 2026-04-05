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
    private readonly GraphViewComponent graphViewComponent;
    private readonly CombatantDetailPanel detailPanel;
    private readonly StatusBarComponent statusBarComponent;
    private TitleBarButton? lockButton;
    private TitleBarButton? viewModeButton;
    private MeterTab? currentActiveTab;
    private DateTime? combatEndTime;
    private int selectedMeterTab;
    private bool wasDrawnLastFrame = true;
    private GifAnimator? gifAnimator;
    private string? gifAnimatorPath;

    private int SelectedMeterTab
    {
        get => selectedMeterTab;
        set
        {
            if (selectedMeterTab != value)
            {
                selectedMeterTab = value;
                plugin.Config.SelectedMeterTab = value;
                plugin.SaveConfig();
            }
        }
    }

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

        this.selectedMeterTab = plugin.Config.SelectedMeterTab;
        this.headerComponent = new EncounterHeaderComponent(plugin.DataService, plugin.Config);
        this.barComponent = new CombatantBarComponent(plugin.Config, textureProvider);
        this.graphViewComponent = new GraphViewComponent(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker);
        this.detailPanel = new CombatantDetailPanel(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker);
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

        viewModeButton = new TitleBarButton
        {
            Icon = FontAwesomeIcon.ChartLine,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () =>
            {
                if (currentActiveTab != null)
                    ImGui.SetTooltip(currentActiveTab.ViewMode == ViewMode.Bars ? "Switch to graph" : "Switch to bars");
                else
                    ImGui.SetTooltip("Toggle view mode");
            },
        };
        viewModeButton.Click = (m) =>
        {
            if (m == ImGuiMouseButton.Left && currentActiveTab != null)
            {
                currentActiveTab.ViewMode = currentActiveTab.ViewMode == ViewMode.Bars ? ViewMode.LineGraph : ViewMode.Bars;
                plugin.SaveConfig();
            }
        };
        TitleBarButtons.Add(viewModeButton);
    }

    public void Dispose()
    {
        gifAnimator?.Dispose();
        gifAnimator = null;
    }

    public override bool DrawConditions()
        => MeterWindowHelper.ShouldDraw(plugin.Config, ref combatEndTime);

    public override void PreDraw()
    {
        RespectCloseHotkey = !this.plugin.Config.IgnoreEscClose;

        var forceShowHeader = MeterWindowHelper.IsModifierActive(plugin.Config);
        var isCollapsed = IsOpen && !wasDrawnLastFrame;
        wasDrawnLastFrame = false;

        if (this.plugin.Config.HideWindowHeader && !forceShowHeader && !isCollapsed)
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

        if (viewModeButton != null)
            viewModeButton.Icon = currentActiveTab?.ViewMode == ViewMode.LineGraph ? FontAwesomeIcon.ChartBar : FontAwesomeIcon.ChartLine;

        Flags |= ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleColor(ImGuiCol.WindowBg, this.plugin.Config.WindowBackgroundColor);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
    }

    public override void Draw()
    {
        wasDrawnLastFrame = true;
        plugin.DataService.CheckStaleness();

        DrawBackgroundImage();

        var padLeft = plugin.Config.WindowPaddingLeft;
        var padRight = plugin.Config.WindowPaddingRight;
        var padTop = plugin.Config.WindowPaddingTop;
        var padBottom = plugin.Config.WindowPaddingBottom;

        // If the status bar is the last visible layout element, skip bottom padding so it sits flush
        var modifierActiveEarly = MeterWindowHelper.IsModifierActive(plugin.Config);
        LayoutElement? lastVisibleEl = null;
        foreach (var el in plugin.Config.Layout)
        {
            if (plugin.Config.CtrlShiftOnlyElements.Contains(el) && !modifierActiveEarly)
                continue;
            lastVisibleEl = el;
        }
        var effectivePadBottom = lastVisibleEl == LayoutElement.StatusBar ? 0f : padBottom;

        ImGui.SetCursorPos(new Vector2(padLeft, ImGui.GetCursorPosY() + padTop));
        var avail = ImGui.GetContentRegionAvail();
        if (!ImGui.BeginChild("##paddedContent", new Vector2(avail.X - padRight, avail.Y - effectivePadBottom), false))
        {
            ImGui.EndChild();
            return;
        }

        using var fontScope = plugin.Config.EnableCustomFont ? plugin.FontService?.PushFont() : null;

        var config = plugin.Config;

        // --- Data resolution (always runs regardless of layout order) ---
        var encounter = headerComponent.SelectedEncounter;
        var currentPlayerName = !string.IsNullOrEmpty(encounter?.PlayerName)
            ? encounter.PlayerName
            : plugin.DataService.PlayerName;

        // Resolve active tab (always resolve so status bar can read per-tab content settings)
        var useTabBar = config.ShowTabBar && config.MeterTabs.Count > 0;
        MeterTab? activeTab = null;

        if (config.MeterTabs.Count > 0 && encounter != null)
        {
            if (selectedMeterTab >= config.MeterTabs.Count)
                SelectedMeterTab = 0;
            // If the selected tab is hidden, fall back to the first visible tab
            if (config.MeterTabs[selectedMeterTab].IsHidden)
            {
                var firstVisible = config.MeterTabs.FindIndex(t => !t.IsHidden);
                if (firstVisible >= 0)
                    SelectedMeterTab = firstVisible;
            }
            activeTab = config.MeterTabs[selectedMeterTab];
        }

        currentActiveTab = activeTab;

        var sortBy = activeTab?.SortBy ?? SortField.EncDps;
        var sortDesc = activeTab?.SortDescending ?? true;

        HashSet<string>? partyNames = null;
        HashSet<string>? allianceNames = null;
        if (activeTab?.GroupFilter is GroupFilter.Party or GroupFilter.Alliance)
        {
            partyNames = plugin.PartyService.GetPartyMemberNames();
            allianceNames = plugin.PartyService.GetAllianceMemberNames();
        }

        List<CombatantEntry>? combatants = null;
        double maxVal = 0;

        if (encounter != null)
        {
            combatants = GetSortedCombatants(encounter, sortBy, sortDesc, activeTab, partyNames, allianceNames);
            if (combatants.Count > 0)
                maxVal = combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy));
        }

        // Calculate height reserved for elements rendered after CombatantBars
        float afterBarsHeight = 0f;
        bool passedBars = false;
        var modifierHeld = MeterWindowHelper.IsModifierActive(config);
        foreach (var el in config.Layout)
        {
            if (config.CtrlShiftOnlyElements.Contains(el) && !modifierHeld
                && !(el == LayoutElement.EncounterSelect && headerComponent.IsComboOpen))
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
                        afterBarsHeight += config.TabButtonHeight;
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
            ImGui.EndChild();
            return;
        }

        var modifierActive = MeterWindowHelper.IsModifierActive(config);
        foreach (var element in config.Layout)
        {
            if (config.CtrlShiftOnlyElements.Contains(element) && !modifierActive
                && !(element == LayoutElement.EncounterSelect && headerComponent.IsComboOpen))
                continue;
            switch (element)
            {
                case LayoutElement.EncounterSelect:
                    headerComponent.Render();
                    if (encounter == null)
                    {
                        if (plugin.DataService.IsConnected)
                        {
                            ImGui.TextDisabled("No combat data, go hit something!");
                        }
                        else
                        {
                            ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
                            if (ImGui.Button("Reconnect"))
                                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
                        }
                        ImGui.EndChild();
                        return;
                    }
                    break;

                case LayoutElement.MeterTabs:
                    if (encounter == null) break;
                    if (useTabBar)
                    {
                        DrawMeterTabButtons(config);

                        var newTab = config.MeterTabs[selectedMeterTab];
                        if (newTab != activeTab)
                        {
                            activeTab = newTab;
                            sortBy = activeTab.SortBy;
                            sortDesc = activeTab.SortDescending;

                            // Re-resolve party context if the new tab needs it
                            if (activeTab.GroupFilter is GroupFilter.Party or GroupFilter.Alliance)
                            {
                                partyNames ??= plugin.PartyService.GetPartyMemberNames();
                                allianceNames ??= plugin.PartyService.GetAllianceMemberNames();
                            }

                            combatants = GetSortedCombatants(encounter, sortBy, sortDesc, activeTab, partyNames, allianceNames);
                            maxVal = combatants.Count > 0
                                ? combatants.Sum(c => CombatantBarComponent.GetSortValue(c, sortBy))
                                : 0;
                        }
                    }
                    break;

                case LayoutElement.StatusBar:
                    if (encounter != null)
                        statusBarComponent.Render(encounter, currentPlayerName, activeTab);
                    break;

                case LayoutElement.CombatantBars:
                    if (encounter == null) break;
                    if (combatants == null || combatants.Count == 0)
                    {
                        ImGui.TextDisabled(useTabBar ? "No combatants match this tab's filter." : "No combatant data.");
                    }
                    else
                    {
                        DrawCombatantBars(combatants, maxVal, sortBy, afterBarsHeight, activeTab, currentPlayerName, encounter, headerComponent.IsViewingLive);
                    }
                    // Anchor post-bars elements to the bottom of the content area
                    if (afterBarsHeight > 0)
                    {
                        var contentMaxY = ImGui.GetWindowContentRegionMax().Y;
                        ImGui.SetCursorPosY(contentMaxY - afterBarsHeight);
                    }
                    break;
            }
        }
        ImGui.EndChild();

        DrawContextMenu();
    }

    private void DrawContextMenu()
    {
        // Open on right-click over the window background (not over other items)
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsAnyItemHovered())
            ImGui.OpenPopup("##MainWindowContext");

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(3, 3));
        if (ImGui.BeginPopup("##MainWindowContext"))
        {
            // Go to Live (hide if already viewing live)
            if (!headerComponent.IsViewingLive)
            {
                if (ImGui.MenuItem("Go to Live"))
                    headerComponent.ResetSelection();
            }

            // Cut encounter (only if active)
            var active = plugin.DataService.Store.ActiveEncounter;
            var isOngoing = active?.Encounter.IsActive == true;
            ImGui.BeginDisabled(!isOngoing);
            if (ImGui.MenuItem("Cut Encounter"))
            {
                plugin.DataService.Store.ArchiveActive();
                plugin.DataService.Store.Save();
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            // Swap view mode
            var viewLabel = currentActiveTab?.ViewMode == ViewMode.LineGraph ? "Swap to Bar View" : "Swap to Graph View";
            ImGui.BeginDisabled(currentActiveTab == null);
            if (ImGui.MenuItem(viewLabel))
            {
                if (currentActiveTab != null)
                {
                    currentActiveTab.ViewMode = currentActiveTab.ViewMode == ViewMode.Bars ? ViewMode.LineGraph : ViewMode.Bars;
                    plugin.SaveConfig();
                }
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            // Lock/Unlock window
            var lockLabel = plugin.Config.PinMainWindow ? "Unlock Window" : "Lock Window";
            if (ImGui.MenuItem(lockLabel))
            {
                if (!plugin.Config.PinMainWindow)
                {
                    plugin.Config.MainWindowPos = ImGui.GetWindowPos();
                    plugin.Config.MainWindowSize = ImGui.GetWindowSize();
                }
                plugin.Config.PinMainWindow = !plugin.Config.PinMainWindow;
                plugin.SaveConfig();
                if (lockButton != null)
                    lockButton.Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }

            // Open settings
            if (ImGui.MenuItem("Open Settings"))
                plugin.OpenConfigUi();

            ImGui.EndPopup();
        }
        ImGui.PopStyleVar();
    }

    private void DrawMeterTabButtons(Configuration config)
    {
        var tabCount = config.MeterTabs.Count;
        if (tabCount == 0) return;

        var visibleCount = config.MeterTabs.Count(t => !t.IsHidden);
        if (visibleCount == 0) return;

        var regionWidth = ImGui.GetContentRegionAvail().X;
        var spacing = config.TabButtonSpacing;
        var buttonHeight = config.TabButtonHeight;
        var rounding = config.TabButtonRounding;

        float buttonWidth;
        if (config.TabButtonStretchToFit)
        {
            buttonWidth = (regionWidth - spacing * (visibleCount - 1)) / visibleCount;
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
            if (tab.IsHidden) continue;
            var isActive = selectedMeterTab == i;

            var bgColor = isActive ? config.TabButtonActiveColor : config.TabButtonColor;
            var textColor = isActive ? config.TabButtonActiveTextColor : config.TabButtonTextColor;

            var textSize = ImGui.CalcTextSize(tab.Name);
            var w = buttonWidth > 0
                ? buttonWidth
                : textSize.X + ImGui.GetStyle().FramePadding.X * 2;

            var btnMin = cursor;
            var btnMax = new Vector2(cursor.X + w, cursor.Y + buttonHeight);

            ImGui.SetCursorScreenPos(cursor);
            ImGui.InvisibleButton($"##mtBtn{i}", new Vector2(w, buttonHeight));

            if (ImGui.IsItemHovered())
                bgColor = isActive ? config.TabButtonActiveColor : config.TabButtonHoveredColor;

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                SelectedMeterTab = i;

            if (ImGui.BeginPopupContextItem($"##tabCtx{i}"))
            {
                if (plugin.IsTabPoppedOut(tab.Id))
                {
                    if (ImGui.MenuItem("Close Popout Window"))
                        plugin.ClosePopoutTab(tab.Id);
                }
                else
                {
                    if (ImGui.MenuItem("Open in Window"))
                        plugin.OpenPopoutTab(tab.Id);
                }
                ImGui.EndPopup();
            }

            drawList.AddRectFilled(btnMin, btnMax, ImGui.ColorConvertFloat4ToU32(bgColor), rounding);

            var textPos = new Vector2(
                btnMin.X + (w - textSize.X) * 0.5f,
                btnMin.Y + (buttonHeight - textSize.Y) * 0.5f);
            drawList.AddText(ImGui.GetFont(), ImGui.GetFontSize(), textPos, ImGui.ColorConvertFloat4ToU32(textColor), tab.Name);

            if (plugin.IsTabPoppedOut(tab.Id))
            {
                var dotCenter = new Vector2(btnMax.X - 5f, btnMin.Y + 5f);
                drawList.AddCircleFilled(dotCenter, 3f, ImGui.ColorConvertFloat4ToU32(config.TabButtonActiveColor));
            }

            cursor = new Vector2(cursor.X + w + spacing, cursor.Y);
        }

        ImGui.GetFont().Scale = prevScale;
        ImGui.PopFont();

        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X, cursor.Y + buttonHeight));
    }

    private void DrawCombatantBars(List<CombatantEntry> combatants, double maxVal, SortField sortBy, float reservedHeight, MeterTab? activeTab, string currentPlayerName, EncounterSnapshot? snapshot, bool isLive)
    {
        var availY = ImGui.GetContentRegionAvail().Y;
        var childHeight = reservedHeight > 0 ? Math.Max(0f, availY - reservedHeight) : 0f;

        if (ImGui.BeginChild("##combatants", new Vector2(0, childHeight), false))
        {
            var viewMode = activeTab?.ViewMode ?? ViewMode.Bars;

            if (viewMode == ViewMode.LineGraph)
            {
                graphViewComponent.Render(combatants, snapshot, isLive, activeTab, currentPlayerName);
            }
            else
            {
                if (plugin.Config.ShowMeterHeader)
                {
                    DrawMeterHeader(activeTab);
                }

                for (int i = 0; i < combatants.Count; i++)
                {
                    var combatant = combatants[i];
                    if (barComponent.Render(combatant, maxVal, i, sortBy, activeTab, currentPlayerName))
                    {
                        detailPanel.Toggle(i);
                    }

                    detailPanel.Render(combatant, i, snapshot, isLive, activeTab);
                }
            }
        }
        ImGui.EndChild();
    }

    private static bool IsGif(string path)
        => path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase);

    private void EnsureGifAnimator(string path)
    {
        if (gifAnimatorPath == path)
            return;

        gifAnimator?.Dispose();
        gifAnimator = null;
        gifAnimatorPath = path;

        if (!IsGif(path))
            return;

        try
        {
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DamageTerror", "gif_frames");
            gifAnimator = new GifAnimator(textureProvider, path, tempDir);
        }
        catch
        {
            gifAnimator = null;
        }
    }

    private void DrawBackgroundImage()
    {
        var path = plugin.Config.BackgroundImagePath;
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            if (gifAnimatorPath != null)
            {
                gifAnimator?.Dispose();
                gifAnimator = null;
                gifAnimatorPath = null;
            }
            return;
        }

        Dalamud.Bindings.ImGui.ImTextureID texHandle;
        float imgW, imgH;

        if (IsGif(path))
        {
            EnsureGifAnimator(path);
            if (gifAnimator == null || !gifAnimator.TryGetCurrentFrame(out texHandle, out var w, out var h))
                return;
            imgW = w;
            imgH = h;
        }
        else
        {
            if (gifAnimatorPath != null)
            {
                gifAnimator?.Dispose();
                gifAnimator = null;
                gifAnimatorPath = null;
            }
            var texture = textureProvider.GetFromFile(path);
            if (!texture.TryGetWrap(out var wrap, out _))
                return;
            texHandle = wrap.Handle;
            imgW = wrap.Width;
            imgH = wrap.Height;
        }

        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        var opacity = plugin.Config.BackgroundImageOpacity;
        var tint = plugin.Config.BackgroundImageTint;
        var color = new Vector4(tint.X, tint.Y, tint.Z, tint.W * opacity);
        var tintU32 = ImGui.ColorConvertFloat4ToU32(color);

        var winW = windowSize.X;
        var winH = windowSize.Y;

        switch (plugin.Config.BackgroundImageScale)
        {
            case BackgroundImageScaleMode.Stretch:
                drawList.AddImage(texHandle, windowPos, windowPos + windowSize, Vector2.Zero, Vector2.One, tintU32);
                break;

            case BackgroundImageScaleMode.Fit:
            {
                var scale = Math.Min(winW / imgW, winH / imgH);
                var drawW = imgW * scale;
                var drawH = imgH * scale;
                var offset = new Vector2((winW - drawW) * 0.5f, (winH - drawH) * 0.5f);
                drawList.AddImage(texHandle, windowPos + offset, windowPos + offset + new Vector2(drawW, drawH), Vector2.Zero, Vector2.One, tintU32);
                break;
            }

            case BackgroundImageScaleMode.Fill:
            {
                var scale = Math.Max(winW / imgW, winH / imgH);
                var drawW = imgW * scale;
                var drawH = imgH * scale;
                var offset = new Vector2((winW - drawW) * 0.5f, (winH - drawH) * 0.5f);
                var uvMin = new Vector2(-offset.X / drawW, -offset.Y / drawH);
                var uvMax = new Vector2((winW - offset.X) / drawW, (winH - offset.Y) / drawH);
                drawList.AddImage(texHandle, windowPos, windowPos + windowSize, uvMin, uvMax, tintU32);
                break;
            }

            case BackgroundImageScaleMode.Tile:
            {
                var tilesX = (int)Math.Ceiling(winW / imgW);
                var tilesY = (int)Math.Ceiling(winH / imgH);
                for (var ty = 0; ty < tilesY; ty++)
                {
                    for (var tx = 0; tx < tilesX; tx++)
                    {
                        var tilePos = windowPos + new Vector2(tx * imgW, ty * imgH);
                        var tileEnd = tilePos + new Vector2(imgW, imgH);
                        drawList.AddImage(texHandle, tilePos, tileEnd, Vector2.Zero, Vector2.One, tintU32);
                    }
                }
                break;
            }
        }
    }

    private void DrawMeterHeader(MeterTab? activeTab)
        => MeterWindowHelper.DrawMeterHeader(plugin.Config, activeTab);

    internal static List<CombatantEntry> GetSortedCombatants(EncounterSnapshot encounter,
        SortField sortBy, bool desc, MeterTab? activeTab,
        HashSet<string>? partyNames = null, HashSet<string>? allianceNames = null)
    {
        var combatants = new List<CombatantEntry>(encounter.Combatants);

        if (activeTab != null)
            combatants.RemoveAll(c => !activeTab.PassesFilter(c, partyNames, allianceNames));

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
