using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class ColorsSection
{
    public static bool Draw(Configuration config)
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
            changed |= ConfigHelpers.ColorEditProp("DoH/DoL", config.DoHLColor, v => config.DoHLColor = v);
            changed |= ConfigHelpers.ColorEditProp("Limit Break", config.LimitBreakColor, v => config.LimitBreakColor = v);
            changed |= ConfigHelpers.ColorEditProp("Unknown/Other", config.DefaultJobColor, v => config.DefaultJobColor = v);
        }
        else
        {
            changed |= ConfigHelpers.DrawPerJobColorGroup("Tanks", JobDataTable.TankJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Healers", JobDataTable.HealerJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Melee DPS", JobDataTable.MeleeDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Phys Ranged DPS", JobDataTable.RangedDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Caster DPS", JobDataTable.CasterDpsJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("DoH/DoL", JobDataTable.DoHLJobs, config);
            changed |= ConfigHelpers.DrawPerJobColorGroup("Base Classes", JobDataTable.BaseClassJobs, config);
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
            config.DoHLColor = new Vector4(0.70f, 0.55f, 0.30f, 1.0f);
            config.LimitBreakColor = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            config.DefaultJobColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            config.JobColors.Clear();
            config.UsePerJobColors = false;
            changed = true;
        }
        }

        return changed;
    }
}
