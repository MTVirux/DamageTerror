using Dalamud.Bindings.ImGui;
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

    public AppearanceTab(PresetManager presetManager)
    {
        this.presetManager = presetManager;
    }

    public bool DrawPresetsPage(Configuration config)
    {
        return DrawPresetSection(config);
    }

    public static bool DrawBarsPage(Configuration config) => DrawBarsTab(config);
    public static bool DrawSelectionBarPage(Configuration config) => DrawSelectionBarTab(config);
    public static bool DrawColorsPage(Configuration config) => DrawColorsTab(config);
    public static bool DrawStatusBarPage(Configuration config) => DrawStatusBarTab(config);
    public static bool DrawDetailsPage(Configuration config) => DrawDetailsTab(config);
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

            var hasCustom = allPresets.Any(p => !p.IsBuiltIn);
            if (hasCustom)
            {
                ImGui.Spacing();
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

        if (ImGui.BeginPopup("##savePresetPopup"))
        {
            ImGui.Text("Save current settings as a custom preset:");
            ImGui.Spacing();

            ImGui.SetNextItemWidth(250);
            ImGui.InputText("Name", ref savePresetName, 128);

            ImGui.SetNextItemWidth(250);
            ImGui.InputText("Description", ref savePresetDesc, 256);

            ImGui.Spacing();

            var canSave = !string.IsNullOrWhiteSpace(savePresetName);
            if (!canSave) ImGui.BeginDisabled();
            if (ImGui.Button("Save"))
            {
                var preset = ThemePreset.CreateFromConfig(config, savePresetName.Trim(), savePresetDesc.Trim());
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

        return changed;
    }

    private static bool DrawBarsTab(Configuration config)
    {
        var changed = false;

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

        if (ImGui.CollapsingHeader("Value Formatting", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var formatIdx = (int)config.ValueDisplayFormat;
        var formatLabels = new[] { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Number format", ref formatIdx, formatLabels, formatLabels.Length))
        {
            config.ValueDisplayFormat = (ValueDisplayFormat)formatIdx;
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
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Colors", ImGuiTreeNodeFlags.DefaultOpen))
        {
        changed |= ConfigHelpers.ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

        var windowRounding = config.WindowRounding;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Window rounding", ref windowRounding, 0.0f, 12.0f, "%.1f"))
        {
            config.WindowRounding = windowRounding;
            changed = true;
        }
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

        if (ImGui.Button("Reset Header"))
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

        if (ImGui.Button("Reset Selection Bar"))
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

            if (ImGui.Button("Reset Per-Job Colors"))
            {
                config.JobColors.Clear();
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.Button("Reset All Colors"))
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

        if (ImGui.CollapsingHeader("Visibility##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
        var showStatusBar = config.ShowStatusBar;
        if (ImGui.Checkbox("Show status bar", ref showStatusBar))
        {
            config.ShowStatusBar = showStatusBar;
            changed = true;
        }
        }

        if (config.ShowStatusBar)
        {
        if (ImGui.CollapsingHeader("Options##statusbar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var showTimer = config.ShowStatusBarTimer;
            if (ImGui.Checkbox("Show combat timer", ref showTimer))
            {
                config.ShowStatusBarTimer = showTimer;
                changed = true;
            }

            var showPersonalDps = config.ShowStatusBarPersonalDps;
            if (ImGui.Checkbox("Show personal DPS", ref showPersonalDps))
            {
                config.ShowStatusBarPersonalDps = showPersonalDps;
                changed = true;
            }

            var showRaidDps = config.ShowStatusBarRaidDps;
            if (ImGui.Checkbox("Show raid DPS", ref showRaidDps))
            {
                config.ShowStatusBarRaidDps = showRaidDps;
                changed = true;
            }

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
        }

        return changed;
    }

    private static bool DrawDetailsTab(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("Choose what to show in the expanded detail view.");

            var showDmg = config.DetailShowDamage;
            if (ImGui.Checkbox("Total damage", ref showDmg))
            {
                config.DetailShowDamage = showDmg;
                changed = true;
            }

            var showCrit = config.DetailShowCritDhStats;
            if (ImGui.Checkbox("Crit / DH / CDH stats", ref showCrit))
            {
                config.DetailShowCritDhStats = showCrit;
                changed = true;
            }

            var showDeaths = config.DetailShowDeaths;
            if (ImGui.Checkbox("Deaths", ref showDeaths))
            {
                config.DetailShowDeaths = showDeaths;
                changed = true;
            }

            var showOh = config.DetailShowOverheal;
            if (ImGui.Checkbox("Overheal %", ref showOh))
            {
                config.DetailShowOverheal = showOh;
                changed = true;
            }

            var showMax = config.DetailShowMaxHit;
            if (ImGui.Checkbox("Max hit", ref showMax))
            {
                config.DetailShowMaxHit = showMax;
                changed = true;
            }

            var showTrend = config.DetailShowDpsTrend;
            if (ImGui.Checkbox("DPS trend (10s/30s/60s)", ref showTrend))
            {
                config.DetailShowDpsTrend = showTrend;
                changed = true;
            }

            ImGui.Spacing();

            ImGui.TextDisabled("Skill breakdown");

            var showSkills = config.DetailShowSkillBreakdown;
            if (ImGui.Checkbox("Show skill breakdown", ref showSkills))
            {
                config.DetailShowSkillBreakdown = showSkills;
                changed = true;
            }

            if (config.DetailShowSkillBreakdown)
            {
                var maxSkills = config.MaxSkillBreakdownCount;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("Max skills shown (0 = all)", ref maxSkills, 0, 30))
                {
                    config.MaxSkillBreakdownCount = maxSkills;
                    changed = true;
                }
            }
        }

        ImGui.Spacing();

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
        changed |= ConfigHelpers.ColorEditProp("Label color", config.DetailLabelColor, v => config.DetailLabelColor = v);
        changed |= ConfigHelpers.ColorEditProp("Death highlight", config.DetailDeathColor, v => config.DetailDeathColor = v);

        ImGui.Spacing();

        if (ImGui.Button("Reset Details"))
        {
            config.DetailLabelColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            config.DetailDeathColor = new Vector4(1f, 0.3f, 0.3f, 1f);
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
        changed |= ConfigHelpers.ColorEditProp("Damage fill", config.SkillDamageFillColor, v => config.SkillDamageFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Healing fill", config.SkillHealingFillColor, v => config.SkillHealingFillColor = v);

        ImGui.Spacing();
        var useDmgTypeColors = config.UseSkillDamageTypeColors;
        if (ImGui.Checkbox("Use per-damage-type colors", ref useDmgTypeColors))
        {
            config.UseSkillDamageTypeColors = useDmgTypeColors;
            changed = true;
        }
        if (config.UseSkillDamageTypeColors)
        {
            changed |= ConfigHelpers.ColorEditProp("Physical fill", config.SkillPhysicalFillColor, v => config.SkillPhysicalFillColor = v);
            changed |= ConfigHelpers.ColorEditProp("Magic fill", config.SkillMagicFillColor, v => config.SkillMagicFillColor = v);
        }
        changed |= ConfigHelpers.ColorEditProp("Row background", config.SkillRowBackgroundColor, v => config.SkillRowBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Skill text", config.SkillTextColor, v => config.SkillTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header text", config.SkillHeaderTextColor, v => config.SkillHeaderTextColor = v);

        ImGui.Spacing();

        if (ImGui.Button("Reset Skill Colors"))
        {
            config.SkillDamageFillColor = new Vector4(0.35f, 0.35f, 0.55f, 0.7f);
            config.SkillHealingFillColor = new Vector4(0.25f, 0.50f, 0.30f, 0.7f);
            config.SkillPhysicalFillColor = new Vector4(0.55f, 0.30f, 0.25f, 0.7f);
            config.SkillMagicFillColor = new Vector4(0.30f, 0.30f, 0.65f, 0.7f);
            config.UseSkillDamageTypeColors = false;
            config.SkillRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
            config.SkillTextColor = new Vector4(1f, 1f, 1f, 0.9f);
            config.SkillHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
            changed = true;
        }
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
                catch { }
            }
        }

        ImGui.TextWrapped("When enabled, allows loading a custom system font. Disable if you experience crashes.");

        ImGui.Spacing();
        ImGui.TextDisabled("Font Selection");

        if (!config.EnableCustomFont)
        {
            ImGui.TextDisabled("Enable custom font above to use font selection.");
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

                if (config.CustomFontSpecJson != null && ImGui.Button("Reset to Default"))
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

        ImGui.Spacing();

        if (ImGui.Button("Reset Sizes"))
        {
            config.BarFontSize = 14f;
            config.HeaderFontSize = 14f;
            config.StatusBarFontSize = 14f;
            config.DetailFontSize = 14f;
            config.SkillFontSize = 14f;
            changed = true;
        }
        }

        return changed;
    }
}
