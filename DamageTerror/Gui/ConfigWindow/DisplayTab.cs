using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public sealed class DisplayTab
{
    public bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Spacing();

            var showJob = config.ShowJobAbbrevOnBar;
            if (ImGui.Checkbox("Job abbreviation text", ref showJob))
            {
                config.ShowJobAbbrevOnBar = showJob;
                changed = true;
            }

            var showRank = config.ShowRankNumber;
            if (ImGui.Checkbox("Rank number", ref showRank))
            {
                config.ShowRankNumber = showRank;
                changed = true;
            }

            var showJobIcons = config.ShowJobIcons;
            if (ImGui.Checkbox("Job icons", ref showJobIcons))
            {
                config.ShowJobIcons = showJobIcons;
                changed = true;
            }

            if (config.ShowJobIcons)
            {
                ImGui.SameLine();
                var styleIdx = (int)config.JobIconStyle;
                var styleLabels = new[] { "Framed", "Plain", "Custom" };
                ImGui.SetNextItemWidth(120);
                if (ImGui.Combo("Icon style", ref styleIdx, styleLabels, styleLabels.Length))
                {
                    config.JobIconStyle = (JobIconStyle)styleIdx;
                    changed = true;
                }

                if (config.JobIconStyle == JobIconStyle.Custom)
                {
                    ImGui.Indent();
                    ImGui.TextUnformatted("Custom Icons");
                    ConfigHelpers.HelpMarker("Set a game icon ID per job (0 = default framed).");
                    ImGui.Spacing();

                    foreach (var abbr in JobIconHelper.AllJobAbbreviations.OrderBy(a => a))
                    {
                        config.CustomJobIcons.TryGetValue(abbr, out var curId);
                        var idInt = (int)curId;
                        ImGui.SetNextItemWidth(100);
                        if (ImGui.InputInt($"{abbr.ToUpperInvariant()}##custicon_{abbr}", ref idInt, 0))
                        {
                            if (idInt < 0) idInt = 0;
                            config.CustomJobIcons[abbr] = (uint)idInt;
                            changed = true;
                        }

                        if (idInt > 0)
                        {
                            ImGui.SameLine();
                            var preview = ServiceManager.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup((uint)idInt));
                            if (preview.TryGetWrap(out var wrap, out _))
                            {
                                ImGui.Image(wrap.Handle, new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight()));
                            }
                        }
                    }

                    ImGui.Unindent();
                }
            }

        }

        return changed;
    }

    internal static Dictionary<BarColumn, string> ColumnLabels => MetricPicker.BarColumnLabels;

    private static readonly string[] FormatLabels = { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };

    internal static bool DrawColumnFormatPopup(BarColumn col, Dictionary<BarColumn, ColumnFormatOverride> overrides)
    {
        var changed = false;
        var label = ColumnLabels.GetValueOrDefault(col, col.ToString());
        ImGui.TextDisabled($"{label} — Format Override");
        ImGui.Spacing();

        var hasOverride = overrides.TryGetValue(col, out var ov);
        var useCustom = hasOverride;

        if (ImGui.Checkbox("Use custom format##colFmt", ref useCustom))
        {
            if (useCustom && !hasOverride)
            {
                ov = new ColumnFormatOverride();
                overrides[col] = ov;
            }
            else if (!useCustom && hasOverride)
            {
                overrides.Remove(col);
                ov = null;
            }
            changed = true;
        }

        if (ov == null)
        {
            ImGui.TextDisabled("Using global format settings.");
            return changed;
        }

        ImGui.Spacing();

        var isValue = ColumnFormatOverride.ValueColumns.Contains(col);
        var isPercent = ColumnFormatOverride.PercentColumns.Contains(col);

        if (isValue)
        {
            var fmtIdx = (int)ov.ValueDisplayFormat;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Number format##colFmt", ref fmtIdx, FormatLabels, FormatLabels.Length))
            {
                ov.ValueDisplayFormat = (ValueDisplayFormat)fmtIdx;
                changed = true;
            }

            if (ov.ValueDisplayFormat == ValueDisplayFormat.Abbreviated)
            {
                var abbDec = ov.AbbreviatedDecimalPlaces;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderInt("Abbreviated decimals##colFmt", ref abbDec, 0, 2))
                {
                    ov.AbbreviatedDecimalPlaces = abbDec;
                    changed = true;
                }

                ImGui.Spacing();
                var kThresh = (float)ov.AbbreviatedKThreshold;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputFloat("K threshold##colFmt", ref kThresh, 0, 0, "%.0f"))
                {
                    if (kThresh >= 0) ov.AbbreviatedKThreshold = kThresh;
                    changed = true;
                }

                var mThresh = (float)ov.AbbreviatedMThreshold;
                ImGui.SetNextItemWidth(120);
                if (ImGui.InputFloat("M threshold##colFmt", ref mThresh, 0, 0, "%.0f"))
                {
                    if (mThresh >= 0) ov.AbbreviatedMThreshold = mThresh;
                    changed = true;
                }
            }
            else
            {
                var rawDec = ov.RawDecimalPlaces;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderInt("Decimal places##colFmt", ref rawDec, 0, 2))
                {
                    ov.RawDecimalPlaces = rawDec;
                    changed = true;
                }
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Preview:");
            double[] samples = { 500, 9_999, 15_000, 1_500_000 };
            foreach (var s in samples)
            {
                var dec = ov.ValueDisplayFormat == ValueDisplayFormat.Abbreviated
                    ? ov.AbbreviatedDecimalPlaces : ov.RawDecimalPlaces;
                ImGui.BulletText($"{s:N0} → {ValueFormatter.Format(s, ov.ValueDisplayFormat, dec, ov.AbbreviatedKThreshold, ov.AbbreviatedMThreshold)}");
            }
        }

        if (isPercent)
        {
            var pctDec = ov.PercentDecimalPlaces;
            ImGui.SetNextItemWidth(120);
            if (ImGui.SliderInt("Percent decimals##colFmt", ref pctDec, 0, 2))
            {
                ov.PercentDecimalPlaces = pctDec;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Preview:");
            double[] pctSamples = { 5.3, 12.75, 48.6 };
            foreach (var s in pctSamples)
                ImGui.BulletText($"{s} → {ValueFormatter.FormatPercent(s, ov.PercentDecimalPlaces)}");
        }

        return changed;
    }
}
