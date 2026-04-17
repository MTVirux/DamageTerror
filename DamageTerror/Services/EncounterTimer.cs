namespace DamageTerror.Services;

using System.Diagnostics;

public sealed class EncounterTimer
{
    private readonly Stopwatch stopwatch = new();
    private float overrideSeconds = -1f;

    public float ElapsedSeconds => overrideSeconds >= 0f ? overrideSeconds : (float)stopwatch.Elapsed.TotalSeconds;

    public bool IsRunning => stopwatch.IsRunning;

    public void Start() => stopwatch.Start();
    public void Restart() { overrideSeconds = -1f; stopwatch.Restart(); }
    public void Stop() => stopwatch.Stop();
    public void Reset() { overrideSeconds = -1f; stopwatch.Reset(); }

    /// <summary>
    /// Override the elapsed time to a fixed value, bypassing the real-time stopwatch.
    /// Used during offline replay (e.g. recalculating from log lines).
    /// Pass a negative value to revert to the real stopwatch.
    /// </summary>
    public void SetElapsed(float seconds) => overrideSeconds = seconds;
}
