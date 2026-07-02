
namespace DamageTerror.Helpers;

public static class JobIconHelper
{
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

        uint? classJobId = JobRegistry.GetClassJobId(job);
        if (classJobId == null)
            return null;

        var offset = style == JobIconStyle.Custom ? GetBaseOffset(JobIconStyle.Framed) : GetBaseOffset(style);
        return offset + classJobId.Value;
    }

    /// <summary>All distinct job abbreviations (combat + DoH/DoL).</summary>
    public static IEnumerable<string> AllJobAbbreviations => JobRegistry.AllAbbreviations;
}
