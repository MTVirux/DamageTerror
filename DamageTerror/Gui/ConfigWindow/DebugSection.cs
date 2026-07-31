namespace DamageTerror.Gui.ConfigWindow;

#if DEBUG

internal static class DebugSection
{
    private static readonly (LogChannel Channel, string Label, string Tooltip, LogChannel? Parent)[] Channels =
    {
        (LogChannel.Plugin,          "Plugin",            "Plugin lifecycle: startup, disposal, territory changes, commands", null),
        (LogChannel.SkillTracker,    "Skill Tracker",     "Skill and damage type lookups", null),
        (LogChannel.PetDebug,        "Pet Debug",         "Pet ownership resolution, pet skill accumulation, ground-effect entities", LogChannel.SkillTracker),
        (LogChannel.DoTDiag,         "DoT Diagnostics",   "DoT/HoT aggregate line processing and status matching", LogChannel.SkillTracker),
        (LogChannel.StatusTracker,   "Status Tracker",    "Status/buff/debuff classification", null),
        (LogChannel.PartyMembership, "Party Membership",  "Party and alliance member queries", null),
        (LogChannel.FontService,     "Font Service",      "Custom font loading, atlas creation, font push/pop", null),
        (LogChannel.EncounterStore,  "Encounter Store",   "Encounter history loading and saving", null),
        (LogChannel.GifAnimator,     "GIF Animator",      "GIF frame extraction and temp file cleanup", null),
        (LogChannel.DataService,     "Data Service",      "Combat data processing and dispatch", null),
        (LogChannel.WebSocket,       "WebSocket",         "WebSocket connection, reconnect, and message handling", null),
        (LogChannel.Ipc,             "IPC",               "IINACT IPC communication", null),
    };

    public static bool Draw(Configuration config)
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Debug UI Visibility##debugUIVis", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var hide = config.HideDebugFeatures;
            if (ImGui.Checkbox("Hide debug-only UI features##hideDebugUI", ref hide))
            {
                config.HideDebugFeatures = hide;
                changed = true;
            }
            ConfigHelpers.HelpMarker(
                "Hides debug-only buttons (Recalculate, Replay CombatData) and any other debug UI surfaces. " +
                "The Debug page itself stays visible so you can flip this back. Has no effect in release builds.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Raw Capture##debugCapture", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var captureRaw = config.CaptureRawFrames;
            if (ImGui.Checkbox("Capture raw frames##captureRawFrames", ref captureRaw))
            {
                config.CaptureRawFrames = captureRaw;
                changed = true;
            }
            ConfigHelpers.HelpMarker(
                "Stores every raw ACT log line and IINACT CombatData frame of an encounter in a sidecar file " +
                "next to the history, enabling the Recalculate and Replay CombatData buttons. Costs memory and " +
                "disk space per encounter, so it stays off unless you need offline reparsing. " +
                "Has no effect in release builds.");
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Log Channels##logCh"))
        {
            ImGui.TextWrapped("Enable or disable log output per service. Disabled channels will not write to the Dalamud log. Useful for silencing noisy services during testing.");
            ImGui.Spacing();

            if (ImGui.Button("Enable All##logch"))
            {
                if (config.DisabledLogChannels.Count > 0)
                {
                    config.DisabledLogChannels.Clear();
                    changed = true;
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Disable All##logch"))
            {
                foreach (var (channel, _, _, _) in Channels)
                {
                    if (config.DisabledLogChannels.Add(channel))
                        changed = true;
                }
            }

            ImGui.Separator();

            foreach (var (channel, label, tooltip, parent) in Channels)
            {
                if (parent.HasValue)
                    ImGui.Indent();

                var parentDisabled = parent.HasValue && config.DisabledLogChannels.Contains(parent.Value);
                if (parentDisabled)
                    ImGui.BeginDisabled();

                var enabled = !config.DisabledLogChannels.Contains(channel);
                if (ImGui.Checkbox($"{label}##logch_{channel}", ref enabled))
                {
                    if (enabled)
                        config.DisabledLogChannels.Remove(channel);
                    else
                        config.DisabledLogChannels.Add(channel);
                    changed = true;
                }

                ConfigHelpers.HelpMarker(tooltip);

                if (parentDisabled)
                    ImGui.EndDisabled();

                if (parent.HasValue)
                    ImGui.Unindent();
            }
        }

        return changed;
    }
}
#endif
