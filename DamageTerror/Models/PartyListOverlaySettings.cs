using System.Runtime.Serialization;

namespace DamageTerror.Models;

/// <summary>
/// Tunables for the native party list integration. Every value is read fresh each frame,
/// so edits apply immediately without reloading the addon.
/// Defaults are the values arrived at by eye in-game.
/// </summary>
public sealed class PartyListOverlaySettings
{
    /// <summary>
    /// Hides everything derived from parse data - the fill bars, the individual metrics and the
    /// header totals - once an encounter has been over for
    /// <see cref="HideOutOfCombatDelay"/> seconds. The restyle stays applied either way, so
    /// the rows don't jump layout on pull. Separate from the meter window's own setting.
    /// </summary>
    public bool HideOutOfCombat { get; set; } = true;
    public float HideOutOfCombatDelay { get; set; } = 5f;

    /// <summary>
    /// Takes over the outline the game draws around every glyph, giving it
    /// <see cref="TextOutlineTint"/> and <see cref="TextOutlineThickness"/>. Off leaves the
    /// game's own edge, which stays dark under a recoloured name. Applies to every party
    /// list text the overlay styles, whether or not that text has a colour of its own.
    /// </summary>
    public bool TintTextOutline { get; set; } = true;

    /// <summary>
    /// The outline colour. Black is the game's own look; a lighter one reads as a glow.
    /// Alpha is unused - the game's own edge alpha is kept so fading text still fades.
    /// </summary>
    public Vector4 TextOutlineTint { get; set; } = new(0f, 0f, 0f, 1f);

    /// <summary>How heavy the outline is drawn.</summary>
    public PartyListOutlineThickness TextOutlineThickness { get; set; } = PartyListOutlineThickness.Thin;

    /// <summary>
    /// Extra pixels of gap between rows, on top of the spacing the game uses. Each row is
    /// pushed down by its index times this, the chocobo and pet rows below the party by the
    /// whole stack, and the backdrop is grown to match so it still covers them.
    /// </summary>
    public float RowSpacing { get; set; } = 0f;

    // DPS fill bar
    public bool ShowBar { get; set; } = true;
    public float IconUnderlap { get; set; } = 7f;
    /// <summary>Bar height in pixels, centred on the job icon.</summary>
    public float BarHeightPixels { get; set; } = 14f;

    /// <summary>
    /// Opacity of the fill, used directly rather than as a floor under the meter's own
    /// alpha - as a floor, lowering it below the meter's value did nothing.
    /// </summary>
    [JsonProperty("BarMinAlpha")]
    public float BarOpacity { get; set; } = 0.75f;

    /// <summary>
    /// Where a row's fill colour comes from - the party list's own palette below, or the
    /// meter window's, so the two agree without having to be kept in step by hand.
    /// </summary>
    public PartyListBarColorMode BarColorMode { get; set; } = PartyListBarColorMode.OwnPalette;

    /// <summary>The party list's own job and role colours, used by <see cref="PartyListBarColorMode.OwnPalette"/>.</summary>
    public JobColorPalette BarColors { get; set; } = new() { UsePerJobColors = false };

    /// <summary>One colour for every row, used by <see cref="PartyListBarColorMode.SingleColor"/>.</summary>
    public Vector4 BarSingleColor { get; set; } = new(0.30f, 0.55f, 0.90f, 1f);

    /// <summary>
    /// Caps the width a 100% bar draws to. Everything scales inside it, so the bars stay
    /// proportional to each other instead of the longest one being clipped. 0 uses the
    /// space available in the row.
    /// </summary>
    public float BarMaxWidth { get; set; } = 400f;
    public float BarOffsetX { get; set; } = 24f;
    public float BarOffsetY { get; set; } = -6f;

    /// <summary>
    /// Draws the fill behind the name, gauges and status icons by putting it in a container
    /// that sits right after the party list's backdrop rather than at the end of the tree.
    /// Off leaves it on top of the row.
    /// </summary>
    public bool BarBehindRowContent { get; set; } = true;

    /// <summary>
    /// The row-wide shift, from before each part was moved on its own node. Kept only to
    /// seed the per-part values below for configs written back then.
    /// </summary>
    public bool ShiftRowContent { get; set; } = true;
    public float RowContentShiftY { get; set; } = -5f;

    [JsonProperty("NameShift")] private RowPartStyle? nameShift = new() { UseCustomColor = true };

    [JsonProperty("HpBarShift")] private RowPartStyle? hpBarShift =
        new() { UseCustomColor = true, Color = new(0.51f, 0.84f, 0.38f, 1f) };

    [JsonProperty("MpBarShift")] private RowPartStyle? mpBarShift =
        new() { OffsetY = -4f, UseCustomColor = true, Color = new(0.75f, 0.55f, 1f, 1f) };

    /// <summary>
    /// Seeded off by default for a config written before the name could be moved on its own:
    /// the container the row shift moved held the gauges but not the name, so it stayed put.
    /// </summary>
    [JsonIgnore] public RowPartStyle NameShift => nameShift ??= new RowPartStyle { Enabled = false, OffsetY = RowContentShiftY };

    [JsonIgnore] public RowPartStyle HpBarShift => hpBarShift ??= LegacyShift();
    [JsonIgnore] public RowPartStyle MpBarShift => mpBarShift ??= LegacyShift();

    private RowPartStyle LegacyShift() => new() { Enabled = ShiftRowContent, OffsetY = RowContentShiftY };

    /// <summary>
    /// The outline around each gauge - the game's empty-bar artwork, which the fill is drawn
    /// over. It is part of the bar's own texture rather than a node of its own, so it takes a
    /// tint and a fade instead of a width. Both follow their bar out of the box, which is
    /// what the backdrop did before it was styled on its own.
    /// </summary>
    public GaugeOutlineStyle HpBarOutline { get; set; } = new();

    public GaugeOutlineStyle MpBarOutline { get; set; } = new();

    /// <summary>
    /// The shield the game draws over the HP bar. It lives inside the HP gauge, so before
    /// these it silently took the HP bar's move, scale and tint.
    /// </summary>
    public ShieldStyle ShieldFill { get; set; } = new();

    /// <summary>
    /// The second bar the game shows above the HP bar for the part of a shield that doesn't
    /// fit inside it, together with the "too big to draw" icon that appears on it.
    /// </summary>
    public ShieldStyle ShieldOverflow { get; set; } = new();

    /// <summary>The slot number drawn before each name, which is a node of its own.</summary>
    public bool AdjustPartyIndex { get; set; } = false;
    public int PartyIndexFontDelta { get; set; } = -2;
    public float PartyIndexOffsetX { get; set; } = 0f;
    public float PartyIndexOffsetY { get; set; } = -4f;

    /// <summary>The number's own colour. Never the name's - the two are separate nodes.</summary>
    public bool PartyIndexUseCustomColor { get; set; } = false;
    public Vector4 PartyIndexColor { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>The number's own outline, which wins over the party list wide tint.</summary>
    public bool PartyIndexUseCustomOutlineColor { get; set; } = false;
    public Vector4 PartyIndexOutlineColor { get; set; } = new(0f, 0f, 0f, 1f);
    public PartyListOutlineThickness PartyIndexOutlineThickness { get; set; } = PartyListOutlineThickness.Thin;

    /// <summary>The face the number is drawn in, which the game picks per addon rather than per node.</summary>
    public PartyListFont PartyIndexFont { get; set; } = PartyListFont.Game;

    /// <summary>Fades the number out, leaving everything the game lays out around it in place.</summary>
    public bool HidePartyIndex { get; set; } = false;

    /// <summary>The plate we draw behind the number.</summary>
    public PartyIndexBadgeStyle PartyIndexBadge { get; set; } = new();

    // Player name font. Delta rather than absolute, so it tracks the game's own size
    // across UI scale settings.
    public bool AdjustNameFont { get; set; } = true;
    public int NameFontDelta { get; set; } = -1;

    /// <summary>Strips the level glyphs the game prefixes to the name text.</summary>
    public bool HideLevel { get; set; } = true;

    /// <summary>
    /// How many metrics the overlay has a text node for per row. Anything past this can be
    /// configured but is not drawn.
    /// </summary>
    public const int MaxMetrics = 12;

    private List<BarColumn>? metrics = new() { BarColumn.DamagePercent };

    /// <summary>
    /// The metrics drawn after the name, in the order they appear. These are the meter's own
    /// columns, so each one reads and formats exactly as it does in the meter window.
    /// </summary>
    [JsonProperty("NameMetrics", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<BarColumn> Metrics
    {
        get => metrics ??= new List<BarColumn>();
        set => metrics = value;
    }

    /// <summary>Follows each metric with its label, the way the party list header does.</summary>
    public bool MetricShowLabels { get; set; } = true;

    /// <summary>
    /// Per-metric label override, used exactly as typed so it carries its own spacing. Missing
    /// or empty falls back to the shared default; nothing but spaces leaves that one metric
    /// unlabelled. A label placed in front of its value doubles as the separator metrics used
    /// to share, which is what the spacing being the user's is for.
    /// </summary>
    [JsonProperty("MetricLabels", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, string> MetricLabels { get; set; } = new()
    {
        [BarColumn.DamagePercent] = "@ ",
    };

    public string? MetricLabel(BarColumn col, IndividualMetricStyle style)
        => LabelAffix(MetricLabels, col, style);

    /// <summary>
    /// What a label adds to a metric's text, gap included: an override exactly as typed, or
    /// the shared default with a single space between it and the value. Null for a metric
    /// that is to go unlabelled.
    /// </summary>
    private static string? LabelAffix(Dictionary<BarColumn, string> labels, BarColumn col,
        IndividualMetricStyle style)
    {
        if (labels.TryGetValue(col, out var custom) && custom.Length > 0)
            return custom.Trim().Length == 0 ? null : custom;

        var fallback = ColumnLabels.DefaultHeaderLabels.GetValueOrDefault(col, col.ToString());
        return style.LabelBeforeValue ? fallback + " " : " " + fallback;
    }

    /// <summary>
    /// The font size every metric used before they were given a node each. Kept only to seed
    /// <see cref="Style"/> for configs written back then, so those keep their look.
    /// </summary>
    public int MetricsFontDelta { get; set; } = -2;

    /// <summary>
    /// Per-metric position, font size and colour. Filled in on demand rather than up front, so
    /// a metric that has never been touched still reads the values above.
    /// </summary>
    [JsonProperty("MetricColumnStyles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, IndividualMetricStyle> MetricStyles { get; set; } = new()
    {
        [BarColumn.DamagePercent] = new IndividualMetricStyle { FontDelta = 0, LabelBeforeValue = true },
        [BarColumn.Dps] = new IndividualMetricStyle { FontDelta = 0, OffsetX = IndividualMetricStyle.ColumnX(1), UseCustomColor = true },
    };

    /// <summary>
    /// A metric's style, seeded into its own column the first time it is asked for so two
    /// metrics added one after the other don't land on top of each other.
    /// </summary>
    public IndividualMetricStyle Style(BarColumn metric)
    {
        if (MetricStyles.TryGetValue(metric, out var style))
            return style;

        style = new IndividualMetricStyle
        {
            FontDelta = MetricsFontDelta,
            OffsetX = IndividualMetricStyle.ColumnX(Metrics.IndexOf(metric)),
        };

        MetricStyles[metric] = style;
        return style;
    }

    /// <summary>
    /// Metrics used to share one string drawn in front of each of them. Labels do the same
    /// job per metric, so a config carrying a separator has it folded into its labels.
    /// </summary>
    [JsonProperty("MetricSeparator", NullValueHandling = NullValueHandling.Ignore)]
    private string? legacySeparator;

    // Metrics used to be a fixed set of six, each with a bool of its own, drawn in an order
    // of their own and styled through a dictionary keyed the same way. All of it is read
    // once on load and folded into the column list, so a config written back then keeps
    // both its selection and its per-metric look.
    [JsonProperty("PrefixDps")] private bool legacyDps;
    [JsonProperty("PrefixDamage")] private bool legacyDamage;
    [JsonProperty("PrefixCrit")] private bool legacyCrit;
    [JsonProperty("PrefixDirectHit")] private bool legacyDirectHit;
    [JsonProperty("PrefixCritDirectHit")] private bool legacyCritDirectHit;
    [JsonProperty("PrefixDamagePercent")] private bool legacyDamagePercent;

    [JsonProperty("MetricOrder")]
    [JsonConverter(typeof(TolerantEnumConverter))]
    private List<NameMetric>? legacyOrder;

    [JsonProperty("MetricStyles")]
    [JsonConverter(typeof(TolerantEnumConverter))]
    private Dictionary<NameMetric, IndividualMetricStyle>? legacyStyles;

    private static readonly NameMetric[] LegacyMetricOrder =
    {
        NameMetric.Dps,
        NameMetric.Damage,
        NameMetric.Crit,
        NameMetric.DirectHit,
        NameMetric.CritDirectHit,
        NameMetric.DamagePercent,
    };

    /// <summary>
    /// The defaults above are for a config being created, not one being read. Dropping them
    /// before a load leaves the seeding below to fill in whatever an older config is missing,
    /// so it keeps the look it was saved with instead of picking up today's defaults.
    /// </summary>
    [OnDeserializing]
    internal void DropCreationDefaults(StreamingContext context)
    {
        nameShift = hpBarShift = mpBarShift = null;
        hpNumbers = mpNumbers = null;
        metrics = null;
        totalsMetrics = null;
        MetricStyles.Clear();
        MetricLabels.Clear();
        TotalsMetricStyles.Clear();
    }

    /// <summary>
    /// The one deserialization callback the type may have - Newtonsoft rejects a second
    /// method carrying the same attribute - so every migration is driven from here.
    /// </summary>
    [OnDeserialized]
    internal void MigrateLegacySettings(StreamingContext context)
    {
        MigrateLegacyMetrics();
        MigrateLegacyMetricPositions();
        MigrateLegacyMetricSeparator();
        MigrateLegacyTotals();
        MigrateLegacyTotalsLabels();
        MigrateLegacyTotalsTitle();
    }

    /// <summary>
    /// A separator sat in front of every metric. The same text as each metric's own label,
    /// placed in front of its value, reads exactly the way it used to.
    /// </summary>
    private void MigrateLegacyMetricSeparator()
    {
        if (legacySeparator == null)
            return;

        var separator = legacySeparator;
        legacySeparator = null;

        // Kept exactly as it was written, spacing and all, since a label is drawn the same
        // way - so a separator that sat flush against its value still does.
        // Labels are set for every metric rather than left to their defaults, so a config
        // that showed bare values doesn't come back with words it never had.
        MetricShowLabels = separator.Trim().Length > 0;
        if (!MetricShowLabels)
            return;

        foreach (var column in Metrics)
        {
            MetricLabels[column] = separator;
            Style(column).LabelBeforeValue = true;
        }
    }

    /// <summary>
    /// The header's labels, and its clock. Runs only for a config written while the clock was
    /// still a toggle, which is the same era its labels were drawn from.
    /// </summary>
    private void MigrateLegacyTotalsLabels()
    {
        var showDuration = legacyTotalsDuration;
        legacyTotalsDuration = null;

        if (showDuration == null)
            return;

        // A header label used to be drawn with a space put in front of it. Labels carry their
        // own spacing now, so that space moves into the label itself.
        foreach (var column in TotalsMetricLabels.Keys.ToList())
            TotalsMetricLabels[column] = " " + TotalsMetricLabels[column];

        if (showDuration != true || TotalsMetrics.Contains(BarColumn.GroupDuration))
            return;

        TotalsMetrics.Insert(0, BarColumn.GroupDuration);

        // The clock was the one header stat that never carried a word, so it is given a blank
        // label to keep it that way under a header that has labels switched on.
        if (TotalsShowLabels)
            TotalsMetricLabels[BarColumn.GroupDuration] = " ";
    }

    /// <summary>
    /// The header's encounter name, which used to be a toggle of its own. Inserted at the
    /// front of the metric list so it still reads ahead of everything else.
    /// </summary>
    private void MigrateLegacyTotalsTitle()
    {
        var showTitle = legacyTotalsTitle;
        legacyTotalsTitle = null;

        if (showTitle != true || TotalsMetrics.Contains(BarColumn.EncounterName))
            return;

        TotalsMetrics.Insert(0, BarColumn.EncounterName);

        // The name never carried a word of its own, so it is given a blank label to keep it
        // that way under a header that has labels switched on.
        if (TotalsShowLabels)
            TotalsMetricLabels[BarColumn.EncounterName] = " ";
    }

    /// <summary>
    /// A metric used to be placed by a gap chained off the end of the name's text, which says
    /// nothing about where it actually sat - the name's length decided that. Those configs get
    /// a column each in list order instead, and their vertical offsets are rebased from the
    /// name's line onto the row's top edge.
    /// </summary>
    private void MigrateLegacyMetricPositions()
    {
        foreach (var (column, style) in MetricStyles)
        {
            if (style.LegacyGap == null)
                continue;

            style.OffsetX = IndividualMetricStyle.ColumnX(Metrics.IndexOf(column));
            style.OffsetY += IndividualMetricStyle.DefaultOffsetY;
            style.LegacyGap = null;
        }
    }

    /// <summary>
    /// Runs only when the config has no column list of its own, so a deliberately empty
    /// selection is never re-filled from the old flags left beside it.
    /// </summary>
    private void MigrateLegacyMetrics()
    {
        if (metrics != null)
            return;

        var order = new List<NameMetric>(legacyOrder ?? Enumerable.Empty<NameMetric>());
        foreach (var metric in LegacyMetricOrder)
            if (!order.Contains(metric))
                order.Add(metric);

        var migrated = new List<BarColumn>();
        foreach (var metric in order)
        {
            var column = LegacyColumn(metric);
            if (LegacyEnabled(metric) && !migrated.Contains(column))
                migrated.Add(column);
        }

        // Kept for every metric rather than only the enabled ones, so one that was styled
        // and then switched off still comes back looking the way it was left.
        if (legacyStyles != null)
            foreach (var (metric, style) in legacyStyles)
                MetricStyles[LegacyColumn(metric)] = style;

        metrics = migrated;
        legacyOrder = null;
        legacyStyles = null;
        legacyDps = legacyDamage = legacyCrit = false;
        legacyDirectHit = legacyCritDirectHit = legacyDamagePercent = false;
    }

    private static BarColumn LegacyColumn(NameMetric metric) => metric switch
    {
        NameMetric.Damage => BarColumn.Damage,
        NameMetric.Crit => BarColumn.Crit,
        NameMetric.DirectHit => BarColumn.DirectHit,
        NameMetric.CritDirectHit => BarColumn.CritDirectHit,
        NameMetric.DamagePercent => BarColumn.DamagePercent,
        _ => BarColumn.Dps,
    };

    private bool LegacyEnabled(NameMetric metric) => metric switch
    {
        NameMetric.Dps => legacyDps,
        NameMetric.Damage => legacyDamage,
        NameMetric.Crit => legacyCrit,
        NameMetric.DirectHit => legacyDirectHit,
        NameMetric.CritDirectHit => legacyCritDirectHit,
        NameMetric.DamagePercent => legacyDamagePercent,
        _ => false,
    };

    // Buff / debuff icons
    public bool AdjustStatusIcons { get; set; } = true;
    public float StatusOffsetX { get; set; } = 0f;
    public float StatusOffsetY { get; set; } = 8f;
    public float StatusScale { get; set; } = 1f;

    /// <summary>
    /// Fills the icon row from its right edge instead of its left, so a member with a few
    /// buffs shows them flush right rather than hugging the left of the empty row.
    /// </summary>
    public bool StatusRightAlign { get; set; } = false;

    /// <summary>
    /// A colour multiply over the icon artwork, so white leaves it as the game draws it.
    /// Icons are textures rather than flat fills, so they can only be tinted, not recoloured.
    /// </summary>
    public Vector4 StatusTint { get; set; } = new(1f, 1f, 1f, 1f);

    // The timer text inside each status icon. It's a child of the icon, so it already
    // inherits the icon scale - these are on top of that.
    public bool AdjustStatusTimers { get; set; } = false;
    public int StatusTimerFontDelta { get; set; } = 1;
    public float StatusTimerOffsetX { get; set; } = 0f;
    public float StatusTimerOffsetY { get; set; } = -11f;
    public bool StatusTimerUseCustomColor { get; set; } = false;
    public Vector4 StatusTimerColor { get; set; } = new(1f, 1f, 1f, 1f);

    // The glows behind a row. Hover and selection each draw more than one node, and the
    // game fades them in on a timeline, so tint is a colour multiply only - writing alpha
    // would pin it and kill the fade.
    public bool AdjustSelectionGlow { get; set; } = true;

    /// <summary>
    /// Which settings a row uses when it is hovered *and* selected. Hover and selection
    /// share one node - the game shows the same TargetGlow for both and marks selection by
    /// additionally showing the job icon glow, with no difference in animation label - so
    /// only one of the two looks can apply at a time.
    /// </summary>
    public bool SelectionOverridesHover { get; set; } = true;

    public float HoverOffsetX { get; set; } = -11f;
    public float HoverOffsetY { get; set; } = -6f;
    public float HoverScale { get; set; } = 1f;
    public Vector4 HoverTint { get; set; } = new(1f, 1f, 1f, 1f);

    public float SelectionOffsetX { get; set; } = -20f;
    public float SelectionOffsetY { get; set; } = -8f;
    public float SelectionScale { get; set; } = 1f;
    public Vector4 SelectionTint { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// Stops the glow's timeline driving position, scale and tint. The game animates those
    /// as the glow appears, which overwrites our values for the length of the animation.
    /// Alpha is left animated, so the fade still plays; only the pop-in movement is lost.
    /// </summary>
    public bool FreezeGlowTransform { get; set; } = true;

    /// <summary>The glow drawn around the job icon, which is a separate node from the row glow.</summary>
    public float IconGlowOffsetX { get; set; } = 0f;
    public float IconGlowOffsetY { get; set; } = 0f;
    public float IconGlowScale { get; set; } = 1f;
    public Vector4 IconGlowTint { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// Hides the game's "Solo" / "Party" header label. Shares the encounter totals' text
    /// handling, since both write to the same node.
    /// </summary>
    public bool HidePartyTypeLabel { get; set; } = true;

    // Encounter totals, drawn on the party list's header text
    public bool ShowEncounterTotals { get; set; } = true;

    private List<BarColumn>? totalsMetrics = new() { BarColumn.GroupDuration, BarColumn.GroupDeaths };

    /// <summary>
    /// The metrics written into the header, in the order they appear. The same columns the
    /// meter's status bar offers: the group ones read as the whole encounter, since the
    /// header has no tab to filter by, and every other one reads as your own stats.
    /// </summary>
    [JsonProperty("HeaderMetrics", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public List<BarColumn> TotalsMetrics
    {
        get => totalsMetrics ??= new List<BarColumn>();
        set => totalsMetrics = value;
    }

    /// <summary>Follows each header metric with its label, the way the status bar does.</summary>
    public bool TotalsShowLabels { get; set; } = false;

    /// <summary>
    /// Per-metric label override, used exactly as typed so it carries its own spacing. Missing
    /// or empty falls back to the shared default; nothing but spaces leaves that one metric
    /// unlabelled.
    /// </summary>
    [JsonProperty("HeaderMetricLabels", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, string> TotalsMetricLabels { get; set; } = new();

    public string? TotalsLabel(BarColumn col, IndividualMetricStyle style)
        => LabelAffix(TotalsMetricLabels, col, style);

    /// <summary>
    /// Per-metric position, font size and colour, the same way the individual metrics have
    /// them. Each header metric owns a text node, so the header is no longer one string in
    /// one font.
    /// </summary>
    [JsonProperty("HeaderMetricStyles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    [JsonConverter(typeof(TolerantEnumConverter))]
    public Dictionary<BarColumn, IndividualMetricStyle> TotalsMetricStyles { get; set; } = new();

    /// <summary>
    /// A header metric's style, seeded into its own column the first time it is asked for.
    /// The header text's own font and colour are the starting point, so a config written
    /// before the metrics were split keeps the look it had as one string.
    /// </summary>
    public IndividualMetricStyle TotalsStyle(BarColumn metric)
    {
        if (TotalsMetricStyles.TryGetValue(metric, out var style))
            return style;

        style = new IndividualMetricStyle
        {
            FontDelta = TotalsFontDelta,
            OffsetX = IndividualMetricStyle.HeaderColumnX(TotalsMetrics.IndexOf(metric)),
            OffsetY = 0f,
            UseCustomColor = TotalsUseCustomColor,
            Color = TotalsColor,
        };

        TotalsMetricStyles[metric] = style;
        return style;
    }

    /// <summary>
    /// Drawn in place of the totals whenever there are none to show - out of combat, or with
    /// no encounter active. Empty leaves the header blank, which is the original behaviour.
    /// </summary>
    public string TotalsHiddenText { get; set; } = "the council";

    // The header text node itself, which the totals are written to.
    public bool AdjustTotalsText { get; set; } = true;
    public int TotalsFontDelta { get; set; } = 2;
    public float TotalsOffsetX { get; set; } = 6f;
    public float TotalsOffsetY { get; set; } = -1f;
    public bool TotalsUseCustomColor { get; set; } = true;
    public Vector4 TotalsColor { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// The header used to show the encounter clock through a toggle of its own. It is a
    /// metric like any other now, so a config carrying the toggle has it folded into the
    /// header's metric list. Null for a config written after the change.
    /// </summary>
    [JsonProperty("TotalsShowDuration", NullValueHandling = NullValueHandling.Ignore)]
    private bool? legacyTotalsDuration;

    /// <summary>
    /// The header used to show the encounter name through a toggle of its own, the same way
    /// the clock did. Null for a config written after the change.
    /// </summary>
    [JsonProperty("TotalsShowTitle", NullValueHandling = NullValueHandling.Ignore)]
    private bool? legacyTotalsTitle;

    // The header used to show a fixed set of encounter stats, each with a bool of its own.
    // Read once on load into the column list, so a config written back then keeps showing
    // the same things.
    [JsonProperty("TotalsShowRaidDps")] private bool legacyTotalsRaidDps;
    [JsonProperty("TotalsShowDamage")] private bool legacyTotalsDamage;
    [JsonProperty("TotalsShowDeaths")] private bool legacyTotalsDeaths;

    /// <summary>
    /// Runs only when the config has no header column list of its own, so a deliberately
    /// empty header is never re-filled from the old flags left beside it.
    /// </summary>
    private void MigrateLegacyTotals()
    {
        if (totalsMetrics != null)
            return;

        var migrated = new List<BarColumn>();
        if (legacyTotalsRaidDps)
            migrated.Add(BarColumn.EncDps);
        if (legacyTotalsDamage)
            migrated.Add(BarColumn.GroupDamage);
        if (legacyTotalsDeaths)
        {
            migrated.Add(BarColumn.GroupDeaths);

            // Deaths was the one stat that used to carry a word of its own, so the labels
            // come on with it to keep the header reading the way it was left.
            TotalsShowLabels = true;
            TotalsMetricLabels[BarColumn.GroupDeaths] = "deaths";
        }

        totalsMetrics = migrated;
        legacyTotalsRaidDps = legacyTotalsDamage = legacyTotalsDeaths = false;
    }

    // Cast bar
    public bool AdjustCastBar { get; set; } = true;
    public float CastBarShiftX { get; set; } = 11f;
    public float CastBarShiftY { get; set; } = 5f;

    /// <summary>Height multiplier, taken from the bar's top edge so the shift still lands.</summary>
    public float CastBarScaleY { get; set; } = 1f;

    /// <summary>A colour multiply over the bar artwork; white leaves the game's own look.</summary>
    public Vector4 CastBarTint { get; set; } = new(1f, 1f, 1f, 1f);

    // Casting spell name
    public bool AdjustCastName { get; set; } = true;
    public float CastNameOffsetX { get; set; } = -5f;
    public float CastNameOffsetY { get; set; } = -1f;
    public int CastNameFontDelta { get; set; } = -3;
    public bool CastNameUseCustomColor { get; set; } = false;
    public Vector4 CastNameColor { get; set; } = new(1f, 1f, 1f, 1f);

    // The companion timer - the bar and number drawn where a player's MP bar would be,
    // counting a chocobo's remaining time down.
    public bool AdjustPetTimer { get; set; } = false;
    public float PetTimerOffsetX { get; set; } = 0f;
    public float PetTimerOffsetY { get; set; } = 0f;
    public int PetTimerFontDelta { get; set; } = 0;
    public bool PetTimerUseCustomColor { get; set; } = false;
    public Vector4 PetTimerColor { get; set; } = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// The settings both gauges' numbers shared before they were split. Kept only to seed
    /// <see cref="HpNumbers"/> and <see cref="MpNumbers"/> for configs written back then.
    /// </summary>
    public bool AdjustGaugeNumbers { get; set; } = false;
    public int GaugeFontDelta { get; set; } = 0;

    /// <summary>
    /// Size of MP's trailing two digits relative to the leading ones. The game draws them
    /// in a second, smaller text node two points down, so -2 is its own look and 0 matches
    /// the leading digits. Measured against the leading node rather than the trailing
    /// node's own size, which can't be trusted once we've written to it.
    /// </summary>
    public int MpTrailingFontDelta { get; set; } = -1;
    public float GaugeNumberOffsetY { get; set; } = -1f;
    public float TrailingDigitsOffsetX { get; set; } = 0f;
    public float TrailingDigitsOffsetY { get; set; } = 0f;

    [JsonProperty("HpNumbers")] private GaugeNumberStyle? hpNumbers = new() { Enabled = true, OffsetY = -7f };
    [JsonProperty("MpNumbers")] private GaugeNumberStyle? mpNumbers = new() { Enabled = true, OffsetY = -2f };

    [JsonIgnore] public GaugeNumberStyle HpNumbers => hpNumbers ??= LegacyGaugeNumbers();
    [JsonIgnore] public GaugeNumberStyle MpNumbers => mpNumbers ??= LegacyGaugeNumbers();

    /// <summary>
    /// The numbers used to ride along with the row shift, since the wrapper that was moved
    /// held them as well as the bar. They are moved on their own now, so the shift is folded
    /// into their offset and the two together land where the pair used to.
    /// </summary>
    private GaugeNumberStyle LegacyGaugeNumbers() => new()
    {
        Enabled = AdjustGaugeNumbers || ShiftRowContent,
        FontDelta = AdjustGaugeNumbers ? GaugeFontDelta : 0,
        OffsetY = (AdjustGaugeNumbers ? GaugeNumberOffsetY : 0f) + (ShiftRowContent ? RowContentShiftY : 0f),
    };
}
