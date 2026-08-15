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

    /// <summary>Same resolution against a palette that isn't the meter's own.</summary>
    public static Vector4 GetEffectiveJobColor(string job, JobColorPalette palette)
    {
        if (palette.UsePerJobColors && !string.IsNullOrEmpty(job))
        {
            if (palette.JobColors.TryGetValue(job, out var custom))
                return custom;

            if (JobRegistry.TryGet(job, out var entry))
                return entry.DefaultColor;
        }

        return JobRegistry.GetRole(job) switch
        {
            JobRole.Tank => palette.TankColor,
            JobRole.Healer => palette.HealerColor,
            JobRole.MeleeDps => palette.MeleeDpsColor,
            JobRole.RangedDps => palette.RangedDpsColor,
            JobRole.CasterDps => palette.CasterDpsColor,
            JobRole.LimitBreak => palette.LimitBreakColor,
            JobRole.DoHL => palette.DoHLColor,
            _ => palette.DefaultJobColor,
        };
    }

    public static Vector4 GetBarColor(string job, float alpha, Configuration config)
    {
        var c = GetEffectiveJobColor(job, config);
        return new Vector4(c.X * 0.8f, c.Y * 0.8f, c.Z * 0.8f, alpha);
    }

    public static Vector4 WithAlpha(Vector4 color, float alpha)
        => new(color.X, color.Y, color.Z, alpha);
}
