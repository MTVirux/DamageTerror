namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Live editor for the native party list integration. Every value is read by the overlay
/// each frame, so changes show up in the party list as the slider moves.
/// <para>
/// Inside each collapsible header the settings are bundled per element - the name, the HP
/// bar, the timers - and each element is split into the same sections: where it sits, how
/// big it is, what colour it is.
/// </para>
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

        changed |= ConfigHelpers.CheckboxProp("Tint text outline##plTintOutline",
            settings.TintTextOutline, v => settings.TintTextOutline = v);
        ConfigHelpers.HelpMarker("The glow around each glyph follows whatever colour the text " +
            "is given below. Off leaves the game's own outline, which stays dark under a " +
            "recoloured name.");

        if (settings.TintTextOutline)
        {
            ImGui.Indent();
            changed |= Slider("Outline darkness##plOutlineDarkness", settings.TextOutlineDarkness,
                0f, 1f, v => settings.TextOutlineDarkness = v,
                "0 matches the text exactly, which reads as thicker glyphs rather than an edge; " +
                "1 is black.", "%.2f");
            ImGui.Unindent();
        }

        ImGui.Separator();

        changed |= DrawBarHeader(config, settings);
        changed |= DrawNameHeader(settings);
        changed |= DrawGaugeHeader(settings);
        changed |= DrawMetricsHeader(settings, plugin);
        changed |= DrawStatusHeader(settings);
        changed |= DrawGlowHeader(settings);
        changed |= DrawTotalsHeader(settings);
        changed |= DrawCastBarHeader(settings);
        changed |= DrawCastNameHeader(settings);

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

    private static bool DrawBarHeader(Configuration config, PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("DPS Bar##plBar", ImGuiTreeNodeFlags.DefaultOpen))
            return false;

        var changed = ConfigHelpers.CheckboxProp("Show bar##plShowBar", settings.ShowBar,
            v => settings.ShowBar = v);

        if (!settings.ShowBar)
            return changed;

        ImGui.Indent();

        Section("Position");
        changed |= Slider("Start under icon##plUnderlap", settings.IconUnderlap, -20f, 40f,
            v => settings.IconUnderlap = v,
            "How far the bar's left edge tucks back under the job icon.");
        changed |= Slider("Horizontal offset##plBarX", settings.BarOffsetX, -80f, 80f,
            v => settings.BarOffsetX = v, null);
        changed |= Slider("Vertical offset##plBarY", settings.BarOffsetY, -40f, 40f,
            v => settings.BarOffsetY = v, null);
        changed |= ConfigHelpers.CheckboxProp("Draw behind row content##plBarBehind",
            settings.BarBehindRowContent, v => settings.BarBehindRowContent = v);
        ConfigHelpers.HelpMarker(
            "Puts the fill under the name, gauges and status icons. Off draws it on top.");
        EndSection();

        Section("Size");
        changed |= Slider("Height##plBarHeight", settings.BarHeightPixels, 2f, 512f,
            v => settings.BarHeightPixels = v, "Bar height in pixels, centred on the job icon.");
        changed |= Slider("Max width##plBarMaxWidth", settings.BarMaxWidth, 0f, 400f,
            v => settings.BarMaxWidth = v,
            "Width a 100% bar draws to; 0 uses the whole row. Bars scale inside this, " +
            "so they stay proportional rather than the longest being clipped.");
        EndSection();

        Section("Color");
        changed |= Slider("Opacity##plBarAlpha", settings.BarOpacity, 0f, 1f,
            v => settings.BarOpacity = v, null, "%.2f");

        if (ImGui.CollapsingHeader("Colors##plBarColors"))
            changed |= DrawBarColors(config, settings);

        EndSection();

        ImGui.Unindent();
        return changed;
    }

    private static bool DrawNameHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Name##plName"))
            return false;

        var changed = false;
        var name = settings.NameShift;

        Group("Player name");

        Section("Text");
        changed |= ConfigHelpers.CheckboxProp("Hide level##plHideLevel", settings.HideLevel,
            v => settings.HideLevel = v);
        ConfigHelpers.HelpMarker(
            "The level isn't a separate node - the game prefixes it to the name as glyphs, " +
            "so this rewrites the name text. Turning it back off restores the game's string.");
        EndSection();

        Section("Position");
        changed |= ConfigHelpers.CheckboxProp("Move name##plShiftName", name.Enabled,
            v => name.Enabled = v);

        if (name.Enabled)
        {
            ImGui.Indent();
            changed |= Slider("Horizontal offset##plNameX", name.OffsetX, -80f, 80f,
                v => name.OffsetX = v, null);
            changed |= Slider("Vertical offset##plNameY", name.OffsetY, -30f, 30f,
                v => name.OffsetY = v,
                "Negative moves the player name up. The metrics after it follow.");
            ImGui.Unindent();
        }

        EndSection();

        Section("Size");
        changed |= ConfigHelpers.CheckboxProp("Resize player name##plAdjustNameFont",
            settings.AdjustNameFont, v => settings.AdjustNameFont = v);
        ConfigHelpers.HelpMarker("A text node's size is its font, so the name is sized here rather than scaled.");

        if (settings.AdjustNameFont)
        {
            ImGui.Indent();
            changed |= SliderInt("Name font size change##plNameFont", settings.NameFontDelta, -8, 8,
                v => settings.NameFontDelta = v,
                "Added to the game's own font size for the player name.");
            ImGui.Unindent();
        }

        EndSection();

        Section("Color");
        changed |= DrawCustomColor("plName", "name", name.UseCustomColor, name.Color,
            v => name.UseCustomColor = v, v => name.Color = v,
            "Off leaves the colour the game gives the row.");
        EndSection();

        EndGroup();

        Group("Slot number");
        changed |= ConfigHelpers.CheckboxProp("Override index##plAdjustIndex", settings.AdjustPartyIndex,
            v => settings.AdjustPartyIndex = v);
        ConfigHelpers.HelpMarker(
            "The party slot number drawn before the name. Off, it takes the name's size " +
            "change, move and colour, so the two stay on one line and match. On, it uses " +
            "the values below instead and the name no longer carries it.");

        if (settings.AdjustPartyIndex)
        {
            Section("Position");
            changed |= Slider("Index horizontal offset##plIndexX", settings.PartyIndexOffsetX, -40f, 40f,
                v => settings.PartyIndexOffsetX = v, null);
            changed |= Slider("Index vertical offset##plIndexY", settings.PartyIndexOffsetY, -30f, 30f,
                v => settings.PartyIndexOffsetY = v, null);
            EndSection();

            Section("Size");
            changed |= SliderInt("Index font size change##plIndexFont", settings.PartyIndexFontDelta, -8, 8,
                v => settings.PartyIndexFontDelta = v,
                "Added to the game's own font size for the slot number.");
            EndSection();

            Section("Color");
            changed |= DrawCustomColor("plIndex", "slot number",
                settings.PartyIndexUseCustomColor, settings.PartyIndexColor,
                v => settings.PartyIndexUseCustomColor = v, v => settings.PartyIndexColor = v,
                "Off leaves the colour the game gives the slot number.");
            EndSection();
        }

        EndGroup();
        return changed;
    }

    private static bool DrawGaugeHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("HP and MP##plGauge"))
            return false;

        Group("HP bar");
        var changed = DrawGaugeBar("plShiftHpBar", "HP bar", settings.HpBarShift,
            "Moves the HP bar only - its number is moved below.");
        EndGroup();

        Group("HP numbers");
        changed |= DrawGaugeNumbers("plHpNumbers", "HP number", settings.HpNumbers);
        EndGroup();

        Group("MP bar");
        changed |= DrawGaugeBar("plShiftMpBar", "MP bar", settings.MpBarShift,
            "Moves the MP bar only - its number is moved below.");
        EndGroup();

        Group("MP numbers");
        changed |= DrawGaugeNumbers("plMpNumbers", "MP number", settings.MpNumbers);

        if (settings.MpNumbers.Enabled)
        {
            Section("Trailing digits");
            changed |= SliderInt("Trailing digits size##plTrailFont", settings.MpTrailingFontDelta, -4, 8,
                v => settings.MpTrailingFontDelta = v,
                "MP's last two digits are a second, smaller text node. " +
                "0 keeps the game's smaller size. Raise it to match the leading digits - " +
                "they're then re-aligned, since the game's baseline offset only suits the small size.");
            changed |= Slider("Trailing digits X##plTrailX", settings.TrailingDigitsOffsetX, -20f, 20f,
                v => settings.TrailingDigitsOffsetX = v, null);
            changed |= Slider("Trailing digits Y##plTrailY", settings.TrailingDigitsOffsetY, -20f, 20f,
                v => settings.TrailingDigitsOffsetY = v, null);
            EndSection();
        }

        EndGroup();
        return changed;
    }

    private static bool DrawMetricsHeader(PartyListOverlaySettings settings, DamageTerrorPlugin plugin)
    {
        if (!ImGui.CollapsingHeader("Metrics After Name##plMetrics"))
            return false;

        var changed = false;

        foreach (var metric in PartyListOverlaySettings.MetricOrder)
            changed |= DrawNameMetric(settings, metric);

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

        return changed;
    }

    private static bool DrawStatusHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Buffs and Debuffs##plStatus"))
            return false;

        Group("Status icons");
        var changed = ConfigHelpers.CheckboxProp("Adjust status icons##plAdjustStatus",
            settings.AdjustStatusIcons, v => settings.AdjustStatusIcons = v);

        if (settings.AdjustStatusIcons)
        {
            Section("Position");
            changed |= Slider("Horizontal offset##plStatusX", settings.StatusOffsetX, -120f, 120f,
                v => settings.StatusOffsetX = v, null);
            changed |= Slider("Vertical offset##plStatusY", settings.StatusOffsetY, -60f, 60f,
                v => settings.StatusOffsetY = v, null);
            EndSection();

            Section("Size");
            changed |= Slider("Scale##plStatusScale", settings.StatusScale, 0.3f, 2.5f,
                v => settings.StatusScale = v,
                "Scales the whole list from its top-left, so icon spacing scales with the icons.",
                "%.2f");
            EndSection();

            Section("Color");
            changed |= DrawTint("plStatusTint", "status icon", settings.StatusTint,
                v => settings.StatusTint = v);
            EndSection();
        }

        EndGroup();

        Group("Timers");
        changed |= ConfigHelpers.CheckboxProp("Adjust timers##plAdjustTimers",
            settings.AdjustStatusTimers, v => settings.AdjustStatusTimers = v);
        ConfigHelpers.HelpMarker(
            "The timer text sits inside each icon, so it already scales with the icon. " +
            "These are on top of that.");

        if (settings.AdjustStatusTimers)
        {
            Section("Position");
            changed |= Slider("Timer horizontal offset##plTimerX", settings.StatusTimerOffsetX, -30f, 30f,
                v => settings.StatusTimerOffsetX = v, null);
            changed |= Slider("Timer vertical offset##plTimerY", settings.StatusTimerOffsetY, -30f, 30f,
                v => settings.StatusTimerOffsetY = v, null);
            EndSection();

            Section("Size");
            changed |= SliderInt("Timer font size change##plTimerFont", settings.StatusTimerFontDelta, -8, 8,
                v => settings.StatusTimerFontDelta = v, null);
            EndSection();

            Section("Color");
            changed |= DrawCustomColor("plTimer", "timer",
                settings.StatusTimerUseCustomColor, settings.StatusTimerColor,
                v => settings.StatusTimerUseCustomColor = v, v => settings.StatusTimerColor = v,
                "Off leaves the game's own colour, which turns as the status runs out.");
            EndSection();
        }

        EndGroup();
        return changed;
    }

    private static bool DrawGlowHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Hover and Selection Glow##plGlow"))
            return false;

        var changed = ConfigHelpers.CheckboxProp("Adjust glows##plAdjustGlow",
            settings.AdjustSelectionGlow, v => settings.AdjustSelectionGlow = v);
        ConfigHelpers.HelpMarker(
            "Each state draws more than one node, and all of them are adjusted together. " +
            "Which settings apply is decided by the row's state, so hover and selection " +
            "stay independent without needing to tell the nodes apart. " +
            "Tint is a colour multiply - the game fades these in on a timeline, and " +
            "writing opacity would pin it and stop the fade.");

        if (!settings.AdjustSelectionGlow)
            return changed;

        Group("Animation");
        changed |= ConfigHelpers.CheckboxProp("Freeze glow animation##plFreezeGlow",
            settings.FreezeGlowTransform, v => settings.FreezeGlowTransform = v);
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
        EndGroup();

        Group("Hover");
        changed |= DrawGlow("plHover", "Hover", settings.HoverOffsetX, settings.HoverOffsetY,
            settings.HoverScale, settings.HoverTint,
            v => settings.HoverOffsetX = v, v => settings.HoverOffsetY = v,
            v => settings.HoverScale = v, v => settings.HoverTint = v);
        EndGroup();

        Group("Selection");
        changed |= DrawGlow("plSel", "Selection", settings.SelectionOffsetX, settings.SelectionOffsetY,
            settings.SelectionScale, settings.SelectionTint,
            v => settings.SelectionOffsetX = v, v => settings.SelectionOffsetY = v,
            v => settings.SelectionScale = v, v => settings.SelectionTint = v);
        EndGroup();

        Group("Job icon glow");
        changed |= DrawGlow("plIconGlow", "Job icon glow", settings.IconGlowOffsetX, settings.IconGlowOffsetY,
            settings.IconGlowScale, settings.IconGlowTint,
            v => settings.IconGlowOffsetX = v, v => settings.IconGlowOffsetY = v,
            v => settings.IconGlowScale = v, v => settings.IconGlowTint = v);
        EndGroup();

        return changed;
    }

    private static bool DrawTotalsHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Encounter Totals##plTotals"))
            return false;

        Group("Contents");
        var changed = ConfigHelpers.CheckboxProp("Show on party list header##plShowTotals",
            settings.ShowEncounterTotals, v => settings.ShowEncounterTotals = v);
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

        EndGroup();

        Group("Header text");
        changed |= ConfigHelpers.CheckboxProp("Adjust header text##plAdjustTotals", settings.AdjustTotalsText,
            v => settings.AdjustTotalsText = v);
        ConfigHelpers.HelpMarker(
            "The header's own text node, whether the totals are written to it or not.");

        if (settings.AdjustTotalsText)
        {
            Section("Position");
            changed |= Slider("Horizontal offset##plTotalsX", settings.TotalsOffsetX, -200f, 200f,
                v => settings.TotalsOffsetX = v, null);
            changed |= Slider("Vertical offset##plTotalsY", settings.TotalsOffsetY, -60f, 60f,
                v => settings.TotalsOffsetY = v, null);
            EndSection();

            Section("Size");
            changed |= SliderInt("Font size change##plTotalsFont", settings.TotalsFontDelta, -8, 12,
                v => settings.TotalsFontDelta = v,
                "Added to the game's own font size for the header.");
            EndSection();

            Section("Color");
            changed |= DrawCustomColor("plTotals", "header text",
                settings.TotalsUseCustomColor, settings.TotalsColor,
                v => settings.TotalsUseCustomColor = v, v => settings.TotalsColor = v,
                "Off leaves the colour the game gives the header.");
            EndSection();
        }

        EndGroup();
        return changed;
    }

    private static bool DrawCastBarHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Cast Bar##plCastBar"))
            return false;

        var changed = ConfigHelpers.CheckboxProp("Adjust cast bar##plAdjustCastBar",
            settings.AdjustCastBar, v => settings.AdjustCastBar = v);

        if (!settings.AdjustCastBar)
            return changed;

        ImGui.Indent();

        Section("Position");
        changed |= Slider("Left inset##plCastBarX", settings.CastBarShiftX, 0f, 80f,
            v => settings.CastBarShiftX = v,
            "Moves the left edge right and narrows the bar by the same amount.");
        changed |= Slider("Vertical offset##plCastBarY", settings.CastBarShiftY, -30f, 30f,
            v => settings.CastBarShiftY = v, null);
        EndSection();

        Section("Size");
        changed |= Slider("Height##plCastBarScaleY", settings.CastBarScaleY, 0.3f, 3f,
            v => settings.CastBarScaleY = v,
            "Grown from the bar's top edge, so the vertical offset still lands where it says.",
            "%.2f");
        EndSection();

        Section("Color");
        changed |= DrawTint("plCastBarTint", "cast bar", settings.CastBarTint,
            v => settings.CastBarTint = v);
        EndSection();

        ImGui.Unindent();
        return changed;
    }

    private static bool DrawCastNameHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Casting Spell Name##plCastName"))
            return false;

        var changed = ConfigHelpers.CheckboxProp("Adjust spell name##plAdjustCastName",
            settings.AdjustCastName, v => settings.AdjustCastName = v);

        if (!settings.AdjustCastName)
            return changed;

        ImGui.Indent();

        Section("Position");
        changed |= Slider("Horizontal offset##plCastNameX", settings.CastNameOffsetX, -40f, 40f,
            v => settings.CastNameOffsetX = v, null);
        changed |= Slider("Vertical offset##plCastNameY", settings.CastNameOffsetY, -30f, 30f,
            v => settings.CastNameOffsetY = v,
            "Measured from the cast bar's centre line.");
        EndSection();

        Section("Size");
        changed |= SliderInt("Font size change##plCastNameFont", settings.CastNameFontDelta, -12, 12,
            v => settings.CastNameFontDelta = v,
            "Added to the game's own font size for the spell name.");
        EndSection();

        Section("Color");
        changed |= DrawCustomColor("plCastName", "spell name",
            settings.CastNameUseCustomColor, settings.CastNameColor,
            v => settings.CastNameUseCustomColor = v, v => settings.CastNameColor = v,
            "Off leaves the colour the game gives the spell name.");
        EndSection();

        ImGui.Unindent();
        return changed;
    }

    /// <summary>Heading colour for an element group, dim enough not to fight the headers.</summary>
    private static readonly Vector4 GroupColor = new(0.55f, 0.75f, 1f, 1f);

    /// <summary>
    /// One element inside a collapsible header - the name, the HP bar, the timers. Opens an
    /// indented block that <see cref="EndGroup"/> closes.
    /// </summary>
    private static void Group(string label)
    {
        ImGui.Spacing();
        ImGui.TextColored(GroupColor, label);
        ImGui.Separator();
        ImGui.Indent();
    }

    private static void EndGroup() => ImGui.Unindent();

    /// <summary>
    /// One aspect of an element - where it sits, how big it is, what colour it is. Opens an
    /// indented block that <see cref="EndSection"/> closes.
    /// </summary>
    private static void Section(string label)
    {
        ImGui.Spacing();
        ImGui.TextDisabled(label);
        ImGui.Indent();
    }

    private static void EndSection() => ImGui.Unindent();

    private static readonly string[] BarColorModeLabels =
    {
        "Match meter window",
        "Party list palette",
        "Single color",
    };

    /// <summary>
    /// One gauge bar - its move toggle with the offsets and scale under it, then its tint.
    /// Colour sits outside the toggle, so a bar can be recoloured where the game already
    /// puts it.
    /// </summary>
    private static bool DrawGaugeBar(string id, string part, RowPartStyle style, string tooltip)
    {
        var changed = ConfigHelpers.CheckboxProp($"Adjust {part}##{id}", style.Enabled,
            v => style.Enabled = v);

        if (style.Enabled)
        {
            Section("Position");
            changed |= Slider($"Horizontal offset##{id}X", style.OffsetX, -80f, 80f,
                v => style.OffsetX = v, null);
            changed |= Slider($"Vertical offset##{id}Y", style.OffsetY, -30f, 30f,
                v => style.OffsetY = v, tooltip);
            EndSection();

            Section("Size");
            changed |= Slider($"Scale##{id}Scale", style.Scale, 0.3f, 2.5f,
                v => style.Scale = v, null, "%.2f");
            EndSection();
        }

        Section("Color");
        changed |= DrawCustomColor($"{id}Color", part, style.UseCustomColor, style.Color,
            v => style.UseCustomColor = v, v => style.Color = v,
            "Off leaves the game's own artwork. On tints it - a texture can be shaded, not repainted.");
        EndSection();

        return changed;
    }

    /// <summary>One gauge's numbers - the text nodes inside the HP or MP bar.</summary>
    private static bool DrawGaugeNumbers(string id, string part, GaugeNumberStyle style)
    {
        var changed = ConfigHelpers.CheckboxProp($"Adjust {part}s##{id}", style.Enabled,
            v => style.Enabled = v);

        if (!style.Enabled)
            return changed;

        Section("Position");
        changed |= Slider($"Horizontal offset##{id}X", style.OffsetX, -60f, 60f,
            v => style.OffsetX = v, null);
        changed |= Slider($"Vertical offset##{id}Y", style.OffsetY, -30f, 30f,
            v => style.OffsetY = v, null);
        EndSection();

        Section("Size");
        changed |= SliderInt($"Font size change##{id}Font", style.FontDelta, -8, 8,
            v => style.FontDelta = v, "Added to the game's own font size for these numbers.");
        EndSection();

        Section("Color");
        changed |= DrawCustomColor($"{id}Color", part, style.UseCustomColor, style.Color,
            v => style.UseCustomColor = v, v => style.Color = v,
            "Off keeps the game's own colour, which reddens as the gauge empties.");
        EndSection();

        return changed;
    }

    /// <summary>One glow's offsets, scale and tint. All three glows are styled the same way.</summary>
    private static bool DrawGlow(string id, string part, float offsetX, float offsetY, float scale,
        Vector4 tint, Action<float> setOffsetX, Action<float> setOffsetY, Action<float> setScale,
        Action<Vector4> setTint)
    {
        Section("Position");
        var changed = Slider($"Horizontal offset##{id}X", offsetX, -60f, 60f, setOffsetX, null);
        changed |= Slider($"Vertical offset##{id}Y", offsetY, -40f, 40f, setOffsetY, null);
        EndSection();

        Section("Size");
        changed |= Slider($"Scale##{id}Scale", scale, 0.3f, 2.5f, setScale, null, "%.2f");
        EndSection();

        Section("Color");
        changed |= ConfigHelpers.ColorEditProp($"{part} tint##{id}Tint", tint, setTint,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
        ConfigHelpers.HelpMarker("A colour multiply, so white leaves the glow as the game draws it.");
        EndSection();

        return changed;
    }

    /// <summary>
    /// A custom-colour toggle with its picker underneath, indented. <paramref name="part"/>
    /// names what is being coloured, so a window full of these can be told apart - it reads
    /// as "Custom HP bar color" with an "HP bar color" picker under it.
    /// </summary>
    private static bool DrawCustomColor(string id, string part, bool useCustom, Vector4 color,
        Action<bool> setUseCustom, Action<Vector4> setColor, string? tooltip)
    {
        var changed = ConfigHelpers.CheckboxProp($"Custom {part} color##{id}Use", useCustom, setUseCustom);

        if (tooltip != null)
            ConfigHelpers.HelpMarker(tooltip);

        if (!useCustom)
            return changed;

        ImGui.Indent();
        changed |= ConfigHelpers.ColorEditProp($"{Capitalise(part)} color##{id}", color, setColor,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
        ImGui.Unindent();
        return changed;
    }

    /// <summary>
    /// Starts a label with the part name in sentence case. Only the first letter is touched,
    /// so names that are already capitalised - "HP bar", "DPS" - keep their own casing.
    /// </summary>
    private static string Capitalise(string value)
        => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    /// <summary>A colour multiply over game artwork, where white is "leave it alone".</summary>
    private static bool DrawTint(string id, string part, Vector4 tint, Action<Vector4> setter)
    {
        var changed = ConfigHelpers.ColorEditProp($"{Capitalise(part)} tint##{id}", tint, setter,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
        ConfigHelpers.HelpMarker("Multiplied over the game's own artwork, so white leaves it unchanged.");
        return changed;
    }

    private static string MetricLabel(NameMetric metric) => metric switch
    {
        NameMetric.Dps => "DPS",
        NameMetric.Damage => "Damage",
        NameMetric.Crit => "Crit %",
        NameMetric.DirectHit => "Direct Hit %",
        NameMetric.CritDirectHit => "Crit Direct Hit %",
        NameMetric.DamagePercent => "Damage %",
        _ => metric.ToString(),
    };

    /// <summary>
    /// One metric's toggle, with its own position, size and colour once it is on. Each metric
    /// is drawn by a node of its own, so all three can differ between them. The toggle is the
    /// group heading here - a separate one would only repeat the metric's name.
    /// </summary>
    private static bool DrawNameMetric(PartyListOverlaySettings settings, NameMetric metric)
    {
        ImGui.PushID((int)metric);

        var label = MetricLabel(metric);
        var enabled = settings.MetricEnabled(metric);
        var changed = ConfigHelpers.CheckboxProp(label, enabled,
            v => settings.SetMetricEnabled(metric, v));

        if (metric == NameMetric.Dps)
            ConfigHelpers.HelpMarker("Drawn after the name, in the order listed. Values and " +
                "formatting match the meter window.");

        if (enabled)
        {
            var style = settings.Style(metric);

            ImGui.Indent();

            Section("Position");
            changed |= Slider("Gap before", style.Gap, -20f, 60f,
                v => style.Gap = v,
                "Space before this metric - measured from where the name's text ends, or from " +
                "the metric before it.");
            changed |= Slider("Vertical offset", style.OffsetY, -30f, 30f,
                v => style.OffsetY = v,
                "Lifts this metric off the name's line. The metrics after it stay where they were.");
            EndSection();

            Section("Size");
            changed |= SliderInt("Font size change", style.FontDelta, -8, 8,
                v => style.FontDelta = v,
                "Offset from the name's font, which the metric otherwise copies exactly.");
            EndSection();

            Section("Color");
            changed |= DrawCustomColor("plMetric", label, style.UseCustomColor, style.Color,
                v => style.UseCustomColor = v, v => style.Color = v,
                "Off follows the name's own colour, the way the game draws it.");
            EndSection();

            ImGui.Unindent();
        }

        ImGui.PopID();
        return changed;
    }

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
