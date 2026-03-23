using Dalamud.Bindings.ImGui;
using DamageTerror.Enums;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

/// <summary>
/// Display tab: sorting, bar content, detail panel, skill breakdown.
/// Controls what information is shown — not how it looks.
/// </summary>
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

        // ===== Sorting =====
        if (ImGui.CollapsingHeader("Sorting", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var sortOptions = Enum.GetNames(typeof(SortField));
            var currentSort = (int)config.SortBy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Sort by", ref currentSort, sortOptions, sortOptions.Length))
            {
                config.SortBy = (SortField)currentSort;
                changed = true;
            }

            var sortDesc = config.SortDescending;
            if (ImGui.Checkbox("Descending (highest first)", ref sortDesc))
            {
                config.SortDescending = sortDesc;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Bar Content =====
        if (ImGui.CollapsingHeader("Bar Content", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled("Choose what to display on each combatant bar.");

            ImGui.Spacing();

            var showHps = config.ShowHps;
            if (ImGui.Checkbox("Show HPS instead of DPS", ref showHps))
            {
                config.ShowHps = showHps;
                changed = true;
            }

            ImGui.Spacing();
            ImGui.Separator();
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

            var showValue = config.ShowValueOnBar;
            if (ImGui.Checkbox("DPS/HPS value", ref showValue))
            {
                config.ShowValueOnBar = showValue;
                changed = true;
            }

            var showPct = config.ShowDamagePercentOnBar;
            if (ImGui.Checkbox("DPS/HPS percent", ref showPct))
            {
                config.ShowDamagePercentOnBar = showPct;
                changed = true;
            }

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

            ImGui.Spacing();
            ImGui.TextDisabled("Hit stats displayed on each bar.");

            var showDh = config.ShowDirectHitOnBar;
            if (ImGui.Checkbox("! (Direct Hit %%)", ref showDh))
            {
                config.ShowDirectHitOnBar = showDh;
                changed = true;
            }

            var showCrit = config.ShowCritOnBar;
            if (ImGui.Checkbox("!! (Critical Hit %%)", ref showCrit))
            {
                config.ShowCritOnBar = showCrit;
                changed = true;
            }

            var showCdh = config.ShowCritDirectHitOnBar;
            if (ImGui.Checkbox("!!! (Crit Direct Hit %%)", ref showCdh))
            {
                config.ShowCritDirectHitOnBar = showCdh;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Detail Panel =====
        if (ImGui.CollapsingHeader("Detail Panel"))
        {
            ImGui.TextDisabled("Choose what to show in the expanded detail view.");

            var showDmg = config.DetailShowDamage;
            if (ImGui.Checkbox("Total damage", ref showDmg))
            {
                config.DetailShowDamage = showDmg;
                changed = true;
            }

            var showCrit = config.DetailShowCritDhStats;
            if (ImGui.Checkbox("Crit / DH / CDH stats", ref showCrit))
            {
                config.DetailShowCritDhStats = showCrit;
                changed = true;
            }

            var showDeaths = config.DetailShowDeaths;
            if (ImGui.Checkbox("Deaths", ref showDeaths))
            {
                config.DetailShowDeaths = showDeaths;
                changed = true;
            }

            var showOh = config.DetailShowOverheal;
            if (ImGui.Checkbox("Overheal %", ref showOh))
            {
                config.DetailShowOverheal = showOh;
                changed = true;
            }

            var showMax = config.DetailShowMaxHit;
            if (ImGui.Checkbox("Max hit", ref showMax))
            {
                config.DetailShowMaxHit = showMax;
                changed = true;
            }

            var showTrend = config.DetailShowDpsTrend;
            if (ImGui.Checkbox("DPS trend (10s/30s/60s)", ref showTrend))
            {
                config.DetailShowDpsTrend = showTrend;
                changed = true;
            }
        }

        ImGui.Spacing();

        // ===== Skill Breakdown =====
        if (ImGui.CollapsingHeader("Skill Breakdown"))
        {
            var showSkills = config.DetailShowSkillBreakdown;
            if (ImGui.Checkbox("Show skill breakdown", ref showSkills))
            {
                config.DetailShowSkillBreakdown = showSkills;
                changed = true;
            }

            if (config.DetailShowSkillBreakdown)
            {
                var maxSkills = config.MaxSkillBreakdownCount;
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderInt("Max skills shown (0 = all)", ref maxSkills, 0, 30))
                {
                    config.MaxSkillBreakdownCount = maxSkills;
                    changed = true;
                }
            }
        }

        return changed;
    }
}
