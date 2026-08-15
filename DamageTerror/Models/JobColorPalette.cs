namespace DamageTerror.Models;

/// <summary>
/// A job and role colour set owned by something other than the meter window. Same shape as
/// the meter's own colours, so the party list can be given a palette of its own without the
/// two having to agree.
/// </summary>
public sealed class JobColorPalette
{
    public bool UsePerJobColors { get; set; } = true;

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public Dictionary<string, Vector4> JobColors { get; set; } = new();

    public Vector4 TankColor { get; set; } = new(0.2f, 0.4f, 0.8f, 1.0f);
    public Vector4 HealerColor { get; set; } = new(0.2f, 0.7f, 0.3f, 1.0f);
    public Vector4 MeleeDpsColor { get; set; } = new(0.8f, 0.2f, 0.2f, 1.0f);
    public Vector4 RangedDpsColor { get; set; } = new(0.9f, 0.5f, 0.2f, 1.0f);
    public Vector4 CasterDpsColor { get; set; } = new(0.6f, 0.3f, 0.8f, 1.0f);
    public Vector4 LimitBreakColor { get; set; } = new(1.0f, 0.80f, 0.0f, 1.0f);
    public Vector4 DoHLColor { get; set; } = new(0.70f, 0.55f, 0.30f, 1.0f);
    public Vector4 DefaultJobColor { get; set; } = new(0.5f, 0.5f, 0.5f, 1.0f);

    /// <summary>Seeds this palette from the meter window's colours, per-job overrides included.</summary>
    public void CopyFrom(Configuration config)
    {
        UsePerJobColors = config.UsePerJobColors;
        JobColors = new Dictionary<string, Vector4>(config.JobColors);
        TankColor = config.TankColor;
        HealerColor = config.HealerColor;
        MeleeDpsColor = config.MeleeDpsColor;
        RangedDpsColor = config.RangedDpsColor;
        CasterDpsColor = config.CasterDpsColor;
        LimitBreakColor = config.LimitBreakColor;
        DoHLColor = config.DoHLColor;
        DefaultJobColor = config.DefaultJobColor;
    }
}
