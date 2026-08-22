namespace DamageTerror.Gui.ConfigWindow;

public sealed class ConfigManagementPage
{
    private const string RestoreConfirmPopup = "Restore configuration?##dt_restore_confirm";
    private const string DeleteConfirmPopup = "Delete recovery file?##dt_delete_confirm";
    private const string ImportConfirmPopup = "Import configuration?##dtImportConfig";

    private readonly DamageTerrorPlugin plugin;
    private string? pendingRestorePath;
    private string? pendingDeletePath;
    private string? pendingImportPath;
    private bool importPopupOpen;
    private string? notice;
    private Vector4 noticeColor;
    private DateTime noticeUtc;

    public ConfigManagementPage(DamageTerrorPlugin plugin)
    {
        this.plugin = plugin;
    }

    public void Draw()
    {
        ImGui.TextUnformatted("Config Management");
        ImGui.Separator();
        ImGui.Spacing();

        DrawBackupAndRecoverySection();

        ImGui.Spacing();

        DrawConfigTransferSection();

        if (notice != null && (DateTime.UtcNow - noticeUtc).TotalSeconds < 30)
        {
            ImGui.Spacing();
            ImGui.TextColored(noticeColor, notice);
        }

        DrawImportConfirmPopup();
    }

    private void DrawBackupAndRecoverySection()
    {
        if (!ImGui.CollapsingHeader("Backup & Recovery", ImGuiTreeNodeFlags.DefaultOpen))
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
                ImGui.Text(ValueFormatter.FormatBytes(entry.SizeBytes));

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

    private void DrawConfigTransferSection()
    {
        if (!ImGui.CollapsingHeader("Import / Export##dtConfigIo", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.TextWrapped("Export writes a copy of the configuration file. Import replaces the live one - the current config is backed up first, and you have to reload the plugin for the imported one to take effect.");
        ImGui.Spacing();

        if (ImGui.Button("Export...##dtExportConfig"))
        {
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

                    if (!plugin.ConfigBackup.IsValidConfigFile(path, out var error))
                    {
                        SetNotice($"Import failed: {error}", new Vector4(1f, 0.5f, 0.5f, 1f));
                        return;
                    }

                    pendingImportPath = path;
                });
        }
        ConfigHelpers.HelpMarker("Replaces the config file with the chosen one after a confirmation.");
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
                SetNotice("Configuration restored. Reload Damage Terror to apply.", new Vector4(1f, 0.8f, 0.3f, 1f));
            else
                SetNotice("Restore failed. Check the plugin log for details.", new Vector4(1f, 0.5f, 0.5f, 1f));

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

    private void DrawImportConfirmPopup()
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
            var backup = plugin.ConfigBackup;
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

    private void SetNotice(string text, Vector4 color)
    {
        notice = text;
        noticeColor = color;
        noticeUtc = DateTime.UtcNow;
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
}
