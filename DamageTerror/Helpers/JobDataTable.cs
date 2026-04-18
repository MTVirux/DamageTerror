
namespace DamageTerror.Helpers;

public static class JobDataTable
{
    public readonly record struct JobEntry(
        string Abbreviation,
        string FullName,
        JobRole Role,
        uint ClassJobId,
        Vector4 DefaultColor,
        bool IsBaseClass);

    public static readonly string[] TankJobs = JobRegistry.TankJobs;
    public static readonly string[] HealerJobs = JobRegistry.HealerJobs;
    public static readonly string[] MeleeDpsJobs = JobRegistry.MeleeDpsJobs;
    public static readonly string[] RangedDpsJobs = JobRegistry.RangedDpsJobs;
    public static readonly string[] CasterDpsJobs = JobRegistry.CasterDpsJobs;
    public static readonly string[] DoHLJobs = JobRegistry.DoHLJobs;
    public static readonly string[] BaseClassJobs = JobRegistry.BaseClassJobs;
    public static readonly string[] AllAbbreviations = JobRegistry.AllAbbreviations;

    public static bool TryGet(string key, out JobEntry entry) => JobRegistry.TryGet(key, out entry);

    public static JobRole GetRole(string job) => JobRegistry.GetRole(job);

    public static string GetFullName(string abbreviation) => JobRegistry.GetFullName(abbreviation);

    public static Vector4 GetDefaultColor(string job) => JobRegistry.GetDefaultColor(job);

    public static uint? GetClassJobId(string job) => JobRegistry.GetClassJobId(job);
}
