namespace DamageTerror.Gui.MainWindow.Detail;

internal sealed class ItemsTabRenderer : IDetailTabRenderer
{
    private readonly Configuration config;
    private readonly SkillTracker skillTracker;
    private readonly DetailPanelState state;

    public ItemsTabRenderer(Configuration config, SkillTracker skillTracker, DetailPanelState state)
    {
        this.config = config;
        this.skillTracker = skillTracker;
        this.state = state;
    }

    public void Render(in DetailRenderContext ctx)
    {
        var combatant = ctx.Combatant;
        var index = ctx.Index;
        var isLive = ctx.IsLive;
        var currentSnapshot = ctx.Snapshot;

        List<SkillUseEvent> items;

        if (isLive)
        {
            items = skillTracker.GetItemEvents(combatant.Name);

            if (items.Count == 0
                && currentSnapshot?.ItemEvents != null
                && currentSnapshot.ItemEvents.TryGetValue(combatant.Name, out var fallback))
                items = fallback;
        }
        else
        {
            items = currentSnapshot?.ItemEvents != null
                && currentSnapshot.ItemEvents.TryGetValue(combatant.Name, out var saved)
                ? saved : [];
        }

        if (items.Count == 0)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("No item usage recorded.");
            ImGui.Spacing();
            return;
        }

        var sorted = items.OrderBy(e => e.TimeSec).ToList();

        if (ImGui.BeginTable($"##itemTable_{index}", 2,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.None, 0.65f);
            ImGui.TableSetupColumn("Timestamp", ImGuiTableColumnFlags.None, 0.35f);
            ImGui.TableHeadersRow();

            foreach (var evt in sorted)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var displayName = ResolveItemName(evt.SkillName);
                ImGui.TextUnformatted(displayName);

                ImGui.TableNextColumn();
                var min = (int)(evt.TimeSec / 60);
                var sec = (int)(evt.TimeSec % 60);
                ImGui.TextUnformatted($"{min}:{sec:00}");
            }

            ImGui.EndTable();
        }
    }

    private string ResolveItemName(string skillName)
    {
        if (!skillName.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
            return skillName;

        var idStr = skillName[5..];
        if (!uint.TryParse(idStr, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out var itemId))
            return idStr;

        if (state.ItemNameCache.TryGetValue(itemId, out var cached))
            return cached;

        var baseId = itemId;
        bool isHq = false;
        if (baseId >= 1_000_000)
        {
            baseId -= 1_000_000;
            isHq = true;
        }
        else if (baseId >= 500_000)
        {
            baseId -= 500_000;
        }

        string resolved = idStr;
        try
        {
            var sheet = ServiceManager.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Item>();
            if (sheet != null)
            {
                var row = sheet.GetRowOrDefault(baseId);
                if (row.HasValue)
                {
                    var name = row.Value.Name.ToString();
                    if (!string.IsNullOrEmpty(name))
                        resolved = isHq ? $"{name} (HQ)" : name;
                }
            }
        }
        catch { }

        state.ItemNameCache[itemId] = resolved;
        return resolved;
    }
}
