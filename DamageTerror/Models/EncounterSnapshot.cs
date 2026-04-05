using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DamageTerror.Models;

public class EncounterSnapshot
{
    public CombatEncounter Encounter { get; set; } = new();

    public List<CombatantEntry> Combatants { get; set; } = new();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>Per-combatant graph samples, keyed by name. Populated on encounter archive.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, List<GraphSample>> GraphData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant timestamped skill use events, keyed by name. Populated on encounter archive.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, List<SkillUseEvent>> SkillEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant timestamped damage-taken events (enemy skills hitting a player), keyed by target name.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, List<SkillUseEvent>> DamageTakenEvents { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant status application history (statuses applied BY this combatant), keyed by source name.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, List<StatusApplication>> StatusHistory { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Per-combatant status received history (statuses applied TO this combatant), keyed by target name.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, List<StatusApplication>> StatusesReceived { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Rebuild dictionaries with case-insensitive comparers after JSON deserialization,
    /// since Newtonsoft.Json creates them with the default (case-sensitive) comparer.
    /// </summary>
    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        if (GraphData.Count > 0 && GraphData.Comparer != StringComparer.OrdinalIgnoreCase)
            GraphData = new Dictionary<string, List<GraphSample>>(GraphData, StringComparer.OrdinalIgnoreCase);
        if (SkillEvents.Count > 0 && SkillEvents.Comparer != StringComparer.OrdinalIgnoreCase)
            SkillEvents = new Dictionary<string, List<SkillUseEvent>>(SkillEvents, StringComparer.OrdinalIgnoreCase);
        if (DamageTakenEvents.Count > 0 && DamageTakenEvents.Comparer != StringComparer.OrdinalIgnoreCase)
            DamageTakenEvents = new Dictionary<string, List<SkillUseEvent>>(DamageTakenEvents, StringComparer.OrdinalIgnoreCase);
        if (StatusHistory is null)
            StatusHistory = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);
        else if (StatusHistory.Count > 0 && StatusHistory.Comparer != StringComparer.OrdinalIgnoreCase)
            StatusHistory = new Dictionary<string, List<StatusApplication>>(StatusHistory, StringComparer.OrdinalIgnoreCase);
        if (StatusesReceived is null)
            StatusesReceived = new Dictionary<string, List<StatusApplication>>(StringComparer.OrdinalIgnoreCase);
        else if (StatusesReceived.Count > 0 && StatusesReceived.Comparer != StringComparer.OrdinalIgnoreCase)
            StatusesReceived = new Dictionary<string, List<StatusApplication>>(StatusesReceived, StringComparer.OrdinalIgnoreCase);
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
}
