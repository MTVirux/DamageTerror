namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class SkillsTabRenderer : IDetailTabRenderer
{
    private readonly Configuration config;
    private readonly DetailPanelState state;

    public SkillsTabRenderer(Configuration config, DetailPanelState state)
    {
        this.config = config;
        this.state = state;
    }

    public void Render(in DetailRenderContext ctx)
    {
        var combatant = ctx.Combatant;
        var index = ctx.Index;
        var activeTab = ctx.ActiveTab;
        var showBreakdown = activeTab?.DetailShowSkillBreakdown ?? config.DetailShowSkillBreakdown;

        if (showBreakdown && combatant.Skills.Count > 0)
        {
            ImGui.Spacing();
            if (DetailRenderHelpers.PersistentTreeNode(config, "Damage Skills", index.ToString()))
            {
                DrawSkillTable(combatant.Skills, index, "dmg", config.SkillDamageFillColor, activeTab);
                ImGui.TreePop();
            }
        }

        if (showBreakdown && combatant.HealingSkills.Count > 0)
        {
            ImGui.Spacing();
            if (DetailRenderHelpers.PersistentTreeNode(config, "Healing Skills", index.ToString()))
            {
                DrawSkillTable(combatant.HealingSkills, index, "heal", config.SkillHealingFillColor, activeTab);
                ImGui.TreePop();
            }
        }

        if (!showBreakdown || (combatant.Skills.Count == 0 && combatant.HealingSkills.Count == 0))
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No skill data available.");
            ImGui.Spacing();
        }
    }

    private void DrawSkillTable(List<SkillEntry> skills, string index, string idPrefix, Vector4 fillColorVec, MeterTab? activeTab)
    {
        var availWidth = ImGui.GetContentRegionAvail().X;
        var skillBarHeight = config.SkillRowHeight;
        var maxSkillVal = skills[0].TotalDamage;
        var drawList = ImGui.GetWindowDrawList();
        var bgColor = ImGui.ColorConvertFloat4ToU32(config.SkillRowBackgroundColor);
        var fillColor = ImGui.ColorConvertFloat4ToU32(fillColorVec);
        var physFillColor = ImGui.ColorConvertFloat4ToU32(config.SkillPhysicalFillColor);
        var magFillColor = ImGui.ColorConvertFloat4ToU32(config.SkillMagicFillColor);
        var textColor = ImGui.ColorConvertFloat4ToU32(config.SkillTextColor);
        var skillRounding = config.SkillBarRounding;

        using var skillFont = FontScope.Push(config.GetFontScale(config.SkillFontSize));

        var maxCount = activeTab?.MaxSkillBreakdownCount ?? config.MaxSkillBreakdownCount;
        var topSkills = maxCount > 0 ? skills.Take(maxCount).ToList() : skills;
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.SkillHeaderTextColor);
        var colPad = config.SkillColumnPadding;
        var isHeal = idPrefix == "heal";
        var valLabel = isHeal ? "Amount" : "Damage";
        var valTooltip = isHeal ? "Amount healed by the skill" : "Damage dealt by the skill";

        float colValW = ImGui.CalcTextSize(valLabel).X;
        float colPctW = ImGui.CalcTextSize("%").X;
        float colHitsW = ImGui.CalcTextSize("Hits").X;
        float colCritW = ImGui.CalcTextSize("!").X;
        float colDhW = ImGui.CalcTextSize("!!").X;
        float colCdhW = ImGui.CalcTextSize("!!!").X;

        foreach (var s in topSkills)
        {
            colValW = Math.Max(colValW, ImGui.CalcTextSize(ValueFormatter.Format(s.TotalDamage, config)).X);
            colPctW = Math.Max(colPctW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.DamagePercent, config.PercentDecimalPlaces)).X);
            colHitsW = Math.Max(colHitsW, ImGui.CalcTextSize($"x{s.HitCount}").X);
            colCritW = Math.Max(colCritW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.CritPct, config.PercentDecimalPlaces)).X);
            colDhW = Math.Max(colDhW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.DirectHitPct, config.PercentDecimalPlaces)).X);
            colCdhW = Math.Max(colCdhW, ImGui.CalcTextSize(ValueFormatter.FormatPercent(s.CritDirectHitPct, config.PercentDecimalPlaces)).X);
        }

        var textHeight = ImGui.CalcTextSize("X").Y;
        var textYOff = (skillBarHeight - textHeight) * 0.5f;

        ImGui.InvisibleButton($"##{idPrefix}_hdr_{index}", new Vector2(availWidth, skillBarHeight));
        var hdrMin = ImGui.GetItemRectMin();
        var hdrMax = ImGui.GetItemRectMax();
        drawList.AddText(new Vector2(hdrMin.X + 3, hdrMin.Y + textYOff), headerColor, "Skill");

        var mousePos = ImGui.GetMousePos();
        var hdrX = hdrMax.X - 3;
        var hdrY = hdrMin.Y + textYOff;
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colHitsW, colPad, "Hits", headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, "Hit Count");
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colCdhW, colPad, "!!!", headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, "Critical Direct Hit %");
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colDhW, colPad, "!!", headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, "Direct Hit %");
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colCritW, colPad, "!", headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, "Critical Hit %");
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colPctW, colPad, "%", headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, "Damage %");
        TableDrawHelper.DrawHeaderColRTL(drawList, ref hdrX, colValW, colPad, valLabel, headerColor, hdrY, mousePos, hdrMin.Y, hdrMax.Y, valTooltip);

        var skillIdx = 0;
        foreach (var skill in topSkills)
        {
            var barFraction = maxSkillVal > 0 ? (float)skill.TotalDamage / maxSkillVal : 0f;
            var hasSubEntries = skill.SubEntries != null && skill.SubEntries.Count > 0;
            var skillKey = $"{idPrefix}_{index}_{skill.Name}";
            var isExpanded = hasSubEntries && state.ExpandedSkills.Contains(skillKey);

            ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}", new Vector2(availWidth, skillBarHeight));
            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();

            if (hasSubEntries && ImGui.IsItemClicked())
            {
                if (isExpanded)
                    state.ExpandedSkills.Remove(skillKey);
                else
                    state.ExpandedSkills.Add(skillKey);
                isExpanded = !isExpanded;
            }

            drawList.AddRectFilled(min, max, bgColor, skillRounding);
            var barColor = skill.DamageType switch
            {
                SkillDamageType.Physical => physFillColor,
                SkillDamageType.Magic => magFillColor,
                _ => fillColor,
            };
            drawList.AddRectFilled(min, new Vector2(min.X + availWidth * barFraction, max.Y), barColor, skillRounding);

            var nameX = min.X + 3;
            if (hasSubEntries)
            {
                var arrow = isExpanded ? "v " : "> ";
                drawList.AddText(new Vector2(nameX, min.Y + textYOff), textColor, arrow);
                nameX += ImGui.CalcTextSize(arrow).X;
            }
            drawList.AddText(new Vector2(nameX, min.Y + textYOff), textColor, skill.Name);

            var x = max.X - 3;
            var rowY = min.Y + textYOff;
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colHitsW, colPad, $"x{skill.HitCount}", textColor, rowY);
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colCdhW, colPad, ValueFormatter.FormatPercent(skill.CritDirectHitPct, config.PercentDecimalPlaces), textColor, rowY);
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colDhW, colPad, ValueFormatter.FormatPercent(skill.DirectHitPct, config.PercentDecimalPlaces), textColor, rowY);
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colCritW, colPad, ValueFormatter.FormatPercent(skill.CritPct, config.PercentDecimalPlaces), textColor, rowY);
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colPctW, colPad, ValueFormatter.FormatPercent(skill.DamagePercent, config.PercentDecimalPlaces), textColor, rowY);
            TableDrawHelper.DrawCenteredColRTL(drawList, ref x, colValW, colPad, ValueFormatter.Format(skill.TotalDamage, config), textColor, rowY);

            if (isExpanded && skill.SubEntries != null)
            {
                var subIndent = 16f;
                var subAvailWidth = availWidth - subIndent;
                var subAlpha = 0.7f;

                foreach (var sub in skill.SubEntries)
                {
                    var subFraction = skill.TotalDamage > 0 ? (float)sub.TotalDamage / maxSkillVal : 0f;

                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + subIndent);
                    ImGui.InvisibleButton($"##{idPrefix}_{index}_{skillIdx}_sub", new Vector2(subAvailWidth, skillBarHeight));
                    var sMin = ImGui.GetItemRectMin();
                    var sMax = ImGui.GetItemRectMax();

                    drawList.AddRectFilled(sMin, sMax, bgColor, skillRounding);
                    var subBarColor = sub.DamageType switch
                    {
                        SkillDamageType.Physical => physFillColor,
                        SkillDamageType.Magic => magFillColor,
                        _ => fillColor,
                    };
                    var subBarColorVec = ImGui.ColorConvertU32ToFloat4(subBarColor);
                    subBarColorVec.W *= subAlpha;
                    var subBarColorU32 = ImGui.ColorConvertFloat4ToU32(subBarColorVec);
                    drawList.AddRectFilled(sMin, new Vector2(sMin.X + subAvailWidth * subFraction, sMax.Y), subBarColorU32, skillRounding);
                    drawList.AddText(new Vector2(sMin.X + 3, sMin.Y + textYOff), textColor, sub.Name);

                    var sx = sMax.X - 3;
                    var sRowY = sMin.Y + textYOff;
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colHitsW, colPad, $"x{sub.HitCount}", textColor, sRowY);
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colCdhW, colPad, ValueFormatter.FormatPercent(sub.CritDirectHitPct, config.PercentDecimalPlaces), textColor, sRowY);
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colDhW, colPad, ValueFormatter.FormatPercent(sub.DirectHitPct, config.PercentDecimalPlaces), textColor, sRowY);
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colCritW, colPad, ValueFormatter.FormatPercent(sub.CritPct, config.PercentDecimalPlaces), textColor, sRowY);
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colPctW, colPad, ValueFormatter.FormatPercent(sub.DamagePercent, config.PercentDecimalPlaces), textColor, sRowY);
                    TableDrawHelper.DrawCenteredColRTL(drawList, ref sx, colValW, colPad, ValueFormatter.Format(sub.TotalDamage, config), textColor, sRowY);
                }
            }

            skillIdx++;
        }
    }
}
