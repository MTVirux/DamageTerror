using System.Runtime.Serialization;

namespace DamageTerror.Models;

public sealed class EncounterSnapshot
{
    public CombatEncounter Encounter { get; set; } = new();

    public List<CombatantEntry> Combatants { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Stable identifier for this encounter, used as the sidecar filename.
    /// Set to <c>Timestamp.UtcTicks</c> on archive if zero.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public long Id { get; set; }

    /// <summary>True if this encounter has a timeline sidecar file on disk.
    /// Persisted with the summary so the picker can offer Replay without disk I/O.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool HasTimeline { get; set; }

    /// <summary>True if this encounter has a raw capture sidecar file on disk.
    /// Persisted with the summary so the debug tools can offer replay without disk I/O.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public bool HasRawCapture { get; set; }

    /// <summary>Per-combatant graph samples, keyed by name. Populated on encounter archive.</summary>
    [JsonIgnore]
    public Dictionary<string, List<GraphSample>> GraphData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant timestamped skill use events, keyed by name. Populated on encounter archive.</summary>
    [JsonIgnore]
    public Dictionary<string, List<SkillUseEvent>> SkillEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant timestamped damage-taken events (enemy skills hitting a player), keyed by target name.</summary>
    [JsonIgnore]
    public Dictionary<string, List<SkillUseEvent>> DamageTakenEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant timestamped item use events, keyed by name. Populated on encounter archive.</summary>
    [JsonIgnore]
    public Dictionary<string, List<SkillUseEvent>> ItemEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant status application history (statuses applied BY this combatant), keyed by source name.</summary>
    [JsonIgnore]
    public Dictionary<string, List<StatusApplication>> StatusHistory { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant status received history (statuses applied TO this combatant), keyed by target name.</summary>
    [JsonIgnore]
    public Dictionary<string, List<StatusApplication>> StatusesReceived { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [Newtonsoft.Json.JsonIgnore]
    internal bool TimelineLoaded { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    internal bool RawCaptureLoaded { get; set; }

    /// <summary>Raw ACT network log lines for the encounter. Populated from imported data or live capture.
    /// Persisted in the raw capture sidecar, not with the summary.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<string> RawLogLines { get; set; } = new();

    /// <summary>Raw IINACT CombatData JSON frames captured during the encounter. Debug-only; used for offline replay
    /// through the parser pipeline. Persisted in the raw capture sidecar, not with the summary.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<string> RawCombatDataFrames { get; set; } = new();

    // Raw capture lives in its own sidecar; deserialization still populates these so
    // pre-split files and older exports migrate on load.
    public bool ShouldSerializeRawLogLines() => false;
    public bool ShouldSerializeRawCombatDataFrames() => false;

    /// <summary>
    /// Rebuild dictionaries with case-insensitive comparers after JSON deserialization,
    /// since Newtonsoft.Json creates them with the default (case-sensitive) comparer,
    /// and restamp the encounter name that the combatants are not stored with.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        GraphData = DictionaryHelpers.EnsureCaseInsensitive(GraphData);
        SkillEvents = DictionaryHelpers.EnsureCaseInsensitive(SkillEvents);
        DamageTakenEvents = DictionaryHelpers.EnsureCaseInsensitive(DamageTakenEvents);
        ItemEvents = DictionaryHelpers.EnsureCaseInsensitive(ItemEvents);
        StatusHistory = DictionaryHelpers.EnsureCaseInsensitive(StatusHistory);
        StatusesReceived = DictionaryHelpers.EnsureCaseInsensitive(StatusesReceived);

        if (Combatants != null)
            foreach (var c in Combatants)
                c.EncounterName = Encounter.Title;
    }

    /// <summary>
    /// Verify encounter data integrity and synthesize missing SkillEvents from
    /// per-combatant Skills data so that skill markers appear on historical graphs.
    /// Returns true if any data was repaired.
    /// </summary>
    public bool ValidateAndRepair()
    {
        if (Combatants == null || Combatants.Count == 0)
            return false;

        var repaired = false;
        var duration = DurationHelper.ParseDuration(Encounter.Duration, 60f);

        foreach (var c in Combatants)
        {
            if (SkillEvents.TryGetValue(c.Name, out var existing) && existing.Count > 0)
                continue;

            var synthesized = SynthesizeEvents(c.Skills, c.HealingSkills, duration);
            if (synthesized.Count > 0)
            {
                SkillEvents[c.Name] = synthesized;
                repaired = true;
            }
        }

        return repaired;
    }

    private static List<SkillUseEvent> SynthesizeEvents(
        List<SkillEntry> skills, List<SkillEntry> healingSkills, float duration)
    {
        var events = new List<SkillUseEvent>();
        AddSkillEvents(events, skills, isHeal: false, duration);
        AddSkillEvents(events, healingSkills, isHeal: true, duration);
        events.Sort((a, b) => a.TimeSec.CompareTo(b.TimeSec));
        return events;
    }

    private static void AddSkillEvents(
        List<SkillUseEvent> events, List<SkillEntry> skills, bool isHeal, float duration)
    {
        if (skills == null) return;

        foreach (var skill in skills)
        {
            if (skill.HitCount <= 0) continue;

            var avgAmount = skill.TotalDamage / skill.HitCount;
            var critRate = skill.CritPct / 100.0;
            var dhRate = skill.DirectHitPct / 100.0;
            var cdhRate = skill.CritDirectHitPct / 100.0;
            var interval = duration / (skill.HitCount + 1);

            for (int i = 0; i < skill.HitCount; i++)
            {
                // Distribute crits/DH proportionally across the hits
                var hitFraction = (double)(i + 1) / skill.HitCount;
                var isCdh = hitFraction <= cdhRate;
                var isCrit = !isCdh && hitFraction <= critRate;
                var isDh = !isCdh && !isCrit && hitFraction <= dhRate;

                events.Add(new SkillUseEvent
                {
                    TimeSec = interval * (i + 1),
                    SkillName = skill.Name,
                    Amount = avgAmount,
                    IsHeal = isHeal,
                    IsCrit = isCdh || isCrit,
                    IsDirectHit = isCdh || isDh,
                });
            }
        }
    }

    /// <summary>
    /// Resets all combat-state fields (live numbers) on this snapshot and its
    /// combatants to zero / default. Used at replay start. Preserves metadata
    /// (encounter title, combatants, recorded skills/statuses) — only zeros
    /// numeric live-stats fields.
    /// </summary>
    public void ResetCombatStateForReplay()
    {
        Encounter.IsActive = true;
        Encounter.Duration = "00:00";
        Encounter.TotalDamage = 0;
        Encounter.TotalHealed = 0;
        Encounter.EncDps = 0;
        Encounter.EncHps = 0;
        Encounter.Kills = 0;
        Encounter.Deaths = 0;

        foreach (var c in Combatants)
            c.ResetCombatStateForReplay();

        SkillEvents = new Dictionary<string, List<SkillUseEvent>>(StringComparer.OrdinalIgnoreCase);
        DamageTakenEvents = new Dictionary<string, List<SkillUseEvent>>(StringComparer.OrdinalIgnoreCase);
        ItemEvents = new Dictionary<string, List<SkillUseEvent>>(StringComparer.OrdinalIgnoreCase);
        GraphData = new Dictionary<string, List<GraphSample>>(StringComparer.OrdinalIgnoreCase);
        StatusHistory = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);
        StatusesReceived = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);

        RawLogLines = new List<string>();
        RawCombatDataFrames = new List<string>();
        HasRawCapture = false;
        RawCaptureLoaded = false;
    }
}
