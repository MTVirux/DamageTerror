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

    public bool Draw(Configuration config, FontService? fontService = null, IUiBuilder? uiBuilder = null)
    {
        var changed = false;

        changed |= DrawPresetSection(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("##appearanceTabs"))
        {
            if (ImGui.BeginTabItem("Bars"))
            {
                changed |= DrawBarsTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Selection Bar"))
            {
                changed |= DrawSelectionBarTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Header"))
            {
                changed |= DrawHeaderTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Colors"))
            {
                changed |= DrawColorsTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Status Bar"))
            {
                changed |= DrawStatusBarTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Skills"))
            {
                changed |= DrawSkillsTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Details"))
            {
                changed |= DrawDetailsTab(config);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Font"))
            {
                changed |= DrawFontTab(config, fontService, uiBuilder);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        return changed;
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
                selectedPresetIndex = -1; // reset selection since list changed
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

        ImGui.Spacing();

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

        var barFontScale = config.BarFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar font scale", ref barFontScale, 0.5f, 2.0f, "%.2f"))
        {
            config.BarFontScale = barFontScale;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Padding");

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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Value Formatting");

        var formatIdx = (int)config.ValueDisplayFormat;
        var formatLabels = new[] { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Number format", ref formatIdx, formatLabels, formatLabels.Length))
        {
            config.ValueDisplayFormat = (ValueDisplayFormat)formatIdx;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Self Highlighting");

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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

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

        return changed;
    }

    private static bool DrawSelectionBarTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();

        var hideWhenPinned = config.HideSelectionBarWhenPinned;
        if (ImGui.Checkbox("Hide when window is pinned", ref hideWhenPinned))
        {
            config.HideSelectionBarWhenPinned = hideWhenPinned;
            changed = true;
        }

        if (config.HideSelectionBarWhenPinned)
        {
            ImGui.Indent();
            var showOnCtrlShift = config.SelectionBarShowOnCtrlShift;
            if (ImGui.Checkbox("Show if Ctrl + Shift is held", ref showOnCtrlShift))
            {
                config.SelectionBarShowOnCtrlShift = showOnCtrlShift;
                changed = true;
            }
            ImGui.Unindent();
        }

        ImGui.Spacing();

        var showSelBar = config.ShowSelectionBar;
        if (ImGui.Checkbox("Show selection bar", ref showSelBar))
        {
            config.ShowSelectionBar = showSelBar;
            changed = true;
        }

        var showEncPicker = config.ShowEncounterPicker;
        if (ImGui.Checkbox("Show encounter picker", ref showEncPicker))
        {
            config.ShowEncounterPicker = showEncPicker;
            changed = true;
        }

        var showSortDd = config.ShowSortDropdown;
        if (ImGui.Checkbox("Show sort dropdown", ref showSortDd))
        {
            config.ShowSortDropdown = showSortDd;
            changed = true;
        }

        ImGui.Spacing();
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

        ImGui.Spacing();

        if (ImGui.Button("Reset Selection Bar"))
        {
            config.ShowSelectionBar = true;
            config.SelectionBarTextColor = new Vector4(1f, 1f, 1f, 1f);
            config.SelectionBarBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.SelectionBarHeight = 0.0f;
            config.ShowEncounterPicker = true;
            config.ShowSortDropdown = true;
            config.ShowSelectionBarSeparator = true;
            config.SelectionBarSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            config.HideSelectionBarWhenPinned = false;
            config.SelectionBarShowOnCtrlShift = true;
            changed = true;
        }

        return changed;
    }

    private static bool DrawHeaderTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();

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

        var headerFontScale = config.HeaderFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header font scale", ref headerFontScale, 0.5f, 2.0f, "%.2f"))
        {
            config.HeaderFontScale = headerFontScale;
            changed = true;
        }

        var headerSep = config.HeaderSeparator;
        if (ImGui.Checkbox("Show separator line", ref headerSep))
        {
            config.HeaderSeparator = headerSep;
            changed = true;
        }

        if (config.HeaderSeparator)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Separator color", config.HeaderSeparatorColor, v => config.HeaderSeparatorColor = v);
            ImGui.Unindent();
        }

        ImGui.Spacing();

        if (ImGui.Button("Reset Header"))
        {
            config.HeaderTextColor = new Vector4(0.7f, 0.7f, 0.7f, 0.9f);
            config.HeaderBackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            config.HeaderHeight = 22.0f;
            config.HeaderFontScale = 1.0f;
            config.HeaderSeparator = false;
            config.HeaderSeparatorColor = new Vector4(0.4f, 0.4f, 0.4f, 0.5f);
            changed = true;
        }

        return changed;
    }

    private static bool DrawColorsTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();

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
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);
        }
        else
        {
            changed |= ConfigHelpers.DrawPerJobColorGroup("Tanks", new[] { "Pld", "War", "Drk", "Gnb" }, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Healers", new[] { "Whm", "Sch", "Ast", "Sge" }, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Melee DPS", new[] { "Mnk", "Drg", "Nin", "Sam", "Rpr", "Vpr" }, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Phys Ranged DPS", new[] { "Brd", "Mch", "Dnc" }, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Caster DPS", new[] { "Blm", "Smn", "Rdm", "Pct", "Blu" }, config);
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
            config.DefaultJobColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            config.JobColors.Clear();
            config.UsePerJobColors = false;
            changed = true;
        }

        return changed;
    }

    private static bool DrawStatusBarTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();

        var showStatusBar = config.ShowStatusBar;
        if (ImGui.Checkbox("Show status bar", ref showStatusBar))
        {
            config.ShowStatusBar = showStatusBar;
            changed = true;
        }

        if (config.ShowStatusBar)
        {
            var above = config.StatusBarAbove;
            if (ImGui.Checkbox("Position above bars (uncheck for below)", ref above))
            {
                config.StatusBarAbove = above;
                changed = true;
            }

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

            var fontScale = config.StatusBarFontScale;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Font size##statusbar", ref fontScale, 0.5f, 2.0f, "%.2f"))
            {
                config.StatusBarFontScale = fontScale;
                changed = true;
            }

            var statusPad = config.StatusBarPadding;
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Padding##statusbar", ref statusPad, 0f, 20f, "%.0f px"))
            {
                config.StatusBarPadding = statusPad;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Colors");

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

    private static bool DrawSkillsTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();
        ImGui.TextDisabled("Appearance");

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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

        changed |= ConfigHelpers.ColorEditProp("Damage fill", config.SkillDamageFillColor, v => config.SkillDamageFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Healing fill", config.SkillHealingFillColor, v => config.SkillHealingFillColor = v);
        changed |= ConfigHelpers.ColorEditProp("Row background", config.SkillRowBackgroundColor, v => config.SkillRowBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Skill text", config.SkillTextColor, v => config.SkillTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Header text", config.SkillHeaderTextColor, v => config.SkillHeaderTextColor = v);

        ImGui.Spacing();

        if (ImGui.Button("Reset Skill Colors"))
        {
            config.SkillDamageFillColor = new Vector4(0.35f, 0.35f, 0.55f, 0.7f);
            config.SkillHealingFillColor = new Vector4(0.25f, 0.50f, 0.30f, 0.7f);
            config.SkillRowBackgroundColor = new Vector4(0.12f, 0.12f, 0.12f, 0.6f);
            config.SkillTextColor = new Vector4(1f, 1f, 1f, 0.9f);
            config.SkillHeaderTextColor = new Vector4(0.6f, 0.6f, 0.6f, 0.9f);
            changed = true;
        }

        return changed;
    }

    private static bool DrawDetailsTab(Configuration config)
    {
        var changed = false;

        ImGui.Spacing();
        ImGui.TextDisabled("Layout");

        var detailIndent = config.DetailIndent;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Indent", ref detailIndent, 0.0f, 24.0f, "%.0f px"))
        {
            config.DetailIndent = detailIndent;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

        changed |= ConfigHelpers.ColorEditProp("Label color", config.DetailLabelColor, v => config.DetailLabelColor = v);
        changed |= ConfigHelpers.ColorEditProp("Death highlight", config.DetailDeathColor, v => config.DetailDeathColor = v);

        ImGui.Spacing();

        if (ImGui.Button("Reset Details"))
        {
            config.DetailLabelColor = new Vector4(0.7f, 0.7f, 0.7f, 1f);
            config.DetailDeathColor = new Vector4(1f, 0.3f, 0.3f, 1f);
            config.DetailIndent = 8.0f;
            config.DetailFontScale = 1.0f;
            changed = true;
        }

        return changed;
    }

    private static bool DrawFontTab(Configuration config, FontService? fontService, IUiBuilder? uiBuilder)
    {
        var changed = false;

        ImGui.Spacing();
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

            if (config.CustomFontPath != null)
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

                if (config.CustomFontPath != null && ImGui.Button("Reset to Default"))
                {
                    fontService.ClearCustomFont();
                    changed = true;
                }

                // Draw the chooser dialog (if open)
                if (fontService.DrawFontChooser())
                    changed = true;
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Scale");
        ImGui.TextWrapped("Master scale applied to all text. Individual scales below are multiplied by this value.");

        var globalScale = config.GlobalFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Global font scale", ref globalScale, 0.5f, 3.0f, "%.2f"))
        {
            config.GlobalFontScale = globalScale;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Per-Component Scales");

        var barFont = config.BarFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Bar text", ref barFont, 0.5f, 2.0f, "%.2f"))
        {
            config.BarFontScale = barFont;
            changed = true;
        }

        var headerFont = config.HeaderFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Header text", ref headerFont, 0.5f, 2.0f, "%.2f"))
        {
            config.HeaderFontScale = headerFont;
            changed = true;
        }

        var statusFont = config.StatusBarFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Status bar text", ref statusFont, 0.5f, 2.0f, "%.2f"))
        {
            config.StatusBarFontScale = statusFont;
            changed = true;
        }

        var detailFont = config.DetailFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Detail panel text", ref detailFont, 0.5f, 2.0f, "%.2f"))
        {
            config.DetailFontScale = detailFont;
            changed = true;
        }

        var skillFont = config.SkillFontScale;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Skill breakdown text", ref skillFont, 0.5f, 2.0f, "%.2f"))
        {
            config.SkillFontScale = skillFont;
            changed = true;
        }

        ImGui.Spacing();

        if (ImGui.Button("Reset Scales"))
        {
            config.GlobalFontScale = 1.0f;
            config.BarFontScale = 1.0f;
            config.HeaderFontScale = 1.0f;
            config.StatusBarFontScale = 1.0f;
            config.DetailFontScale = 1.0f;
            config.SkillFontScale = 1.0f;
            changed = true;
        }

        return changed;
    }
}
