namespace DamageTerror.Gui.ConfigWindow;

internal static class MeterTabSectionHelpers
{
    public static bool DrawColumnColorPopup(BarColumn col, Dictionary<BarColumn, Vector4> colors)
    {
        var changed = false;
        var hasColor = colors.TryGetValue(col, out var current);

        if (!hasColor)
        {
            current = new Vector4(1f, 1f, 1f, 1f);
        }

        var useCustom = hasColor;
        if (ImGui.Checkbox("Use custom color", ref useCustom))
        {
            if (useCustom)
            {
                colors[col] = current;
            }
            else
            {
                colors.Remove(col);
            }
            changed = true;
        }

        if (useCustom)
        {
            if (ImGui.ColorEdit4($"##colClr_{col}", ref current, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
            {
                colors[col] = current;
                changed = true;
            }
        }

        return changed;
    }

    public static bool DrawColorButton(BarColumn col, string idSlug, Dictionary<BarColumn, Vector4> colors)
    {
        var hasColor = colors.ContainsKey(col);
        return DrawColumnButtonPopup(col, "C", idSlug, hasColor,
            hasColor ? colors[col] : default,
            "Custom value color (click to edit)", "Set custom value color",
            () => DrawColumnColorPopup(col, colors));
    }

    public static bool DrawColumnButtonPopup(BarColumn col, string buttonLabel, string idSlug, bool highlighted, Vector4 highlightColor, string tooltipOn, string tooltipOff, Func<bool> drawBody)
    {
        var changed = false;

        ImGui.SameLine();
        if (highlighted)
            ImGui.PushStyleColor(ImGuiCol.Text, highlightColor);
        if (ImGui.SmallButton($"{buttonLabel}##{idSlug}_{col}"))
            ImGui.OpenPopup($"##{idSlug}Popup_{col}");
        if (highlighted)
            ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(highlighted ? tooltipOn : tooltipOff);
        if (ImGui.BeginPopup($"##{idSlug}Popup_{col}"))
        {
            changed |= drawBody();
            ImGui.EndPopup();
        }

        return changed;
    }

    public static bool DrawLabelOverride<TKey>(TKey key, string idPrefix, string defaultLabel, Dictionary<TKey, string> labels, string tooltip)
        where TKey : notnull
    {
        var changed = false;

        ImGui.SameLine();
        labels.TryGetValue(key, out var current);
        current ??= "";
        ImGui.SetNextItemWidth(60);
        if (ImGui.InputTextWithHint($"##{idPrefix}{key}", defaultLabel, ref current, 32))
        {
            if (string.IsNullOrEmpty(current))
                labels.Remove(key);
            else
                labels[key] = current;
            changed = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);

        return changed;
    }
}
