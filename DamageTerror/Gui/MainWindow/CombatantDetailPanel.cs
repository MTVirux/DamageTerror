using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Renders expanded detail stats for a combatant when their bar is clicked.
/// Shows: Crit%, DirectHit%, CritDH%, Deaths, Overheal%, MaxHit, Last10/30/60 DPS.
/// </summary>
public class CombatantDetailPanel
{
    private int expandedIndex = -1;

    /// <summary>
    /// Toggle expansion of a combatant's detail panel.
    /// </summary>
    public void Toggle(int index)
    {
        expandedIndex = expandedIndex == index ? -1 : index;
    }

    /// <summary>
    /// Returns true if the given combatant index is currently expanded.
    /// </summary>
    public bool IsExpanded(int index) => expandedIndex == index;

    /// <summary>
    /// Render the detail panel for a combatant if it's expanded.
    /// </summary>
    public void Render(CombatantEntry combatant, int index)
    {
        if (expandedIndex != index)
            return;

        ImGui.Indent(8.0f);

        // Row 1: Damage stats
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Damage:");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{combatant.Damage:N0}  ({combatant.DamagePercent})");

        // Row 2: Crit/DH stats
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Crit:");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{combatant.CritPct:F1}%");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  DH:");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{combatant.DirectHitPct:F1}%");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  CDH:");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{combatant.CritDirectHitPct:F1}%");

        // Row 3: Deaths and Overheal
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Deaths:");
        ImGui.SameLine();
        if (combatant.Deaths > 0)
            ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), combatant.Deaths.ToString());
        else
            ImGui.TextUnformatted("0");

        if (combatant.OverhealPct > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  Overheal:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.OverhealPct:F1}%");
        }

        // Row 4: Max Hit
        if (!string.IsNullOrEmpty(combatant.MaxHit))
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Max Hit:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.MaxHit} ({combatant.MaxHitDamage:N0})");
        }

        // Row 5: DPS trend (Last 10/30/60)
        if (combatant.Last10Dps > 0 || combatant.Last30Dps > 0 || combatant.Last60Dps > 0)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "DPS 10s/30s/60s:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.Last10Dps:F1} / {combatant.Last30Dps:F1} / {combatant.Last60Dps:F1}");
        }

        ImGui.Unindent(8.0f);
        ImGui.Spacing();
    }

    /// <summary>
    /// Collapse any expanded detail panel.
    /// </summary>
    public void CollapseAll()
    {
        expandedIndex = -1;
    }
}
