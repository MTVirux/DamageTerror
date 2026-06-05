using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

public sealed class MainWindow : Window, IDisposable
{
    private static string GetTitleWithVersion()
    {
        try
        {
            var ver = typeof(DamageTerrorPlugin).Assembly.GetName().Version?.ToString() ?? string.Empty;
            var version = string.IsNullOrEmpty(ver) ? "" : $"  -  v{ver}";

#if DEBUG
            return $"Damage Terror{version} [TESTING]###DamageTerrorMain";
#else
            return $"Damage Terror{version}###DamageTerrorMain";
#endif
        }
        catch
        {
            return "Damage Terror###DamageTerrorMain";
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
    private MeterVisibilityState visibilityState;
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
        this.detailPanel = new CombatantDetailPanel(plugin.Config, plugin.DataService.GraphTracker, plugin.DataService.SkillTracker, plugin.DataService.StatusTracker);
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

        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cut,
            IconOffset = new Vector2(2, 2),
            ShowTooltip = () => ImGui.SetTooltip("Cut encounter"),
            Click = (m) =>
            {
                if (m == ImGuiMouseButton.Left)
                {
                    plugin.DataService.EndEncounter();
                    plugin.DataService.Store.ArchiveActive();
                    plugin.DataService.Store.Save();
                }
            },
        });
    }

    public void Dispose()
    {
        gifAnimator?.Dispose();
        gifAnimator = null;
    }

    public override bool DrawConditions()
    {
        var ok = MeterWindowHelper.ShouldDraw(plugin.Config, ref visibilityState);
        if (!ok)
            wasDrawnLastFrame = false;
        return ok;
    }

    public bool WasDrawnLastFrame => wasDrawnLastFrame;

    public void RequestVisibilityOverride()
    {
        visibilityState.UserOverride = true;
        visibilityState.ObservedCombatSinceOverride = false;
    }

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
            if (pos.X >= 0f && pos.Y >= 0f && size.X > 1f && size.Y > 1f)
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

        if (!plugin.Config.PinMainWindow)
            plugin.Config.MainWindowSize = ImGui.GetWindowSize();

        plugin.DataService.CheckStaleness();
        plugin.DataService.Store.TickSampleSimulation();

        DrawBackgroundImage();

        var padLeft = plugin.Config.WindowPaddingLeft;
        var padRight = plugin.Config.WindowPaddingRight;
        var padTop = plugin.Config.WindowPaddingTop;
        var padBottom = plugin.Config.WindowPaddingBottom;

        var modifierActive = MeterWindowHelper.IsModifierActive(plugin.Config);
        LayoutElement? lastVisibleEl = null;
        foreach (var el in plugin.Config.Layout)
        {
            if (plugin.Config.CtrlShiftOnlyElements.Contains(el) && !modifierActive
                && !(el == LayoutElement.ReplayBar && plugin.Config.ReplayBarPinned))
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

        var encounter = headerComponent.SelectedEncounter;
        if (encounter != null)
            plugin.DataService.Store.EnsureTimelineLoaded(encounter);

        var currentPlayerName = !string.IsNullOrEmpty(encounter?.PlayerName)
            ? encounter.PlayerName
            : plugin.DataService.PlayerName;

        var useTabBar = config.ShowTabBar && config.MeterTabs.Count > 0;
        MeterTab? activeTab = null;

        if (config.MeterTabs.Count > 0 && encounter != null)
        {
            if (selectedMeterTab >= config.MeterTabs.Count)
                SelectedMeterTab = 0;
            if (config.MeterTabs[selectedMeterTab].IsHidden)
            {
                var firstVisible = config.MeterTabs.FindIndex(t => !t.IsHidden);
                if (firstVisible >= 0)
                    SelectedMeterTab = firstVisible;
                else
                    config.MeterTabs[0].IsHidden = false;
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
        GroupAggregates? groupAggregates = null;

        if (encounter != null)
        {
            combatants = GetSortedCombatants(encounter, sortBy, sortDesc, activeTab, partyNames, allianceNames);
            StampRanks(combatants);
            groupAggregates = GroupAggregates.Compute(combatants);
            if (combatants.Count > 0)
                maxVal = combatants.Max(c => CombatantBarComponent.GetSortValue(c, sortBy));
        }

        var afterBarsHeight = MeterWindowHelper.CalculateAfterBarsHeight(
            config, statusBarComponent.GetHeight, headerComponent.GetHeight,
            encounter != null, useTabBar, plugin.DataService.Store.IsReplayActive);

        if (!plugin.DataService.IsConnected && encounter == null)
        {
            if (!plugin.DataService.DisconnectNoticeDismissed)
            {
                ImGui.TextDisabled("No encounter data. Make sure IINACT is running.");
                if (ImGui.Button("Reconnect##disconnect-notice"))
                    SpawnReconnect();
                ImGui.SameLine();
                if (ImGui.Button("Dismiss##disconnect-notice"))
                    plugin.DataService.DismissDisconnectNotice();
            }
            ImGui.EndChild();
            return;
        }

        var ctx = new MeterWindowContext
        {
            Config = config,
            Encounter = encounter,
            ActiveTab = activeTab,
            SortBy = sortBy,
            SortDescending = sortDesc,
            CurrentPlayerName = currentPlayerName,
            Combatants = combatants,
            MaxVal = maxVal,
            GroupAggregates = groupAggregates,
            AfterBarsHeight = afterBarsHeight,
            UseTabBar = useTabBar,
            IsViewingLive = headerComponent.IsViewingLive,
            ChildId = "##combatants",
            DrawReplayBar = DrawReplayBar,
            DrawMeterTabButtons = null,
            HeaderComponent = headerComponent,
            BarComponent = barComponent,
            GraphViewComponent = graphViewComponent,
            DetailPanel = detailPanel,
            StatusBarComponent = statusBarComponent,
            IsConnected = plugin.DataService.IsConnected,
            DisconnectNoticeDismissed = plugin.DataService.DisconnectNoticeDismissed,
            SpawnReconnect = SpawnReconnect,
            DismissDisconnectNotice = () => plugin.DataService.DismissDisconnectNotice(),
            ReconnectButtonIdSuffix = "",
            IsReplayActive = plugin.DataService.Store.IsReplayActive,
        };

        ctx.DrawMeterTabButtons = () =>
        {
            DrawMeterTabButtons(config);
            var newTab = config.MeterTabs[selectedMeterTab];
            if (newTab != ctx.ActiveTab)
            {
                ctx.ActiveTab = newTab;
                ctx.SortBy = newTab.SortBy;
                ctx.SortDescending = newTab.SortDescending;

                if (newTab.GroupFilter is GroupFilter.Party or GroupFilter.Alliance)
                {
                    partyNames ??= plugin.PartyService.GetPartyMemberNames();
                    allianceNames ??= plugin.PartyService.GetAllianceMemberNames();
                }

                var newCombatants = GetSortedCombatants(encounter!, ctx.SortBy, ctx.SortDescending, newTab, partyNames, allianceNames);
                StampRanks(newCombatants);
                var newGroupAgg = GroupAggregates.Compute(newCombatants);
                var newMaxVal = newCombatants.Count > 0
                    ? newCombatants.Sum(c => CombatantBarComponent.GetSortValue(c, ctx.SortBy))
                    : 0;

                ctx.Combatants = newCombatants;
                ctx.GroupAggregates = newGroupAgg;
                ctx.MaxVal = newMaxVal;
                currentActiveTab = newTab;
            }
        };

        var earlyReturn = false;
        MeterWindowHelper.RenderLayoutElements(ref ctx, ref earlyReturn);
        if (earlyReturn)
        {
            ImGui.EndChild();
            return;
        }

        ImGui.EndChild();

        DrawContextMenu();
    }

    private void DrawContextMenu()
    {
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows) && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !ImGui.IsAnyItemHovered())
            ImGui.OpenPopup("##MainWindowContext");
        if (headerComponent.RequestContextMenu)
            ImGui.OpenPopup("##MainWindowContext");

        var mainWindowPos = ImGui.GetWindowPos();
        var mainWindowSize = ImGui.GetWindowSize();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        if (ImGui.BeginPopup("##MainWindowContext"))
        {
            if (!headerComponent.IsViewingLive)
            {
                if (IconMenuItem("Go to Live", FontAwesomeIcon.Play))
                    headerComponent.ResetSelection();
            }

            var active = plugin.DataService.Store.ActiveEncounter;
            var isOngoing = active?.Encounter.IsActive == true;
            ImGui.BeginDisabled(!isOngoing);
            if (IconMenuItem("Cut Encounter", FontAwesomeIcon.Cut))
            {
                plugin.DataService.EndEncounter();
                plugin.DataService.Store.ArchiveActive();
                plugin.DataService.Store.Save();
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            var viewIcon = currentActiveTab?.ViewMode == ViewMode.LineGraph ? FontAwesomeIcon.ChartBar : FontAwesomeIcon.ChartLine;
            var viewLabel = currentActiveTab?.ViewMode == ViewMode.LineGraph ? "Swap to Bar View" : "Swap to Graph View";
            ImGui.BeginDisabled(currentActiveTab == null);
            if (IconMenuItem(viewLabel, viewIcon))
            {
                if (currentActiveTab != null)
                {
                    currentActiveTab.ViewMode = currentActiveTab.ViewMode == ViewMode.Bars ? ViewMode.LineGraph : ViewMode.Bars;
                    plugin.SaveConfig();
                }
            }
            ImGui.EndDisabled();

            ImGui.Separator();

            var lockIcon = plugin.Config.PinMainWindow ? FontAwesomeIcon.LockOpen : FontAwesomeIcon.Lock;
            var lockLabel = plugin.Config.PinMainWindow ? "Unlock Window" : "Lock Window";
            if (IconMenuItem(lockLabel, lockIcon))
            {
                if (!plugin.Config.PinMainWindow)
                {
                    plugin.Config.MainWindowPos = mainWindowPos;
                    plugin.Config.MainWindowSize = mainWindowSize;
                }
                plugin.Config.PinMainWindow = !plugin.Config.PinMainWindow;
                plugin.SaveConfig();
                if (lockButton != null)
                    lockButton.Icon = plugin.Config.PinMainWindow ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
            }

            if (IconMenuItem("Open Settings", FontAwesomeIcon.Cog))
                plugin.OpenConfigUi();

            ImGui.Separator();

            if (IconMenuItem("Close Window", FontAwesomeIcon.Times))
                IsOpen = false;

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

        var buttonWidth = config.TabButtonStretchToFit
            ? (regionWidth - spacing * (visibleCount - 1)) / visibleCount
            : config.TabButtonWidth;

        var drawList = ImGui.GetWindowDrawList();
        var cursor = ImGui.GetCursorScreenPos();

        using var tabFont = FontScope.Push(config.GetFontScale(config.TabButtonFontSize));

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

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
            if (ImGui.BeginPopupContextItem($"##tabCtx{i}"))
            {
                if (plugin.IsTabPoppedOut(tab.Id))
                {
                    if (IconMenuItem("Close Popout Window", FontAwesomeIcon.Times))
                        plugin.ClosePopoutTab(tab.Id);
                }
                else
                {
                    if (IconMenuItem("Open in Window", FontAwesomeIcon.ExternalLinkAlt))
                        plugin.OpenPopoutTab(tab.Id);
                }
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();

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

        tabFont.Dispose();

        ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().X, cursor.Y + buttonHeight));
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


    private void DrawReplayBar()
    {
        var sim = plugin.DataService.Store.ReplaySimulator;
        if (sim == null) return;

        var rowHeight = ImGui.GetFrameHeight() + 6f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.20f, 0.05f, 0.05f, 0.85f));
        if (ImGui.BeginChild("##replayBar", new Vector2(-1, rowHeight), false))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(new Vector4(1f, 0.55f, 0.55f, 1f), "[REPLAY]");

            ImGui.SameLine();
            var pinned = plugin.Config.ReplayBarPinned;
            var pinIcon = (pinned ? FontAwesomeIcon.Thumbtack : FontAwesomeIcon.MapPin).ToIconString();
            if (pinned)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.30f, 0.55f, 0.30f, 1f));
            ImGui.PushFont(UiBuilder.IconFont);
            var pinClicked = ImGui.SmallButton($"{pinIcon}##rpyPin");
            ImGui.PopFont();
            if (pinned)
                ImGui.PopStyleColor();
            if (pinClicked)
            {
                plugin.Config.ReplayBarPinned = !pinned;
                plugin.Config.Save?.Invoke();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(pinned
                    ? "Pinned: replay bar stays visible even if the modifier key is required.\nClick to unpin."
                    : "Pin the replay bar so it stays visible without holding the modifier key.");

            ImGui.SameLine();
            var playLabel = sim.IsRunning ? "Pause##rpyToggle" : "Play##rpyToggle";
            if (ImGui.SmallButton(playLabel))
            {
                if (sim.IsRunning) sim.Pause();
                else sim.Resume();
            }

            ImGui.SameLine();
            var d = sim.DurationSeconds;
            var seekTarget = sim.ElapsedSeconds;
            ImGui.SetNextItemWidth(-235f);
            var sliderLabel = $"{FormatMmSs(sim.ElapsedSeconds)} / {FormatMmSs(d)}";
            if (ImGui.SliderFloat("##rpySeek", ref seekTarget, 0f, d, sliderLabel, ImGuiSliderFlags.NoInput))
                sim.Seek(seekTarget);

            ImGui.SameLine();
            DrawSpeedButton(sim, 0.5f, "0.5x##rpy0_5");
            ImGui.SameLine();
            DrawSpeedButton(sim, 1f, "1x##rpy1");
            ImGui.SameLine();
            DrawSpeedButton(sim, 2f, "2x##rpy2");
            ImGui.SameLine();
            DrawSpeedButton(sim, 4f, "4x##rpy4");

            ImGui.SameLine();
            if (ImGui.SmallButton("Stop##rpyStop"))
                plugin.DataService.Store.ClearSampleData();
        }
        ImGui.EndChild();
        ImGui.PopStyleColor();
    }

    private static void DrawSpeedButton(EncounterReplaySimulator sim, float speed, string label)
    {
        var isActive = Math.Abs(sim.Speed - speed) < 0.01f;
        if (isActive)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.30f, 0.55f, 0.30f, 1f));
        if (ImGui.SmallButton(label))
            sim.Speed = speed;
        if (isActive)
            ImGui.PopStyleColor();
    }

    private static string FormatMmSs(float t)
    {
        if (t < 0f) t = 0f;
        var mins = (int)(t / 60f);
        var secs = (int)(t % 60f);
        return $"{mins:D2}:{secs:D2}";
    }

    internal static List<CombatantEntry> GetSortedCombatants(EncounterSnapshot encounter,
        SortField sortBy, bool desc, MeterTab? activeTab,
        HashSet<string>? partyNames = null, HashSet<string>? allianceNames = null)
    {
        var combatants = new List<CombatantEntry>(encounter.Combatants);

        combatants.RemoveAll(c => JobDataTable.GetRole(c.Job) == JobRole.Default);

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

    internal static void StampRanks(List<CombatantEntry> combatants)
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

    private static bool IconMenuItem(string label, FontAwesomeIcon icon)
    {
        var iconStr = icon.ToIconString();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(iconStr);
        ImGui.PopFont();
        ImGui.SameLine();
        return ImGui.Selectable(label);
    }

    private void SpawnReconnect() =>
        Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
}
