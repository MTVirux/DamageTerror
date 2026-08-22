namespace DamageTerror.Gui.ConfigWindow;

public sealed class ConfigManagementPage
{
    private const string RestoreConfirmPopup = "Restore configuration?##dt_restore_confirm";
    private const string DeleteConfirmPopup = "Delete recovery file?##dt_delete_confirm";
    private const string ImportConfirmPopup = "Import configuration?##dtImportConfig";

    private readonly DamageTerrorPlugin plugin;
    private readonly HashSet<ConfigCategory> exportCategories = new(ConfigCategories.DefaultSelection);
    private string? pendingRestorePath;
    private string? pendingDeletePath;
    private string? pendingImportPath;
    private ImportContents? pendingImportContents;
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

        ImGui.TextWrapped("Export writes the ticked settings to a file. Import merges a file back in - only the settings it carries change, the rest are left as they are. The current config is backed up first, and you have to reload the plugin for an import to take effect.");
        ImGui.Spacing();

        DrawExportCategoryPicker();

        ImGui.Spacing();

        var canExport = exportCategories.Count > 0;
        if (!canExport) ImGui.BeginDisabled();
        if (ImGui.Button("Export...##dtExportConfig"))
        {
            var defaultName = $"DamageTerror-config-{DateTime.Now:yyyyMMdd-HHmmss}";
            var selection = exportCategories.ToList();
            AppearanceTab.FileDialogManager.SaveFileDialog(
                "Export Damage Terror Config",
                "Damage Terror Config{.dtcnf}",
                defaultName,
                ".dtcnf",
                (ok, path) =>
                {
                    if (!ok || string.IsNullOrEmpty(path))
                        return;

                    plugin.SaveConfig();
                    if (plugin.ConfigBackup.ExportToFile(path, selection))
                        SetNotice($"Exported {selection.Count} categories to {path}", new Vector4(0.5f, 0.9f, 0.5f, 1f));
                    else
                        SetNotice("Export failed. Check the plugin log for details.", new Vector4(1f, 0.5f, 0.5f, 1f));
                });
        }
        if (!canExport) ImGui.EndDisabled();
        ConfigHelpers.HelpMarker("Saves the current settings, then writes the ticked categories to the chosen location.");

        ImGui.SameLine();

        if (ImGui.Button("Import...##dtImportConfig"))
        {
            AppearanceTab.FileDialogManager.OpenFileDialog(
                "Import Damage Terror Config",
                "Damage Terror Config{.dtcnf,.json}",
                (ok, path) =>
                {
                    if (!ok || string.IsNullOrEmpty(path))
                        return;

                    if (!plugin.ConfigBackup.IsValidConfigFile(path, out var error))
                    {
                        SetNotice($"Import failed: {error}", new Vector4(1f, 0.5f, 0.5f, 1f));
                        return;
                    }

                    pendingImportContents = plugin.ConfigBackup.InspectImportFile(path);
                    pendingImportPath = path;
                });
        }
        ConfigHelpers.HelpMarker("Merges the chosen file into the config after a confirmation.");
    }

    private void DrawExportCategoryPicker()
    {
        if (!ImGui.TreeNodeEx($"Settings to export ({exportCategories.Count}/{ConfigCategories.All.Count})##dtExportPicker",
            ImGuiTreeNodeFlags.DefaultOpen))
            return;

        if (ImGui.SmallButton("All##dtExportAll"))
        {
            foreach (var info in ConfigCategories.All)
                exportCategories.Add(info.Category);
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("None##dtExportNone"))
            exportCategories.Clear();
        ImGui.SameLine();
        if (ImGui.SmallButton("Reset##dtExportReset"))
        {
            exportCategories.Clear();
            foreach (var category in ConfigCategories.DefaultSelection)
                exportCategories.Add(category);
        }

        ImGui.Spacing();

        var appearanceDrawn = false;
        foreach (var info in ConfigCategories.All)
        {
            if (info.Group == null)
            {
                DrawCategoryCheckbox(info);
                continue;
            }

            if (appearanceDrawn)
                continue;

            appearanceDrawn = true;
            DrawAppearanceGroup();
        }

        ImGui.TreePop();
    }

    private void DrawAppearanceGroup()
    {
        var members = ConfigCategories.All.Where(c => c.Group == ConfigCategories.AppearanceGroup).ToList();

        var allSelected = members.All(m => exportCategories.Contains(m.Category));
        if (ImGui.Checkbox("##dtExportAppearanceAll", ref allSelected))
        {
            foreach (var member in members)
            {
                if (allSelected)
                    exportCategories.Add(member.Category);
                else
                    exportCategories.Remove(member.Category);
            }
        }

        ImGui.SameLine();
        if (!ImGui.TreeNodeEx($"{ConfigCategories.AppearanceGroup}##dtExportAppearance", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        foreach (var member in members)
            DrawCategoryCheckbox(member);

        ImGui.TreePop();
    }

    private void DrawCategoryCheckbox(ConfigCategories.CategoryInfo info)
    {
        var selected = exportCategories.Contains(info.Category);
        if (ImGui.Checkbox($"{info.Label}##dtExportCat{info.Category}", ref selected))
        {
            if (selected)
                exportCategories.Add(info.Category);
            else
                exportCategories.Remove(info.Category);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(info.Tooltip);
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
            ClearPendingImport();
            return;
        }

        var contents = pendingImportContents;
        if (contents is { IsPartial: true })
        {
            ImGui.Text("Import these settings from the file?");
            ImGui.Spacing();
            foreach (var category in contents.Categories)
                ImGui.BulletText(ConfigCategories.Label(category));
            ImGui.Spacing();
            ImGui.TextDisabled("Everything else keeps its current value.");
        }
        else
        {
            ImGui.Text("Replace the current configuration with this file?");
            if (contents != null && contents.Categories.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextDisabled($"Whole config file - {contents.SettingCount} settings.");
            }
        }

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

            if (backup.MergeFromFile(pendingImportPath))
                SetNotice("Config imported. Reload Damage Terror to apply.", new Vector4(1f, 0.8f, 0.3f, 1f));
            else
                SetNotice("Import failed. Check the plugin log for details.", new Vector4(1f, 0.5f, 0.5f, 1f));

            ClearPendingImport();
            ImGui.CloseCurrentPopup();
        }
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(120, 0)))
        {
            ClearPendingImport();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void ClearPendingImport()
    {
        pendingImportPath = null;
        pendingImportContents = null;
        importPopupOpen = false;
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
