using System.Diagnostics;
using System.Reflection;

namespace DamageTerror.Services;

public enum ConfigCategory
{
    General,
    Tabs,
    Layout,
    PartyList,
    History,
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
/// Splits <see cref="Configuration"/>'s properties into the groups the settings
/// sidebar uses, so an export can carry a subset of them. Every property has to
/// land in exactly one category - <see cref="CheckCoverageOrThrow"/> enforces
/// that in Debug builds so a new setting can't silently become unexportable.
/// </summary>
public static class ConfigCategories
{
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

        new(ConfigCategory.PartyList, "Party List", null,
            "The native party list overlay and everything that styles it.", true,
            [
                nameof(Configuration.ShowPartyListDps),
                nameof(Configuration.PartyList),
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

    private static readonly Lazy<Dictionary<string, ConfigCategory>> byProperty = new(() =>
    {
        var map = new Dictionary<string, ConfigCategory>(StringComparer.Ordinal);
        foreach (var info in All)
            foreach (var prop in info.Properties)
                map[prop] = info.Category;
        return map;
    });

    public static IEnumerable<ConfigCategory> DefaultSelection
        => All.Where(c => c.SelectedByDefault).Select(c => c.Category);

    public static CategoryInfo? Get(ConfigCategory category) => All.FirstOrDefault(c => c.Category == category);

    /// <summary>Falls back to the raw name for a category this build doesn't offer (a Debug-build export read by a release build).</summary>
    public static string Label(ConfigCategory category) => Get(category)?.Label ?? category.ToString();

    /// <summary>Category a config property belongs to, or null for metadata and unknown keys.</summary>
    public static ConfigCategory? Of(string propertyName)
        => byProperty.Value.TryGetValue(propertyName, out var category) ? category : null;

    /// <summary>Property names covered by the given categories, plus <c>Version</c>.</summary>
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
    /// DEBUG-only guard: every serialized <see cref="Configuration"/> property must be
    /// listed in exactly one category, and every listed name must still exist. Throws
    /// listing whatever drifted.
    /// </summary>
    [Conditional("DEBUG")]
    public static void CheckCoverageOrThrow(IPluginLog log)
    {
        var configProps = SerializedPropertyNames();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicated = new List<string>();
        var unknown = new List<string>();

        foreach (var info in All)
        {
            foreach (var prop in info.Properties)
            {
                if (!configProps.Contains(prop))
                    unknown.Add($"{info.Label}.{prop}");
                else if (!seen.Add(prop))
                    duplicated.Add(prop);
            }
        }

        var uncategorized = configProps.Where(p => !seen.Contains(p)).ToList();

        var problems = new List<string>();
        if (uncategorized.Count > 0)
            problems.Add("not in any category: " + string.Join(", ", uncategorized));
        if (duplicated.Count > 0)
            problems.Add("in more than one category: " + string.Join(", ", duplicated));
        if (unknown.Count > 0)
            problems.Add("no such Configuration property: " + string.Join(", ", unknown));

        if (problems.Count > 0)
            throw new InvalidOperationException("ConfigCategories drift - " + string.Join("; ", problems));

        log.Debug($"ConfigCategories: {seen.Count} properties across {All.Count} categories");
    }

    /// <summary>Public read/write properties Newtonsoft writes to the config file, minus <c>Version</c>.</summary>
    private static HashSet<string> SerializedPropertyNames()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prop in typeof(Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
            if (MetadataProperties.Contains(prop.Name)) continue;
            result.Add(prop.Name);
        }
        return result;
    }
}
