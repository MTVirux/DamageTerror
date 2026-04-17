using Dalamud.Bindings.ImGui;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace DamageTerror.Gui.ConfigWindow;

internal static class FormattingSection
{
    private static readonly double[] PreviewSamples = { 0, 42, 500, 1_234, 9_999, 15_000, 150_000, 999_999, 1_500_000, 12_345_678 };
    private static readonly double[] PreviewPctSamples = { 0, 5.3, 12.75, 48.6, 100 };

    public static bool Draw(Configuration config)
    {
        var changed = false;

        var formatIdx = (int)config.ValueDisplayFormat;
        var formatLabels = new[] { "Abbreviated (12.3K)", "Commas (12,345)", "Raw (12345.6)" };
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Number format", ref formatIdx, formatLabels, formatLabels.Length))
        {
            config.ValueDisplayFormat = (ValueDisplayFormat)formatIdx;
            changed = true;
        }

        ImGui.Spacing();

        var abbrevDec = config.AbbreviatedDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Abbreviated decimal places", ref abbrevDec, 0, 2))
        {
            config.AbbreviatedDecimalPlaces = abbrevDec;
            changed = true;
        }
        ConfigHelpers.HelpMarker("K / M suffixed values");

        var rawDec = config.RawDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Value decimal places", ref rawDec, 0, 2))
        {
            config.RawDecimalPlaces = rawDec;
            changed = true;
        }
        ConfigHelpers.HelpMarker("Raw / Commas values");

        var pctDec = config.PercentDecimalPlaces;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Percent decimal places", ref pctDec, 0, 2))
        {
            config.PercentDecimalPlaces = pctDec;
            changed = true;
        }

        if (config.ValueDisplayFormat == ValueDisplayFormat.Abbreviated)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled("Abbreviation Thresholds");

            var kThresh = (float)config.AbbreviatedKThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputFloat("K threshold", ref kThresh, 1000f, 5000f, "%.0f"))
            {
                config.AbbreviatedKThreshold = Math.Max(0, kThresh);
                changed = true;
            }
            ConfigHelpers.HelpMarker("Values >= this show as K");

            var mThresh = (float)config.AbbreviatedMThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputFloat("M threshold", ref mThresh, 100000f, 500000f, "%.0f"))
            {
                config.AbbreviatedMThreshold = Math.Max(0, mThresh);
                changed = true;
            }
            ConfigHelpers.HelpMarker("Values >= this show as M");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Skill Name Abbreviation");

        var skillLen = config.MaxHitSkillNameLength;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Max skill name length", ref skillLen, 0, 30))
        {
            config.MaxHitSkillNameLength = skillLen;
            changed = true;
        }
        ConfigHelpers.HelpMarker("Shorten Max Hit / Max Heal skill names when they exceed this length.\nEach word after the first is replaced by its initial. 0 = disabled.");

        if (config.MaxHitSkillNameLength > 0)
        {
            var truncSkill = config.TruncateSkillNames;
            if (ImGui.Checkbox("Truncate instead of abbreviate", ref truncSkill))
            {
                config.TruncateSkillNames = truncSkill;
                changed = true;
            }

            var preview = ValueFormatter.AbbreviateSkillName("Midare Setsugekka", config.MaxHitSkillNameLength, config.TruncateSkillNames);
            ConfigHelpers.HelpMarker($"e.g. Midare Setsugekka → {preview}");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextDisabled("Preview");

        if (ImGui.BeginTable("##fmtPreview", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Formatted", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var sample in PreviewSamples)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sample:N0}");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ValueFormatter.Format(sample, config));
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Percent Preview");

        if (ImGui.BeginTable("##pctPreview", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Formatted", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var sample in PreviewPctSamples)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{sample}%");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ValueFormatter.FormatPercent(sample, config.PercentDecimalPlaces));
            }

            ImGui.EndTable();
        }

        return changed;
    }
}
