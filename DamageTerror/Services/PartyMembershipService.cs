using ECommons.PartyFunctions;

namespace DamageTerror.Services;

/// <summary>
/// Resolves party/alliance membership from ECommons UniversalParty and provides
/// name sets for group filtering. Caches results with a short TTL to avoid
/// querying the party list every frame.
/// </summary>
public sealed class PartyMembershipService
{
    private readonly object syncLock = new();
    private DateTime lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMilliseconds(250);

    private HashSet<string> cachedPartyNames = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> cachedAllianceNames = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns names of all players in the current party (including local player).
    /// Alliance members that are NOT in the local player's party group are excluded.
    /// </summary>
    public HashSet<string> GetPartyMemberNames()
    {
        Refresh();
        return cachedPartyNames;
    }

    /// <summary>
    /// Returns names of all players in the current alliance (all groups).
    /// In non-alliance content this is the same as party members.
    /// </summary>
    public HashSet<string> GetAllianceMemberNames()
    {
        Refresh();
        return cachedAllianceNames;
    }

    private void Refresh()
    {
        lock (syncLock)
        {
            var now = DateTime.UtcNow;
            if (now - lastRefresh < CacheTtl)
                return;
            lastRefresh = now;

            cachedPartyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            cachedAllianceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var members = UniversalParty.Members;
                foreach (var member in members)
                {
                    if (string.IsNullOrEmpty(member.Name))
                        continue;

                    // Store name in both "First Last" and "First Last@World" formats
                    // so we match regardless of how IINACT reports the name.
                    cachedAllianceNames.Add(member.Name);
                    var nameWithWorld = member.NameWithWorld;
                    if (!string.IsNullOrEmpty(nameWithWorld))
                        cachedAllianceNames.Add(nameWithWorld);
                }

                // In non-alliance content, all Members are the party.
                // In alliance content, we need to identify which members are in the
                // local player's party group vs other alliance groups.
                if (UniversalParty.IsAlliance)
                {
                    // UniversalParty.Members iterates all groups in alliance.
                    // For party-only filtering, we use Svc.Party which contains
                    // only the local player's party group, plus the local player themselves.
                    if (Player.Available && !string.IsNullOrEmpty(Player.Name))
                    {
                        cachedPartyNames.Add(Player.Name);
                        var worldName = Player.Object?.HomeWorld.ValueNullable?.Name.ToString();
                        if (!string.IsNullOrEmpty(worldName))
                            cachedPartyNames.Add($"{Player.Name}@{worldName}");
                    }

                    foreach (var pm in Svc.Party)
                    {
                        var name = pm.Name.ToString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            cachedPartyNames.Add(name);
                            var worldName = pm.World.ValueNullable?.Name.ToString();
                            if (!string.IsNullOrEmpty(worldName))
                                cachedPartyNames.Add($"{name}@{worldName}");
                        }
                    }
                }
                else
                {
                    // Not alliance — party = everyone returned by UniversalParty.Members
                    foreach (var name in cachedAllianceNames)
                        cachedPartyNames.Add(name);
                }
            }
            catch (Exception ex)
            {
                ServiceManager.PluginLog.Debug($"Party query failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Checks if a combatant name matches any name in the provided set.
    /// Handles cross-world name formats: tries the raw name, then strips
    /// a "@World" suffix if present, so both "Name" and "Name@World" match.
    /// </summary>
    public static bool MatchesName(HashSet<string> nameSet, string combatantName)
    {
        if (nameSet.Contains(combatantName))
            return true;

        var atIndex = combatantName.IndexOf('@');
        if (atIndex > 0)
            return nameSet.Contains(combatantName[..atIndex]);

        return false;
    }
}
