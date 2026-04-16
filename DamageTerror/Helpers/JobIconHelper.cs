using DamageTerror.Enums;

namespace DamageTerror.Helpers;

public static class JobIconHelper
{
    private static readonly Dictionary<string, uint> ExtraClassJobIds = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Crp", 8 }, { "Bsm", 9 }, { "Arm", 10 },
        { "Gsm", 11 }, { "Ltw", 12 }, { "Wvr", 13 },
        { "Alc", 14 }, { "Cul", 15 },
        { "Min", 16 }, { "Btn", 17 }, { "Fsh", 18 },
    };

    private static readonly string[] ExtraAbbreviations = ExtraClassJobIds.Keys.ToArray();

    private const uint PlainIconOffset = 62000;
    private const uint FramedIconOffset = 62100;
    private const uint LimitBreakIconId = 103;

    private static uint GetBaseOffset(JobIconStyle style) => style switch
    {
        JobIconStyle.Plain => PlainIconOffset,
        JobIconStyle.Framed => FramedIconOffset,
        _ => FramedIconOffset,
    };

    public static uint? GetIconId(string job, JobIconStyle style = JobIconStyle.Framed,
        Dictionary<string, uint>? customIcons = null)
    {
        if (string.IsNullOrEmpty(job))
            return null;

        if (style == JobIconStyle.Custom
            && customIcons != null
            && customIcons.TryGetValue(job, out var customId)
            && customId != 0)
            return customId;

        if (job.Equals("Lmb", StringComparison.OrdinalIgnoreCase)
            || job.Equals("Limit Break", StringComparison.OrdinalIgnoreCase))
            return LimitBreakIconId;

        uint? classJobId = JobDataTable.GetClassJobId(job);
        if (classJobId == null && ExtraClassJobIds.TryGetValue(job, out var extraId))
            classJobId = extraId;

        if (classJobId == null)
            return null;

        var offset = style == JobIconStyle.Custom ? GetBaseOffset(JobIconStyle.Framed) : GetBaseOffset(style);
        return offset + classJobId.Value;
    }

    /// <summary>All distinct job abbreviations (combat + DoH/DoL).</summary>
    public static IEnumerable<string> AllJobAbbreviations =>
        JobDataTable.AllAbbreviations.Concat(ExtraAbbreviations);
}
