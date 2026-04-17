
namespace DamageTerror.Helpers;

public static class NameFormatHelper
{
    public static string FormatName(string name, string job, NameDisplayFormat fmt, int truncateLength = 12)
    {
        switch (fmt)
        {
            case NameDisplayFormat.FirstNameOnly:
            {
                var spaceIdx = name.IndexOf(' ');
                return spaceIdx > 0 ? name[..spaceIdx] : name;
            }
            case NameDisplayFormat.LastNameOnly:
            {
                var spaceIdx = name.LastIndexOf(' ');
                return spaceIdx >= 0 ? name[(spaceIdx + 1)..] : name;
            }
            case NameDisplayFormat.Initials:
            {
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2
                    ? $"{parts[0][0]}. {parts[1][0]}."
                    : name;
            }
            case NameDisplayFormat.JobAbbreviation:
                return !string.IsNullOrEmpty(job) ? job.ToUpperInvariant() : name;
            case NameDisplayFormat.JobFullName:
                return !string.IsNullOrEmpty(job) ? JobDataTable.GetFullName(job) : name;
            case NameDisplayFormat.Truncated:
                return name.Length > truncateLength ? name[..truncateLength] + "..." : name;
            default:
                return name;
        }
    }

    public static string GetDisplayName(string name, string job, bool isLocalPlayer, Configuration config)
    {
        if (isLocalPlayer && config.ShowYouOnBar)
            return "YOU";
        var fmt = isLocalPlayer ? config.SelfNameFormat : config.OthersNameFormat;
        return FormatName(name, job, fmt, config.NameTruncateLength);
    }
}
