using Dalamud.Bindings.ImGui;
using DamageTerror.Enums;
using DamageTerror.Helpers;
using DamageTerror.Services;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

public class DisplayTab
{
    private static readonly string[] NameFormatLabels = new[]
    {
        "Full Name",
        "First Name Only",
        "Last Name Only",
        "Initials (F. L.)",
        "Job Abbreviation",
        "Job Full Name",
    };

    public bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("General bar display options.");

            ImGui.Spacing();

            var showName = config.ShowNameOnBar;
            if (ImGui.Checkbox("Player name", ref showName))
            {
                config.ShowNameOnBar = showName;
                changed = true;
            }

            var showYou = config.ShowYouOnBar;
            if (ImGui.Checkbox("Show \"YOU\" instead of character name", ref showYou))
            {
                config.ShowYouOnBar = showYou;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Name display format.");

            var selfFmt = (int)config.SelfNameFormat;
            if (ImGui.Combo("Your name", ref selfFmt, NameFormatLabels, NameFormatLabels.Length))
            {
                config.SelfNameFormat = (NameDisplayFormat)selfFmt;
                changed = true;
            }

            var othersFmt = (int)config.OthersNameFormat;
            if (ImGui.Combo("Others' names", ref othersFmt, NameFormatLabels, NameFormatLabels.Length))
            {
                config.OthersNameFormat = (NameDisplayFormat)othersFmt;
                changed = true;
            }

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
                    ImGui.TextDisabled("Set a game icon ID per job (0 = default framed).");
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

                        // Show a small preview of the icon
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

    private static readonly Dictionary<BarColumn, string> ColumnLabels = new()
    {
        { BarColumn.Dps, "DPS" },
        { BarColumn.Hps, "HPS" },
        { BarColumn.Damage, "Damage" },
        { BarColumn.Healed, "Healed" },
        { BarColumn.DamagePercent, "Damage/Heal %" },
        { BarColumn.DirectHit, "Direct Hit %" },
        { BarColumn.Crit, "Critical Hit %" },
        { BarColumn.CritDirectHit, "Crit Direct Hit %" },
        { BarColumn.Deaths, "Deaths" },
        { BarColumn.DamageTaken, "Damage Taken" },
        { BarColumn.Overheal, "Overheal %" },
    };

    public static bool DrawBarColumns(List<BarColumn> columnOrder, Func<BarColumn, bool> getEnabled, Action<BarColumn, bool> setEnabled, Dictionary<BarColumn, string> headerLabels)
    {
        var changed = false;

        for (var i = 0; i < columnOrder.Count; i++)
        {
            var col = columnOrder[i];
            var label = ColumnLabels.GetValueOrDefault(col, col.ToString());

            ImGui.PushID(i);

            var canUp = i > 0;
            if (!canUp) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##up", ImGuiDir.Up))
            {
                (columnOrder[i], columnOrder[i - 1]) = (columnOrder[i - 1], columnOrder[i]);
                changed = true;
            }
            if (!canUp) ImGui.EndDisabled();

            ImGui.SameLine();

            var canDown = i < columnOrder.Count - 1;
            if (!canDown) ImGui.BeginDisabled();
            if (ImGui.ArrowButton("##down", ImGuiDir.Down))
            {
                (columnOrder[i], columnOrder[i + 1]) = (columnOrder[i + 1], columnOrder[i]);
                changed = true;
            }
            if (!canDown) ImGui.EndDisabled();

            ImGui.SameLine();

            var enabled = getEnabled(col);
            if (ImGui.Checkbox(label, ref enabled))
            {
                setEnabled(col, enabled);
                changed = true;
            }

            ImGui.SameLine();
            var defaultLabel = Configuration.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
            headerLabels.TryGetValue(col, out var currentHeader);
            currentHeader ??= "";
            ImGui.SetNextItemWidth(60);
            if (ImGui.InputTextWithHint($"##hdr_{col}", defaultLabel, ref currentHeader, 32))
            {
                if (string.IsNullOrEmpty(currentHeader))
                    headerLabels.Remove(col);
                else
                    headerLabels[col] = currentHeader;
                changed = true;
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(label);

            ImGui.PopID();
        }

        return changed;
    }

    public static bool GetTabColumnEnabled(MeterTab tab, BarColumn col) => col switch
    {
        BarColumn.Dps => tab.ShowDpsOnBar,
        BarColumn.Hps => tab.ShowHpsOnBar,
        BarColumn.Damage => tab.ShowDamageOnBar,
        BarColumn.Healed => tab.ShowHealedOnBar,
        BarColumn.DamagePercent => tab.ShowDamagePercentOnBar,
        BarColumn.DirectHit => tab.ShowDirectHitOnBar,
        BarColumn.Crit => tab.ShowCritOnBar,
        BarColumn.CritDirectHit => tab.ShowCritDirectHitOnBar,
        BarColumn.Deaths => tab.ShowDeathsOnBar,
        BarColumn.DamageTaken => tab.ShowDamageTakenOnBar,
        BarColumn.Overheal => tab.ShowOverhealOnBar,
        _ => false,
    };

    public static void SetTabColumnEnabled(MeterTab tab, BarColumn col, bool value)
    {
        switch (col)
        {
            case BarColumn.Dps: tab.ShowDpsOnBar = value; break;
            case BarColumn.Hps: tab.ShowHpsOnBar = value; break;
            case BarColumn.Damage: tab.ShowDamageOnBar = value; break;
            case BarColumn.Healed: tab.ShowHealedOnBar = value; break;
            case BarColumn.DamagePercent: tab.ShowDamagePercentOnBar = value; break;
            case BarColumn.DirectHit: tab.ShowDirectHitOnBar = value; break;
            case BarColumn.Crit: tab.ShowCritOnBar = value; break;
            case BarColumn.CritDirectHit: tab.ShowCritDirectHitOnBar = value; break;
            case BarColumn.Deaths: tab.ShowDeathsOnBar = value; break;
            case BarColumn.DamageTaken: tab.ShowDamageTakenOnBar = value; break;
            case BarColumn.Overheal: tab.ShowOverhealOnBar = value; break;
        }
    }
}
