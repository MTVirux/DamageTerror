namespace DamageTerror.Gui.ConfigWindow;

#if DEBUG

internal static class DebugSection
{
    private static readonly (LogChannel Channel, string Label, string Tooltip, LogChannel? Parent)[] Channels =
    {
        (LogChannel.Plugin,          "Plugin",            "Plugin lifecycle: startup, disposal, territory changes, commands", null),
        (LogChannel.SkillTracker,    "Skill Tracker",     "Skill and damage type lookups", null),
        (LogChannel.PetDebug,        "Pet Debug",         "Pet ownership resolution, pet skill accumulation, ground-effect entities", LogChannel.SkillTracker),
        (LogChannel.DoTDiag,         "DoT Diagnostics",   "DoT/HoT aggregate line processing and status matching", LogChannel.SkillTracker),
        (LogChannel.StatusTracker,   "Status Tracker",    "Status/buff/debuff classification", null),
        (LogChannel.PartyMembership, "Party Membership",  "Party and alliance member queries", null),
        (LogChannel.FontService,     "Font Service",      "Custom font loading, atlas creation, font push/pop", null),
        (LogChannel.EncounterStore,  "Encounter Store",   "Encounter history loading and saving", null),
        (LogChannel.GifAnimator,     "GIF Animator",      "GIF frame extraction and temp file cleanup", null),
        (LogChannel.DataService,     "Data Service",      "Combat data processing and dispatch", null),
        (LogChannel.WebSocket,       "WebSocket",         "WebSocket connection, reconnect, and message handling", null),
        (LogChannel.Ipc,             "IPC",               "IINACT IPC communication", null),
    };

    private const string ImportConfirmPopup = "Import configuration?##dtImportConfig";

    private static string? pendingImportPath;
    private static bool importPopupOpen;
    private static string? notice;
    private static Vector4 noticeColor;
    private static DateTime noticeUtc;

    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Debug UI Visibility##debugUIVis", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var hide = config.HideDebugFeatures;
            if (ImGui.Checkbox("Hide debug-only UI features##hideDebugUI", ref hide))
            {
                config.HideDebugFeatures = hide;
                changed = true;
            }
            ConfigHelpers.HelpMarker(
                "Hides debug-only buttons (Recalculate, Replay CombatData) and any other debug UI surfaces.\n" +
                "The Debug page itself stays visible so you can flip this back.\nHas no effect in release builds.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Raw Capture##debugCapture", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var captureRaw = config.CaptureRawFrames;
            if (ImGui.Checkbox("Capture raw frames##captureRawFrames", ref captureRaw))
            {
                config.CaptureRawFrames = captureRaw;
                changed = true;
            }
            ConfigHelpers.HelpMarker(
                "Stores every raw ACT log line and IINACT CombatData frame of an encounter in a sidecar file " +
                "next to the history, enabling the Recalculate and Replay CombatData buttons.\nCosts memory and " +
                "disk space per encounter, so it stays off unless you need offline reparsing.\n" +
                "Has no effect in release builds.");
        }

        ImGui.Spacing();

        DrawConfigTransferSection();

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Log Channels##logCh"))
        {
            ImGui.TextWrapped("Enable or disable log output per service. Disabled channels will not write to the Dalamud log. Useful for silencing noisy services during testing.");
            ImGui.Spacing();

            if (ImGui.Button("Enable All##logch"))
            {
                if (config.DisabledLogChannels.Count > 0)
                {
                    config.DisabledLogChannels.Clear();
                    changed = true;
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Disable All##logch"))
            {
                foreach (var (channel, _, _, _) in Channels)
                {
                    if (config.DisabledLogChannels.Add(channel))
                        changed = true;
                }
            }

            ImGui.Separator();

            foreach (var (channel, label, tooltip, parent) in Channels)
            {
                if (parent.HasValue)
                    ImGui.Indent();

                var parentDisabled = parent.HasValue && config.DisabledLogChannels.Contains(parent.Value);
                if (parentDisabled)
                    ImGui.BeginDisabled();

                var enabled = !config.DisabledLogChannels.Contains(channel);
                if (ImGui.Checkbox($"{label}##logch_{channel}", ref enabled))
                {
                    if (enabled)
                        config.DisabledLogChannels.Remove(channel);
                    else
                        config.DisabledLogChannels.Add(channel);
                    changed = true;
                }

                ConfigHelpers.HelpMarker(tooltip);

                if (parentDisabled)
                    ImGui.EndDisabled();

                if (parent.HasValue)
                    ImGui.Unindent();
            }
        }

        return changed;
    }

    private static void DrawConfigTransferSection()
    {
        if (ImGui.CollapsingHeader("Import / Export Config##debugConfigIo"))
        {
            ImGui.TextWrapped("Export writes a copy of the configuration file. Import replaces the live one - the current config is backed up first, and you have to reload the plugin for the imported one to take effect.");
            ImGui.Spacing();

            if (ImGui.Button("Export...##dtExportConfig"))
            {
                var plugin = DamageTerrorPlugin.Instance;
                var defaultName = $"DamageTerror-config-{DateTime.Now:yyyyMMdd-HHmmss}";
                AppearanceTab.FileDialogManager.SaveFileDialog(
                    "Export Damage Terror Config",
                    "Configuration{.json}",
                    defaultName,
                    ".json",
                    (ok, path) =>
                    {
                        if (!ok || string.IsNullOrEmpty(path))
                            return;

                        plugin.SaveConfig();
                        if (plugin.ConfigBackup.ExportToFile(path))
                            SetNotice($"Config exported to {path}", new Vector4(0.5f, 0.9f, 0.5f, 1f));
                        else
                            SetNotice("Export failed. Check the plugin log for details.", new Vector4(1f, 0.5f, 0.5f, 1f));
                    });
            }
            ConfigHelpers.HelpMarker("Saves the current settings, then copies the config file to the chosen location.");

            ImGui.SameLine();

            if (ImGui.Button("Import...##dtImportConfig"))
            {
                AppearanceTab.FileDialogManager.OpenFileDialog(
                    "Import Damage Terror Config",
                    "Configuration{.json}",
                    (ok, path) =>
                    {
                        if (!ok || string.IsNullOrEmpty(path))
                            return;

                        if (!DamageTerrorPlugin.Instance.ConfigBackup.IsValidConfigFile(path, out var error))
                        {
                            SetNotice($"Import failed: {error}", new Vector4(1f, 0.5f, 0.5f, 1f));
                            return;
                        }

                        pendingImportPath = path;
                    });
            }
            ConfigHelpers.HelpMarker("Replaces the config file with the chosen one after a confirmation.");

            if (notice != null && (DateTime.UtcNow - noticeUtc).TotalSeconds < 30)
            {
                ImGui.Spacing();
                ImGui.TextColored(noticeColor, notice);
            }
        }

        DrawImportConfirmPopup();
    }

    private static void DrawImportConfirmPopup()
    {
        if (pendingImportPath == null)
            return;

        if (!importPopupOpen)
        {
            ImGui.OpenPopup(ImportConfirmPopup);
            importPopupOpen = true;
        }

        var open = true;
        if (!ImGui.BeginPopupModal(ImportConfirmPopup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            pendingImportPath = null;
            importPopupOpen = false;
            return;
        }

        ImGui.Text("Replace the current configuration with this file?");
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f),
            "You'll need to reload Damage Terror for the imported config to take effect.");
        ImGui.TextDisabled("Don't change any settings before reloading - a save would overwrite the import.");
        ImGui.Spacing();
        ImGui.TextDisabled(pendingImportPath);
        ImGui.Spacing();

        if (ImGui.Button("Import", new Vector2(120, 0)))
        {
            var backup = DamageTerrorPlugin.Instance.ConfigBackup;
            backup.WriteBackupFromLiveConfig(force: true);

            if (backup.RestoreFromFile(pendingImportPath))
                SetNotice("Config imported. Reload Damage Terror to apply.", new Vector4(1f, 0.8f, 0.3f, 1f));
            else
                SetNotice("Import failed. Check the plugin log for details.", new Vector4(1f, 0.5f, 0.5f, 1f));

            pendingImportPath = null;
            importPopupOpen = false;
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
        {
            pendingImportPath = null;
            importPopupOpen = false;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private static void SetNotice(string text, Vector4 color)
    {
        notice = text;
        noticeColor = color;
        noticeUtc = DateTime.UtcNow;
    }
}
#endif
