namespace DamageTerror.Gui.ConfigWindow;

public sealed class GeneralTab
{
    private const string RestoreConfirmPopup = "Restore configuration?##dt_restore_confirm";
    private const string DeleteConfirmPopup = "Delete recovery file?##dt_delete_confirm";

    private readonly DamageTerrorPlugin plugin;
    private string wsUrlBuffer;
    private string? pendingRestorePath;
    private string? pendingDeletePath;
    private string? lastRestoreNotice;
    private DateTime lastRestoreNoticeUtc;

    public GeneralTab(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
        this.wsUrlBuffer = plugin.Config.WebSocketUrl;
    }

    public bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.Button("Run setup wizard"))
            plugin.OpenSetupWizard();
        ConfigHelpers.HelpMarker("Walks through data source, theme preset, and core behavior again. Nothing changes until you pick something.");
        if (ImGui.Button("Run customization wizard"))
            plugin.OpenCustomizationWizard();
        ConfigHelpers.HelpMarker("A quick pass over colors, icons, and markings. The full set of options can be found under Appearance.");
        if (ImGui.Button("Run column wizard"))
            plugin.OpenColumnWizard();
        ConfigHelpers.HelpMarker("Pick which columns a meter tab shows and their order. Per-column extras live under Appearance.");
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Connection", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Prefer IPC (in-process, lowest latency)", config.PreferIpc, v => config.PreferIpc = v);

            ImGui.SetNextItemWidth(280);
            if (ImGui.InputText("WebSocket URL", ref wsUrlBuffer, 256))
            {
                config.WebSocketUrl = wsUrlBuffer;
                changed = true;
            }

            ImGui.TextDisabled($"Status: {plugin.DataService.ConnectionStatus}");

            if (ImGui.Button("Reconnect"))
            {
                Task.Run(async () => await plugin.DataService.ReconnectAsync().ConfigureAwait(false));
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Behavior", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Open meter on plugin start", config.ShowOnStart, v => config.ShowOnStart = v);

            changed |= ConfigHelpers.CheckboxProp("Hide when out of combat", config.HideOutOfCombat, v => config.HideOutOfCombat = v);

            if (config.HideOutOfCombat)
            {
                ImGui.Indent();
                changed |= ConfigHelpers.SliderFloatProp("Hide delay (seconds)", config.HideOutOfCombatDelay, 0f, 30f, "%.1f", v => config.HideOutOfCombatDelay = v, 150);
                ImGui.Unindent();
            }

            changed |= ConfigHelpers.CheckboxProp("Don't store 0 eDPS encounters", config.SkipZeroEdpsEncounters, v => config.SkipZeroEdpsEncounters = v);

            if (config.SkipZeroEdpsEncounters)
            {
                var zeroCount = plugin.DataService.Store.CountZeroEdpsEncounters();
                if (zeroCount > 0)
                {
                    ImGui.Indent();
                    ImGui.TextColored(new System.Numerics.Vector4(1f, 0.8f, 0.3f, 1f),
                        $"Found {zeroCount} encounter{(zeroCount != 1 ? "s" : "")} with 0 eDPS in history.");
                    ImGui.SameLine();
                    if (ImGui.Button($"Clean up##{zeroCount}"))
                    {
                        plugin.DataService.Store.RemoveZeroEdpsEncounters();
                    }
                    ImGui.Unindent();
                }
            }

            changed |= ConfigHelpers.CheckboxProp("Enable encounter replays", config.EnableReplays, v =>
            {
                config.EnableReplays = v;
                if (!v)
                    plugin.DataService.Store.StopActiveReplay();
            });
            ConfigHelpers.HelpMarker("Play a finished encounter back through the meter. When off, the Replay buttons and the Replay Bar layout entry are hidden.");

            changed |= ConfigHelpers.CheckboxProp("Ignore ESC key closing the meter", config.IgnoreEscClose, v => config.IgnoreEscClose = v);

            ImGui.Spacing();

            var dotCalcLabels = new[] { "DamageTerror (recommended)", "IINACT / ACT (no DoT Breakdown)" };
            changed |= ConfigHelpers.ComboProp("DoT calculation", (int)config.DotCalcMode, dotCalcLabels, v => config.DotCalcMode = (DotCalcMode)v, 280);
            ConfigHelpers.HelpMarker(
                "DamageTerror: distributes aggregated DoT ticks across active statuses using potency weights. (needed for dot skill breakdown)\n" +
                "IINACT / ACT: trusts the parser's own DoT simulation and attributes each tick to the named source as-is. (no DoT skill breakdown)");

            var endEncLabels = new[] { "/echo end (ACT + IINACT)", "/endenc (IINACT only) (Silent)" };
            changed |= ConfigHelpers.ComboProp("Encounter cut command", (int)config.EndEncounterMode, endEncLabels, v => config.EndEncounterMode = (EndEncounterMode)v, 280);
            ConfigHelpers.HelpMarker(
                "/echo end: sends a visible echo message that both ACT and IINACT recognize as an encounter split trigger.\n" +
                "/endenc: IINACT's built-in Dalamud command. Silent, but only works with IINACT.");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var comboNames = new[] { "Ctrl + Shift", "Ctrl + Alt", "Shift + Alt", "Ctrl", "Shift", "Alt" };
            changed |= ConfigHelpers.ComboProp("Modifier keys", (int)config.ModifierKeyCombo, comboNames, v => config.ModifierKeyCombo = (ModifierCombo)v, 150);
            ConfigHelpers.HelpMarker("Modifier key used by hidden layout elements and header reveal.");

            var modeNames = new[] { "Hold", "Toggle" };
            changed |= ConfigHelpers.ComboProp("Modifier mode", (int)config.ModifierKeyMode, modeNames, v => config.ModifierKeyMode = (ModifierMode)v, 150);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hold: active only while keys are pressed.\nToggle: press once to activate, press again to deactivate.");


        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Position & Size"))
        {
            if (config.PinMainWindow)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f),
                    "Window is locked. Using these controls will update its pinned position and size.");
                ImGui.Spacing();
            }

            ImGui.TextDisabled("Snap the meter window to a screen edge or corner.");
            ImGui.Spacing();

            var viewport = ImGui.GetMainViewport();
            var workPos = viewport.WorkPos;
            var workSize = viewport.WorkSize;
            var windowSize = config.MainWindowSize;
            var btnSize = new Vector2(80, 0);

            void Dock(float x, float y)
            {
                x = Math.Max(workPos.X, Math.Min(x, workPos.X + workSize.X - windowSize.X));
                y = Math.Max(workPos.Y, Math.Min(y, workPos.Y + workSize.Y - windowSize.Y));
                config.MainWindowPos = new Vector2(x, y);
                config.PinMainWindow = true;
                changed = true;
            }

            if (ImGui.Button("Top-Left", btnSize))
                Dock(workPos.X, workPos.Y);
            ImGui.SameLine();
            if (ImGui.Button("Top", btnSize))
                Dock(workPos.X + (workSize.X - windowSize.X) / 2f, workPos.Y);
            ImGui.SameLine();
            if (ImGui.Button("Top-Right", btnSize))
                Dock(workPos.X + workSize.X - windowSize.X, workPos.Y);

            if (ImGui.Button("Left", btnSize))
                Dock(workPos.X, workPos.Y + (workSize.Y - windowSize.Y) / 2f);
            ImGui.SameLine();
            ImGui.Dummy(btnSize);
            ImGui.SameLine();
            if (ImGui.Button("Right", btnSize))
                Dock(workPos.X + workSize.X - windowSize.X, workPos.Y + (workSize.Y - windowSize.Y) / 2f);

            if (ImGui.Button("Bot-Left", btnSize))
                Dock(workPos.X, workPos.Y + workSize.Y - windowSize.Y);
            ImGui.SameLine();
            if (ImGui.Button("Bottom", btnSize))
                Dock(workPos.X + (workSize.X - windowSize.X) / 2f, workPos.Y + workSize.Y - windowSize.Y);
            ImGui.SameLine();
            if (ImGui.Button("Bot-Right", btnSize))
                Dock(workPos.X + workSize.X - windowSize.X, workPos.Y + workSize.Y - windowSize.Y);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            changed |= ConfigHelpers.SliderFloatProp("Width", config.MainWindowSize.X, 250f, workSize.X, "%.0f", v => config.MainWindowSize = new Vector2(v, config.MainWindowSize.Y), 200);
            changed |= ConfigHelpers.SliderFloatProp("Height", config.MainWindowSize.Y, 150f, workSize.Y, "%.0f", v => config.MainWindowSize = new Vector2(config.MainWindowSize.X, v), 200);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Duty Filters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Overworld / Open World", config.EnableInOverworld, v => config.EnableInOverworld = v);
            changed |= ConfigHelpers.CheckboxProp("Dungeons", config.EnableInDungeons, v => config.EnableInDungeons = v);
            changed |= ConfigHelpers.CheckboxProp("Trials", config.EnableInTrials, v => config.EnableInTrials = v);
            changed |= ConfigHelpers.CheckboxProp("Raids (Savage / Ultimate)", config.EnableInRaids, v => config.EnableInRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Alliance Raids", config.EnableInAllianceRaids, v => config.EnableInAllianceRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Deep Dungeons (PotD / HoH / EO)", config.EnableInDeepDungeons, v => config.EnableInDeepDungeons = v);
            changed |= ConfigHelpers.CheckboxProp("Field Operations (Eureka / Bozja)", config.EnableInFieldOperations, v => config.EnableInFieldOperations = v);
            changed |= ConfigHelpers.CheckboxProp("Field Raids (Delubrum / Dalriada)", config.EnableInFieldRaids, v => config.EnableInFieldRaids = v);
            changed |= ConfigHelpers.CheckboxProp("Criterion Dungeons", config.EnableInCriterion, v => config.EnableInCriterion = v);
            changed |= ConfigHelpers.CheckboxProp("Variant Dungeons", config.EnableInVariant, v => config.EnableInVariant = v);
            changed |= ConfigHelpers.CheckboxProp("PvP", config.EnableInPvP, v => config.EnableInPvP = v);
        }

        ImGui.Spacing();

        DrawBackupAndRecoverySection();

        return changed;
    }

    private void DrawBackupAndRecoverySection()
    {
        if (!ImGui.CollapsingHeader("Backup & Recovery"))
            return;

        var backup = plugin.ConfigBackup;

        ImGui.TextDisabled("Damage Terror keeps an automatic copy of your configuration so a corrupt or unreadable file can be recovered.");
        ImGui.Spacing();

        var lastBackup = backup.LastBackupUtc;
        if (lastBackup == DateTime.MinValue)
            ImGui.TextDisabled("No backup written this session.");
        else
            ImGui.TextDisabled($"Last backup written: {FormatRelativeTime(lastBackup)} ({lastBackup.ToLocalTime():yyyy-MM-dd HH:mm:ss})");

        ImGui.SameLine();
        if (ImGui.Button("Back up now##dt_backup_now"))
        {
            backup.WriteBackupFromLiveConfig(force: true);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Force-write the .bak file even if the throttle window hasn't elapsed.");

        if (lastRestoreNotice != null && (DateTime.UtcNow - lastRestoreNoticeUtc).TotalSeconds < 30)
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), lastRestoreNotice);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var entries = backup.ListRecoveryFiles();
        if (entries.Count == 0)
        {
            ImGui.TextDisabled("No recovery files on disk yet.");
            return;
        }

        if (ImGui.BeginTable("dt_recovery_table", 4,
            ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("When", ImGuiTableColumnFlags.WidthFixed, 220f);
            ImGui.TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, 90f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                ImGui.PushID($"dt_recovery_{i}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (entry.Kind == RecoveryKind.Backup)
                    ImGui.TextColored(new Vector4(0.5f, 0.9f, 0.5f, 1f), "Backup");
                else
                    ImGui.TextColored(new Vector4(1f, 0.7f, 0.4f, 1f), "Broken");

                ImGui.TableNextColumn();
                ImGui.Text($"{FormatRelativeTime(entry.Timestamp)} ({entry.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss})");

                ImGui.TableNextColumn();
                ImGui.Text(FormatSize(entry.SizeBytes));

                ImGui.TableNextColumn();
                if (ImGui.Button("Restore##restore"))
                {
                    pendingRestorePath = entry.FilePath;
                    ImGui.OpenPopup(RestoreConfirmPopup);
                }
                ImGui.SameLine();
                if (ImGui.Button("Delete##delete"))
                {
                    pendingDeletePath = entry.FilePath;
                    ImGui.OpenPopup(DeleteConfirmPopup);
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(entry.FilePath);

                DrawRestoreConfirmPopup();
                DrawDeleteConfirmPopup();

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
    }

    private void DrawRestoreConfirmPopup()
    {
        var open = true;
        if (!ImGui.BeginPopupModal(RestoreConfirmPopup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text("Replace the current configuration with this recovery file?");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f),
            "You'll need to reload Damage Terror for the restored config to take effect.");
        ImGui.TextDisabled("In-game: Settings -> Plugin Installer -> Damage Terror -> Disable then Enable,");
        ImGui.TextDisabled("or run /xlplugin and toggle the plugin from there.");
        ImGui.Spacing();
        ImGui.TextDisabled(pendingRestorePath ?? string.Empty);
        ImGui.Spacing();

        if (ImGui.Button("Restore", new Vector2(120, 0)))
        {
            if (pendingRestorePath != null && plugin.ConfigBackup.RestoreFromFile(pendingRestorePath))
            {
                lastRestoreNotice = "Configuration restored. Reload Damage Terror to apply.";
                lastRestoreNoticeUtc = DateTime.UtcNow;
            }
            else
            {
                lastRestoreNotice = "Restore failed. Check the plugin log for details.";
                lastRestoreNoticeUtc = DateTime.UtcNow;
            }
            pendingRestorePath = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
        {
            pendingRestorePath = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawDeleteConfirmPopup()
    {
        var open = true;
        if (!ImGui.BeginPopupModal(DeleteConfirmPopup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.Text("Delete this recovery file?");
        ImGui.Spacing();
        ImGui.TextDisabled(pendingDeletePath ?? string.Empty);
        ImGui.Spacing();

        if (ImGui.Button("Delete", new Vector2(120, 0)))
        {
            if (pendingDeletePath != null)
                plugin.ConfigBackup.DeleteRecoveryFile(pendingDeletePath);
            pendingDeletePath = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
        {
            pendingDeletePath = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static string FormatRelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} d ago";
        return utc.ToLocalTime().ToString("yyyy-MM-dd");
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
