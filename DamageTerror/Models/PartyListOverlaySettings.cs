namespace DamageTerror.Models;

/// <summary>
/// Tunables for the native party list integration. Every value is read fresh each frame,
/// so edits apply immediately without reloading the addon.
/// Defaults are the values arrived at by eye in-game.
/// </summary>
public sealed class PartyListOverlaySettings
{
    /// <summary>
    /// Hides everything derived from parse data - the fill bars, the name metrics and the
    /// header totals - once an encounter has been over for
    /// <see cref="HideOutOfCombatDelay"/> seconds. The restyle stays applied either way, so
    /// the rows don't jump layout on pull. Separate from the meter window's own setting.
    /// </summary>
    public bool HideOutOfCombat { get; set; } = true;
    public float HideOutOfCombatDelay { get; set; } = 5f;

    /// <summary>
    /// Matches the outline the game draws around every glyph to whatever colour the text was
    /// given. Off leaves the game's own edge, which stays dark under a recoloured name.
    /// Applies to every party list text with a custom colour.
    /// </summary>
    public bool TintTextOutline { get; set; } = true;

    /// <summary>
    /// How far the outline is darkened away from the text colour. 0 matches the text exactly,
    /// which reads as a fatter glyph rather than an edge; 1 is black.
    /// </summary>
    public float TextOutlineDarkness { get; set; } = 0.65f;

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
    /// Where a row's fill colour comes from. The meter window's palette by default, so the
    /// two agree until the party list is deliberately given colours of its own.
    /// </summary>
    public PartyListBarColorMode BarColorMode { get; set; } = PartyListBarColorMode.MatchMeter;

    /// <summary>The party list's own job and role colours, used by <see cref="PartyListBarColorMode.OwnPalette"/>.</summary>
    public JobColorPalette BarColors { get; set; } = new();

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

    [JsonProperty("NameShift")] private RowPartStyle? nameShift;
    [JsonProperty("HpBarShift")] private RowPartStyle? hpBarShift;
    [JsonProperty("MpBarShift")] private RowPartStyle? mpBarShift;

    /// <summary>
    /// Off by default: the container the row shift moved holds the gauges but not the name,
    /// so the name stayed put before it could be moved on its own.
    /// </summary>
    [JsonIgnore] public RowPartStyle NameShift => nameShift ??= new RowPartStyle { Enabled = false, OffsetY = RowContentShiftY };

    [JsonIgnore] public RowPartStyle HpBarShift => hpBarShift ??= LegacyShift();
    [JsonIgnore] public RowPartStyle MpBarShift => mpBarShift ??= LegacyShift();

    private RowPartStyle LegacyShift() => new() { Enabled = ShiftRowContent, OffsetY = RowContentShiftY };

    /// <summary>The slot number drawn before each name, which is a node of its own.</summary>
    public bool AdjustPartyIndex { get; set; } = false;
    public int PartyIndexFontDelta { get; set; } = 0;
    public float PartyIndexOffsetX { get; set; } = 0f;
    public float PartyIndexOffsetY { get; set; } = 0f;

    /// <summary>Off, the slot number follows whatever colour the name is given.</summary>
    public bool PartyIndexUseCustomColor { get; set; } = false;
    public Vector4 PartyIndexColor { get; set; } = new(1f, 1f, 1f, 1f);

    // Player name font. Delta rather than absolute, so it tracks the game's own size
    // across UI scale settings.
    public bool AdjustNameFont { get; set; } = false;
    public int NameFontDelta { get; set; } = -4;

    /// <summary>Strips the level glyphs the game prefixes to the name text.</summary>
    public bool HideLevel { get; set; } = false;

    // Metrics appended to the name. Any combination; they appear in this order.
    // JSON names are pinned to the originals so existing configs keep their selection.
    [JsonProperty("PrefixDps")] public bool MetricDps { get; set; } = false;
    [JsonProperty("PrefixDamage")] public bool MetricDamage { get; set; } = false;
    [JsonProperty("PrefixCrit")] public bool MetricCrit { get; set; } = false;
    [JsonProperty("PrefixDirectHit")] public bool MetricDirectHit { get; set; } = false;
    [JsonProperty("PrefixCritDirectHit")] public bool MetricCritDirectHit { get; set; } = false;
    [JsonProperty("PrefixDamagePercent")] public bool MetricDamagePercent { get; set; } = false;

    /// <summary>
    /// Drawn before every metric, so it lands between the name and the first one and between
    /// each pair after that. Part of the metric's own text, so it takes that metric's font
    /// and colour. Empty for none.
    /// </summary>
    public string MetricSeparator { get; set; } = string.Empty;

    /// <summary>
    /// The font size and gap every metric used before they were given a node each. Kept only
    /// to seed <see cref="Style"/> for configs written back then, so those keep their look.
    /// </summary>
    public int MetricsFontDelta { get; set; } = -2;
    public float MetricsGap { get; set; } = 7f;

    /// <summary>
    /// Per-metric font size, gap and colour. Filled in on demand rather than up front, so a
    /// metric that has never been touched still reads the values above.
    /// </summary>
    public Dictionary<NameMetric, NameMetricStyle> MetricStyles { get; set; } = new();

    /// <summary>Draw order, which is also the order they are listed in the config.</summary>
    public static readonly NameMetric[] MetricOrder =
    {
        NameMetric.Dps,
        NameMetric.Damage,
        NameMetric.Crit,
        NameMetric.DirectHit,
        NameMetric.CritDirectHit,
        NameMetric.DamagePercent,
    };

    public NameMetricStyle Style(NameMetric metric)
    {
        if (MetricStyles.TryGetValue(metric, out var style))
            return style;

        style = new NameMetricStyle { FontDelta = MetricsFontDelta, Gap = MetricsGap };
        MetricStyles[metric] = style;
        return style;
    }

    public bool MetricEnabled(NameMetric metric) => metric switch
    {
        NameMetric.Dps => MetricDps,
        NameMetric.Damage => MetricDamage,
        NameMetric.Crit => MetricCrit,
        NameMetric.DirectHit => MetricDirectHit,
        NameMetric.CritDirectHit => MetricCritDirectHit,
        NameMetric.DamagePercent => MetricDamagePercent,
        _ => false,
    };

    public void SetMetricEnabled(NameMetric metric, bool value)
    {
        switch (metric)
        {
            case NameMetric.Dps: MetricDps = value; break;
            case NameMetric.Damage: MetricDamage = value; break;
            case NameMetric.Crit: MetricCrit = value; break;
            case NameMetric.DirectHit: MetricDirectHit = value; break;
            case NameMetric.CritDirectHit: MetricCritDirectHit = value; break;
            case NameMetric.DamagePercent: MetricDamagePercent = value; break;
        }
    }

    [JsonIgnore]
    public bool AnyNameMetric
        => MetricDps || MetricDamage || MetricCrit || MetricDirectHit
           || MetricCritDirectHit || MetricDamagePercent;

    // Buff / debuff icons
    public bool AdjustStatusIcons { get; set; } = false;
    public float StatusOffsetX { get; set; } = 1f;
    public float StatusOffsetY { get; set; } = 8f;
    public float StatusScale { get; set; } = 1.01f;

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
    public bool TotalsShowTitle { get; set; } = true;
    public bool TotalsShowDuration { get; set; } = true;
    public bool TotalsShowRaidDps { get; set; } = true;
    public bool TotalsShowDamage { get; set; } = false;
    public bool TotalsShowDeaths { get; set; } = true;

    /// <summary>
    /// Drawn in place of the totals whenever there are none to show - out of combat, or with
    /// no encounter active. Empty leaves the header blank, which is the original behaviour.
    /// </summary>
    public string TotalsHiddenText { get; set; } = string.Empty;

    // The header text node itself, which the totals are written to.
    public bool AdjustTotalsText { get; set; } = false;
    public int TotalsFontDelta { get; set; } = 0;
    public float TotalsOffsetX { get; set; } = 0f;
    public float TotalsOffsetY { get; set; } = 0f;
    public bool TotalsUseCustomColor { get; set; } = false;
    public Vector4 TotalsColor { get; set; } = new(1f, 1f, 1f, 1f);

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
    public int MpTrailingFontDelta { get; set; } = 0;
    public float GaugeNumberOffsetY { get; set; } = -1f;
    public float TrailingDigitsOffsetX { get; set; } = 0f;
    public float TrailingDigitsOffsetY { get; set; } = 0f;

    [JsonProperty("HpNumbers")] private GaugeNumberStyle? hpNumbers;
    [JsonProperty("MpNumbers")] private GaugeNumberStyle? mpNumbers;

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
