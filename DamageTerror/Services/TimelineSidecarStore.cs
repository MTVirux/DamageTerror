
namespace DamageTerror.Services;

/// <summary>
/// Reads, writes, and deletes per-encounter timeline sidecar files at
/// <c>&lt;baseDir&gt;/timelines/&lt;id&gt;.json</c>. Each sidecar contains a
/// <see cref="TimelineBundle"/>. Failures are logged on the
/// <see cref="LogChannel.EncounterStore"/> channel and otherwise swallowed -
/// the caller falls back to a no-timeline view.
/// </summary>
public sealed class TimelineSidecarStore : SidecarStore<TimelineBundle>
{
    public TimelineSidecarStore(string configFilePath) : base(configFilePath, "timelines") { }

    public bool Save(TimelineBundle bundle) => Save(bundle.EncounterId, bundle);
}
