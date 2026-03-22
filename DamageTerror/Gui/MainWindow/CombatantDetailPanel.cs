using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

/// <summary>
/// Renders expanded detail stats for a combatant when their bar is clicked.
/// Shows: Crit%, DirectHit%, CritDH%, Deaths, Overheal%, MaxHit, Last10/30/60 DPS.
/// </summary>
public class CombatantDetailPanel
{
    private readonly Configuration config;
    private int expandedIndex = -1;

    public CombatantDetailPanel(Configuration config)
    {
        this.config = config;
    }

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
        if (config.DetailShowDamage)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Damage:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.Damage:N0}  ({combatant.DamagePercent})");
        }

        // Row 2: Crit/DH stats
        if (config.DetailShowCritDhStats)
        {
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
        }

        // Row 3: Deaths and Overheal
        if (config.DetailShowDeaths)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Deaths:");
            ImGui.SameLine();
            if (combatant.Deaths > 0)
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), combatant.Deaths.ToString());
            else
                ImGui.TextUnformatted("0");
        }

        if (config.DetailShowOverheal && combatant.OverhealPct > 0)
        {
            if (config.DetailShowDeaths) ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "  Overheal:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.OverhealPct:F1}%");
        }

        // Row 4: Max Hit
        if (config.DetailShowMaxHit && !string.IsNullOrEmpty(combatant.MaxHit))
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "Max Hit:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.MaxHit} ({combatant.MaxHitDamage:N0})");
        }

        // Row 5: DPS trend
        if (config.DetailShowDpsTrend && (combatant.Last10Dps > 0 || combatant.Last30Dps > 0 || combatant.Last60Dps > 0))
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "DPS 10s/30s/60s:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{combatant.Last10Dps:F1} / {combatant.Last30Dps:F1} / {combatant.Last60Dps:F1}");
        }

        // Row 6: Skill Breakdown (Damage)
        if (config.DetailShowSkillBreakdown && combatant.Skills.Count > 0)
        {
            ImGui.Spacing();
            if (ImGui.TreeNodeEx($"Damage Skills##{index}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawSkillTable(combatant.Skills, index, "dmg", config.SkillDamageFillColor);
                ImGui.TreePop();
            }
        }

        // Row 7: Skill Breakdown (Healing)
        if (config.DetailShowSkillBreakdown && combatant.HealingSkills.Count > 0)
        {
            ImGui.Spacing();
            if (ImGui.TreeNodeEx($"Healing Skills##{index}", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawSkillTable(combatant.HealingSkills, index, "heal", config.SkillHealingFillColor);
                ImGui.TreePop();
            }
        }

        ImGui.Unindent(8.0f);
        ImGui.Spacing();
    }

    private void DrawSkillTable(List<SkillEntry> skills, int index, string idPrefix, Vector4 fillColorVec)
    {
        var availWidth = ImGui.GetContentRegionAvail().X;
        var skillBarHeight = config.SkillRowHeight;
        var maxSkillVal = skills[0].TotalDamage; // Already sorted descending
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = ImGui.ColorConvertFloat4ToU32(config.SkillRowBackgroundColor);
        var fillColor = ImGui.ColorConvertFloat4ToU32(fillColorVec);
        var textColor = ImGui.ColorConvertFloat4ToU32(config.SkillTextColor);

        var topSkills = config.MaxSkillBreakdownCount > 0 ? skills.Take(config.MaxSkillBreakdownCount).ToList() : skills;
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.SkillHeaderTextColor);
        var colPad = config.SkillColumnPadding;

        // Measure widest text per column across all skills
        float colValW = ImGui.CalcTextSize("Amount").X;
        float colPctW = ImGui.CalcTextSize("%").X;
        float colHitsW = ImGui.CalcTextSize("Hits").X;
        float colCritW = ImGui.CalcTextSize("!").X;
        float colDhW = ImGui.CalcTextSize("!!").X;
        float colCdhW = ImGui.CalcTextSize("!!!").X;

        foreach (var s in topSkills)
        {
            colValW = Math.Max(colValW, ImGui.CalcTextSize($"{s.TotalDamage:N0}").X);
            colPctW = Math.Max(colPctW, ImGui.CalcTextSize($"{s.DamagePercent:F1}%").X);
            colHitsW = Math.Max(colHitsW, ImGui.CalcTextSize($"x{s.HitCount}").X);
            colCritW = Math.Max(colCritW, ImGui.CalcTextSize($"{s.CritPct:F0}%").X);
            colDhW = Math.Max(colDhW, ImGui.CalcTextSize($"{s.DirectHitPct:F0}%").X);
            colCdhW = Math.Max(colCdhW, ImGui.CalcTextSize($"{s.CritDirectHitPct:F0}%").X);
        }

        // Draw header row
        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, skillBarHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y), headerColor, "Skill");

        var hdrX = hdrMax.X - 3;
        hdrX -= colHitsW; drawList.AddText(new Vector2(hdrX + colHitsW - ImGui.CalcTextSize("Hits").X, hdrMin.Y), headerColor, "Hits"); hdrX -= colPad;
        hdrX -= colCdhW; drawList.AddText(new Vector2(hdrX + colCdhW - ImGui.CalcTextSize("!!!").X, hdrMin.Y), headerColor, "!!!"); hdrX -= colPad;
        hdrX -= colDhW; drawList.AddText(new Vector2(hdrX + colDhW - ImGui.CalcTextSize("!!").X, hdrMin.Y), headerColor, "!!"); hdrX -= colPad;
        hdrX -= colCritW; drawList.AddText(new Vector2(hdrX + colCritW - ImGui.CalcTextSize("!").X, hdrMin.Y), headerColor, "!"); hdrX -= colPad;
        hdrX -= colPctW; drawList.AddText(new Vector2(hdrX + colPctW - ImGui.CalcTextSize("%").X, hdrMin.Y), headerColor, "%"); hdrX -= colPad;
        hdrX -= colValW; drawList.AddText(new Vector2(hdrX + colValW - ImGui.CalcTextSize("Amount").X, hdrMin.Y), headerColor, "Amount");

        var skillIdx = 0;
        foreach (var skill in topSkills)
        {
            var barFraction = maxSkillVal > 0 ? (float)skill.TotalDamage / maxSkillVal : 0f;

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}", new Vector2(availWidth, skillBarHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            drawList.AddRectFilled(min, max, bgColor);
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), fillColor);
            drawList.AddText(new Vector2(min.X + 3, min.Y), textColor, skill.Name);

            var x = max.X - 3;
            var hitsText = $"x{skill.HitCount}";
            x -= colHitsW; drawList.AddText(new Vector2(x + colHitsW - ImGui.CalcTextSize(hitsText).X, min.Y), textColor, hitsText); x -= colPad;
            var cdhText = $"{skill.CritDirectHitPct:F0}%";
            x -= colCdhW; drawList.AddText(new Vector2(x + colCdhW - ImGui.CalcTextSize(cdhText).X, min.Y), textColor, cdhText); x -= colPad;
            var dhText = $"{skill.DirectHitPct:F0}%";
            x -= colDhW; drawList.AddText(new Vector2(x + colDhW - ImGui.CalcTextSize(dhText).X, min.Y), textColor, dhText); x -= colPad;
            var critText = $"{skill.CritPct:F0}%";
            x -= colCritW; drawList.AddText(new Vector2(x + colCritW - ImGui.CalcTextSize(critText).X, min.Y), textColor, critText); x -= colPad;
            var pctText = $"{skill.DamagePercent:F1}%";
            x -= colPctW; drawList.AddText(new Vector2(x + colPctW - ImGui.CalcTextSize(pctText).X, min.Y), textColor, pctText); x -= colPad;
            var valText = $"{skill.TotalDamage:N0}";
            x -= colValW; drawList.AddText(new Vector2(x + colValW - ImGui.CalcTextSize(valText).X, min.Y), textColor, valText);

            skillIdx++;
        }
    }

    /// <summary>
    /// Collapse any expanded detail panel.
    /// </summary>
    public void CollapseAll()
    {
        expandedIndex = -1;
    }
}
