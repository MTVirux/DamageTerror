namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Live editor for the native party list integration. Every value is read by the overlay
/// each frame, so changes show up in the party list as the slider moves.
/// </summary>
internal static class PartyListSection
{
    public static bool Draw(Configuration config, DamageTerrorPlugin plugin)
    {
        var settings = config.PartyList;
        var changed = false;

        var enabled = config.ShowPartyListDps;
        if (ImGui.Checkbox("Enable party list integration", ref enabled))
        {
            config.ShowPartyListDps = enabled;
            plugin.SetPartyListOverlayEnabled(enabled);
            changed = true;
        }

        ConfigHelpers.HelpMarker("Turning this off restores every node the game owns.");

        if (!enabled)
            ImGui.BeginDisabled();

        changed |= ConfigHelpers.CheckboxProp("Hide metrics when out of combat##plHideOoc",
            settings.HideOutOfCombat, v => settings.HideOutOfCombat = v);
        ConfigHelpers.HelpMarker("The bar, name metrics and totals only. The restyle stays.");

        if (settings.HideOutOfCombat)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.SliderFloatProp("##plHideOocDelay",
                settings.HideOutOfCombatDelay, 0f, 30f, "%.1f",
                v => settings.HideOutOfCombatDelay = v, 150);
            ConfigHelpers.HelpMarker("Seconds after combat ends before the metrics are hidden.");
            ImGui.Unindent();
        }

        changed |= ConfigHelpers.CheckboxProp("Override \"Solo\" / \"Party\" label##plHidePartyType",
            settings.HidePartyTypeLabel, v => settings.HidePartyTypeLabel = v);
        ConfigHelpers.HelpMarker("Drops the game's header text and puts the encounter totals or " +
            "the text below in its place. Restored when turned off.");

        if (settings.HidePartyTypeLabel)
        {
            ImGui.Indent();

            if (!settings.ShowEncounterTotals)
                ImGui.BeginDisabled();

            var hiddenText = settings.TotalsHiddenText;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##plTotalsHidden", ref hiddenText, 128))
            {
                settings.TotalsHiddenText = hiddenText;
                changed = true;
            }

            ConfigHelpers.HelpMarker("Shown on the party list header in place of the encounter " +
                "totals, out of combat and between encounters. Leave empty for a blank header.");

            if (!settings.ShowEncounterTotals)
                ImGui.EndDisabled();

            ImGui.Unindent();
        }

        ImGui.Separator();

        if (ImGui.CollapsingHeader("DPS Bar##plBar", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ConfigHelpers.CheckboxProp("Show bar##plShowBar", settings.ShowBar,
                v => settings.ShowBar = v);

            if (settings.ShowBar)
            {
                ImGui.Indent();
                changed |= Slider("Start under icon##plUnderlap", settings.IconUnderlap, -20f, 40f,
                    v => settings.IconUnderlap = v,
                    "How far the bar's left edge tucks back under the job icon.");
                changed |= Slider("Height##plBarHeight", settings.BarHeightPixels, 2f, 512f,
                    v => settings.BarHeightPixels = v, "Bar height in pixels, centred on the job icon.");
                changed |= Slider("Opacity##plBarAlpha", settings.BarOpacity, 0f, 1f,
                    v => settings.BarOpacity = v, null, "%.2f");
                changed |= Slider("Horizontal offset##plBarX", settings.BarOffsetX, -80f, 80f,
                    v => settings.BarOffsetX = v, null);
                changed |= Slider("Vertical offset##plBarY", settings.BarOffsetY, -40f, 40f,
                    v => settings.BarOffsetY = v, null);
                changed |= Slider("Max width##plBarMaxWidth", settings.BarMaxWidth, 0f, 400f,
                    v => settings.BarMaxWidth = v,
                    "Width a 100% bar draws to; 0 uses the whole row. Bars scale inside this, " +
                    "so they stay proportional rather than the longest being clipped.");
                changed |= ConfigHelpers.CheckboxProp("Draw behind row content##plBarBehind",
                    settings.BarBehindRowContent, v => settings.BarBehindRowContent = v);
                ConfigHelpers.HelpMarker(
                    "Puts the fill under the name, gauges and status icons. Off draws it on top.");
                ImGui.Unindent();
            }
        }

        if (settings.ShowBar && ImGui.CollapsingHeader("DPS Bar Colors##plBarColors"))
            changed |= DrawBarColors(config, settings);

        if (ImGui.CollapsingHeader("Name, HP and MP##plRow"))
        {
            changed |= ConfigHelpers.CheckboxProp("Move row content##plShiftRow", settings.ShiftRowContent,
                v => settings.ShiftRowContent = v);

            if (settings.ShiftRowContent)
            {
                ImGui.Indent();
                changed |= Slider("Vertical offset##plRowY", settings.RowContentShiftY, -30f, 30f,
                    v => settings.RowContentShiftY = v,
                    "Negative moves the name, HP and MP up.");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            changed |= ConfigHelpers.CheckboxProp("Hide level##plHideLevel", settings.HideLevel,
                v => settings.HideLevel = v);
            ConfigHelpers.HelpMarker(
                "The level isn't a separate node - the game prefixes it to the name as glyphs, " +
                "so this rewrites the name text. Turning it back off restores the game's string.");

            changed |= ConfigHelpers.CheckboxProp("Resize player name##plAdjustNameFont", settings.AdjustNameFont,
                v => settings.AdjustNameFont = v);

            if (settings.AdjustNameFont)
            {
                ImGui.Indent();
                changed |= SliderInt("Name font size change##plNameFont", settings.NameFontDelta, -8, 8,
                    v => settings.NameFontDelta = v,
                    "Added to the game's own font size for the player name.");
                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("Metrics After Name##plMetrics"))
        {
            changed |= ConfigHelpers.CheckboxProp("DPS##plMetricDps", settings.MetricDps,
                v => settings.MetricDps = v);
            ConfigHelpers.HelpMarker("Drawn after the name, in the order listed. Values and " +
                "formatting match the meter window.");
            changed |= ConfigHelpers.CheckboxProp("Damage##plMetricDamage", settings.MetricDamage,
                v => settings.MetricDamage = v);
            changed |= ConfigHelpers.CheckboxProp("Crit %##plMetricCrit", settings.MetricCrit,
                v => settings.MetricCrit = v);
            changed |= ConfigHelpers.CheckboxProp("Direct Hit %##plMetricDh", settings.MetricDirectHit,
                v => settings.MetricDirectHit = v);
            changed |= ConfigHelpers.CheckboxProp("Crit Direct Hit %##plMetricCdh", settings.MetricCritDirectHit,
                v => settings.MetricCritDirectHit = v);
            changed |= ConfigHelpers.CheckboxProp("Damage %##plMetricDamagePct", settings.MetricDamagePercent,
                v => settings.MetricDamagePercent = v);

            if (settings.AnyNameMetric)
            {
                ImGui.Indent();
                changed |= SliderInt("Font size change##plMetricsFont", settings.MetricsFontDelta, -8, 8,
                    v => settings.MetricsFontDelta = v,
                    "Offset from the name's font, which the metrics otherwise copy exactly.");
                changed |= Slider("Gap after name##plMetricsGap", settings.MetricsGap, -20f, 60f,
                    v => settings.MetricsGap = v,
                    "Measured from where the name's text actually ends.");
                ImGui.Unindent();
            }

            ImGui.Spacing();

            if (ImGui.Button("Reset to name only##plMetricReset"))
            {
                settings.MetricDps = false;
                settings.MetricDamage = false;
                settings.MetricCrit = false;
                settings.MetricDirectHit = false;
                settings.MetricCritDirectHit = false;
                settings.MetricDamagePercent = false;
                plugin.ResyncPartyListNames();
                changed = true;
            }

            ConfigHelpers.HelpMarker(
                "Clears every metric and re-reads each name from the game, dropping anything " +
                "an earlier session left on it.");
        }

        if (ImGui.CollapsingHeader("Buffs and Debuffs##plStatus"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust status icons##plAdjustStatus", settings.AdjustStatusIcons,
                v => settings.AdjustStatusIcons = v);

            if (settings.AdjustStatusIcons)
            {
                ImGui.Indent();
                changed |= Slider("Horizontal offset##plStatusX", settings.StatusOffsetX, -120f, 120f,
                    v => settings.StatusOffsetX = v, null);
                changed |= Slider("Vertical offset##plStatusY", settings.StatusOffsetY, -60f, 60f,
                    v => settings.StatusOffsetY = v, null);
                changed |= Slider("Scale##plStatusScale", settings.StatusScale, 0.3f, 2.5f,
                    v => settings.StatusScale = v,
                    "Scales the whole list from its top-left, so icon spacing scales with the icons.",
                    "%.2f");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            changed |= ConfigHelpers.CheckboxProp("Adjust timers##plAdjustTimers", settings.AdjustStatusTimers,
                v => settings.AdjustStatusTimers = v);
            ConfigHelpers.HelpMarker(
                "The timer text sits inside each icon, so it already scales with the icon. " +
                "These are on top of that.");

            if (settings.AdjustStatusTimers)
            {
                ImGui.Indent();
                changed |= SliderInt("Timer font size change##plTimerFont", settings.StatusTimerFontDelta, -8, 8,
                    v => settings.StatusTimerFontDelta = v, null);
                changed |= Slider("Timer horizontal offset##plTimerX", settings.StatusTimerOffsetX, -30f, 30f,
                    v => settings.StatusTimerOffsetX = v, null);
                changed |= Slider("Timer vertical offset##plTimerY", settings.StatusTimerOffsetY, -30f, 30f,
                    v => settings.StatusTimerOffsetY = v, null);
                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("Hover and Selection Glow##plGlow"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust glows##plAdjustGlow", settings.AdjustSelectionGlow,
                v => settings.AdjustSelectionGlow = v);
            ConfigHelpers.HelpMarker(
                "Each state draws more than one node, and all of them are adjusted together. " +
                "Which settings apply is decided by the row's state, so hover and selection " +
                "stay independent without needing to tell the nodes apart. " +
                "Tint is a colour multiply - the game fades these in on a timeline, and " +
                "writing opacity would pin it and stop the fade.");

            if (settings.AdjustSelectionGlow)
            {
                ImGui.Indent();

                changed |= ConfigHelpers.CheckboxProp("Freeze glow animation##plFreezeGlow", settings.FreezeGlowTransform,
                    v => settings.FreezeGlowTransform = v);
                ConfigHelpers.HelpMarker(
                    "The game animates the glow's position, scale and tint as it appears, which " +
                    "overwrites these settings while it plays. This stops the timeline driving " +
                    "those, leaving the fade animated. Turn it off to keep the pop-in movement, " +
                    "at the cost of the settings below not holding until the animation ends.");

                changed |= ConfigHelpers.CheckboxProp("Selection wins over hover##plGlowPrecedence",
                    settings.SelectionOverridesHover, v => settings.SelectionOverridesHover = v);
                ConfigHelpers.HelpMarker(
                    "The game draws one node for both states - selection is marked by also showing " +
                    "the job icon glow, not by a second highlight - so a row that is hovered and " +
                    "selected can only use one of the two. On, a selected row keeps its selection " +
                    "look while you hover it; off, hovering switches it to the hover look.");

                ImGui.Spacing();
                ImGui.TextDisabled("Hover");
                changed |= Slider("Horizontal offset##plHoverX", settings.HoverOffsetX, -60f, 60f,
                    v => settings.HoverOffsetX = v, null);
                changed |= Slider("Vertical offset##plHoverY", settings.HoverOffsetY, -40f, 40f,
                    v => settings.HoverOffsetY = v, null);
                changed |= Slider("Scale##plHoverScale", settings.HoverScale, 0.3f, 2.5f,
                    v => settings.HoverScale = v, null, "%.2f");
                changed |= ConfigHelpers.ColorEditProp("Tint##plHoverTint", settings.HoverTint,
                    v => settings.HoverTint = v, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);

                ImGui.Spacing();
                ImGui.TextDisabled("Selection");
                changed |= Slider("Horizontal offset##plSelX", settings.SelectionOffsetX, -60f, 60f,
                    v => settings.SelectionOffsetX = v, null);
                changed |= Slider("Vertical offset##plSelY", settings.SelectionOffsetY, -40f, 40f,
                    v => settings.SelectionOffsetY = v, null);
                changed |= Slider("Scale##plSelScale", settings.SelectionScale, 0.3f, 2.5f,
                    v => settings.SelectionScale = v, null, "%.2f");
                changed |= ConfigHelpers.ColorEditProp("Tint##plSelTint", settings.SelectionTint,
                    v => settings.SelectionTint = v, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);

                ImGui.Spacing();
                ImGui.TextDisabled("Job icon glow");
                changed |= Slider("Horizontal offset##plIconGlowX", settings.IconGlowOffsetX, -60f, 60f,
                    v => settings.IconGlowOffsetX = v, null);
                changed |= Slider("Vertical offset##plIconGlowY", settings.IconGlowOffsetY, -40f, 40f,
                    v => settings.IconGlowOffsetY = v, null);
                changed |= Slider("Scale##plIconGlowScale", settings.IconGlowScale, 0.3f, 2.5f,
                    v => settings.IconGlowScale = v, null, "%.2f");
                changed |= ConfigHelpers.ColorEditProp("Tint##plIconGlowTint", settings.IconGlowTint,
                    v => settings.IconGlowTint = v, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);

                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("Encounter Totals##plTotals"))
        {
            changed |= ConfigHelpers.CheckboxProp("Show on party list header##plShowTotals", settings.ShowEncounterTotals,
                v => settings.ShowEncounterTotals = v);
            ConfigHelpers.HelpMarker("Appended to the \"Party\" / \"Light Party\" label above the list.");

            if (settings.ShowEncounterTotals)
            {
                ImGui.Indent();
                changed |= ConfigHelpers.CheckboxProp("Encounter name##plTotalsTitle", settings.TotalsShowTitle,
                    v => settings.TotalsShowTitle = v);
                changed |= ConfigHelpers.CheckboxProp("Duration##plTotalsDuration", settings.TotalsShowDuration,
                    v => settings.TotalsShowDuration = v);
                changed |= ConfigHelpers.CheckboxProp("Raid DPS##plTotalsDps", settings.TotalsShowRaidDps,
                    v => settings.TotalsShowRaidDps = v);
                changed |= ConfigHelpers.CheckboxProp("Total damage##plTotalsDamage", settings.TotalsShowDamage,
                    v => settings.TotalsShowDamage = v);
                changed |= ConfigHelpers.CheckboxProp("Deaths##plTotalsDeaths", settings.TotalsShowDeaths,
                    v => settings.TotalsShowDeaths = v);
                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("Cast Bar##plCastBar"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust cast bar##plAdjustCastBar", settings.AdjustCastBar,
                v => settings.AdjustCastBar = v);

            if (settings.AdjustCastBar)
            {
                ImGui.Indent();
                changed |= Slider("Left inset##plCastBarX", settings.CastBarShiftX, 0f, 80f,
                    v => settings.CastBarShiftX = v,
                    "Moves the left edge right and narrows the bar by the same amount.");
                changed |= Slider("Vertical offset##plCastBarY", settings.CastBarShiftY, -30f, 30f,
                    v => settings.CastBarShiftY = v, null);
                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("Casting Spell Name##plCastName"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust spell name##plAdjustCastName", settings.AdjustCastName,
                v => settings.AdjustCastName = v);

            if (settings.AdjustCastName)
            {
                ImGui.Indent();
                changed |= Slider("Horizontal offset##plCastNameX", settings.CastNameOffsetX, -40f, 40f,
                    v => settings.CastNameOffsetX = v, null);
                changed |= Slider("Vertical offset##plCastNameY", settings.CastNameOffsetY, -30f, 30f,
                    v => settings.CastNameOffsetY = v,
                    "Measured from the cast bar's centre line.");
                changed |= SliderInt("Font size change##plCastNameFont", settings.CastNameFontDelta, -12, 12,
                    v => settings.CastNameFontDelta = v,
                    "Added to the game's own font size for the spell name.");
                ImGui.Unindent();
            }
        }

        if (ImGui.CollapsingHeader("HP / MP Numbers##plGauge"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust numbers##plAdjustGauge", settings.AdjustGaugeNumbers,
                v => settings.AdjustGaugeNumbers = v);

            if (settings.AdjustGaugeNumbers)
            {
                ImGui.Indent();
                changed |= SliderInt("Font size change##plGaugeFont", settings.GaugeFontDelta, -8, 8,
                    v => settings.GaugeFontDelta = v,
                    "Added to the game's own font size for the HP and MP numbers.");
                changed |= Slider("Vertical offset##plGaugeY", settings.GaugeNumberOffsetY, -20f, 20f,
                    v => settings.GaugeNumberOffsetY = v,
                    "Applied to both the HP and MP numbers.");

                ImGui.Spacing();

                changed |= SliderInt("Trailing digits size##plTrailFont", settings.MpTrailingFontDelta, -4, 8,
                    v => settings.MpTrailingFontDelta = v,
                    "MP's last two digits are a second, smaller text node. " +
                    "0 keeps the game's smaller size. Raise it to match the leading digits - " +
                    "they're then re-aligned, since the game's baseline offset only suits the small size.");

                changed |= Slider("Trailing digits X##plTrailX", settings.TrailingDigitsOffsetX, -20f, 20f,
                    v => settings.TrailingDigitsOffsetX = v, null);
                changed |= Slider("Trailing digits Y##plTrailY", settings.TrailingDigitsOffsetY, -20f, 20f,
                    v => settings.TrailingDigitsOffsetY = v, null);
                ImGui.Unindent();
            }
        }

        if (!enabled)
            ImGui.EndDisabled();

        ImGui.Separator();

        if (ImGui.Button("Reset to defaults##plReset"))
        {
            config.PartyList = new PartyListOverlaySettings();
            changed = true;
        }

        return changed;
    }

    private static readonly string[] BarColorModeLabels =
    {
        "Match meter window",
        "Party list palette",
        "Single color",
    };

    /// <summary>
    /// The bar's own colours. Everything here is scoped under one id, so the palette can reuse
    /// the meter's colour widgets without their labels colliding with this window's.
    /// </summary>
    private static bool DrawBarColors(Configuration config, PartyListOverlaySettings settings)
    {
        ImGui.PushID("plBarColors");

        var changed = ConfigHelpers.ComboProp("Color source", (int)settings.BarColorMode,
            BarColorModeLabels, v => settings.BarColorMode = (PartyListBarColorMode)v, 180f);

        ConfigHelpers.HelpMarker(
            "Match meter window: the same job colours the meter draws its bars with, dimmed the " +
            "same way. Party list palette: colours kept for the party list alone. " +
            "Single color: one colour for every row. " +
            "Opacity comes from the bar's own setting, not from these.");

        ImGui.Spacing();

        switch (settings.BarColorMode)
        {
            case PartyListBarColorMode.SingleColor:
                ImGui.Indent();
                changed |= ConfigHelpers.ColorEditProp("Bar color", settings.BarSingleColor,
                    v => settings.BarSingleColor = v,
                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
                ImGui.Unindent();
                break;

            case PartyListBarColorMode.OwnPalette:
                ImGui.Indent();

                if (ImGui.Button("Copy from meter"))
                {
                    settings.BarColors.CopyFrom(config);
                    changed = true;
                }

                ConfigHelpers.HelpMarker("Fills this palette with the meter window's current colours.");

                ImGui.SameLine();

                if (ConfigHelpers.ShiftResetButton("Reset colors"))
                {
                    settings.BarColors = new JobColorPalette();
                    changed = true;
                }

                ImGui.Spacing();
                changed |= ConfigHelpers.DrawJobColorPalette(settings.BarColors);
                ImGui.Unindent();
                break;
        }

        ImGui.PopID();
        return changed;
    }

    private static bool Slider(string label, float value, float min, float max, Action<float> setter,
        string? tooltip, string format = "%.0f")
    {
        var result = ConfigHelpers.SliderFloatProp(label, value, min, max, format, setter, 180f);
        if (tooltip != null)
            ConfigHelpers.HelpMarker(tooltip);
        return result;
    }

    private static bool SliderInt(string label, int value, int min, int max, Action<int> setter, string? tooltip)
    {
        var result = ConfigHelpers.SliderIntProp(label, value, min, max, setter, 180f);
        if (tooltip != null)
            ConfigHelpers.HelpMarker(tooltip);
        return result;
    }
}
