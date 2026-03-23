using Dalamud.Bindings.ImGui;
using DamageTerror.Helpers;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Appearance tab: visual styling grouped into child tabs.
/// Bars, Selection Bar, Header, Colors, Status Bar, Skills.
/// </summary>
public class AppearanceTab
{
    public bool Draw(Configuration config)
    {
        var changed = false;

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

            ImGui.EndTabBar();
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

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Colors");

        changed |= ConfigHelpers.ColorEditProp("Name text", config.NameTextColor, v => config.NameTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Value text", config.ValueTextColor, v => config.ValueTextColor = v);
        changed |= ConfigHelpers.ColorEditProp("Bar background", config.BarBackgroundColor, v => config.BarBackgroundColor = v);
        changed |= ConfigHelpers.ColorEditProp("Window background", config.WindowBackgroundColor, v => config.WindowBackgroundColor = v);

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
}
