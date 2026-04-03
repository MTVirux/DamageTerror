namespace DamageTerror.Services;

using System.Diagnostics;

/// <summary>
/// Shared encounter stopwatch consumed by both <see cref="GraphDataTracker"/> and
/// <see cref="SkillTracker"/> so that graph samples and skill-use events share a
/// single time base, eliminating marker-vs-line alignment drift.
/// </summary>
public sealed class EncounterTimer
{
    private readonly Stopwatch stopwatch = new();

    /// <summary>Seconds elapsed since the encounter timer was started.</summary>
    public float ElapsedSeconds => (float)stopwatch.Elapsed.TotalSeconds;

    public bool IsRunning => stopwatch.IsRunning;

    public void Start() => stopwatch.Start();
    public void Restart() => stopwatch.Restart();
    public void Stop() => stopwatch.Stop();
    public void Reset() => stopwatch.Reset();
}
