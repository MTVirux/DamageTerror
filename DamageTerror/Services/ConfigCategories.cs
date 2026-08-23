using System.Diagnostics;
using Newtonsoft.Json.Serialization;

namespace DamageTerror.Services;

public enum ConfigCategory
{
    General,
    Tabs,
    Layout,
    History,
    PartyListGeneral,
    PartyListBar,
    PartyListName,
    PartyListSlotNumber,
    PartyListGauges,
    PartyListMetrics,
    PartyListStatus,
    PartyListGlow,
    PartyListHeader,
    PartyListCastBar,
    PartyListCastName,
    PartyListPetTimer,
    WindowBackground,
    MeterBars,
    NameFormat,
    ValueFormatting,
    EncounterSelect,
    JobRoleColors,
    StatusBar,
    Tooltips,
    DetailsPanel,
    GraphView,
    Fonts,
    WindowState,
    Debug,
}

/// <summary>
/// Splits the config into the groups the settings sidebar uses, so an export can
/// carry a subset of them. Entries are the names Newtonsoft writes, not always the
/// C# ones, since it's the JSON that gets filtered; a dotted path reaches inside a
/// nested block like <c>PartyList</c>, which is split across several categories of
/// its own. Every setting has to land in exactly one category -
/// <see cref="CheckCoverageOrThrow"/> enforces that in Debug builds so a new one
/// can't silently become unexportable.
/// </summary>
public static class ConfigCategories
{
    /// <summary>
    /// Written by Newtonsoft but not settings: fields kept purely so an old config can
    /// still be migrated. They're read once on load and folded into the values above, so
    /// there's nothing to export and no category to put them in.
    /// </summary>
    private static readonly HashSet<string> LegacyProperties = new(StringComparer.Ordinal)
    {
        "PartyList.MetricSeparator",
        "PartyList.PrefixDps",
        "PartyList.PrefixDamage",
        "PartyList.PrefixCrit",
        "PartyList.PrefixDirectHit",
        "PartyList.PrefixCritDirectHit",
        "PartyList.PrefixDamagePercent",
        "PartyList.MetricOrder",
        "PartyList.MetricStyles",
        "PartyList.TotalsShowDuration",
        "PartyList.TotalsShowTitle",
        "PartyList.TotalsShowRaidDps",
        "PartyList.TotalsShowDamage",
        "PartyList.TotalsShowDeaths",
    };

    /// <summary>
    /// Stamped on every config and copied into every export whatever the selection:
    /// they say which schema and which game patch the data came from, so they belong
    /// to no single category and a partial import must never overwrite them.
    /// </summary>
    public static readonly string[] MetadataProperties =
    [
        nameof(Configuration.Version),
        nameof(Configuration.LastGameVersion),
    ];

    public const string AppearanceGroup = "Appearance";
    public const string PartyListGroup = "Party List";

    public sealed record CategoryInfo(
        ConfigCategory Category,
        string Label,
        string? Group,
        string Tooltip,
        bool SelectedByDefault,
        string[] Properties);

    public static readonly IReadOnlyList<CategoryInfo> All = new CategoryInfo[]
    {
        new(ConfigCategory.General, "General", null,
            "Connection, where the meter is allowed to show, combat and encounter behaviour.", true,
            [
                nameof(Configuration.WebSocketUrl),
                nameof(Configuration.PreferIpc),
                nameof(Configuration.ShowOnStart),
                nameof(Configuration.EnableInOverworld),
                nameof(Configuration.EnableInDungeons),
                nameof(Configuration.EnableInTrials),
                nameof(Configuration.EnableInRaids),
                nameof(Configuration.EnableInAllianceRaids),
                nameof(Configuration.EnableInDeepDungeons),
                nameof(Configuration.EnableInFieldOperations),
                nameof(Configuration.EnableInFieldRaids),
                nameof(Configuration.EnableInCriterion),
                nameof(Configuration.EnableInVariant),
                nameof(Configuration.EnableInPvP),
                nameof(Configuration.EnableReplays),
                nameof(Configuration.ModifierKeyCombo),
                nameof(Configuration.ModifierKeyMode),
                nameof(Configuration.HideOutOfCombat),
                nameof(Configuration.HideOutOfCombatDelay),
                nameof(Configuration.SkipZeroEdpsEncounters),
                nameof(Configuration.DotCalcMode),
                nameof(Configuration.EndEncounterMode),
                nameof(Configuration.IgnoreEscClose),
            ]),

        new(ConfigCategory.Tabs, "Tabs", null,
            "Your meter tabs - their columns, sorting and filters - plus the tab button styling.", true,
            [
                nameof(Configuration.ShowTabBar),
                nameof(Configuration.MeterTabs),
                nameof(Configuration.TabButtonColor),
                nameof(Configuration.TabButtonHoveredColor),
                nameof(Configuration.TabButtonActiveColor),
                nameof(Configuration.TabButtonTextColor),
                nameof(Configuration.TabButtonActiveTextColor),
                nameof(Configuration.TabButtonHeight),
                nameof(Configuration.TabButtonSpacing),
                nameof(Configuration.TabButtonRounding),
                nameof(Configuration.TabButtonWidth),
                nameof(Configuration.TabButtonFontSize),
                nameof(Configuration.TabButtonStretchToFit),
            ]),

        new(ConfigCategory.Layout, "Layout", null,
            "Which elements the meter window stacks, in what order, and what the modifier key hides.", true,
            [
                nameof(Configuration.Layout),
                nameof(Configuration.CtrlShiftOnlyElements),
                nameof(Configuration.ReplayBarPinned),
                nameof(Configuration.HideWindowHeader),
            ]),

        new(ConfigCategory.History, "History", null,
            "How many encounters and timelines are kept, and for how long.", true,
            [
                nameof(Configuration.HistoryLimitMode),
                nameof(Configuration.MaxEncounterHistory),
                nameof(Configuration.MaxEncounterHistoryDays),
                nameof(Configuration.TimelineRetentionMode),
                nameof(Configuration.MaxTimelineCount),
                nameof(Configuration.MaxTimelineDays),
            ]),

        new(ConfigCategory.PartyListGeneral, "General", PartyListGroup,
            "Whether the overlay runs at all, plus what applies to every row.", true,
            [
                nameof(Configuration.ShowPartyListDps),
                "PartyList.HideOutOfCombat",
                "PartyList.HideOutOfCombatDelay",
                "PartyList.TintTextOutline",
                "PartyList.TextOutlineTint",
                "PartyList.TextOutlineThickness",
                "PartyList.RowSpacing",
            ]),

        new(ConfigCategory.PartyListBar, "DPS Bar", PartyListGroup,
            "The fill bar drawn across each row - its size, placement and colours.", true,
            [
                "PartyList.ShowBar",
                "PartyList.IconUnderlap",
                "PartyList.BarHeightPixels",
                "PartyList.BarMinAlpha",
                "PartyList.BarColorMode",
                "PartyList.BarColors",
                "PartyList.BarSingleColor",
                "PartyList.BarMaxWidth",
                "PartyList.BarOffsetX",
                "PartyList.BarOffsetY",
                "PartyList.BarBehindRowContent",
                "PartyList.ShiftRowContent",
                "PartyList.RowContentShiftY",
            ]),

        new(ConfigCategory.PartyListName, "Name", PartyListGroup,
            "The player name - its font, level glyphs and position.", true,
            [
                "PartyList.AdjustNameFont",
                "PartyList.NameFontDelta",
                "PartyList.HideLevel",
                "PartyList.NameShift",
            ]),

        new(ConfigCategory.PartyListSlotNumber, "Slot Number", PartyListGroup,
            "The party slot number before each name, and the badge behind it.", true,
            [
                "PartyList.AdjustPartyIndex",
                "PartyList.PartyIndexFontDelta",
                "PartyList.PartyIndexOffsetX",
                "PartyList.PartyIndexOffsetY",
                "PartyList.PartyIndexUseCustomColor",
                "PartyList.PartyIndexColor",
                "PartyList.PartyIndexUseCustomOutlineColor",
                "PartyList.PartyIndexOutlineColor",
                "PartyList.PartyIndexOutlineThickness",
                "PartyList.PartyIndexFont",
                "PartyList.HidePartyIndex",
                "PartyList.PartyIndexBadge",
            ]),

        new(ConfigCategory.PartyListGauges, "HP and MP", PartyListGroup,
            "The HP and MP gauges, their shields, and the numbers drawn on them.", true,
            [
                "PartyList.HpBarOutline",
                "PartyList.MpBarOutline",
                "PartyList.HpBarShift",
                "PartyList.MpBarShift",
                "PartyList.ShieldFill",
                "PartyList.ShieldOverflow",
                "PartyList.HpNumbers",
                "PartyList.MpNumbers",
                "PartyList.AdjustGaugeNumbers",
                "PartyList.GaugeFontDelta",
                "PartyList.MpTrailingFontDelta",
                "PartyList.GaugeNumberOffsetY",
                "PartyList.TrailingDigitsOffsetX",
                "PartyList.TrailingDigitsOffsetY",
            ]),

        new(ConfigCategory.PartyListMetrics, "Individual Metrics", PartyListGroup,
            "The metrics drawn after each name - which ones, their labels and their styling.", true,
            [
                "PartyList.NameMetrics",
                "PartyList.MetricShowLabels",
                "PartyList.MetricLabels",
                "PartyList.MetricsFontDelta",
                "PartyList.MetricColumnStyles",
            ]),

        new(ConfigCategory.PartyListStatus, "Buffs and Debuffs", PartyListGroup,
            "Status icons and their timers.", true,
            [
                "PartyList.AdjustStatusIcons",
                "PartyList.StatusOffsetX",
                "PartyList.StatusOffsetY",
                "PartyList.StatusScale",
                "PartyList.StatusRightAlign",
                "PartyList.StatusTint",
                "PartyList.AdjustStatusTimers",
                "PartyList.StatusTimerFontDelta",
                "PartyList.StatusTimerOffsetX",
                "PartyList.StatusTimerOffsetY",
                "PartyList.StatusTimerUseCustomColor",
                "PartyList.StatusTimerColor",
            ]),

        new(ConfigCategory.PartyListGlow, "Hover and Selection", PartyListGroup,
            "The glow the game draws on a hovered or targeted row.", true,
            [
                "PartyList.AdjustSelectionGlow",
                "PartyList.SelectionOverridesHover",
                "PartyList.HoverOffsetX",
                "PartyList.HoverOffsetY",
                "PartyList.HoverScale",
                "PartyList.HoverTint",
                "PartyList.SelectionOffsetX",
                "PartyList.SelectionOffsetY",
                "PartyList.SelectionScale",
                "PartyList.SelectionTint",
                "PartyList.FreezeGlowTransform",
                "PartyList.IconGlowOffsetX",
                "PartyList.IconGlowOffsetY",
                "PartyList.IconGlowScale",
                "PartyList.IconGlowTint",
            ]),

        new(ConfigCategory.PartyListHeader, "Party Header", PartyListGroup,
            "The header above the list and the encounter totals drawn into it.", true,
            [
                "PartyList.HidePartyTypeLabel",
                "PartyList.ShowEncounterTotals",
                "PartyList.HeaderMetrics",
                "PartyList.TotalsShowLabels",
                "PartyList.HeaderMetricLabels",
                "PartyList.HeaderMetricStyles",
                "PartyList.TotalsHiddenText",
                "PartyList.AdjustTotalsText",
                "PartyList.TotalsFontDelta",
                "PartyList.TotalsOffsetX",
                "PartyList.TotalsOffsetY",
                "PartyList.TotalsUseCustomColor",
                "PartyList.TotalsColor",
            ]),

        new(ConfigCategory.PartyListCastBar, "Cast Bar", PartyListGroup,
            "The casting bar the game draws on a row.", true,
            [
                "PartyList.AdjustCastBar",
                "PartyList.CastBarShiftX",
                "PartyList.CastBarOffsetX",
                "PartyList.CastBarShiftY",
                "PartyList.CastBarScaleY",
                "PartyList.CastBarTint",
            ]),

        new(ConfigCategory.PartyListCastName, "Spell Name", PartyListGroup,
            "The spell name drawn beside the cast bar.", true,
            [
                "PartyList.AdjustCastName",
                "PartyList.CastNameOffsetX",
                "PartyList.CastNameOffsetY",
                "PartyList.CastNameFontDelta",
                "PartyList.CastNameUseCustomColor",
                "PartyList.CastNameColor",
            ]),

        new(ConfigCategory.PartyListPetTimer, "Pet Timer", PartyListGroup,
            "The companion timer drawn where a player's MP bar would be.", true,
            [
                "PartyList.AdjustPetTimer",
                "PartyList.PetTimerOffsetX",
                "PartyList.PetTimerOffsetY",
                "PartyList.PetTimerFontDelta",
                "PartyList.PetTimerUseCustomColor",
                "PartyList.HidePetTimerIcon",
                "PartyList.PetTimerColor",
            ]),

        new(ConfigCategory.WindowBackground, "Window & Background", AppearanceGroup,
            "Meter window background colour, background image and window padding.", true,
            [
                nameof(Configuration.WindowBackgroundColor),
                nameof(Configuration.BackgroundImagePath),
                nameof(Configuration.BackgroundImageOpacity),
                nameof(Configuration.BackgroundImageTint),
                nameof(Configuration.BackgroundImageScale),
                nameof(Configuration.WindowPaddingLeft),
                nameof(Configuration.WindowPaddingRight),
                nameof(Configuration.WindowPaddingTop),
                nameof(Configuration.WindowPaddingBottom),
            ]),

        new(ConfigCategory.MeterBars, "Meter Bars", AppearanceGroup,
            "Bar sizing, padding, job icons and the column header row.", true,
            [
                nameof(Configuration.BarHeight),
                nameof(Configuration.BarSpacing),
                nameof(Configuration.BarRounding),
                nameof(Configuration.BarAlpha),
                nameof(Configuration.BarFontSize),
                nameof(Configuration.BarLeftPadding),
                nameof(Configuration.BarRightPadding),
                nameof(Configuration.BarColumnSpacing),
                nameof(Configuration.IconSize),
                nameof(Configuration.IconTextPadding),
                nameof(Configuration.BarBackgroundColor),
                nameof(Configuration.NameTextColor),
                nameof(Configuration.ValueTextColor),
                nameof(Configuration.SelfBarHighlight),
                nameof(Configuration.SelfBarHighlightColor),
                nameof(Configuration.ShowJobIcons),
                nameof(Configuration.JobIconStyle),
                nameof(Configuration.CustomJobIcons),
                nameof(Configuration.ShowJobAbbrevOnBar),
                nameof(Configuration.ShowRankNumber),
                nameof(Configuration.ShowMeterHeader),
                nameof(Configuration.HeaderTextColor),
                nameof(Configuration.HeaderBackgroundColor),
                nameof(Configuration.HeaderHeight),
                nameof(Configuration.HeaderFontSize),
                nameof(Configuration.HeaderSeparator),
                nameof(Configuration.HeaderSeparatorColor),
            ]),

        new(ConfigCategory.NameFormat, "Name Format", AppearanceGroup,
            "How combatant names are written on the bars, including your own highlight colour.", true,
            [
                nameof(Configuration.ShowNameOnBar),
                nameof(Configuration.ShowYouOnBar),
                nameof(Configuration.SelfNameFormat),
                nameof(Configuration.OthersNameFormat),
                nameof(Configuration.NameTruncateLength),
                nameof(Configuration.UseSelfNameColor),
                nameof(Configuration.SelfNameColor),
            ]),

        new(ConfigCategory.ValueFormatting, "Value Formatting", AppearanceGroup,
            "Number abbreviation, decimal places and skill name truncation.", true,
            [
                nameof(Configuration.ValueDisplayFormat),
                nameof(Configuration.AbbreviatedDecimalPlaces),
                nameof(Configuration.RawDecimalPlaces),
                nameof(Configuration.PercentDecimalPlaces),
                nameof(Configuration.AbbreviatedKThreshold),
                nameof(Configuration.AbbreviatedMThreshold),
                nameof(Configuration.MaxHitSkillNameLength),
                nameof(Configuration.TruncateSkillNames),
            ]),

        new(ConfigCategory.EncounterSelect, "Encounter Select", AppearanceGroup,
            "The encounter picker bar above the meter.", true,
            [
                nameof(Configuration.SelectionBarTextColor),
                nameof(Configuration.SelectionBarBackgroundColor),
                nameof(Configuration.SelectionBarHeight),
                nameof(Configuration.ShowEncounterPicker),
                nameof(Configuration.ShowSelectionBarSeparator),
                nameof(Configuration.SelectionBarSeparatorColor),
            ]),

        new(ConfigCategory.JobRoleColors, "Job/Role Colors", AppearanceGroup,
            "Role colours and any per-job overrides.", true,
            [
                nameof(Configuration.UsePerJobColors),
                nameof(Configuration.JobColors),
                nameof(Configuration.TankColor),
                nameof(Configuration.HealerColor),
                nameof(Configuration.MeleeDpsColor),
                nameof(Configuration.RangedDpsColor),
                nameof(Configuration.CasterDpsColor),
                nameof(Configuration.LimitBreakColor),
                nameof(Configuration.DoHLColor),
                nameof(Configuration.DefaultJobColor),
            ]),

        new(ConfigCategory.StatusBar, "Encounter Status Bar", AppearanceGroup,
            "The encounter summary bar - which metrics it shows and how it looks.", true,
            [
                nameof(Configuration.ShowStatusBar),
                nameof(Configuration.ShowStatusBarTimer),
                nameof(Configuration.StatusBarMetrics),
                nameof(Configuration.StatusBarFontSize),
                nameof(Configuration.StatusBarHeight),
                nameof(Configuration.StatusBarPadding),
                nameof(Configuration.ShowStatusBarSeparator),
                nameof(Configuration.StatusBarSeparatorColor),
                nameof(Configuration.StatusBarBackgroundColor),
                nameof(Configuration.StatusBarActiveColor),
                nameof(Configuration.StatusBarInactiveColor),
                nameof(Configuration.StatusBarLabelColor),
            ]),

        new(ConfigCategory.Tooltips, "Tooltips", AppearanceGroup,
            "Hover tooltips - the fields they list and their styling.", true,
            [
                nameof(Configuration.ShowTooltip),
                nameof(Configuration.TooltipDelay),
                nameof(Configuration.TooltipFields),
                nameof(Configuration.TooltipTopSkillCount),
                nameof(Configuration.TooltipBackgroundColor),
                nameof(Configuration.TooltipTextColor),
                nameof(Configuration.TooltipLabelColor),
                nameof(Configuration.TooltipFontSize),
                nameof(Configuration.TooltipRounding),
                nameof(Configuration.TooltipPadding),
            ]),

        new(ConfigCategory.DetailsPanel, "Details Panel", AppearanceGroup,
            "The per-combatant panel: its tabs, skill and buff rows, and the graph inside it.", true,
            [
                nameof(Configuration.DetailBackgroundColor),
                nameof(Configuration.DetailLabelColor),
                nameof(Configuration.DetailIndent),
                nameof(Configuration.DetailFontSize),
                nameof(Configuration.DetailVisibleColumns),
                nameof(Configuration.DetailNewLineColumns),
                nameof(Configuration.DetailShowDetailsTab),
                nameof(Configuration.DetailShowSkillsTab),
                nameof(Configuration.DetailShowGraphTab),
                nameof(Configuration.DetailShowBuffsTab),
                nameof(Configuration.DetailShowItemTab),
                nameof(Configuration.DetailShowSkillBreakdown),
                nameof(Configuration.MaxSkillBreakdownCount),
                nameof(Configuration.DetailMarkers),
                nameof(Configuration.SkillDamageFillColor),
                nameof(Configuration.SkillPhysicalFillColor),
                nameof(Configuration.SkillMagicFillColor),
                nameof(Configuration.SkillHealingFillColor),
                nameof(Configuration.SkillRowBackgroundColor),
                nameof(Configuration.SkillTextColor),
                nameof(Configuration.SkillHeaderTextColor),
                nameof(Configuration.SkillRowHeight),
                nameof(Configuration.SkillColumnPadding),
                nameof(Configuration.SkillBarRounding),
                nameof(Configuration.SkillFontSize),
                nameof(Configuration.BuffFillColor),
                nameof(Configuration.DebuffFillColor),
                nameof(Configuration.BuffRowBackgroundColor),
                nameof(Configuration.BuffTextColor),
                nameof(Configuration.BuffHeaderTextColor),
                nameof(Configuration.BuffRowHeight),
                nameof(Configuration.BuffColumnPadding),
                nameof(Configuration.BuffBarRounding),
                nameof(Configuration.BuffFontSize),
                nameof(Configuration.GraphHeight),
                nameof(Configuration.GraphLineThickness),
                nameof(Configuration.GraphDpsColor),
                nameof(Configuration.GraphHpsColor),
                nameof(Configuration.GraphDtpsColor),
                nameof(Configuration.GraphBackgroundColor),
                nameof(Configuration.GraphGridColor),
                nameof(Configuration.GraphShowLegend),
                nameof(Configuration.GraphShowGrid),
                nameof(Configuration.GraphShowXAxisLabels),
                nameof(Configuration.GraphShowYAxisLabels),
                nameof(Configuration.GraphShowDps),
                nameof(Configuration.GraphShowHps),
                nameof(Configuration.GraphShowDtps),
                nameof(Configuration.GraphSmoothingWindow),
                nameof(Configuration.GraphUpdateInterval),
                nameof(Configuration.GraphShowLabels),
                nameof(Configuration.GraphLabelOffsetX),
                nameof(Configuration.GraphLabelOffsetY),
                nameof(Configuration.GraphMouseTextOpacity),
                nameof(Configuration.GraphYAxisHeadroom),
                nameof(Configuration.GraphYAxisTickCount),
                nameof(Configuration.GraphXAxisPadding),
                nameof(Configuration.GraphXAxisMinSec),
                nameof(Configuration.GraphAutoScroll),
                nameof(Configuration.GraphAutoScrollWindow),
                nameof(Configuration.GraphAutoScrollSmoothing),
                nameof(Configuration.GraphFontSize),
            ]),

        new(ConfigCategory.GraphView, "Graph View", AppearanceGroup,
            "The full-size graph layout element.", true,
            [
                nameof(Configuration.GraphViewAutoHeight),
                nameof(Configuration.GraphViewHeight),
                nameof(Configuration.GraphViewLineThickness),
                nameof(Configuration.GraphViewBackgroundColor),
                nameof(Configuration.GraphViewGridColor),
                nameof(Configuration.GraphViewSmoothingWindow),
                nameof(Configuration.GraphViewUpdateInterval),
                nameof(Configuration.GraphViewShowLegend),
                nameof(Configuration.GraphViewShowGrid),
                nameof(Configuration.GraphViewShowXAxisLabels),
                nameof(Configuration.GraphViewShowYAxisLabels),
                nameof(Configuration.GraphViewHighlightSelf),
                nameof(Configuration.GraphViewSelfLineThickness),
                nameof(Configuration.GraphViewShowLabels),
                nameof(Configuration.GraphViewLabelOffsetX),
                nameof(Configuration.GraphViewLabelOffsetY),
                nameof(Configuration.GraphViewFontSize),
                nameof(Configuration.GraphViewXAxisPadding),
                nameof(Configuration.GraphViewXAxisMinSec),
                nameof(Configuration.GraphViewAutoScroll),
                nameof(Configuration.GraphViewAutoScrollWindow),
                nameof(Configuration.GraphViewAutoScrollSmoothing),
                nameof(Configuration.GraphViewYAxisHeadroom),
                nameof(Configuration.GraphViewYAxisTickCount),
                nameof(Configuration.GraphViewMouseTextOpacity),
                nameof(Configuration.GraphViewMarkers),
            ]),

        new(ConfigCategory.Fonts, "Fonts", AppearanceGroup,
            "The custom font. The file has to exist on the machine importing it.", true,
            [
                nameof(Configuration.EnableCustomFont),
                nameof(Configuration.CustomFontPath),
                nameof(Configuration.CustomFontIndex),
                nameof(Configuration.CustomFontSizePt),
                nameof(Configuration.CustomFontDisplayName),
                nameof(Configuration.CustomFontSpecJson),
            ]),

        new(ConfigCategory.WindowState, "Window positions & state", null,
            "Window positions, sizes, pins, popouts and which wizards you've finished. Rarely worth sharing.", false,
            [
                nameof(Configuration.PinMainWindow),
                nameof(Configuration.MainWindowPos),
                nameof(Configuration.MainWindowSize),
                nameof(Configuration.PinConfigWindow),
                nameof(Configuration.ConfigWindowPos),
                nameof(Configuration.ConfigWindowSize),
                nameof(Configuration.ConfigSidebarWidth),
                nameof(Configuration.SelectedMeterTab),
                nameof(Configuration.PopoutTabIds),
                nameof(Configuration.PopoutWindowPins),
                nameof(Configuration.DetailExpandedSections),
                nameof(Configuration.HasCompletedSetup),
                nameof(Configuration.HasCompletedCustomizationWizard),
                nameof(Configuration.HasCompletedColumnWizard),
            ]),

#if DEBUG
        new(ConfigCategory.Debug, "Debug", null,
            "Debug features, raw frame capture and log channel filtering.", true,
            [
                nameof(Configuration.HideDebugFeatures),
                nameof(Configuration.CaptureRawFrames),
                nameof(Configuration.DisabledLogChannels),
            ]),
#endif
    };

    private static readonly Lazy<Dictionary<string, ConfigCategory>> byPath = new(() =>
    {
        var map = new Dictionary<string, ConfigCategory>(StringComparer.Ordinal);
        foreach (var info in All)
            foreach (var path in info.Properties)
                map[path] = info.Category;
        return map;
    });

    private static readonly Lazy<HashSet<string>> containers = new(() =>
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var info in All)
            foreach (var path in info.Properties)
                foreach (var ancestor in Ancestors(path))
                    result.Add(ancestor);
        return result;
    });

    /// <summary>Every path above this one, e.g. <c>a.b.c</c> yields <c>a.b</c> then <c>a</c>.</summary>
    public static IEnumerable<string> Ancestors(string path)
    {
        for (var cut = path.LastIndexOf('.'); cut > 0; cut = path.LastIndexOf('.'))
        {
            path = path[..cut];
            yield return path;
        }
    }

    /// <summary>True for a block that categories reach inside of rather than take whole.</summary>
    public static bool IsContainer(string path) => containers.Value.Contains(path);

    public static IEnumerable<ConfigCategory> DefaultSelection
        => All.Where(c => c.SelectedByDefault).Select(c => c.Category);

    public static CategoryInfo? Get(ConfigCategory category) => All.FirstOrDefault(c => c.Category == category);

    /// <summary>Falls back to the raw name for a category this build doesn't offer (a Debug-build export read by a release build).</summary>
    public static string Label(ConfigCategory category) => Get(category)?.Label ?? category.ToString();

    /// <summary>Category a config path belongs to, or null for metadata, legacy and unknown keys.</summary>
    public static ConfigCategory? Of(string path)
        => byPath.Value.TryGetValue(path, out var category) ? category : null;

    /// <summary>Paths covered by the given categories, plus the metadata stamps.</summary>
    public static HashSet<string> PropertiesFor(IEnumerable<ConfigCategory> categories)
    {
        var selected = new HashSet<ConfigCategory>(categories);
        var result = new HashSet<string>(MetadataProperties, StringComparer.Ordinal);
        foreach (var info in All)
        {
            if (!selected.Contains(info.Category))
                continue;
            foreach (var prop in info.Properties)
                result.Add(prop);
        }
        return result;
    }

    /// <summary>
    /// DEBUG-only guard: every path Newtonsoft writes has to be listed in exactly one
    /// category, and every listed path has to still exist. Throws listing whatever drifted.
    /// </summary>
    [Conditional("DEBUG")]
    public static void CheckCoverageOrThrow(IPluginLog log)
    {
        var expected = new HashSet<string>(SerializedPaths(typeof(Configuration), string.Empty), StringComparer.Ordinal);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicated = new List<string>();
        var unknown = new List<string>();

        foreach (var info in All)
        {
            foreach (var path in info.Properties)
            {
                if (!expected.Contains(path))
                    unknown.Add($"{info.Label}.{path}");
                else if (!seen.Add(path))
                    duplicated.Add(path);
            }
        }

        var uncategorized = expected.Where(p => !seen.Contains(p)).ToList();

        var problems = new List<string>();
        if (uncategorized.Count > 0)
            problems.Add("not in any category: " + string.Join(", ", uncategorized));
        if (duplicated.Count > 0)
            problems.Add("in more than one category: " + string.Join(", ", duplicated));
        if (unknown.Count > 0)
            problems.Add("no such config path: " + string.Join(", ", unknown));

        if (problems.Count > 0)
            throw new InvalidOperationException("ConfigCategories drift - " + string.Join("; ", problems));

        log.Debug($"ConfigCategories: {seen.Count} paths across {All.Count} categories");
    }

    /// <summary>
    /// Paths Newtonsoft writes for a type, minus metadata and legacy keys, descending into
    /// any block the categories split. Asks the serializer rather than reflecting directly,
    /// so a <c>[JsonProperty("Other")]</c> rename or a serialized private field is seen the
    /// way the config file sees it.
    /// </summary>
    private static IEnumerable<string> SerializedPaths(Type type, string prefix)
    {
        var contract = (JsonObjectContract)resolver.ResolveContract(type);
        foreach (var property in contract.Properties)
        {
            if (property.Ignored || property.PropertyName == null || property.PropertyType == null)
                continue;

            var path = prefix.Length == 0 ? property.PropertyName : prefix + "." + property.PropertyName;
            if (MetadataProperties.Contains(path) || LegacyProperties.Contains(path))
                continue;

            if (IsContainer(path))
            {
                foreach (var nested in SerializedPaths(property.PropertyType, path))
                    yield return nested;
            }
            else
            {
                yield return path;
            }
        }
    }

    private static readonly IContractResolver resolver = new DefaultContractResolver();
}
