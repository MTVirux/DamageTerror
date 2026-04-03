using Dalamud.Game.ClientState.Conditions;
using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.MainWindow;

internal static class MeterWindowHelper
{
    public static bool IsDutyTypeEnabled(Configuration config)
    {
        var contentType = Content.ContentType;
        return contentType switch
        {
            ECommons.GameHelpers.ContentType.Dungeon => config.EnableInDungeons,
            ECommons.GameHelpers.ContentType.Trial => config.EnableInTrials,
            ECommons.GameHelpers.ContentType.Raid => config.EnableInRaids,
            ECommons.GameHelpers.ContentType.ARaid => config.EnableInAllianceRaids,
            ECommons.GameHelpers.ContentType.DeepDungeon => config.EnableInDeepDungeons,
            ECommons.GameHelpers.ContentType.FieldOperations => config.EnableInFieldOperations,
            ECommons.GameHelpers.ContentType.FieldRaid => config.EnableInFieldRaids,
            ECommons.GameHelpers.ContentType.Criterion => config.EnableInCriterion,
            ECommons.GameHelpers.ContentType.Variant => config.EnableInVariant,
            ECommons.GameHelpers.ContentType.PVP => config.EnableInPvP,
            ECommons.GameHelpers.ContentType.OverWorld => config.EnableInOverworld,
            _ => config.EnableInOverworld,
        };
    }

    public static bool ShouldDraw(Configuration config, ref DateTime? combatEndTime)
    {
        if (!Svc.ClientState.IsLoggedIn)
            return false;

        if (!IsDutyTypeEnabled(config))
            return false;

        if (!config.HideOutOfCombat)
        {
            combatEndTime = null;
            return true;
        }

        if (Svc.Condition[ConditionFlag.InCombat])
        {
            combatEndTime = null;
            return true;
        }

        combatEndTime ??= DateTime.UtcNow;
        var elapsed = (DateTime.UtcNow - combatEndTime.Value).TotalSeconds;
        return elapsed < config.HideOutOfCombatDelay;
    }

    public static void DrawMeterHeader(Configuration config, MeterTab? activeTab)
    {
        var headerHeight = config.HeaderHeight;
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var cursorPos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var headerColor = ImGui.ColorConvertFloat4ToU32(config.HeaderTextColor);

        var headerBg = config.HeaderBackgroundColor;
        if (headerBg.W > 0f)
        {
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + windowWidth, cursorPos.Y + headerHeight),
                ImGui.ColorConvertFloat4ToU32(headerBg));
        }

        var prevHdrScale = ImGui.GetFont().Scale;
        ImGui.GetFont().Scale = config.GetFontScale(config.HeaderFontSize);
        ImGui.PushFont(ImGui.GetFont());

        var textY = cursorPos.Y + (headerHeight - ImGui.GetTextLineHeight()) * 0.5f;
        var textStartX = cursorPos.X + config.BarLeftPadding;

        if (config.ShowRankNumber)
        {
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "#");
            textStartX += ImGui.CalcTextSize("#. ").X;
        }

        if (config.ShowJobIcons)
            textStartX += config.IconSize + config.IconTextPadding;

        if (config.ShowJobAbbrevOnBar)
        {
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "Job");
            textStartX += ImGui.CalcTextSize("[WHM] ").X;
        }

        if (config.ShowNameOnBar)
            drawList.AddText(new Vector2(textStartX, textY), headerColor, "Name");

        var rightX = cursorPos.X + windowWidth - config.BarRightPadding;
        var colSpacing = config.BarColumnSpacing;

        var columnOrder = activeTab?.ColumnOrder ?? new List<BarColumn>();
        CombatantBarComponent.EnsureColumnOrderComplete(columnOrder);

        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.BarFontSize);
        ImGui.PushFont(ImGui.GetFont());
        var colWidths = new Dictionary<BarColumn, float>();
        foreach (var col in columnOrder)
        {
            if (CombatantBarComponent.ColumnWidthTemplates.TryGetValue(col, out var template))
                colWidths[col] = ImGui.CalcTextSize(template).X;
            else
                colWidths[col] = 0f;
        }
        ImGui.PopFont();
        ImGui.GetFont().Scale = config.GetFontScale(config.HeaderFontSize);
        ImGui.PushFont(ImGui.GetFont());

        for (var ci = columnOrder.Count - 1; ci >= 0; ci--)
        {
            var col = columnOrder[ci];
            if (activeTab == null || !activeTab.IsColumnVisible(col)) continue;

            var headerLabel = activeTab?.GetHeaderLabel(col) ?? Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            var colW = colWidths[col];
            var lw = ImGui.CalcTextSize(headerLabel).X;
            rightX -= colW;
            var textPos = new Vector2(rightX + (colW - lw) * 0.5f, textY);
            drawList.AddText(textPos, headerColor, headerLabel);

            var hitMin = new Vector2(rightX, textY);
            var hitMax = new Vector2(rightX + colW, textY + ImGui.GetFontSize());
            if (ImGui.IsMouseHoveringRect(hitMin, hitMax))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString()));
                ImGui.EndTooltip();
            }

            rightX -= colSpacing;
        }

        ImGui.PopFont();
        ImGui.GetFont().Scale = prevHdrScale;

        if (config.HeaderSeparator)
        {
            var sepY = cursorPos.Y + headerHeight;
            drawList.AddLine(
                new Vector2(cursorPos.X, sepY),
                new Vector2(cursorPos.X + windowWidth, sepY),
                ImGui.ColorConvertFloat4ToU32(config.HeaderSeparatorColor));
        }

        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + headerHeight + config.BarSpacing));
    }
}
