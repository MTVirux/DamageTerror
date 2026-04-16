namespace DamageTerror.Helpers;

public static class JobColorHelper
{
    public static readonly string[] TankJobs = JobDataTable.TankJobs;
    public static readonly string[] HealerJobs = JobDataTable.HealerJobs;
    public static readonly string[] MeleeDpsJobs = JobDataTable.MeleeDpsJobs;
    public static readonly string[] RangedDpsJobs = JobDataTable.RangedDpsJobs;
    public static readonly string[] CasterDpsJobs = JobDataTable.CasterDpsJobs;
    public static readonly string[] BaseClassJobs = JobDataTable.BaseClassJobs;
    public static readonly string[] AllJobAbbreviations = JobDataTable.AllAbbreviations;

    public static Vector4 GetDefaultJobColor(string job) => JobDataTable.GetDefaultColor(job);

    public static JobRole GetRole(string job) => JobDataTable.GetRole(job);

    public static Vector4 GetColor(string job, Configuration config)
    {
        if (config.UsePerJobColors && !string.IsNullOrEmpty(job))
        {
            if (config.JobColors.TryGetValue(job, out var custom))
                return custom;

            if (JobDataTable.TryGet(job, out var entry))
                return entry.DefaultColor;
        }

        return GetRole(job) switch
        {
            JobRole.Tank => config.TankColor,
            JobRole.Healer => config.HealerColor,
            JobRole.MeleeDps => config.MeleeDpsColor,
            JobRole.RangedDps => config.RangedDpsColor,
            JobRole.CasterDps => config.CasterDpsColor,
            JobRole.LimitBreak => config.LimitBreakColor,
            _ => config.DefaultJobColor,
        };
    }

    public static Vector4 GetBarColor(string job, float alpha, Configuration config)
    {
        var c = GetColor(job, config);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }
}
