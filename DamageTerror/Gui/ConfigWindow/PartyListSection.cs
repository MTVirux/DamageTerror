namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Live editor for the native party list integration. Every value is read by the overlay
/// each frame, so changes show up in the party list as the slider moves.
/// <para>
/// Inside each collapsible header the settings are bundled per element - the name, the HP
/// bar, the timers - and each element is split into the same sections: where it sits, how
/// big it is, what colour it is. Both fold away as well, so a window this long can be cut
/// back to whatever is being worked on.
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
        ConfigHelpers.HelpMarker("The bar, individual metrics and totals only.\nThe restyle stays.");

        if (settings.HideOutOfCombat)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.SliderFloatProp("##plHideOocDelay",
                settings.HideOutOfCombatDelay, 0f, 30f, "%.1f",
                v => settings.HideOutOfCombatDelay = v, 150);
            ConfigHelpers.HelpMarker("Seconds after combat ends before the metrics are hidden.");
            ImGui.Unindent();
        }

        changed |= ConfigHelpers.CheckboxProp("Restyle text outline##plTintOutline",
            settings.TintTextOutline, v => settings.TintTextOutline = v);
        ConfigHelpers.HelpMarker("Gives the outline around each glyph the colour and weight " +
            "below.\nOff leaves the game's own outline, which stays dark under a recoloured " +
            "name.");

        if (settings.TintTextOutline)
        {
            ImGui.Indent();
            changed |= ConfigHelpers.ColorEditProp("Outline color##plOutlineTint",
                settings.TextOutlineTint, v => settings.TextOutlineTint = v,
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
            ConfigHelpers.HelpMarker("Black is the game's own look; a lighter colour reads as " +
                "a glow around the text.");
            changed |= ConfigHelpers.ComboProp("Outline thickness##plOutlineThickness",
                (int)settings.TextOutlineThickness, OutlineThicknessLabels,
                v => settings.TextOutlineThickness = (PartyListOutlineThickness)v, 180f);
            ConfigHelpers.HelpMarker("The game has no outline width, only two passes: thin is " +
                "its usual edge, thick adds the wider glare pass on top.\nNone drops the " +
                "outline entirely.");
            ImGui.Unindent();
        }

        changed |= Slider("Extra space between rows##plRowSpacing", settings.RowSpacing,
            0f, 40f, v => settings.RowSpacing = v,
            "Pixels added between each party member.\nThe backdrop grows to match, and the " +
            "chocobo and pet rows move down with the rest.");

        ImGui.Separator();

        changed |= DrawBarHeader(config, settings);
        changed |= DrawNameHeader(settings);
        changed |= DrawPartyIndexHeader(settings);
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

        if (ConfigHelpers.ShiftResetButton("Reset to defaults##plReset"))
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
        ImGui.PushID("plBar");

        if (Section("Position"))
        {
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
                "Puts the fill under the name, gauges and status icons.\nOff draws it on top.");
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= Slider("Height##plBarHeight", settings.BarHeightPixels, 2f, 512f,
                v => settings.BarHeightPixels = v, "Bar height in pixels, centred on the job icon.");
            changed |= Slider("Max width##plBarMaxWidth", settings.BarMaxWidth, 0f, 400f,
                v => settings.BarMaxWidth = v,
                "Width a 100% bar draws to; 0 uses the whole row.\nBars scale inside this, " +
                "so they stay proportional rather than the longest being clipped.");
            EndSection();
        }

        if (Section("Color"))
        {
            changed |= Slider("Opacity##plBarAlpha", settings.BarOpacity, 0f, 1f,
                v => settings.BarOpacity = v, null, "%.2f");

            if (ImGui.CollapsingHeader("Colors##plBarColors"))
                changed |= DrawBarColors(config, settings);

            EndSection();
        }

        ImGui.PopID();
        ImGui.Unindent();
        return changed;
    }

    private static bool DrawNameHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Name##plName"))
            return false;

        var changed = false;
        var name = settings.NameShift;

        if (Group("Player name"))
        {
            if (Section("Text"))
            {
                changed |= ConfigHelpers.CheckboxProp("Hide level##plHideLevel", settings.HideLevel,
                    v => settings.HideLevel = v);
                ConfigHelpers.HelpMarker(
                    "The level isn't a separate node - the game prefixes it to the name as glyphs, " +
                    "so this rewrites the name text.\nTurning it back off restores the game's string.");
                EndSection();
            }

            if (Section("Position"))
            {
                changed |= ConfigHelpers.CheckboxProp("Move name##plShiftName", name.Enabled,
                    v => name.Enabled = v);

                if (name.Enabled)
                {
                    ImGui.Indent();
                    changed |= Slider("Horizontal offset##plNameX", name.OffsetX, -80f, 80f,
                        v => name.OffsetX = v, null);
                    changed |= Slider("Vertical offset##plNameY", name.OffsetY, -30f, 30f,
                        v => name.OffsetY = v,
                        "Negative moves the player name up.\nThe metrics after it follow.");
                    ImGui.Unindent();
                }

                EndSection();
            }

            if (Section("Size"))
            {
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
            }

            if (Section("Color"))
            {
                changed |= DrawCustomColor("plName", "name", name.UseCustomColor, name.Color,
                    v => name.UseCustomColor = v, v => name.Color = v,
                    "Off leaves the colour the game gives the row.");
                EndSection();
            }

            EndGroup();
        }

        return changed;
    }

    /// <summary>
    /// The party slot number. Its own header rather than a corner of the name's: the two are
    /// separate nodes, and everything here applies to the number alone - the badge behind it
    /// included, which is a node of ours rather than anything the game draws.
    /// </summary>
    private static bool DrawPartyIndexHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Slot Number##plIndex"))
            return false;

        var changed = false;
        var badge = settings.PartyIndexBadge;

        if (Group("Number"))
        {
            if (Section("Text"))
            {
                changed |= ConfigHelpers.CheckboxProp("Hide slot number##plIndexHide",
                    settings.HidePartyIndex, v => settings.HidePartyIndex = v);
                ConfigHelpers.HelpMarker(
                    "Fades the number out. The game lays the row out around it either way, so " +
                    "nothing beside it moves.");

                changed |= ConfigHelpers.ComboProp("Font##plIndexFace", (int)settings.PartyIndexFont,
                    FontLabels, v => settings.PartyIndexFont = (PartyListFont)v, 180f);
                ConfigHelpers.HelpMarker(
                    "The face the number is drawn in.\nGame's own leaves it alone; the rest are " +
                    "the faces the game ships, and only Axis carries every glyph.");

                EndSection();
            }

            if (Section("Position and size"))
            {
                changed |= ConfigHelpers.CheckboxProp("Override index##plAdjustIndex",
                    settings.AdjustPartyIndex, v => settings.AdjustPartyIndex = v);
                ConfigHelpers.HelpMarker(
                    "Off, the number takes the name's size change and move, so the two stay on " +
                    "one line.\nOn, it uses the values below instead and the name no longer " +
                    "carries it.");

                if (settings.AdjustPartyIndex)
                {
                    ImGui.Indent();
                    changed |= Slider("Index horizontal offset##plIndexX", settings.PartyIndexOffsetX,
                        -40f, 40f, v => settings.PartyIndexOffsetX = v, null);
                    changed |= Slider("Index vertical offset##plIndexY", settings.PartyIndexOffsetY,
                        -30f, 30f, v => settings.PartyIndexOffsetY = v, null);
                    changed |= SliderInt("Index font size change##plIndexFont", settings.PartyIndexFontDelta,
                        -8, 8, v => settings.PartyIndexFontDelta = v,
                        "Added to the game's own font size for the slot number.");
                    ImGui.Unindent();
                }

                EndSection();
            }

            if (Section("Color"))
            {
                changed |= DrawCustomColor("plIndex", "slot number",
                    settings.PartyIndexUseCustomColor, settings.PartyIndexColor,
                    v => settings.PartyIndexUseCustomColor = v, v => settings.PartyIndexColor = v,
                    "Off leaves the colour the game gives the slot number. The name's colour is " +
                    "never used - the two are separate nodes.");

                changed |= DrawCustomColor("plIndexOutline", "slot number outline",
                    settings.PartyIndexUseCustomOutlineColor, settings.PartyIndexOutlineColor,
                    v => settings.PartyIndexUseCustomOutlineColor = v,
                    v => settings.PartyIndexOutlineColor = v,
                    "Wins over the party list wide outline above, being the narrower setting.");

                if (settings.PartyIndexUseCustomOutlineColor)
                {
                    ImGui.Indent();
                    changed |= ConfigHelpers.ComboProp("Outline thickness##plIndexOutlineWeight",
                        (int)settings.PartyIndexOutlineThickness, OutlineThicknessLabels,
                        v => settings.PartyIndexOutlineThickness = (PartyListOutlineThickness)v, 180f);
                    ImGui.Unindent();
                }

                EndSection();
            }

            EndGroup();
        }

        if (Group("Badge"))
        {
            changed |= ConfigHelpers.CheckboxProp("Draw a badge behind the number##plBadge",
                badge.Enabled, v => badge.Enabled = v);
            ConfigHelpers.HelpMarker(
                "A plate of ours, sized to the number's own box, so it follows wherever the " +
                "number has been put.\nIt is drawn from behind the rows, which is what keeps " +
                "it under the number rather than over it.");

            if (badge.Enabled)
            {
                if (Section("Position"))
                {
                    changed |= Slider("Badge horizontal offset##plBadgeX", badge.OffsetX, -40f, 40f,
                        v => badge.OffsetX = v, null);
                    changed |= Slider("Badge vertical offset##plBadgeY", badge.OffsetY, -30f, 30f,
                        v => badge.OffsetY = v, null);
                    EndSection();
                }

                if (Section("Size"))
                {
                    changed |= Slider("Horizontal padding##plBadgePadX", badge.PaddingX, -10f, 30f,
                        v => badge.PaddingX = v,
                        "Added to each side of the number's box. Negative pulls the plate inside it.");
                    changed |= Slider("Vertical padding##plBadgePadY", badge.PaddingY, -10f, 30f,
                        v => badge.PaddingY = v, null);
                    EndSection();
                }

                if (Section("Color"))
                {
                    changed |= ConfigHelpers.ColorEditProp("Badge color##plBadgeColor", badge.Color,
                        v => badge.Color = v,
                        ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
                    changed |= Slider("Opacity##plBadgeAlpha", badge.Opacity, 0f, 1f,
                        v => badge.Opacity = v, null, "%.2f");
                    EndSection();
                }
            }

            EndGroup();
        }

        return changed;
    }

    private static bool DrawGaugeHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("HP and MP##plGauge"))
            return false;

        var changed = false;

        if (Group("HP bar"))
        {
            changed |= DrawGaugeBar("plShiftHpBar", "HP bar", settings.HpBarShift,
                "Moves the HP bar only - its number is moved below.");
            changed |= DrawGaugeOutline("plHpOutline", settings.HpBarOutline);
            EndGroup();
        }

        if (Group("HP numbers"))
        {
            changed |= DrawGaugeNumbers("plHpNumbers", "HP number", settings.HpNumbers);
            EndGroup();
        }

        if (Group("Shield"))
        {
            changed |= DrawShield("plShield", "shield", settings.ShieldFill,
                "Moves the shield only - the HP bar under it stays put.");
            EndGroup();
        }

        if (Group("Shield overflow"))
        {
            changed |= DrawShield("plShieldOverflow", "shield overflow", settings.ShieldOverflow,
                "The second bar shown for the part of a shield too big to fit inside the HP bar.");
            EndGroup();
        }

        if (Group("MP bar"))
        {
            changed |= DrawGaugeBar("plShiftMpBar", "MP bar", settings.MpBarShift,
                "Moves the MP bar only - its number is moved below.");
            changed |= DrawGaugeOutline("plMpOutline", settings.MpBarOutline);
            EndGroup();
        }

        if (Group("MP numbers"))
        {
            changed |= DrawGaugeNumbers("plMpNumbers", "MP number", settings.MpNumbers);

            if (settings.MpNumbers.Enabled)
            {
                if (Section("Trailing digits"))
                {
                    changed |= SliderInt("Trailing digits size##plTrailFont", settings.MpTrailingFontDelta, -4, 8,
                        v => settings.MpTrailingFontDelta = v,
                        "MP's last two digits are a second, smaller text node.\n" +
                        "0 keeps the game's smaller size.\nRaise it to match the leading digits - " +
                        "they're then re-aligned, since the game's baseline offset only suits the small size.");
                    changed |= Slider("Trailing digits X##plTrailX", settings.TrailingDigitsOffsetX, -20f, 20f,
                        v => settings.TrailingDigitsOffsetX = v, null);
                    changed |= Slider("Trailing digits Y##plTrailY", settings.TrailingDigitsOffsetY, -20f, 20f,
                        v => settings.TrailingDigitsOffsetY = v, null);
                    EndSection();
                }
            }

            EndGroup();
        }

        return changed;
    }

    private static bool DrawMetricsHeader(PartyListOverlaySettings settings, DamageTerrorPlugin plugin)
    {
        if (!ImGui.CollapsingHeader("Individual Metrics##plMetrics"))
            return false;

        var changed = false;

        changed |= ConfigHelpers.CheckboxProp("Metric labels##plMetricLabels", settings.MetricShowLabels,
            v => settings.MetricShowLabels = v);
        ConfigHelpers.HelpMarker(
            "Writes each metric's label beside its value, as the party list header does.\n" +
            "Each one can be reworded, and put in front of its value to read as a separator.");

        ImGui.Spacing();

        ImGui.TextDisabled("Each metric is placed by its own offsets.");
        ConfigHelpers.HelpMarker(
            "Any metric the meter window can show.\nUse the tabs below to add more; the order " +
            "here only decides where a newly added one starts out.\nOpen a metric's name for " +
            "its own label, position, size and colour.\nValues and formatting match the meter.");

        if (settings.Metrics.Count > PartyListOverlaySettings.MaxMetrics)
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f),
                $"Only the first {PartyListOverlaySettings.MaxMetrics} are drawn.");

        changed |= MetricPicker.Draw(
            "plMetrics",
            settings.Metrics,
            MetricPicker.GetBarColumnLabel,
            MetricPicker.PartyListMetricCategories,
            metric => DrawMetricStyle(metric, settings.Style(metric), "plMetric",
                settings.MetricLabels, settings.MetricShowLabels,
                hideWhileCasting: true, canFloat: false),
            metric => MetricPicker.BarColumnDescriptions.GetValueOrDefault(metric),
            collapsibleExtras: true);

        ImGui.Spacing();

        if (ConfigHelpers.ShiftResetButton("Reset to name only##plMetricReset"))
        {
            settings.Metrics.Clear();
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

        var changed = false;

        if (Group("Status icons"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust status icons##plAdjustStatus",
                settings.AdjustStatusIcons, v => settings.AdjustStatusIcons = v);

            if (settings.AdjustStatusIcons)
            {
                if (Section("Position"))
                {
                    changed |= ConfigHelpers.CheckboxProp("Right align##plStatusRightAlign",
                        settings.StatusRightAlign, v => settings.StatusRightAlign = v);
                    ConfigHelpers.HelpMarker(
                        "Fills the icon row from its right edge, so a member with a few buffs shows " +
                        "them flush right instead of hugging the left.");
                    changed |= Slider("Horizontal offset##plStatusX", settings.StatusOffsetX, -120f, 120f,
                        v => settings.StatusOffsetX = v, null);
                    changed |= Slider("Vertical offset##plStatusY", settings.StatusOffsetY, -60f, 60f,
                        v => settings.StatusOffsetY = v, null);
                    EndSection();
                }

                if (Section("Size"))
                {
                    changed |= Slider("Scale##plStatusScale", settings.StatusScale, 0.3f, 2.5f,
                        v => settings.StatusScale = v,
                        "Scales the whole list from its top-left, so icon spacing scales with the icons.",
                        "%.2f");
                    EndSection();
                }

                if (Section("Color"))
                {
                    changed |= DrawTint("plStatusTint", "status icon", settings.StatusTint,
                        v => settings.StatusTint = v);
                    EndSection();
                }
            }

            EndGroup();
        }

        if (Group("Timers"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust timers##plAdjustTimers",
                settings.AdjustStatusTimers, v => settings.AdjustStatusTimers = v);
            ConfigHelpers.HelpMarker(
                "The timer text sits inside each icon, so it already scales with the icon.\n" +
                "These are on top of that.");

            if (settings.AdjustStatusTimers)
            {
                if (Section("Position"))
                {
                    changed |= Slider("Timer horizontal offset##plTimerX", settings.StatusTimerOffsetX, -30f, 30f,
                        v => settings.StatusTimerOffsetX = v, null);
                    changed |= Slider("Timer vertical offset##plTimerY", settings.StatusTimerOffsetY, -30f, 30f,
                        v => settings.StatusTimerOffsetY = v, null);
                    EndSection();
                }

                if (Section("Size"))
                {
                    changed |= SliderInt("Timer font size change##plTimerFont", settings.StatusTimerFontDelta, -8, 8,
                        v => settings.StatusTimerFontDelta = v, null);
                    EndSection();
                }

                if (Section("Color"))
                {
                    changed |= DrawCustomColor("plTimer", "timer",
                        settings.StatusTimerUseCustomColor, settings.StatusTimerColor,
                        v => settings.StatusTimerUseCustomColor = v, v => settings.StatusTimerColor = v,
                        "Off leaves the game's own colour, which turns as the status runs out.");
                    EndSection();
                }
            }

            EndGroup();
        }

        return changed;
    }

    private static bool DrawGlowHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Hover and Selection Glow##plGlow"))
            return false;

        var changed = ConfigHelpers.CheckboxProp("Adjust glows##plAdjustGlow",
            settings.AdjustSelectionGlow, v => settings.AdjustSelectionGlow = v);
        ConfigHelpers.HelpMarker(
            "Each state draws more than one node, and all of them are adjusted together.\n" +
            "Which settings apply is decided by the row's state, so hover and selection " +
            "stay independent without needing to tell the nodes apart.\n" +
            "Tint is a colour multiply - the game fades these in on a timeline, and " +
            "writing opacity would pin it and stop the fade.");

        if (!settings.AdjustSelectionGlow)
            return changed;

        if (Group("Animation"))
        {
            changed |= ConfigHelpers.CheckboxProp("Freeze glow animation##plFreezeGlow",
                settings.FreezeGlowTransform, v => settings.FreezeGlowTransform = v);
            ConfigHelpers.HelpMarker(
                "The game animates the glow's position, scale and tint as it appears, which " +
                "overwrites these settings while it plays.\nThis stops the timeline driving " +
                "those, leaving the fade animated.\nTurn it off to keep the pop-in movement, " +
                "at the cost of the settings below not holding until the animation ends.");

            changed |= ConfigHelpers.CheckboxProp("Selection wins over hover##plGlowPrecedence",
                settings.SelectionOverridesHover, v => settings.SelectionOverridesHover = v);
            ConfigHelpers.HelpMarker(
                "The game draws one node for both states - selection is marked by also showing " +
                "the job icon glow, not by a second highlight - so a row that is hovered and " +
                "selected can only use one of the two.\nOn, a selected row keeps its selection " +
                "look while you hover it; off, hovering switches it to the hover look.");
            EndGroup();
        }

        if (Group("Hover"))
        {
            changed |= DrawGlow("plHover", "Hover", settings.HoverOffsetX, settings.HoverOffsetY,
                settings.HoverScale, settings.HoverTint,
                v => settings.HoverOffsetX = v, v => settings.HoverOffsetY = v,
                v => settings.HoverScale = v, v => settings.HoverTint = v);
            EndGroup();
        }

        if (Group("Selection"))
        {
            changed |= DrawGlow("plSel", "Selection", settings.SelectionOffsetX, settings.SelectionOffsetY,
                settings.SelectionScale, settings.SelectionTint,
                v => settings.SelectionOffsetX = v, v => settings.SelectionOffsetY = v,
                v => settings.SelectionScale = v, v => settings.SelectionTint = v);
            EndGroup();
        }

        if (Group("Job icon glow"))
        {
            changed |= DrawGlow("plIconGlow", "Job icon glow", settings.IconGlowOffsetX, settings.IconGlowOffsetY,
                settings.IconGlowScale, settings.IconGlowTint,
                v => settings.IconGlowOffsetX = v, v => settings.IconGlowOffsetY = v,
                v => settings.IconGlowScale = v, v => settings.IconGlowTint = v);
            EndGroup();
        }

        return changed;
    }

    private static bool DrawTotalsHeader(PartyListOverlaySettings settings)
    {
        if (!ImGui.CollapsingHeader("Party Header##plTotals"))
            return false;

        var changed = false;

        if (Group("Contents"))
        {
            changed |= ConfigHelpers.CheckboxProp("Override \"Solo\" / \"Party\" label##plHidePartyType",
                settings.HidePartyTypeLabel, v => settings.HidePartyTypeLabel = v);
            ConfigHelpers.HelpMarker("Drops the game's header text and puts the encounter totals " +
                "or the text below in its place.\nRestored when turned off.");

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

                ConfigHelpers.HelpMarker("Shown on the party list header in place of the " +
                    "encounter totals, out of combat and between encounters.\nLeave empty for " +
                    "a blank header.");

                if (!settings.ShowEncounterTotals)
                    ImGui.EndDisabled();

                ImGui.Unindent();
            }

            changed |= ConfigHelpers.CheckboxProp("Show on party list header##plShowTotals",
                settings.ShowEncounterTotals, v => settings.ShowEncounterTotals = v);
            ConfigHelpers.HelpMarker("Appended to the \"Party\" / \"Light Party\" label above the list.");

            if (settings.ShowEncounterTotals)
            {
                ImGui.Indent();
                changed |= ConfigHelpers.CheckboxProp("Encounter name##plTotalsTitle", settings.TotalsShowTitle,
                    v => settings.TotalsShowTitle = v);
                ConfigHelpers.HelpMarker(
                    "Written to the header's own text node, beside the game's label.\n" +
                    "The duration is a metric of its own now - add \"Encounter Duration\" below.");
                changed |= ConfigHelpers.CheckboxProp("Metric labels##plTotalsLabels", settings.TotalsShowLabels,
                    v => settings.TotalsShowLabels = v);
                ConfigHelpers.HelpMarker(
                    "Writes each metric's label beside its value, as the meter's status bar does.\n" +
                    "Each one can be reworded, and put in front of its value to read as a separator.");

                ImGui.Spacing();
                ImGui.TextDisabled("Written into the header text, in the order below.");
                ConfigHelpers.HelpMarker(
                    "Your own stats, except for the Encounter ones, which cover everybody.\n" +
                    "Open a metric's name to reword it, or to float it onto a node of its own " +
                    "where it gets its own position, size and colour.");

                changed |= MetricPicker.Draw(
                    "plTotalsMetrics",
                    settings.TotalsMetrics,
                    MetricPicker.GetBarColumnLabel,
                    MetricPicker.HeaderMetricCategories,
                    metric => DrawMetricStyle(metric, settings.TotalsStyle(metric), "plTotals",
                        settings.TotalsMetricLabels, settings.TotalsShowLabels,
                        hideWhileCasting: false, canFloat: true),
                    metric => MetricPicker.BarColumnDescriptions.GetValueOrDefault(metric),
                    collapsibleExtras: true);
                ImGui.Unindent();
            }

            EndGroup();
        }

        if (Group("Header text"))
        {
            changed |= ConfigHelpers.CheckboxProp("Adjust header text##plAdjustTotals", settings.AdjustTotalsText,
                v => settings.AdjustTotalsText = v);
            ConfigHelpers.HelpMarker(
                "The header's own text node, which carries the game's label, the encounter name " +
                "and every metric left inline.\nA floating metric has a node of its own and is " +
                "not moved by this - it is only where it starts out when first floated.");

            if (settings.AdjustTotalsText)
            {
                if (Section("Position"))
                {
                    changed |= Slider("Horizontal offset##plTotalsX", settings.TotalsOffsetX, -200f, 200f,
                        v => settings.TotalsOffsetX = v, null);
                    changed |= Slider("Vertical offset##plTotalsY", settings.TotalsOffsetY, -60f, 60f,
                        v => settings.TotalsOffsetY = v, null);
                    EndSection();
                }

                if (Section("Size"))
                {
                    changed |= SliderInt("Font size change##plTotalsFont", settings.TotalsFontDelta, -8, 12,
                        v => settings.TotalsFontDelta = v,
                        "Added to the game's own font size for the header.");
                    EndSection();
                }

                if (Section("Color"))
                {
                    changed |= DrawCustomColor("plTotals", "header text",
                        settings.TotalsUseCustomColor, settings.TotalsColor,
                        v => settings.TotalsUseCustomColor = v, v => settings.TotalsColor = v,
                        "Off leaves the colour the game gives the header.");
                    EndSection();
                }
            }

            EndGroup();
        }

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
        ImGui.PushID("plCastBar");

        if (Section("Position"))
        {
            changed |= Slider("Left inset##plCastBarX", settings.CastBarShiftX, 0f, 80f,
                v => settings.CastBarShiftX = v,
                "Moves the left edge right and narrows the bar by the same amount.");
            changed |= Slider("Vertical offset##plCastBarY", settings.CastBarShiftY, -30f, 30f,
                v => settings.CastBarShiftY = v, null);
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= Slider("Height##plCastBarScaleY", settings.CastBarScaleY, 0.3f, 3f,
                v => settings.CastBarScaleY = v,
                "Grown from the bar's top edge, so the vertical offset still lands where it says.",
                "%.2f");
            EndSection();
        }

        if (Section("Color"))
        {
            changed |= DrawTint("plCastBarTint", "cast bar", settings.CastBarTint,
                v => settings.CastBarTint = v);
            EndSection();
        }

        ImGui.PopID();
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
        ImGui.PushID("plCastName");

        if (Section("Position"))
        {
            changed |= Slider("Horizontal offset##plCastNameX", settings.CastNameOffsetX, -40f, 40f,
                v => settings.CastNameOffsetX = v, null);
            changed |= Slider("Vertical offset##plCastNameY", settings.CastNameOffsetY, -30f, 30f,
                v => settings.CastNameOffsetY = v,
                "Measured from the cast bar's centre line.");
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= SliderInt("Font size change##plCastNameFont", settings.CastNameFontDelta, -12, 12,
                v => settings.CastNameFontDelta = v,
                "Added to the game's own font size for the spell name.");
            EndSection();
        }

        if (Section("Color"))
        {
            changed |= DrawCustomColor("plCastName", "spell name",
                settings.CastNameUseCustomColor, settings.CastNameColor,
                v => settings.CastNameUseCustomColor = v, v => settings.CastNameColor = v,
                "Off leaves the colour the game gives the spell name.");
            EndSection();
        }

        ImGui.PopID();
        ImGui.Unindent();
        return changed;
    }

    /// <summary>Heading colour for an element group, dim enough not to fight the headers.</summary>
    private static readonly Vector4 GroupColor = new(0.55f, 0.75f, 1f, 1f);

    private const ImGuiTreeNodeFlags SubHeaderFlags =
        ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen;

    /// <summary>
    /// One element inside a collapsible header - the name, the HP bar, the timers. When it
    /// returns true it has opened an indented block that <see cref="EndGroup"/> closes.
    /// </summary>
    private static bool Group(string label) => SubHeader(label, GroupColor);

    private static void EndGroup() => ImGui.TreePop();

    /// <summary>
    /// One aspect of an element - where it sits, how big it is, what colour it is. When it
    /// returns true it has opened an indented block that <see cref="EndSection"/> closes.
    /// </summary>
    private static bool Section(string label)
        => SubHeader(label, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);

    private static void EndSection() => ImGui.TreePop();

    /// <summary>
    /// A collapsible heading below the window's own headers. Tree nodes are used rather than
    /// nested collapsing headers so the arrows read as a hierarchy, and ImGui keeps each
    /// one's open state between sessions.
    /// </summary>
    private static bool SubHeader(string label, Vector4 color)
    {
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        var open = ImGui.TreeNodeEx(label, SubHeaderFlags);
        ImGui.PopStyleColor();
        return open;
    }

    private static readonly string[] FontLabels =
    {
        "Game's own",
        "Axis",
        "Miedinger medium",
        "Miedinger",
        "Trump Gothic",
        "Jupiter",
        "Jupiter large",
    };

    private static readonly string[] OutlineThicknessLabels =
    {
        "None",
        "Thin",
        "Thick",
    };

    private static readonly string[] GaugeOutlineColorModeLabels =
    {
        "Match the bar",
        "Game's own",
        "Custom",
    };

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
            if (Section("Position"))
            {
                changed |= Slider($"Horizontal offset##{id}X", style.OffsetX, -80f, 80f,
                    v => style.OffsetX = v, null);
                changed |= Slider($"Vertical offset##{id}Y", style.OffsetY, -30f, 30f,
                    v => style.OffsetY = v, tooltip);
                EndSection();
            }

            if (Section("Size"))
            {
                changed |= Slider($"Scale##{id}Scale", style.Scale, 0.3f, 2.5f,
                    v => style.Scale = v, null, "%.2f");
                EndSection();
            }
        }

        if (Section("Color"))
        {
            changed |= DrawCustomColor($"{id}Color", part, style.UseCustomColor, style.Color,
                v => style.UseCustomColor = v, v => style.Color = v,
                "Off leaves the game's own artwork.\nOn tints it - a texture can be shaded, not repainted.");
            EndSection();
        }

        return changed;
    }

    /// <summary>
    /// A gauge's outline - the empty bar the fill is drawn over. The game paints it into the
    /// bar's own texture rather than giving it a node, so it takes a colour and a fade but
    /// has no width to set.
    /// </summary>
    private static bool DrawGaugeOutline(string id, GaugeOutlineStyle style)
    {
        var changed = false;

        if (Section("Outline"))
        {
            changed |= ConfigHelpers.CheckboxProp($"Hide outline##{id}Hide", style.Hidden,
                v => style.Hidden = v);
            ConfigHelpers.HelpMarker("The empty bar behind the fill. Its outline and the groove " +
                "inside it are one piece of artwork, so they go together.");

            if (!style.Hidden)
            {
                changed |= ConfigHelpers.ComboProp($"Outline color##{id}Mode", (int)style.ColorMode,
                    GaugeOutlineColorModeLabels, v => style.ColorMode = (GaugeOutlineColorMode)v, 180f);
                ConfigHelpers.HelpMarker("Matching the bar gives the outline whatever colour the bar " +
                    "itself is tinted with.\nArtwork can be shaded, not repainted, so a custom colour " +
                    "tints it rather than replacing it.");

                if (style.ColorMode == GaugeOutlineColorMode.Custom)
                {
                    ImGui.Indent();
                    changed |= ConfigHelpers.ColorEditProp($"Outline tint##{id}Color", style.Color,
                        v => style.Color = v, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
                    ImGui.Unindent();
                }

                changed |= Slider($"Outline opacity##{id}Opacity", style.Opacity, 0f, 1f,
                    v => style.Opacity = v,
                    "Multiplied over the alpha the game gives the artwork, so 1 leaves it alone.",
                    "%.2f");
            }

            EndSection();
        }

        return changed;
    }

    /// <summary>
    /// One piece of the shield over the HP bar. Laid out like a gauge bar, with a hide toggle
    /// and an opacity slider after it - the shield is artwork the game layers on top, so
    /// fading it back is as useful as recolouring it.
    /// </summary>
    private static bool DrawShield(string id, string part, ShieldStyle style, string tooltip)
    {
        var changed = ConfigHelpers.CheckboxProp($"Hide {part}##{id}Hide", style.Hidden,
            v => style.Hidden = v);

        if (style.Hidden)
            return changed;

        changed |= DrawGaugeBar(id, part, style, tooltip);

        if (Section("Opacity"))
        {
            changed |= Slider($"Opacity##{id}Opacity", style.Opacity, 0f, 1f,
                v => style.Opacity = v,
                "Multiplied over the alpha the game gives the artwork, so 1 leaves it alone.",
                "%.2f");
            EndSection();
        }

        return changed;
    }

    /// <summary>One gauge's numbers - the text nodes inside the HP or MP bar.</summary>
    private static bool DrawGaugeNumbers(string id, string part, GaugeNumberStyle style)
    {
        var changed = ConfigHelpers.CheckboxProp($"Adjust {part}s##{id}", style.Enabled,
            v => style.Enabled = v);

        if (!style.Enabled)
            return changed;

        if (Section("Position"))
        {
            changed |= Slider($"Horizontal offset##{id}X", style.OffsetX, -60f, 60f,
                v => style.OffsetX = v, null);
            changed |= Slider($"Vertical offset##{id}Y", style.OffsetY, -30f, 30f,
                v => style.OffsetY = v, null);
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= SliderInt($"Font size change##{id}Font", style.FontDelta, -8, 8,
                v => style.FontDelta = v, "Added to the game's own font size for these numbers.");
            EndSection();
        }

        if (Section("Color"))
        {
            changed |= DrawCustomColor($"{id}Color", part, style.UseCustomColor, style.Color,
                v => style.UseCustomColor = v, v => style.Color = v,
                "Off keeps the game's own colour, which reddens as the gauge empties.");
            EndSection();
        }

        return changed;
    }

    /// <summary>One glow's offsets, scale and tint. All three glows are styled the same way.</summary>
    private static bool DrawGlow(string id, string part, float offsetX, float offsetY, float scale,
        Vector4 tint, Action<float> setOffsetX, Action<float> setOffsetY, Action<float> setScale,
        Action<Vector4> setTint)
    {
        var changed = false;

        if (Section("Position"))
        {
            changed |= Slider($"Horizontal offset##{id}X", offsetX, -60f, 60f, setOffsetX, null);
            changed |= Slider($"Vertical offset##{id}Y", offsetY, -40f, 40f, setOffsetY, null);
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= Slider($"Scale##{id}Scale", scale, 0.3f, 2.5f, setScale, null, "%.2f");
            EndSection();
        }

        if (Section("Color"))
        {
            changed |= ConfigHelpers.ColorEditProp($"{part} tint##{id}Tint", tint, setTint,
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
            ConfigHelpers.HelpMarker("A colour multiply, so white leaves the glow as the game draws it.");
            EndSection();
        }

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

    /// <summary>
    /// One enabled metric's position, size and colour, hung off its entry in the picker.
    /// Each metric is drawn by a node of its own, so all three can differ between them.
    /// The picker collapses this under the metric's name, which does the indenting.
    /// </summary>
    /// <summary>
    /// One metric's own look, shared by the row metrics and the header ones so both offer the
    /// same settings. Two are particular to where the metric is drawn: only a row has a cast
    /// bar to step aside for, and only the header has text to be written into instead.
    /// </summary>
    private static bool DrawMetricStyle(BarColumn metric, IndividualMetricStyle style, string idPrefix,
        Dictionary<BarColumn, string> labels, bool showLabels, bool hideWhileCasting, bool canFloat)
    {
        var label = MetricPicker.GetBarColumnLabel(metric);
        var changed = false;

        if (canFloat)
        {
            changed |= ConfigHelpers.CheckboxProp("Floating", style.Floating,
                v => style.Floating = v);
            ConfigHelpers.HelpMarker(
                "Gives this metric a text node of its own, which the settings below then apply " +
                "to.\nOff writes it into the header text beside the game's label, where it shares " +
                "that text's position, size and colour and can only be worded.");
        }

        if (showLabels && Section("Label"))
        {
            ImGui.TextDisabled("Text");
            changed |= MeterTabSectionHelpers.DrawLabelOverride(metric, idPrefix + "Lbl_",
                ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(metric, metric.ToString()), labels, label);
            ConfigHelpers.HelpMarker(
                "Drawn exactly as typed, so the spacing around the value is yours: \"x\" gives " +
                "\"24%x\" and \" x\" gives \"24% x\".\nLeave it empty for the default shown, " +
                "which is drawn with a single space, or put nothing but spaces in it to leave " +
                "this one metric unlabelled.");
            changed |= ConfigHelpers.CheckboxProp("Before the value", style.LabelBeforeValue,
                v => style.LabelBeforeValue = v);
            ConfigHelpers.HelpMarker(
                "Puts the label in front of the value, which is how a label reads as a separator.");
            EndSection();
        }

        // Everything past here needs a node of its own to apply to.
        if (canFloat && !style.Floating)
            return changed;

        if (hideWhileCasting && Section("Visibility"))
        {
            changed |= ConfigHelpers.CheckboxProp("Hide while casting", style.HideWhileCasting,
                v => style.HideWhileCasting = v);
            ConfigHelpers.HelpMarker(
                "The cast bar takes over the name's line and is drawn under the metrics.\n" +
                "On, this metric steps aside until the cast is over.");
            EndSection();
        }

        if (Section("Position"))
        {
            changed |= Slider("Horizontal offset", style.OffsetX, -40f, 400f,
                v => style.OffsetX = v,
                "Measured from the left edge of the row, or of the header text the game draws.\n" +
                "Nothing about the text beside it has a say, so it holds the same place every frame.");
            changed |= Slider("Vertical offset", style.OffsetY, -20f, 80f,
                v => style.OffsetY = v,
                "Measured from the top edge of the row, or of the header text the game draws.");
            EndSection();
        }

        if (Section("Size"))
        {
            changed |= SliderInt("Font size change", style.FontDelta, -8, 8,
                v => style.FontDelta = v,
                "Offset from the font of the text this metric is placed against, which it " +
                "otherwise copies exactly.");
            EndSection();
        }

        if (Section("Color"))
        {
            var hadColor = style.UseCustomColor;
            var hadOutline = style.UseCustomOutlineColor;

            changed |= DrawCustomColor($"{idPrefix}Metric", label,
                style.UseCustomColor, style.Color,
                v => style.UseCustomColor = v, v => style.Color = v,
                "Off draws the metric in the colour the game gives the text it sits beside.");
            changed |= DrawCustomColor($"{idPrefix}MetricOutline", $"{label} outline",
                style.UseCustomOutlineColor, style.OutlineColor,
                v => style.UseCustomOutlineColor = v, v => style.OutlineColor = v,
                "Off uses the outline the game gives that text.");

            // Switching a metric to a custom colour starts it on the game's own name colours, so
            // pinning one changes nothing until they pick something. Only a metric still carrying
            // the untouched default is seeded.
            if (!hadColor && style.UseCustomColor && style.Color == IndividualMetricStyle.DefaultColor
                && GameUiColors.PartyListName is { } nameColor)
                style.Color = nameColor;

            if (!hadOutline && style.UseCustomOutlineColor
                && style.OutlineColor == IndividualMetricStyle.DefaultOutlineColor
                && GameUiColors.PartyListNameOutline is { } outlineColor)
                style.OutlineColor = outlineColor;

            EndSection();
        }

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
            "same way.\nParty list palette: colours kept for the party list alone.\n" +
            "Single color: one colour for every row.\n" +
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
