using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiFileDialog;
using DamageTerror.Enums;
using DamageTerror.Helpers;
using DamageTerror.Services;
using Dalamud.Interface;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class AppearanceTab
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

    public static bool DrawBarsPage(Configuration config) => DrawBarsTab(config);
    public static bool DrawAppearanceGeneralPage(Configuration config) => DrawAppearanceGeneralTab(config);
    public static bool DrawNameFormatPage(Configuration config) => DrawNameFormatTab(config);
    public static bool DrawFormattingPage(Configuration config) => DrawFormattingTab(config);
    public static bool DrawSelectionBarPage(Configuration config) => DrawSelectionBarTab(config);
    public static bool DrawColorsPage(Configuration config) => DrawColorsTab(config);
    public static bool DrawStatusBarPage(Configuration config) => DrawStatusBarTab(config);
    public static bool DrawTooltipPage(Configuration config) => DrawTooltipTab(config);
    public static bool DrawDetailsPage(Configuration config) => DrawDetailsTab(config);
    public static bool DrawGraphViewPage(Configuration config) => DrawGraphViewTab(config);
    public static bool DrawFontPage(Configuration config, FontService? fontService, IUiBuilder? uiBuilder)
        => DrawFontTab(config, fontService, uiBuilder);

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

    private static bool DrawNameFormatTab(Configuration config)
    {
        var changed = false;

        var showName = config.ShowNameOnBar;
        if (ImGui.Checkbox("Show player name on bars", ref showName))
        {
            config.ShowNameOnBar = showName;
            changed = true;
        }
        ConfigHelpers.HelpMarker("These settings apply everywhere player names are displayed.");

        var showYou = config.ShowYouOnBar;
        if (ImGui.Checkbox("Show \"YOU\" instead of character name", ref showYou))
        {
            config.ShowYouOnBar = showYou;
            changed = true;
        }

        ImGui.Spacing();

        var nameFormatLabels = new[]
        {
            "Full Name",
            "First Name Only",
            "Last Name Only",
            "Initials (F. L.)",
            "Job Abbreviation",
            "Job Full Name",
            "Truncated (Name...)",
        };

        var selfFmt = (int)config.SelfNameFormat;
        if (ImGui.Combo("Your name", ref selfFmt, nameFormatLabels, nameFormatLabels.Length))
        {
            config.SelfNameFormat = (NameDisplayFormat)selfFmt;
            changed = true;
        }

        var othersFmt = (int)config.OthersNameFormat;
        if (ImGui.Combo("Others' names", ref othersFmt, nameFormatLabels, nameFormatLabels.Length))
        {
            config.OthersNameFormat = (NameDisplayFormat)othersFmt;
            changed = true;
        }

        if (config.SelfNameFormat == NameDisplayFormat.Truncated
            || config.OthersNameFormat == NameDisplayFormat.Truncated)
        {
            var truncLen = config.NameTruncateLength;
            if (ImGui.SliderInt("Max name length", ref truncLen, 3, 30))
            {
                config.NameTruncateLength = truncLen;
                changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Self name color.");

        var useSelfNameColor = config.UseSelfNameColor;
        if (ImGui.Checkbox("Custom name color for local player", ref useSelfNameColor))
        {
            config.UseSelfNameColor = useSelfNameColor;
            changed = true;
        }

        if (config.UseSelfNameColor)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Self name color", config.SelfNameColor, v => config.SelfNameColor = v);
            ImGui.Unindent();
        }

        return changed;
    }

    private static bool DrawAppearanceGeneralTab(Configuration config)
    {
        var changed = false;

        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

        var padLeft = config.WindowPaddingLeft;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding left", ref padLeft, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingLeft = padLeft;
            changed = true;
        }

        var padRight = config.WindowPaddingRight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding right", ref padRight, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingRight = padRight;
            changed = true;
        }

        var padTop = config.WindowPaddingTop;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding top", ref padTop, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingTop = padTop;
            changed = true;
        }

        var padBottom = config.WindowPaddingBottom;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Padding bottom", ref padBottom, 0.0f, 32.0f, "%.0f"))
        {
            config.WindowPaddingBottom = padBottom;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Background Image", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ConfigHelpers.HelpMarker("Display a custom image behind the meter window.");

            var hasImage = !string.IsNullOrEmpty(config.BackgroundImagePath);
            var pathDisplay = hasImage ? config.BackgroundImagePath! : "(none)";
            ImGui.Text($"Image: {pathDisplay}");

            if (ImGui.Button("Browse..."))
            {
                FileDialogManager.OpenFileDialog(
                    "Select Background Image",
                    "Image files{.png,.jpg,.jpeg,.gif}",
                    (ok, path) =>
                    {
                        if (ok && !string.IsNullOrEmpty(path))
                        {
                            config.BackgroundImagePath = path;
                            DamageTerrorPlugin.Instance.SaveConfig();
                        }
                    });
            }

            if (hasImage)
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    config.BackgroundImagePath = null;
                    changed = true;
                }

                var opacity = config.BackgroundImageOpacity;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Opacity", ref opacity, 0.0f, 1.0f, "%.2f"))
                {
                    config.BackgroundImageOpacity = opacity;
                    changed = true;
                }

                changed |= ConfigHelpers.ColorEditProp("Tint", config.BackgroundImageTint, v => config.BackgroundImageTint = v);

                var scaleIdx = (int)config.BackgroundImageScale;
                var scaleLabels = new[] { "Stretch", "Fit", "Fill", "Tile" };
                ImGui.SetNextItemWidth(200);
                if (ImGui.Combo("Scale mode", ref scaleIdx, scaleLabels, scaleLabels.Length))
                {
                    config.BackgroundImageScale = (BackgroundImageScaleMode)scaleIdx;
                    changed = true;
                }

                // Preview thumbnail
                if (System.IO.File.Exists(config.BackgroundImagePath))
                {
                    var preview = ServiceManager.TextureProvider.GetFromFile(config.BackgroundImagePath);
                    if (preview.TryGetWrap(out var wrap, out _))
                    {
                        ImGui.Spacing();
                        var previewHeight = 80f;
                        var aspect = (float)wrap.Width / wrap.Height;
                        ImGui.Image(wrap.Handle, new Vector2(previewHeight * aspect, previewHeight));
                    }
                }
            }
        }

        return changed;
    }

    private static bool DrawBarsTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Naming", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Spacing();

            var showJob = config.ShowJobAbbrevOnBar;
            if (ImGui.Checkbox("Job abbreviation text", ref showJob))
            {
                config.ShowJobAbbrevOnBar = showJob;
                changed = true;
            }

            var showRank = config.ShowRankNumber;
            if (ImGui.Checkbox("Rank number", ref showRank))
            {
                config.ShowRankNumber = showRank;
                changed = true;
            }

            var showJobIcons = config.ShowJobIcons;
            if (ImGui.Checkbox("Job icons", ref showJobIcons))
            {
                config.ShowJobIcons = showJobIcons;
                changed = true;
            }

            if (config.ShowJobIcons)
            {
                ImGui.SameLine();
                var styleIdx = (int)config.JobIconStyle;
                var styleLabels = new[] { "Framed", "Plain", "Custom" };
                ImGui.SetNextItemWidth(120);
                if (ImGui.Combo("Icon style", ref styleIdx, styleLabels, styleLabels.Length))
                {
                    config.JobIconStyle = (JobIconStyle)styleIdx;
                    changed = true;
                }

                if (config.JobIconStyle == JobIconStyle.Custom)
                {
                    ImGui.Indent();
                    ImGui.TextUnformatted("Custom Icons");
                    ConfigHelpers.HelpMarker("Set a game icon ID per job (0 = default framed).");
                    ImGui.Spacing();

                    foreach (var abbr in JobIconHelper.AllJobAbbreviations.OrderBy(a => a))
                    {
                        config.CustomJobIcons.TryGetValue(abbr, out var curId);
                        var idInt = (int)curId;
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"{abbr.ToUpperInvariant()}##custicon_{abbr}", ref idInt, 0))
                        {
                            if (idInt < 0) idInt = 0;
                            config.CustomJobIcons[abbr] = (uint)idInt;
                            changed = true;
                        }

                        if (idInt > 0)
                        {
                            ImGui.SameLine();
                            var preview = ServiceManager.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup((uint)idInt));
                            if (preview.TryGetWrap(out var wrap, out _))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight()));
                            }
                        }
                    }

                    ImGui.Unindent();
                }
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var barHeight = config.BarHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar height", ref barHeight, 14.0f, 40.0f, "%.0f px"))
        {
            config.BarHeight = barHeight;
            changed = true;
        }

        var barSpacing = config.BarSpacing;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar spacing", ref barSpacing, 0.0f, 8.0f, "%.0f px"))
        {
            config.BarSpacing = barSpacing;
            changed = true;
        }

        var barRounding = config.BarRounding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar rounding", ref barRounding, 0.0f, 12.0f, "%.1f"))
        {
            config.BarRounding = barRounding;
            changed = true;
        }

        var iconSize = config.IconSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Icon size", ref iconSize, 10.0f, 32.0f, "%.0f px"))
        {
            config.IconSize = iconSize;
            changed = true;
        }

        var barAlpha = config.BarAlpha;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar opacity", ref barAlpha, 0.1f, 1.0f, "%.2f"))
        {
            config.BarAlpha = barAlpha;
            changed = true;
        }

        var barFontSize = config.BarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar font size", ref barFontSize, 6f, 40f, "%.1fpt"))
        {
            config.BarFontSize = barFontSize;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Padding", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var barLeftPad = config.BarLeftPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Left padding", ref barLeftPad, 0.0f, 20.0f, "%.0f px"))
        {
            config.BarLeftPadding = barLeftPad;
            changed = true;
        }

        var barRightPad = config.BarRightPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Right padding", ref barRightPad, 0.0f, 20.0f, "%.0f px"))
        {
            config.BarRightPadding = barRightPad;
            changed = true;
        }

        var barColSpacing = config.BarColumnSpacing;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Column spacing", ref barColSpacing, 0.0f, 16.0f, "%.0f px"))
        {
            config.BarColumnSpacing = barColSpacing;
            changed = true;
        }

        var iconTextPad = config.IconTextPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Icon-text padding", ref iconTextPad, 0.0f, 12.0f, "%.0f px"))
        {
            config.IconTextPadding = iconTextPad;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Self Highlighting", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var selfHighlight = config.SelfBarHighlight;
        if (ImGui.Checkbox("Highlight local player bar", ref selfHighlight))
        {
            config.SelfBarHighlight = selfHighlight;
            changed = true;
        }

        if (config.SelfBarHighlight)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Accent color", config.SelfBarHighlightColor, v => config.SelfBarHighlightColor = v);
            ImGui.Unindent();
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Header Row", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var showHeader = config.ShowMeterHeader;
        if (ImGui.Checkbox("Show header row", ref showHeader))
        {
            config.ShowMeterHeader = showHeader;
            changed = true;
        }

        changed |= ConfigHelpers.ColorEditProp("Header text color", config.HeaderTextColor, v => config.HeaderTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header background", config.HeaderBackgroundColor, v => config.HeaderBackgroundColor = v);

        var headerHeight = config.HeaderHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header height", ref headerHeight, 14.0f, 40.0f, "%.0f px"))
        {
            config.HeaderHeight = headerHeight;
            changed = true;
        }

        var headerFontSize = config.HeaderFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header font size", ref headerFontSize, 6f, 40f, "%.1fpt"))
        {
            config.HeaderFontSize = headerFontSize;
            changed = true;
        }

        var headerSep = config.HeaderSeparator;
        if (ImGui.Checkbox("Show separator line##header", ref headerSep))
        {
            config.HeaderSeparator = headerSep;
            changed = true;
        }

        if (config.HeaderSeparator)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Separator color##header", config.HeaderSeparatorColor, v => config.HeaderSeparatorColor = v);
            ImGui.Unindent();
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Header"))
        {
            config.HeaderTextColor = new Vector4(0.7f, 0.7f, 0.7f, 0.9f);
            config.HeaderBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.HeaderHeight = 22.0f;
            config.HeaderFontSize = 14f;
            config.HeaderSeparator = false;
            config.HeaderSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            changed = true;
        }
        }

        return changed;
    }

    private static readonly double[] PreviewSamples = { 0, 42, 500, 1_234, 9_999, 15_000, 150_000, 999_999, 1_500_000, 12_345_678 };
    private static readonly double[] PreviewPctSamples = { 0, 5.3, 12.75, 48.6, 100 };

    private static bool DrawFormattingTab(Configuration config)
    {
        var changed = false;

        var formatIdx = (int)config.ValueDisplayFormat;
        var formatLabels = new[] { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Number format", ref formatIdx, formatLabels, formatLabels.Length))
        {
            config.ValueDisplayFormat = (ValueDisplayFormat)formatIdx;
            changed = true;
        }

        ImGui.Spacing();

        var abbrevDec = config.AbbreviatedDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Abbreviated decimal places", ref abbrevDec, 0, 2))
        {
            config.AbbreviatedDecimalPlaces = abbrevDec;
            changed = true;
        }
        ConfigHelpers.HelpMarker("K / M suffixed values");

        var rawDec = config.RawDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Value decimal places", ref rawDec, 0, 2))
        {
            config.RawDecimalPlaces = rawDec;
            changed = true;
        }
        ConfigHelpers.HelpMarker("Raw / Commas values");

        var pctDec = config.PercentDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Percent decimal places", ref pctDec, 0, 2))
        {
            config.PercentDecimalPlaces = pctDec;
            changed = true;
        }

        if (config.ValueDisplayFormat == ValueDisplayFormat.Abbreviated)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled("Abbreviation Thresholds");

            var kThresh = (float)config.AbbreviatedKThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputFloat("K threshold", ref kThresh, 1000f, 5000f, "%.0f"))
            {
                config.AbbreviatedKThreshold = Math.Max(0, kThresh);
                changed = true;
            }
            ConfigHelpers.HelpMarker("Values >= this show as K");

            var mThresh = (float)config.AbbreviatedMThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputFloat("M threshold", ref mThresh, 100000f, 500000f, "%.0f"))
            {
                config.AbbreviatedMThreshold = Math.Max(0, mThresh);
                changed = true;
            }
            ConfigHelpers.HelpMarker("Values >= this show as M");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Skill Name Abbreviation");

        var skillLen = config.MaxHitSkillNameLength;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Max skill name length", ref skillLen, 0, 30))
        {
            config.MaxHitSkillNameLength = skillLen;
            changed = true;
        }
        ConfigHelpers.HelpMarker("Shorten Max Hit / Max Heal skill names when they exceed this length.\nEach word after the first is replaced by its initial. 0 = disabled.");

        if (config.MaxHitSkillNameLength > 0)
        {
            var truncSkill = config.TruncateSkillNames;
            if (ImGui.Checkbox("Truncate instead of abbreviate", ref truncSkill))
            {
                config.TruncateSkillNames = truncSkill;
                changed = true;
            }

            var preview = ValueFormatter.AbbreviateSkillName("Midare Setsugekka", config.MaxHitSkillNameLength, config.TruncateSkillNames);
            ConfigHelpers.HelpMarker($"e.g. Midare Setsugekka → {preview}");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Preview");

        if (ImGui.BeginTable("##fmtPreview", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Formatted", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var sample in PreviewSamples)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sample:N0}");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ValueFormatter.Format(sample, config));
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Percent Preview");

        if (ImGui.BeginTable("##pctPreview", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Formatted", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var sample in PreviewPctSamples)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sample}%");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ValueFormatter.FormatPercent(sample, config.PercentDecimalPlaces));
            }

            ImGui.EndTable();
        }

        return changed;
    }

    private static bool DrawSelectionBarTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Style", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Text color", config.SelectionBarTextColor, v => config.SelectionBarTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Background color", config.SelectionBarBackgroundColor, v => config.SelectionBarBackgroundColor = v);

        var selBarHeight = config.SelectionBarHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Extra padding", ref selBarHeight, 0.0f, 16.0f, "%.0f px"))
        {
            config.SelectionBarHeight = selBarHeight;
            changed = true;
        }

        var showSelSep = config.ShowSelectionBarSeparator;
        if (ImGui.Checkbox("Show separator line", ref showSelSep))
        {
            config.ShowSelectionBarSeparator = showSelSep;
            changed = true;
        }

        if (config.ShowSelectionBarSeparator)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Separator color", config.SelectionBarSeparatorColor, v => config.SelectionBarSeparatorColor = v);
            ImGui.Unindent();
        }
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Selection Bar"))
        {
            config.SelectionBarTextColor = new Vector4(1f, 1f, 1f, 1f);
            config.SelectionBarBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.SelectionBarHeight = 0.0f;
            config.ShowEncounterPicker = true;
            config.ShowSelectionBarSeparator = true;
            config.SelectionBarSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            changed = true;
        }

        return changed;
    }

    private static bool DrawColorsTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Job / Role Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var usePerJob = config.UsePerJobColors;
        if (ImGui.Checkbox("Use per-job colors", ref usePerJob))
        {
            config.UsePerJobColors = usePerJob;
            changed = true;
        }

        ImGui.Spacing();

        if (!config.UsePerJobColors)
        {
            changed |= ConfigHelpers.ColorEditProp("Tank", config.TankColor, v => config.TankColor = v);
            changed |= ConfigHelpers.ColorEditProp("Healer", config.HealerColor, v => config.HealerColor = v);
            changed |= ConfigHelpers.ColorEditProp("Melee DPS", config.MeleeDpsColor, v => config.MeleeDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Phys Ranged DPS", config.RangedDpsColor, v => config.RangedDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Caster DPS", config.CasterDpsColor, v => config.CasterDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Limit Break", config.LimitBreakColor, v => config.LimitBreakColor = v);
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);
        }
        else
        {
            changed |= ConfigHelpers.DrawPerJobColorGroup("Tanks", JobColorHelper.TankJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Healers", JobColorHelper.HealerJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Melee DPS", JobColorHelper.MeleeDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Phys Ranged DPS", JobColorHelper.RangedDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Caster DPS", JobColorHelper.CasterDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Base Classes", JobColorHelper.BaseClassJobs, config);
            changed |= ConfigHelpers.ColorEditProp("Limit Break", config.LimitBreakColor, v => config.LimitBreakColor = v);
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);

            if (ConfigHelpers.ShiftResetButton("Reset Per-Job Colors"))
            {
                config.JobColors.Clear();
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset All Colors"))
        {
            config.TankColor = new Vector4(0.2f, 0.4f, 0.8f, 1.0f);
            config.HealerColor = new Vector4(0.2f, 0.7f, 0.3f, 1.0f);
            config.MeleeDpsColor = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
            config.RangedDpsColor = new Vector4(0.9f, 0.5f, 0.2f, 1.0f);
            config.CasterDpsColor = new Vector4(0.6f, 0.3f, 0.8f, 1.0f);
            config.LimitBreakColor = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            config.DefaultJobColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            config.JobColors.Clear();
            config.UsePerJobColors = false;
            changed = true;
        }
        }

        return changed;
    }

    private static bool DrawStatusBarTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Options##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showSep = config.ShowStatusBarSeparator;
            if (ImGui.Checkbox("Show separator line", ref showSep))
            {
                config.ShowStatusBarSeparator = showSep;
                changed = true;
            }

            var barHeight = config.StatusBarHeight;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Height##statusbar", ref barHeight, 14f, 40f, "%.0f"))
            {
                config.StatusBarHeight = barHeight;
                changed = true;
            }

            var fontSizeSb = config.StatusBarFontSize;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Font size##statusbar", ref fontSizeSb, 6f, 40f, "%.1fpt"))
            {
                config.StatusBarFontSize = fontSizeSb;
                changed = true;
            }

            var statusPad = config.StatusBarPadding;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Padding##statusbar", ref statusPad, 0f, 20f, "%.0f px"))
            {
                config.StatusBarPadding = statusPad;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var activeColor = config.StatusBarActiveColor;
            if (ImGui.ColorEdit4("In combat##statusbar", ref activeColor))
            {
                config.StatusBarActiveColor = activeColor;
                changed = true;
            }

            var inactiveColor = config.StatusBarInactiveColor;
            if (ImGui.ColorEdit4("Out of combat##statusbar", ref inactiveColor))
            {
                config.StatusBarInactiveColor = inactiveColor;
                changed = true;
            }

            var labelColor = config.StatusBarLabelColor;
            if (ImGui.ColorEdit4("Labels##statusbar", ref labelColor))
            {
                config.StatusBarLabelColor = labelColor;
                changed = true;
            }

            var bgColor = config.StatusBarBackgroundColor;
            if (ImGui.ColorEdit4("Background##statusbar", ref bgColor))
            {
                config.StatusBarBackgroundColor = bgColor;
                changed = true;
            }

            if (config.ShowStatusBarSeparator)
            {
                var sepColor = config.StatusBarSeparatorColor;
                if (ImGui.ColorEdit4("Separator##statusbar", ref sepColor))
                {
                    config.StatusBarSeparatorColor = sepColor;
                    changed = true;
                }
            }
        }

        return changed;
    }

    public static Dictionary<TooltipField, string> TooltipFieldLabels => MetricPicker.TooltipFieldLabels;

    public static (string Name, TooltipField[] Fields)[] DisabledTooltipCategories => MetricPicker.TooltipFieldCategories;

    private static bool DrawTooltipTab(Configuration config)
    {
        var changed = false;

        var showTooltip = config.ShowTooltip;
        if (ImGui.Checkbox("Show tooltip on hover", ref showTooltip))
        {
            config.ShowTooltip = showTooltip;
            changed = true;
        }

        if (!config.ShowTooltip)
        {
            ImGui.BeginDisabled();
        }

        var delay = config.TooltipDelay;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Hover delay", ref delay, 0.0f, 1.0f, "%.2f s"))
        {
            config.TooltipDelay = delay;
            changed = true;
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var fontSize = config.TooltipFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Font size", ref fontSize, 8f, 24f, "%.1f pt"))
            {
                config.TooltipFontSize = fontSize;
                changed = true;
            }

            var rounding = config.TooltipRounding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Rounding", ref rounding, 0f, 12f, "%.1f"))
            {
                config.TooltipRounding = rounding;
                changed = true;
            }

            var padding = config.TooltipPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Padding", ref padding, 0f, 16f, "%.0f px"))
            {
                config.TooltipPadding = padding;
                changed = true;
            }

            ImGui.Spacing();

            if (ConfigHelpers.ColorEditProp("Background", config.TooltipBackgroundColor, v => config.TooltipBackgroundColor = v))
                changed = true;

            if (ConfigHelpers.ColorEditProp("Text Color", config.TooltipTextColor, v => config.TooltipTextColor = v))
                changed = true;

            if (ConfigHelpers.ColorEditProp("Label Color", config.TooltipLabelColor, v => config.TooltipLabelColor = v))
                changed = true;
        }

        if (ImGui.CollapsingHeader("Top Skills"))
        {
            var skillCount = config.TooltipTopSkillCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Skills to show", ref skillCount, 1, 10))
            {
                config.TooltipTopSkillCount = skillCount;
                changed = true;
            }
            ConfigHelpers.HelpMarker("Number of top skills to show when \"Top Damage Skills\" or\n\"Top Healing Skills\" tooltip fields are enabled.");
        }

        if (!config.ShowTooltip)
        {
            ImGui.EndDisabled();
        }

        return changed;
    }

    internal static (string Name, BarColumn[] Columns)[] DetailCategories => MetricPicker.BarColumnCategories;

    private static bool DrawDetailsTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Layout", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var detailIndent = config.DetailIndent;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Indent", ref detailIndent, 0.0f, 24.0f, "%.0f px"))
        {
            config.DetailIndent = detailIndent;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##details", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Background", config.DetailBackgroundColor, v => config.DetailBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Label color", config.DetailLabelColor, v => config.DetailLabelColor = v);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Details"))
        {
            config.DetailBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
            config.DetailLabelColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            config.DetailIndent = 8.0f;
            config.DetailFontSize = 14f;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Breakdown — Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var skillRowHeight = config.SkillRowHeight;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Row height", ref skillRowHeight, 10.0f, 30.0f, "%.0f px"))
        {
            config.SkillRowHeight = skillRowHeight;
            changed = true;
        }

        var skillColPad = config.SkillColumnPadding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Column padding", ref skillColPad, 0.0f, 16.0f, "%.0f px"))
        {
            config.SkillColumnPadding = skillColPad;
            changed = true;
        }

        var skillRounding = config.SkillBarRounding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar rounding##skills", ref skillRounding, 0.0f, 12.0f, "%.1f"))
        {
            config.SkillBarRounding = skillRounding;
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Breakdown — Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Unknown damage fill", config.SkillDamageFillColor, v => config.SkillDamageFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Physical damage fill", config.SkillPhysicalFillColor, v => config.SkillPhysicalFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Magic damage fill", config.SkillMagicFillColor, v => config.SkillMagicFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Healing fill", config.SkillHealingFillColor, v => config.SkillHealingFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Row background", config.SkillRowBackgroundColor, v => config.SkillRowBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Skill text", config.SkillTextColor, v => config.SkillTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header text", config.SkillHeaderTextColor, v => config.SkillHeaderTextColor = v);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Skill Colors"))
        {
            config.SkillDamageFillColor = new Vector4(0.35f, 0.35f, 0.55f, 0.7f);
            config.SkillHealingFillColor = new Vector4(0.25f, 0.50f, 0.30f, 0.7f);
            config.SkillPhysicalFillColor = new Vector4(0.55f, 0.30f, 0.25f, 0.7f);
            config.SkillMagicFillColor = new Vector4(0.30f, 0.30f, 0.65f, 0.7f);
            config.SkillRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
            config.SkillTextColor = new Vector4(1f, 1f, 1f, 0.9f);
            config.SkillHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
            changed = true;
        }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Graph", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var graphHeight = config.GraphHeight;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Graph height", ref graphHeight, 60f, 300f, "%.0f px"))
            {
                config.GraphHeight = graphHeight;
                changed = true;
            }

            var lineThickness = config.GraphLineThickness;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Line thickness", ref lineThickness, 1f, 5f, "%.1f"))
            {
                config.GraphLineThickness = lineThickness;
                changed = true;
            }

            var graphFontSize = config.GraphFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Font size##graph", ref graphFontSize, 6f, 40f, "%.1fpt"))
            {
                config.GraphFontSize = graphFontSize;
                changed = true;
            }

            var smoothing = config.GraphSmoothingWindow;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Smoothing window", ref smoothing, 1f, 30f, "%.0f sec"))
            {
                config.GraphSmoothingWindow = smoothing;
                changed = true;
            }

            var updateInterval = config.GraphUpdateInterval;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Update interval", ref updateInterval, 0.1f, 2f, "%.2f sec"))
            {
                config.GraphUpdateInterval = updateInterval;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Series visibility");

            var showDps = config.GraphShowDps;
            if (ImGui.Checkbox("Show iDPS##graph", ref showDps))
            {
                config.GraphShowDps = showDps;
                changed = true;
            }

            var showHps = config.GraphShowHps;
            if (ImGui.Checkbox("Show iHPS##graph", ref showHps))
            {
                config.GraphShowHps = showHps;
                changed = true;
            }

            var showDtps = config.GraphShowDtps;
            if (ImGui.Checkbox("Show iDTPS##graph", ref showDtps))
            {
                config.GraphShowDtps = showDtps;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Colors");

            changed |= ConfigHelpers.ColorEditProp("iDPS line", config.GraphDpsColor, v => config.GraphDpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("iHPS line", config.GraphHpsColor, v => config.GraphHpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("iDTPS line", config.GraphDtpsColor, v => config.GraphDtpsColor = v);
            changed |= ConfigHelpers.ColorEditProp("Graph background", config.GraphBackgroundColor, v => config.GraphBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphGridColor, v => config.GraphGridColor = v);

            ImGui.Spacing();
            ImGui.TextDisabled("Display Options");

            var graphShowLegend = config.GraphShowLegend;
            if (ImGui.Checkbox("Show legend##graph", ref graphShowLegend))
            {
                config.GraphShowLegend = graphShowLegend;
                changed = true;
            }

            var graphShowGrid = config.GraphShowGrid;
            if (ImGui.Checkbox("Show grid lines##graph", ref graphShowGrid))
            {
                config.GraphShowGrid = graphShowGrid;
                changed = true;
            }

            var graphShowXAxis = config.GraphShowXAxisLabels;
            if (ImGui.Checkbox("Show X axis labels##graph", ref graphShowXAxis))
            {
                config.GraphShowXAxisLabels = graphShowXAxis;
                changed = true;
            }

            var graphShowYAxis = config.GraphShowYAxisLabels;
            if (ImGui.Checkbox("Show Y axis labels##graph", ref graphShowYAxis))
            {
                config.GraphShowYAxisLabels = graphShowYAxis;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Value Labels");

            var showLabels = config.GraphShowLabels;
            if (ImGui.Checkbox("Show value labels##graph", ref showLabels))
            {
                config.GraphShowLabels = showLabels;
                changed = true;
            }

            var labelOffsetX = config.GraphLabelOffsetX;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset X##graph", ref labelOffsetX, -20f, 40f, "%.0f px"))
            {
                config.GraphLabelOffsetX = labelOffsetX;
                changed = true;
            }

            var labelOffsetY = config.GraphLabelOffsetY;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset Y##graph", ref labelOffsetY, -20f, 20f, "%.0f px"))
            {
                config.GraphLabelOffsetY = labelOffsetY;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Axis & Mouse Text");

            var autoScroll = config.GraphAutoScroll;
            if (ImGui.Checkbox("Auto-scroll##graph", ref autoScroll))
            {
                config.GraphAutoScroll = autoScroll;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("During combat, scroll the graph to show only the most recent time window instead of the full encounter.");

            if (config.GraphAutoScroll)
            {
                var scrollWindow = config.GraphAutoScrollWindow;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll window##graph", ref scrollWindow, 15f, 300f, "%.0f sec"))
                {
                    config.GraphAutoScrollWindow = scrollWindow;
                    changed = true;
                }

                var scrollSmooth = config.GraphAutoScrollSmoothing;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll smoothing##graph", ref scrollSmooth, 1f, 30f, "%.1f"))
                {
                    config.GraphAutoScrollSmoothing = scrollSmooth;
                    changed = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("How quickly the graph scrolls to the new position. Higher = snappier, lower = smoother.");
            }

            var xPadding = config.GraphXAxisPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("X axis padding##graph", ref xPadding, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphXAxisPadding = xPadding;
                changed = true;
            }

            var yHeadroom = config.GraphYAxisHeadroom;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Y axis headroom##graph", ref yHeadroom, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphYAxisHeadroom = yHeadroom;
                changed = true;
            }

            var yTickCount = config.GraphYAxisTickCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Y axis tick count##graph", ref yTickCount, 2, 16))
            {
                config.GraphYAxisTickCount = yTickCount;
                changed = true;
            }

            var mouseOpacity = config.GraphMouseTextOpacity;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Mouse text opacity##graph", ref mouseOpacity, 0f, 1f, "%.2f"))
            {
                config.GraphMouseTextOpacity = mouseOpacity;
                changed = true;
            }

            ImGui.Spacing();

            if (ConfigHelpers.ShiftResetButton("Reset Graph"))
            {
                config.GraphHeight = 120f;
                config.GraphLineThickness = 2f;
                config.GraphDpsColor = new Vector4(0.9f, 0.4f, 0.4f, 1f);
                config.GraphHpsColor = new Vector4(0.4f, 0.85f, 0.4f, 1f);
                config.GraphDtpsColor = new Vector4(0.4f, 0.55f, 0.9f, 1f);
                config.GraphBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
                config.GraphGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
                config.GraphShowLegend = true;
                config.GraphShowGrid = true;
                config.GraphShowXAxisLabels = true;
                config.GraphShowYAxisLabels = true;
                config.GraphShowDps = true;
                config.GraphShowHps = true;
                config.GraphSmoothingWindow = 5f;
                config.GraphUpdateInterval = 0.25f;
                config.GraphShowDtps = true;
                config.GraphShowLabels = true;
                config.GraphLabelOffsetX = 8f;
                config.GraphLabelOffsetY = 0f;
                config.GraphAutoScroll = false;
                config.GraphAutoScrollWindow = 60f;
                config.GraphAutoScrollSmoothing = 8f;
                config.GraphXAxisPadding = 1.25f;
                config.GraphYAxisHeadroom = 1.1f;
                config.GraphYAxisTickCount = 8;
                config.GraphMouseTextOpacity = 0.6f;
                config.GraphFontSize = 14f;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Markers##details", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dps", "DPS Markers", config.DetailDpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_hps", "HPS Markers", config.DetailHpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("dt_dtps", "DTPS Markers", config.DetailDtpsMarkers);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Buffs / Debuffs — Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var buffRowHeight = config.BuffRowHeight;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Row height##buffs", ref buffRowHeight, 10.0f, 30.0f, "%.0f px"))
            {
                config.BuffRowHeight = buffRowHeight;
                changed = true;
            }

            var buffColPad = config.BuffColumnPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Column padding##buffs", ref buffColPad, 0.0f, 16.0f, "%.0f px"))
            {
                config.BuffColumnPadding = buffColPad;
                changed = true;
            }

            var buffRounding = config.BuffBarRounding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Bar rounding##buffs", ref buffRounding, 0.0f, 12.0f, "%.1f"))
            {
                config.BuffBarRounding = buffRounding;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Buffs / Debuffs — Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.ColorEditProp("Buff fill", config.BuffFillColor, v => config.BuffFillColor = v);
            changed |= ConfigHelpers.ColorEditProp("Debuff fill", config.DebuffFillColor, v => config.DebuffFillColor = v);
            changed |= ConfigHelpers.ColorEditProp("Row background##buffs", config.BuffRowBackgroundColor, v => config.BuffRowBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Text##buffs", config.BuffTextColor, v => config.BuffTextColor = v);
            changed |= ConfigHelpers.ColorEditProp("Header text##buffs", config.BuffHeaderTextColor, v => config.BuffHeaderTextColor = v);

            ImGui.Spacing();

            if (ConfigHelpers.ShiftResetButton("Reset Buff Colors"))
            {
                config.BuffFillColor = new Vector4(0.30f, 0.50f, 0.60f, 0.7f);
                config.DebuffFillColor = new Vector4(0.60f, 0.30f, 0.30f, 0.7f);
                config.BuffRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
                config.BuffTextColor = new Vector4(1f, 1f, 1f, 0.9f);
                config.BuffHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
                changed = true;
            }
        }

        return changed;
    }

    private static bool DrawGraphViewTab(Configuration config)
    {
        var changed = false;

        ImGui.TextWrapped("Configure the appearance of the graph view mode. Each tab can be switched to graph view in the Tabs settings page.");
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Dimensions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var autoViewHeight = config.GraphViewAutoHeight;
            if (ImGui.Checkbox("Auto-fit height##graphview", ref autoViewHeight))
            {
                config.GraphViewAutoHeight = autoViewHeight;
                changed = true;
            }

            if (!config.GraphViewAutoHeight)
            {
                var height = config.GraphViewHeight;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Graph height", ref height, 100f, 600f, "%.0f px"))
                {
                    config.GraphViewHeight = height;
                    changed = true;
                }
            }

            var lineThickness = config.GraphViewLineThickness;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Line thickness", ref lineThickness, 1f, 6f, "%.1f"))
            {
                config.GraphViewLineThickness = lineThickness;
                changed = true;
            }

            var gvFontSize = config.GraphViewFontSize;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Font size##graphview", ref gvFontSize, 6f, 40f, "%.1fpt"))
            {
                config.GraphViewFontSize = gvFontSize;
                changed = true;
            }

            var gvSmoothing = config.GraphViewSmoothingWindow;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Smoothing window##graphview", ref gvSmoothing, 1f, 30f, "%.0f sec"))
            {
                config.GraphViewSmoothingWindow = gvSmoothing;
                changed = true;
            }

            var gvUpdateInterval = config.GraphViewUpdateInterval;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Update interval##graphview", ref gvUpdateInterval, 0.1f, 2f, "%.2f sec"))
            {
                config.GraphViewUpdateInterval = gvUpdateInterval;
                changed = true;
            }

        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Display Options", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showLegend = config.GraphViewShowLegend;
            if (ImGui.Checkbox("Show legend", ref showLegend))
            {
                config.GraphViewShowLegend = showLegend;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Show a legend below the graph with combatant names and current values.");

            var showGrid = config.GraphViewShowGrid;
            if (ImGui.Checkbox("Show grid lines", ref showGrid))
            {
                config.GraphViewShowGrid = showGrid;
                changed = true;
            }

            var showXAxis = config.GraphViewShowXAxisLabels;
            if (ImGui.Checkbox("Show X axis labels", ref showXAxis))
            {
                config.GraphViewShowXAxisLabels = showXAxis;
                changed = true;
            }

            var showYAxis = config.GraphViewShowYAxisLabels;
            if (ImGui.Checkbox("Show Y axis labels", ref showYAxis))
            {
                config.GraphViewShowYAxisLabels = showYAxis;
                changed = true;
            }

            var highlightSelf = config.GraphViewHighlightSelf;
            if (ImGui.Checkbox("Highlight self (thicker line)", ref highlightSelf))
            {
                config.GraphViewHighlightSelf = highlightSelf;
                changed = true;
            }

            if (config.GraphViewHighlightSelf)
            {
                var selfThickness = config.GraphViewSelfLineThickness;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Self line thickness", ref selfThickness, 1f, 8f, "%.1f"))
                {
                    config.GraphViewSelfLineThickness = selfThickness;
                    changed = true;
                }
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.ColorEditProp("Background", config.GraphViewBackgroundColor, v => config.GraphViewBackgroundColor = v);
            changed |= ConfigHelpers.ColorEditProp("Grid lines", config.GraphViewGridColor, v => config.GraphViewGridColor = v);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Value Labels##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showViewLabels = config.GraphViewShowLabels;
            if (ImGui.Checkbox("Show value labels##graphview", ref showViewLabels))
            {
                config.GraphViewShowLabels = showViewLabels;
                changed = true;
            }

            var labelOffsetX = config.GraphViewLabelOffsetX;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset X##graphview", ref labelOffsetX, -20f, 40f, "%.0f px"))
            {
                config.GraphViewLabelOffsetX = labelOffsetX;
                changed = true;
            }

            var labelOffsetY = config.GraphViewLabelOffsetY;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Label offset Y##graphview", ref labelOffsetY, -20f, 20f, "%.0f px"))
            {
                config.GraphViewLabelOffsetY = labelOffsetY;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Axis & Mouse Text##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var gvAutoScroll = config.GraphViewAutoScroll;
            if (ImGui.Checkbox("Auto-scroll##graphview", ref gvAutoScroll))
            {
                config.GraphViewAutoScroll = gvAutoScroll;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("During combat, scroll the graph to show only the most recent time window instead of the full encounter.");

            if (config.GraphViewAutoScroll)
            {
                var gvScrollWindow = config.GraphViewAutoScrollWindow;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll window##graphview", ref gvScrollWindow, 15f, 300f, "%.0f sec"))
                {
                    config.GraphViewAutoScrollWindow = gvScrollWindow;
                    changed = true;
                }

                var gvScrollSmooth = config.GraphViewAutoScrollSmoothing;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("Scroll smoothing##graphview", ref gvScrollSmooth, 1f, 30f, "%.1f"))
                {
                    config.GraphViewAutoScrollSmoothing = gvScrollSmooth;
                    changed = true;
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("How quickly the graph scrolls to the new position. Higher = snappier, lower = smoother.");
            }

            var gvXPadding = config.GraphViewXAxisPadding;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("X axis padding##graphview", ref gvXPadding, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphViewXAxisPadding = gvXPadding;
                changed = true;
            }

            var gvYHeadroom = config.GraphViewYAxisHeadroom;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Y axis headroom##graphview", ref gvYHeadroom, 1.0f, 2.0f, "%.2fx"))
            {
                config.GraphViewYAxisHeadroom = gvYHeadroom;
                changed = true;
            }

            var gvYTickCount = config.GraphViewYAxisTickCount;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Y axis tick count##graphview", ref gvYTickCount, 2, 16))
            {
                config.GraphViewYAxisTickCount = gvYTickCount;
                changed = true;
            }

            var gvMouseOpacity = config.GraphViewMouseTextOpacity;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Mouse text opacity##graphview", ref gvMouseOpacity, 0f, 1f, "%.2f"))
            {
                config.GraphViewMouseTextOpacity = gvMouseOpacity;
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Skill Markers##graphview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dps", "DPS Markers", config.GraphViewDpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_hps", "HPS Markers", config.GraphViewHpsMarkers);
            ImGui.Spacing();
            changed |= ConfigHelpers.DrawSkillMarkerSection("gv_dtps", "DTPS Markers", config.GraphViewDtpsMarkers);
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Graph View"))
        {
            config.GraphViewAutoHeight = true;
            config.GraphViewHeight = 300f;
            config.GraphViewLineThickness = 2f;
            config.GraphViewSmoothingWindow = 5f;
            config.GraphViewUpdateInterval = 0.25f;
            config.GraphViewBackgroundColor = new Vector4(0.08f, 0.08f, 0.08f, 0.6f);
            config.GraphViewGridColor = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
            config.GraphViewShowLegend = true;
            config.GraphViewShowGrid = true;
            config.GraphViewShowXAxisLabels = true;
            config.GraphViewShowYAxisLabels = true;
            config.GraphViewHighlightSelf = true;
            config.GraphViewSelfLineThickness = 3.5f;
            config.GraphViewShowLabels = true;
            config.GraphViewLabelOffsetX = 8f;
            config.GraphViewLabelOffsetY = 0f;
            config.GraphViewFontSize = 14f;
            config.GraphViewAutoScroll = false;
            config.GraphViewAutoScrollWindow = 60f;
            config.GraphViewAutoScrollSmoothing = 8f;
            config.GraphViewXAxisPadding = 1.25f;
            config.GraphViewYAxisHeadroom = 1.1f;
            config.GraphViewYAxisTickCount = 8;
            config.GraphViewMouseTextOpacity = 0.6f;
            config.GraphViewDpsMarkers = new SkillMarkerConfig();
            config.GraphViewHpsMarkers = new SkillMarkerConfig();
            config.GraphViewDtpsMarkers = new SkillMarkerConfig();
            changed = true;
        }

        return changed;
    }

    private static bool DrawFontTab(Configuration config, FontService? fontService, IUiBuilder? uiBuilder)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Font Selection", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var enableFont = config.EnableCustomFont;
        if (ImGui.Checkbox("Enable custom font", ref enableFont))
        {
            config.EnableCustomFont = enableFont;
            changed = true;
            if (enableFont && fontService != null && uiBuilder != null && !fontService.IsInitialized)
            {
                try { fontService.Initialize(uiBuilder); }
                catch (Exception ex) { ServiceManager.PluginLog.Error($"Failed to initialize font service: {ex.Message}"); }
            }
        }

        ImGui.TextWrapped("When enabled, allows loading a custom system font. Disable if you experience crashes.");

        ImGui.Spacing();
        ImGui.TextDisabled("Font Selection");

        if (!config.EnableCustomFont)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Choose Font...");
            ConfigHelpers.HelpMarker("Enable custom font above to use font selection.");
            ImGui.EndDisabled();
        }
        else
        {
            var fontName = config.CustomFontDisplayName ?? "Dalamud Default";
            ImGui.Text($"Current: {fontName}");

            if (config.CustomFontSpecJson != null)
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"({config.CustomFontSizePt:F0}pt)");
            }

            if (fontService != null)
            {
                if (ImGui.Button("Choose Font..."))
                {
                    fontService.OpenFontChooser();
                }

                ImGui.SameLine();

                if (config.CustomFontSpecJson != null && ConfigHelpers.ShiftResetButton("Reset to Default"))
                {
                    fontService.ClearCustomFont();
                    changed = true;
                }

                if (fontService.DrawFontChooser())
                    changed = true;
            }
        }

        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Font Sizes", ImGuiTreeNodeFlags.DefaultOpen))
        {
        ImGui.TextWrapped("Set the font size for each component independently.");

        var barFont = config.BarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar text", ref barFont, 6f, 40f, "%.1fpt"))
        {
            config.BarFontSize = barFont;
            changed = true;
        }

        var headerFont = config.HeaderFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header text", ref headerFont, 6f, 40f, "%.1fpt"))
        {
            config.HeaderFontSize = headerFont;
            changed = true;
        }

        var statusFont = config.StatusBarFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Status bar text", ref statusFont, 6f, 40f, "%.1fpt"))
        {
            config.StatusBarFontSize = statusFont;
            changed = true;
        }

        var detailFont = config.DetailFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Detail panel text", ref detailFont, 6f, 40f, "%.1fpt"))
        {
            config.DetailFontSize = detailFont;
            changed = true;
        }

        var skillFont = config.SkillFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Skill breakdown text", ref skillFont, 6f, 40f, "%.1fpt"))
        {
            config.SkillFontSize = skillFont;
            changed = true;
        }

        var buffFont = config.BuffFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Buff/debuff text", ref buffFont, 6f, 40f, "%.1fpt"))
        {
            config.BuffFontSize = buffFont;
            changed = true;
        }

        var graphFont = config.GraphFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Graph labels (detail)", ref graphFont, 6f, 40f, "%.1fpt"))
        {
            config.GraphFontSize = graphFont;
            changed = true;
        }

        var graphViewFont = config.GraphViewFontSize;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Graph labels (overview)", ref graphViewFont, 6f, 40f, "%.1fpt"))
        {
            config.GraphViewFontSize = graphViewFont;
            changed = true;
        }

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset Sizes"))
        {
            config.BarFontSize = 14f;
            config.HeaderFontSize = 14f;
            config.StatusBarFontSize = 14f;
            config.DetailFontSize = 14f;
            config.SkillFontSize = 14f;
            config.BuffFontSize = 14f;
            config.GraphFontSize = 14f;
            config.GraphViewFontSize = 14f;
            changed = true;
        }
        }

        return changed;
    }
}
