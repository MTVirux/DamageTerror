namespace DamageTerror.Services;

using System.Diagnostics;

public sealed class EncounterTimer
{
    private readonly Stopwatch stopwatch = new();

    public float ElapsedSeconds => (float)stopwatch.Elapsed.TotalSeconds;

    public bool IsRunning => stopwatch.IsRunning;

    public void Start() => stopwatch.Start();
    public void Restart() => stopwatch.Restart();
    public void Stop() => stopwatch.Stop();
    public void Reset() => stopwatch.Reset();
}
