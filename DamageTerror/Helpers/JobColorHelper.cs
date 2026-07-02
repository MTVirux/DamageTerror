namespace DamageTerror.Helpers;

public static class JobColorHelper
{
    public static Vector4 GetEffectiveJobColor(string job, Configuration config)
    {
        if (config.UsePerJobColors && !string.IsNullOrEmpty(job))
        {
            if (config.JobColors.TryGetValue(job, out var custom))
                return custom;

            if (JobRegistry.TryGet(job, out var entry))
                return entry.DefaultColor;
        }

        return JobRegistry.GetRole(job) switch
        {
            JobRole.Tank => config.TankColor,
            JobRole.Healer => config.HealerColor,
            JobRole.MeleeDps => config.MeleeDpsColor,
            JobRole.RangedDps => config.RangedDpsColor,
            JobRole.CasterDps => config.CasterDpsColor,
            JobRole.LimitBreak => config.LimitBreakColor,
            JobRole.DoHL => config.DoHLColor,
            _ => config.DefaultJobColor,
        };
    }

    public static Vector4 GetBarColor(string job, float alpha, Configuration config)
    {
        var c = GetEffectiveJobColor(job, config);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }
}
