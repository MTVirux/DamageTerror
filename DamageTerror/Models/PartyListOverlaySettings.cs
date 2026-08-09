namespace DamageTerror.Models;

/// <summary>
/// Tunables for the native party list integration. Every value is read fresh each frame,
/// so edits apply immediately without reloading the addon.
/// Defaults are the values arrived at by eye in-game.
/// </summary>
public sealed class PartyListOverlaySettings
{
    // DPS fill bar
    public bool ShowBar { get; set; } = true;
    public float IconUnderlap { get; set; } = 4f;
    /// <summary>
    /// Bar height in pixels, centred on the job icon. Defaults to the selection
    /// background's own height - its corner artwork insets 20px top and bottom, so below
    /// about 42px there is no flat middle left and the corners bow into an oval.
    /// </summary>
    public float BarHeightPixels { get; set; } = 48f;

    /// <summary>
    /// Opacity of the fill, used directly rather than as a floor under the meter's own
    /// alpha - as a floor, lowering it below the meter's value did nothing.
    /// </summary>
    [JsonProperty("BarMinAlpha")]
    public float BarOpacity { get; set; } = 0.35f;

    /// <summary>
    /// Caps the width a 100% bar draws to. Everything scales inside it, so the bars stay
    /// proportional to each other instead of the longest one being clipped. 0 uses the
    /// space available in the row.
    /// </summary>
    public float BarMaxWidth { get; set; } = 0f;
    public float BarOffsetX { get; set; } = 0f;
    public float BarOffsetY { get; set; } = 0f;

    /// <summary>
    /// Draws the fill behind the name, gauges and status icons by putting it in a container
    /// that sits right after the party list's backdrop rather than at the end of the tree.
    /// Off leaves it on top of the row.
    /// </summary>
    public bool BarBehindRowContent { get; set; } = true;

    // DPS number
    public bool ShowText { get; set; } = true;
    public float TextWidth { get; set; } = 58f;
    public float TextHeight { get; set; } = 18f;
    public float TextRightMargin { get; set; } = 4f;
    public int TextFontSize { get; set; } = 12;

    // The row's own name, HP and MP
    public bool ShiftRowContent { get; set; } = true;
    public float RowContentShiftY { get; set; } = -7f;

    // Player name font. Delta rather than absolute, so it tracks the game's own size
    // across UI scale settings.
    public bool AdjustNameFont { get; set; } = false;
    public int NameFontDelta { get; set; } = 0;

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
    /// The metrics live in their own text node - a single Atk text node only has one font
    /// size, which is why the game splits its own MP value across two nodes. The node
    /// copies the name's font so the two read as one line; this is an offset from it.
    /// </summary>
    public int MetricsFontDelta { get; set; } = 0;
    public float MetricsGap { get; set; } = 6f;

    [JsonIgnore]
    public bool AnyNameMetric
        => MetricDps || MetricDamage || MetricCrit || MetricDirectHit
           || MetricCritDirectHit || MetricDamagePercent;

    // Buff / debuff icons
    public bool AdjustStatusIcons { get; set; } = false;
    public float StatusOffsetX { get; set; } = 0f;
    public float StatusOffsetY { get; set; } = 0f;
    public float StatusScale { get; set; } = 1f;

    // The timer text inside each status icon. It's a child of the icon, so it already
    // inherits the icon scale - these are on top of that.
    public bool AdjustStatusTimers { get; set; } = false;
    public int StatusTimerFontDelta { get; set; } = 0;
    public float StatusTimerOffsetX { get; set; } = 0f;
    public float StatusTimerOffsetY { get; set; } = 0f;

    // The glows behind a row. Hover and selection each draw more than one node, and the
    // game fades them in on a timeline, so tint is a colour multiply only - writing alpha
    // would pin it and kill the fade.
    public bool AdjustSelectionGlow { get; set; } = false;

    /// <summary>
    /// Which settings a row uses when it is hovered *and* selected. Hover and selection
    /// share one node - the game shows the same TargetGlow for both and marks selection by
    /// additionally showing the job icon glow, with no difference in animation label - so
    /// only one of the two looks can apply at a time.
    /// </summary>
    public bool SelectionOverridesHover { get; set; } = true;

    public float HoverOffsetX { get; set; } = 0f;
    public float HoverOffsetY { get; set; } = 0f;
    public float HoverScale { get; set; } = 1f;
    public Vector4 HoverTint { get; set; } = new(1f, 1f, 1f, 1f);

    public float SelectionOffsetX { get; set; } = 0f;
    public float SelectionOffsetY { get; set; } = 0f;
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
    public bool HidePartyTypeLabel { get; set; } = false;

    // Encounter totals, drawn on the party list's header text
    public bool ShowEncounterTotals { get; set; } = false;
    public bool TotalsShowTitle { get; set; } = false;
    public bool TotalsShowDuration { get; set; } = false;
    public bool TotalsShowRaidDps { get; set; } = true;
    public bool TotalsShowDamage { get; set; } = false;
    public bool TotalsShowDeaths { get; set; } = false;

    // Cast bar
    public bool AdjustCastBar { get; set; } = true;
    public float CastBarShiftX { get; set; } = 10f;
    public float CastBarShiftY { get; set; } = 4f;

    // Casting spell name
    public bool AdjustCastName { get; set; } = true;
    public float CastNameOffsetX { get; set; } = -6f;
    public float CastNameOffsetY { get; set; } = -2f;
    public int CastNameFontDelta { get; set; } = -4;

    // HP/MP numbers
    public bool AdjustGaugeNumbers { get; set; } = true;
    public int GaugeFontDelta { get; set; } = 0;

    /// <summary>
    /// Size of MP's trailing two digits relative to the leading ones. The game draws them
    /// in a second, smaller text node two points down, so -2 is its own look and 0 matches
    /// the leading digits. Measured against the leading node rather than the trailing
    /// node's own size, which can't be trusted once we've written to it.
    /// </summary>
    public int MpTrailingFontDelta { get; set; } = -2;
    public float GaugeNumberOffsetY { get; set; } = -1f;
    public float TrailingDigitsOffsetX { get; set; } = 1f;
    public float TrailingDigitsOffsetY { get; set; } = 1f;
}
