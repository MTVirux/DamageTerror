using Dalamud.Interface.ImGuiFileDialog;

namespace DamageTerror.Gui.ConfigWindow;

public sealed class AppearanceTab
{
    private readonly PresetManager presetManager;
    private int selectedPresetIndex = -1;
    private string savePresetName = string.Empty;
    private string savePresetDesc = string.Empty;
    private string importJson = string.Empty;
    private string? importError;

    internal static readonly FileDialogManager FileDialogManager = new();

    public AppearanceTab(PresetManager presetManager)
    {
        this.presetManager = presetManager;
    }

    public bool DrawPresetsPage(Configuration config)
    {
        return DrawPresetSection(config);
    }

    private bool DrawPresetSection(Configuration config)
    {
        var changed = false;
        var allPresets = presetManager.GetAllPresets().ToList();

        ImGui.TextDisabled("Theme Preset");

        var previewLabel = selectedPresetIndex >= 0 && selectedPresetIndex < allPresets.Count
            ? allPresets[selectedPresetIndex].Name
            : "Select a preset...";

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 220);
        if (ImGui.BeginCombo("##presetCombo", previewLabel))
        {
            var hasCustom = allPresets.Any(p => !p.IsBuiltIn);
            if (hasCustom)
            {
                ImGui.TextDisabled("Custom");
                for (var i = 0; i < allPresets.Count; i++)
                {
                    var preset = allPresets[i];
                    if (!preset.IsBuiltIn)
                    {
                        var isSelected = selectedPresetIndex == i;
                        if (ImGui.Selectable($"  {preset.Name}##preset{i}", isSelected))
                            selectedPresetIndex = i;

                        if (!string.IsNullOrEmpty(preset.Description) && ImGui.IsItemHovered())
                            ImGui.SetTooltip(preset.Description);

                        if (ImGui.BeginPopupContextItem($"##presetCtx{i}"))
                        {
                            if (ImGui.MenuItem("Export to Clipboard"))
                            {
                                var json = presetManager.ExportPreset(preset);
                                ImGui.SetClipboardText(json);
                            }

                            if (ImGui.MenuItem("Delete"))
                            {
                                presetManager.DeleteCustomPreset(preset.Name);
                                if (selectedPresetIndex == i) selectedPresetIndex = -1;
                            }

                            ImGui.EndPopup();
                        }
                    }
                }

                ImGui.Spacing();
            }

            ImGui.TextDisabled("Built-in");
            for (var i = 0; i < allPresets.Count; i++)
            {
                var preset = allPresets[i];
                if (preset.IsBuiltIn)
                {
                    var isSelected = selectedPresetIndex == i;
                    if (ImGui.Selectable($"  {preset.Name}##preset{i}", isSelected))
                        selectedPresetIndex = i;

                    if (!string.IsNullOrEmpty(preset.Description) && ImGui.IsItemHovered())
                        ImGui.SetTooltip(preset.Description);
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Apply") && selectedPresetIndex >= 0 && selectedPresetIndex < allPresets.Count)
        {
            allPresets[selectedPresetIndex].ApplyTo(config);
            changed = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("Save Current"))
        {
            savePresetName = string.Empty;
            savePresetDesc = string.Empty;
            ImGui.OpenPopup("##savePresetPopup");
        }

        ImGui.SameLine();
        if (ImGui.Button("Import"))
        {
            importJson = string.Empty;
            importError = null;
            ImGui.OpenPopup("##importPresetPopup");
        }
        ConfigHelpers.HelpMarker("Tip: Right-click custom presets in the dropdown to delete or export them.");

        if (ImGui.BeginPopup("##savePresetPopup"))
        {
            ImGui.Text("Save current settings as a custom preset:");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(250);
            ImGui.InputText("Name", ref savePresetName, 128);

            ImGui.SetNextItemWidth(250);
            ImGui.InputText("Description", ref savePresetDesc, 256);

            ImGui.Spacing();

            var trimmedName = savePresetName.Trim();
            var isBuiltInName = presetManager.BuiltInPresets.Any(p => p.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
            var canSave = !string.IsNullOrWhiteSpace(savePresetName) && !isBuiltInName;
            if (isBuiltInName && !string.IsNullOrWhiteSpace(savePresetName))
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), "Cannot overwrite a built-in preset.");
            if (!canSave) ImGui.BeginDisabled();
            if (ImGui.Button("Save"))
            {
                var preset = ThemePreset.CreateFromConfig(config, trimmedName, savePresetDesc.Trim());
                presetManager.SaveCustomPreset(preset);
                selectedPresetIndex = -1;
                ImGui.CloseCurrentPopup();
            }
            if (!canSave) ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        if (ImGui.BeginPopup("##importPresetPopup"))
        {
            ImGui.Text("Paste preset JSON from clipboard:");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(350);
            ImGui.InputTextMultiline("##importJson", ref importJson, 8192, new Vector2(350, 150));

            ImGui.Spacing();

            if (ImGui.Button("Paste from Clipboard"))
                importJson = ImGui.GetClipboardText() ?? string.Empty;

            ImGui.SameLine();
            var canImport = !string.IsNullOrWhiteSpace(importJson);
            if (!canImport) ImGui.BeginDisabled();
            if (ImGui.Button("Import & Apply"))
            {
                var preset = presetManager.ImportPreset(importJson, out importError);
                if (preset != null)
                {
                    presetManager.SaveCustomPreset(preset);
                    preset.ApplyTo(config);
                    changed = true;
                    selectedPresetIndex = -1;
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Import Only"))
            {
                var preset = presetManager.ImportPreset(importJson, out importError);
                if (preset != null)
                {
                    presetManager.SaveCustomPreset(preset);
                    selectedPresetIndex = -1;
                    ImGui.CloseCurrentPopup();
                }
            }
            if (!canImport) ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel##import"))
                ImGui.CloseCurrentPopup();

            if (importError != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), importError);
            }

            ImGui.EndPopup();
        }

        if (selectedPresetIndex >= 0 && selectedPresetIndex < allPresets.Count)
        {
            var preset = allPresets[selectedPresetIndex];
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled($"Preview — {preset.Name}");
            if (!string.IsNullOrEmpty(preset.Description))
                ImGui.TextWrapped(preset.Description);
            ImGui.Spacing();

            DrawPresetBreakdown(preset);
        }

        return changed;
    }

    private static void DrawPresetBreakdown(ThemePreset preset)
    {
        var dimColor = new Vector4(0.6f, 0.6f, 0.6f, 1f);

        if (ImGui.CollapsingHeader("Bars##presetBars", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            PresetRow("Bar Height", $"{preset.BarHeight:0}px");
            PresetRow("Bar Spacing", $"{preset.BarSpacing:0}px");
            PresetRow("Bar Rounding", $"{preset.BarRounding:0.#}");
            PresetRow("Bar Opacity", $"{preset.BarAlpha:P0}");
            PresetRow("Bar Font Size", $"{preset.BarFontSize:0.#}pt");
            PresetRow("Icon Size", $"{preset.IconSize:0}px");
            PresetRow("Left Padding", $"{preset.BarLeftPadding:0}px");
            PresetRow("Right Padding", $"{preset.BarRightPadding:0}px");
            PresetRow("Column Spacing", $"{preset.BarColumnSpacing:0}px");
            PresetRow("Icon-Text Padding", $"{preset.IconTextPadding:0}px");
            PresetRow("Window Padding", $"L:{preset.WindowPaddingLeft:0} R:{preset.WindowPaddingRight:0} T:{preset.WindowPaddingTop:0} B:{preset.WindowPaddingBottom:0}");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Value Format##presetFmt"))
        {
            ImGui.Indent();
            var fmtLabel = preset.ValueDisplayFormat switch
            {
                ValueDisplayFormat.Abbreviated => "Abbreviated (12.3K)",
                ValueDisplayFormat.Commas => "Commas (12,345)",
                ValueDisplayFormat.Raw => "Raw (12345.6)",
                _ => preset.ValueDisplayFormat.ToString()
            };
            PresetRow("Number Format", fmtLabel);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Display Flags##presetFlags"))
        {
            ImGui.Indent();
            PresetToggle("Job Icons", preset.ShowJobIcons);
            PresetToggle("Name", preset.ShowNameOnBar);
            PresetToggle("Job Abbreviation", preset.ShowJobAbbrevOnBar);
            PresetToggle("Rank Number", preset.ShowRankNumber);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Self Highlighting##presetSelf"))
        {
            ImGui.Indent();
            PresetToggle("Highlight Player Bar", preset.SelfBarHighlight);
            if (preset.SelfBarHighlight)
                PresetColor("Accent Color", preset.SelfBarHighlightColor);
            PresetToggle("Custom Name Color", preset.UseSelfNameColor);
            if (preset.UseSelfNameColor)
                PresetColor("Name Color", preset.SelfNameColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Role Colors##presetRoleColors"))
        {
            ImGui.Indent();
            PresetToggle("Per-Job Colors", preset.UsePerJobColors);
            PresetColor("Tank", preset.TankColor);
            PresetColor("Healer", preset.HealerColor);
            PresetColor("Melee DPS", preset.MeleeDpsColor);
            PresetColor("Ranged DPS", preset.RangedDpsColor);
            PresetColor("Caster DPS", preset.CasterDpsColor);
            PresetColor("Limit Break", preset.LimitBreakColor);
            PresetColor("Default", preset.DefaultJobColor);
            if (preset.UsePerJobColors && preset.JobColors is { Count: > 0 })
                PresetRow("Custom Job Colors", $"{preset.JobColors.Count} jobs");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Text & Background Colors##presetTxtBg"))
        {
            ImGui.Indent();
            PresetColor("Name Text", preset.NameTextColor);
            PresetColor("Value Text", preset.ValueTextColor);
            PresetColor("Bar Background", preset.BarBackgroundColor);
            PresetColor("Window Background", preset.WindowBackgroundColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Selection Bar##presetSelBar"))
        {
            ImGui.Indent();
            PresetToggle("Encounter Picker", preset.ShowEncounterPicker);
            if (preset.SelectionBarHeight > 0)
                PresetRow("Height", $"{preset.SelectionBarHeight:0}px");
            PresetColor("Text Color", preset.SelectionBarTextColor);
            PresetColor("Background", preset.SelectionBarBackgroundColor);
            PresetToggle("Separator", preset.ShowSelectionBarSeparator);
            if (preset.ShowSelectionBarSeparator)
                PresetColor("Separator Color", preset.SelectionBarSeparatorColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Header Row##presetHdr"))
        {
            ImGui.Indent();
            PresetToggle("Show Header", preset.ShowMeterHeader);
            PresetRow("Height", $"{preset.HeaderHeight:0}px");
            PresetRow("Font Size", $"{preset.HeaderFontSize:0.#}pt");
            PresetColor("Text Color", preset.HeaderTextColor);
            PresetColor("Background", preset.HeaderBackgroundColor);
            PresetToggle("Separator", preset.HeaderSeparator);
            if (preset.HeaderSeparator)
                PresetColor("Separator Color", preset.HeaderSeparatorColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Status Bar##presetStatus"))
        {
            ImGui.Indent();
            PresetToggle("Show Status Bar", preset.ShowStatusBar);
            PresetToggle("Timer", preset.ShowStatusBarTimer);
            PresetRow("Height", $"{preset.StatusBarHeight:0}px");
            PresetRow("Font Size", $"{preset.StatusBarFontSize:0.#}pt");
            PresetRow("Padding", $"{preset.StatusBarPadding:0}px");
            PresetColor("Background", preset.StatusBarBackgroundColor);
            PresetColor("Active", preset.StatusBarActiveColor);
            PresetColor("Inactive", preset.StatusBarInactiveColor);
            PresetColor("Labels", preset.StatusBarLabelColor);
            PresetToggle("Separator", preset.ShowStatusBarSeparator);
            if (preset.ShowStatusBarSeparator)
                PresetColor("Separator Color", preset.StatusBarSeparatorColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Font##presetFont"))
        {
            ImGui.Indent();
            PresetToggle("Custom Font", preset.EnableCustomFont);
            if (preset.EnableCustomFont && !string.IsNullOrEmpty(preset.CustomFontDisplayName))
                PresetRow("Font", preset.CustomFontDisplayName);
            if (preset.EnableCustomFont)
                PresetRow("Size", $"{preset.CustomFontSizePt:0.#}pt");
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Skill Breakdown##presetSkill"))
        {
            ImGui.Indent();
            PresetRow("Row Height", $"{preset.SkillRowHeight:0}px");
            PresetRow("Column Padding", $"{preset.SkillColumnPadding:0}px");
            PresetRow("Bar Rounding", $"{preset.SkillBarRounding:0.#}");
            PresetRow("Font Size", $"{preset.SkillFontSize:0.#}pt");
            PresetColor("Damage Fill", preset.SkillDamageFillColor);
            PresetColor("Physical Fill", preset.SkillPhysicalFillColor);
            PresetColor("Magic Fill", preset.SkillMagicFillColor);
            PresetColor("Healing Fill", preset.SkillHealingFillColor);
            PresetColor("Row Background", preset.SkillRowBackgroundColor);
            PresetColor("Text", preset.SkillTextColor);
            PresetColor("Header Text", preset.SkillHeaderTextColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Detail Panel##presetDetail"))
        {
            ImGui.Indent();
            PresetRow("Font Size", $"{preset.DetailFontSize:0.#}pt");
            PresetRow("Indent", $"{preset.DetailIndent:0}px");
            PresetColor("Label Color", preset.DetailLabelColor);
            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Tooltip##presetTooltip"))
        {
            ImGui.Indent();
            PresetToggle("Show Tooltip", preset.ShowTooltip);
            PresetRow("Delay", $"{preset.TooltipDelay:0.##}s");
            PresetRow("Font Size", $"{preset.TooltipFontSize:0.#}pt");
            PresetRow("Rounding", $"{preset.TooltipRounding:0.#}");
            PresetRow("Padding", $"{preset.TooltipPadding:0}px");
            PresetColor("Background", preset.TooltipBackgroundColor);
            PresetColor("Text", preset.TooltipTextColor);
            PresetColor("Labels", preset.TooltipLabelColor);
            ImGui.Unindent();
        }
    }

    private static void PresetRow(string label, string value)
    {
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), label + ":");
        ImGui.SameLine();
        ImGui.Text(value);
    }

    private static void PresetToggle(string label, bool enabled)
    {
        var icon = enabled ? "+" : "-";
        var color = enabled
            ? new Vector4(0.4f, 0.8f, 0.4f, 1f)
            : new Vector4(0.5f, 0.5f, 0.5f, 0.6f);
        ImGui.TextColored(color, icon);
        ImGui.SameLine();
        if (!enabled)
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 0.6f), label);
        else
            ImGui.Text(label);
    }

    private static void PresetColor(string label, Vector4 color)
    {
        ImGui.ColorButton($"##prev_{label}", color, ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new Vector2(12, 12));
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f), label);
    }
}
