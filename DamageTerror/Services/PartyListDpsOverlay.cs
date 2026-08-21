using System.Globalization;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using ByteColor = FFXIVClientStructs.FFXIV.Client.Graphics.ByteColor;

namespace DamageTerror.Services;

/// <summary>
/// Draws encounter DPS into the game's native party list, and restyles the row to make
/// room for it. Each row gets a job-coloured fill bar sized to the player's share of the
/// top DPS, which hangs off the row component so it inherits the party list's visibility,
/// position and scale.
/// <para>
/// It also adjusts nodes the game owns - the name, gauges, cast bar and spell name. Every
/// such change captures the original value before its first write and restores it on
/// teardown or when its option is switched off. The capture is self-correcting: if a value
/// isn't the one we last wrote, the game or another plugin owns it now and the current
/// value becomes the new original.
/// </para>
/// <para>
/// Nodes are only ever appended to a component's child list. Inserting at the front shifts
/// the indices the game addresses its own nodes by, which corrupts the addon until it is
/// destroyed and rebuilt.
/// </para>
/// </summary>
public sealed unsafe class PartyListDpsOverlay : IDisposable
{
    private const int MaxRows = 8;

    /// <summary>Private node id ranges, so we can never collide with the game or another plugin.</summary>
    private const uint BarNodeIdBase = 0x44540100;
    private const uint MetricNodeIdBase = 0x44540200;
    private const uint OverlayRootNodeId = 0x44540300;
    private const uint BarRootNodeId = 0x44540301;

    /// <summary>One text node per metric per row - a text node has one font size and one colour.</summary>
    private const int MetricSlots = PartyListOverlaySettings.MaxMetrics;

    /// <summary>Cast bar background and fill.</summary>
    private const int CastBarSlots = 2;

    /// <summary>Text nodes tracked per gauge - MP uses two, at different sizes.</summary>
    private const int GaugeTextSlots = 4;

    /// <summary>HP and MP.</summary>
    private const int GaugeCount = 2;

    /// <summary>HP's gauge index - the one whose number has an arrow beside it.</summary>
    private const int HpGaugeIndex = 0;

    /// <summary>Upper bound on status icons tracked per row.</summary>
    private const int StatusIconSlots = 20;

    /// <summary>Children of the glow container scanned per row.</summary>
    private const int GlowSlots = 8;

    /// <summary>
    /// Captured state is stored per node - the row glow and the job icon glow - never per
    /// settings group. Hover and selection share the row glow node, so giving them a slot
    /// each meant that on every state change one slot read the other's output as the game's
    /// original and re-based on it, compounding the offset every frame.
    /// </summary>
    private const int GlowGroups = 2;
    private const int GlowStateRow = 0;
    private const int GlowStateIcon = 1;

    /// <summary>Our own nodes, so the glow sweep doesn't pick up the DPS fill we attached.</summary>
    private const uint OwnNodeIdMin = 0x44540000;
    private const uint OwnNodeIdMax = 0x445403FF;

    /// <summary>How much smaller the game draws MP's trailing digits than its leading ones.</summary>
    private const int GameMpTrailingFontDelta = -2;

    /// <summary>Name, HP bar, MP bar, then cast bar background and fill.</summary>
    private const int ShiftSlots = 5;

    /// <summary>
    /// The rows the extra spacing moves. Slotted by which array the game keeps a row in -
    /// party members, then duty support / trust NPCs, then the chocobo and pet - rather than
    /// by where it lands on screen: a screen row flips between a party node and a trust one
    /// as the party's makeup changes, and a slot that changed node underneath the capture
    /// would re-base on our own offset and compound it.
    /// </summary>
    private const int SpacingSlots = MaxRows * 2 + 2;
    private const int TrustSpacingSlot = MaxRows;
    private const int ChocoboSpacingSlot = MaxRows * 2;
    private const int PetSpacingSlot = MaxRows * 2 + 1;

    /// <summary>
    /// The leading shift slots that are row parts with a style of their own - name, HP bar,
    /// MP bar. The cast bar slots after them take only the vertical shift; their horizontal
    /// position and width belong to the cast bar layout instead.
    /// </summary>
    private const int RowPartSlots = 3;

    /// <summary>The gauges' row-part slots - their tints cannot go on the owner node.</summary>
    private const int HpBarSlot = 1;
    private const int MpBarSlot = 2;

    /// <summary>A gauge bar's artwork - the backdrop, the fill, and its two transition layers.</summary>
    private const int GaugeArtSlots = 4;

    /// <summary>
    /// The backdrop's slot in that artwork. It is the empty bar - the outline and the groove
    /// inside it - so it is styled on its own rather than along with the fill drawn over it.
    /// </summary>
    private const int GaugeOutlineArt = 0;

    /// <summary>The fill and its transition layers, which take the bar's own colour.</summary>
    private const int GaugeFillArtFirst = GaugeOutlineArt + 1;

    /// <summary>The shield pieces: the fill inside the HP bar, then the overflow bar above it.</summary>
    private const int ShieldGroups = 2;
    private const int ShieldFillGroup = 0;
    private const int ShieldOverflowGroup = 1;

    /// <summary>
    /// Nodes per shield piece - three fill layers each, plus the overflow's "too big to draw"
    /// icon in its fourth slot.
    /// </summary>
    private const int ShieldNodeSlots = 4;
    private const int ShieldMaxIconIndex = 3;

    /// <summary>
    /// The private-use block the game uses for the level digits it prefixes to the name.
    /// </summary>
    private const char LevelGlyphFirst = '\uE060';
    private const char LevelGlyphLast = '\uE06F';

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMilliseconds(250);

    private readonly DataService dataService;
    private readonly Configuration config;
    private readonly AddonController<AddonPartyList> controller;
    /// <remarks>
    /// A nine-grid over a texture we generate and own. Borrowing the game's parts list also
    /// produced rounded ends, but that pointer belongs to the addon's ULD - leaving the
    /// party freed it underneath our live node and crashed the game.
    /// </remarks>
    private readonly ImGuiImageNode?[] barNodes = new ImGuiImageNode?[MaxRows];

    /// <summary>
    /// One container of ours under the addon's root, holding every node we add. Nothing is
    /// attached to the party member components. Attaching there also registers the node in
    /// that component's UldManager node list, and the game's UpdateCollisionNodeList walks
    /// the list calling a virtual on every entry whose type reads as a component - one stale
    /// entry and the call lands on garbage, which crashed the game whenever membership
    /// changed, on leaving a party or a duty.
    /// </summary>
    private ResNode? overlayRoot;

    /// <summary>
    /// A second container, sitting just after the party list's backdrop instead of at the end
    /// of the root's children, so the fills draw behind the rows. Same transform as
    /// <see cref="overlayRoot"/>, so bar coordinates are the same either way.
    /// </summary>
    private ResNode? barRoot;

    private readonly string? barTexturePath = EnsureBarTexture();
    private readonly bool[] barTextureApplied = new bool[MaxRows];
    private readonly bool[] barOnBarRoot = new bool[MaxRows];

    /// <remarks>
    /// One wrap per row, never shared: the node takes ownership and disposes the wrap with
    /// itself, so handing the same one to every row left the rest pointing at a disposed
    /// texture the moment the party list was torn down.
    /// </remarks>
    private readonly Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap?[] pendingBarTexture
        = new Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap?[MaxRows];
    private readonly bool[] barTextureRequested = new bool[MaxRows];

    /// <summary>
    /// Our own bar artwork: a white body so the job colour tints it exactly, a darker
    /// outline that stays visible once tinted, and an alpha ramp fading out to the right.
    /// Written once to the plugin's config directory - delete the file to regenerate it.
    /// </summary>
    private static string? EnsureBarTexture()
    {
        try
        {
            var directory = Svc.PluginInterface.ConfigDirectory;
            if (!directory.Exists)
                directory.Create();

            var path = Path.Combine(directory.FullName, "party-list-bar.png");
            if (File.Exists(path))
                return path;

            const int width = 256;
            const int height = 64;
            const int outline = 3;
            const float rampStart = width * 0.66f;

            using var bitmap = new System.Drawing.Bitmap(width, height);

            for (var x = 0; x < width; x++)
            {
                var ramp = x < rampStart ? 1f : 1f - ((x - rampStart) / (width - rampStart));
                var alpha = (int)Math.Clamp(ramp * 255f, 0f, 255f);

                for (var y = 0; y < height; y++)
                {
                    var edge = y < outline || y >= height - outline;
                    var value = edge ? 90 : 255;
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(alpha, value, value, value));
                }
            }

            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            Svc.Log.Debug($"[PartyList] Generated bar texture at '{path}'");
            return path;
        }
        catch (Exception ex)
        {
            ServiceManager.LogWarning(LogChannel.PartyMembership, $"Bar texture generation failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Rents the image for a row and hands it to that row's node, which owns it from then on.
    /// Rented rather than borrowed: the shared-cache wrap is only valid for the frame it is
    /// fetched in, so a node holding it ends up pointing at a released texture and draws
    /// nothing. The rent completes off the framework thread, so the hand-off waits for the
    /// next update pass rather than happening in the continuation.
    /// </summary>
    private void ApplyBarTexture(ImGuiImageNode bar, int row)
    {
        if (barTexturePath == null)
            return;

        var pending = Interlocked.Exchange(ref pendingBarTexture[row], null);
        if (pending != null)
        {
            bar.LoadTexture(pending);
            bar.FitTexture = true;
            barTextureApplied[row] = true;

            // The node owns that wrap now, so a replacement node needs a rent of its own.
            barTextureRequested[row] = false;
            return;
        }

        if (barTextureRequested[row])
            return;

        barTextureRequested[row] = true;
        Svc.Texture.GetFromFile(barTexturePath).RentAsync().ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                Interlocked.Exchange(ref pendingBarTexture[row], task.Result)?.Dispose();
            else
            {
                barTextureRequested[row] = false;
                ServiceManager.LogWarning(LogChannel.PartyMembership, "Bar texture rent failed.");
            }
        });
    }
    private readonly TextNode?[,] metricNodes = new TextNode?[MaxRows, MetricSlots];
    private readonly string[,] lastMetricText = new string[MaxRows, MetricSlots];

    /// <summary>How the game paints a resting party list name on the player's UI theme, which is
    /// what a metric with no colour of its own follows. Null while the palette can't be read.</summary>
    private Vector4? paletteNameColor;
    private Vector4? paletteNameOutline;

    private readonly float[] lastBarWidth = new float[MaxRows];
    private readonly float[] lastBarHeight = new float[MaxRows];
    private readonly Vector2[] lastBarPos = new Vector2[MaxRows];
    private readonly float[,] originalShiftY = new float[MaxRows, ShiftSlots];
    private readonly float[,] appliedShiftY = new float[MaxRows, ShiftSlots];
    private readonly float[,] originalShiftX = new float[MaxRows, RowPartSlots];
    private readonly float[,] appliedShiftX = new float[MaxRows, RowPartSlots];
    private readonly float[,] originalPartScale = new float[MaxRows, RowPartSlots];
    private readonly float[,] appliedPartScale = new float[MaxRows, RowPartSlots];
    private readonly float[,] originalPartOriginX = new float[MaxRows, RowPartSlots];
    private readonly float[,] originalPartOriginY = new float[MaxRows, RowPartSlots];
    private readonly bool[,] shiftApplied = new bool[MaxRows, ShiftSlots];
    private readonly float[] originalSpacingY = new float[SpacingSlots];
    private readonly float[] appliedSpacingY = new float[SpacingSlots];
    private readonly bool[] spacingApplied = new bool[SpacingSlots];
    private readonly float[] spacingRowY = new float[SpacingSlots];
    private ushort originalBackdropHeight;
    private ushort appliedBackdropHeight;
    private bool backdropHeightApplied;
    private readonly NodeTintState[,,] gaugeArtTint = new NodeTintState[MaxRows, RowPartSlots, GaugeArtSlots];
    private readonly NodeAlphaState[,] gaugeOutlineAlpha = new NodeAlphaState[MaxRows, RowPartSlots];
    private readonly ShieldNodeState[,,] shieldState = new ShieldNodeState[MaxRows, ShieldGroups, ShieldNodeSlots];
    private readonly float[] originalCastNameX = new float[MaxRows];
    private readonly float[] originalCastNameY = new float[MaxRows];
    private readonly ushort[] originalCastNameHeight = new ushort[MaxRows];
    private readonly byte[] originalCastNameFont = new byte[MaxRows];
    private readonly float[] appliedCastNameX = new float[MaxRows];
    private readonly float[] appliedCastNameY = new float[MaxRows];
    private readonly ushort[] appliedCastNameHeight = new ushort[MaxRows];
    private readonly byte[] appliedCastNameFont = new byte[MaxRows];
    private readonly bool[] castNameApplied = new bool[MaxRows];
    private readonly float[,] originalCastBarX = new float[MaxRows, CastBarSlots];
    private readonly float[,] appliedCastBarX = new float[MaxRows, CastBarSlots];
    private readonly float[,] originalCastBarScaleX = new float[MaxRows, CastBarSlots];
    private readonly float[,] appliedCastBarScaleX = new float[MaxRows, CastBarSlots];
    private readonly float[,] originalCastBarOriginX = new float[MaxRows, CastBarSlots];
    private readonly float[,] originalCastBarScaleY = new float[MaxRows, CastBarSlots];
    private readonly float[,] appliedCastBarScaleY = new float[MaxRows, CastBarSlots];
    private readonly float[,] originalCastBarOriginY = new float[MaxRows, CastBarSlots];
    private readonly bool[,] castBarApplied = new bool[MaxRows, CastBarSlots];
    private readonly NodeTintState[,] castBarTint = new NodeTintState[MaxRows, CastBarSlots];
    private readonly TextColorState[] castNameColor = new TextColorState[MaxRows];
    private readonly float[,] originalStatusX = new float[MaxRows, StatusIconSlots];
    private readonly float[,] originalStatusY = new float[MaxRows, StatusIconSlots];
    private readonly float[,] originalStatusScale = new float[MaxRows, StatusIconSlots];
    private readonly float[,] originalStatusOriginX = new float[MaxRows, StatusIconSlots];
    private readonly float[,] originalStatusOriginY = new float[MaxRows, StatusIconSlots];
    private readonly float[,] appliedStatusX = new float[MaxRows, StatusIconSlots];
    private readonly float[,] appliedStatusY = new float[MaxRows, StatusIconSlots];
    private readonly float[,] appliedStatusScale = new float[MaxRows, StatusIconSlots];
    private readonly bool[,] statusApplied = new bool[MaxRows, StatusIconSlots];
    private readonly NodeTintState[,] statusTint = new NodeTintState[MaxRows, StatusIconSlots];
    private readonly TextColorState[,] timerColor = new TextColorState[MaxRows, StatusIconSlots];
    private readonly float[,] originalGlowX = new float[MaxRows, GlowGroups];
    private readonly float[,] originalGlowY = new float[MaxRows, GlowGroups];
    private readonly float[,] originalGlowScale = new float[MaxRows, GlowGroups];
    private readonly float[,] originalGlowOriginX = new float[MaxRows, GlowGroups];
    private readonly float[,] originalGlowOriginY = new float[MaxRows, GlowGroups];
    private readonly byte[,,] originalGlowMultiply = new byte[MaxRows, GlowGroups, 3];
    private readonly AtkTimelineMask[,] originalGlowMask = new AtkTimelineMask[MaxRows, GlowGroups];
    private readonly float[,] appliedGlowX = new float[MaxRows, GlowGroups];
    private readonly float[,] appliedGlowY = new float[MaxRows, GlowGroups];
    private readonly float[,] appliedGlowScale = new float[MaxRows, GlowGroups];
    private readonly bool[,] glowApplied = new bool[MaxRows, GlowGroups];

    private readonly byte[,] originalRowGlowMultiply = new byte[MaxRows, 3];
    private readonly bool[] rowGlowTintApplied = new bool[MaxRows];
    private readonly bool[] originalIconGlowOnTop = new bool[MaxRows];
    private readonly bool[] iconGlowOnTopApplied = new bool[MaxRows];
    private readonly byte[,] originalTimerFont = new byte[MaxRows, StatusIconSlots];
    private readonly byte[,] appliedTimerFont = new byte[MaxRows, StatusIconSlots];
    private readonly float[,] originalTimerX = new float[MaxRows, StatusIconSlots];
    private readonly float[,] originalTimerY = new float[MaxRows, StatusIconSlots];
    private readonly float[,] appliedTimerX = new float[MaxRows, StatusIconSlots];
    private readonly float[,] appliedTimerY = new float[MaxRows, StatusIconSlots];
    private readonly bool[,] timerApplied = new bool[MaxRows, StatusIconSlots];
    private readonly byte[] originalNameFont = new byte[MaxRows];
    private readonly byte[] appliedNameFont = new byte[MaxRows];
    private readonly bool[] nameFontApplied = new bool[MaxRows];
    private readonly TextColorState[] nameColor = new TextColorState[MaxRows];
    private readonly byte[] originalIndexFont = new byte[MaxRows];
    private readonly byte[] appliedIndexFont = new byte[MaxRows];
    private readonly float[] originalIndexX = new float[MaxRows];
    private readonly float[] originalIndexY = new float[MaxRows];
    private readonly float[] appliedIndexX = new float[MaxRows];
    private readonly float[] appliedIndexY = new float[MaxRows];
    private readonly bool[] indexApplied = new bool[MaxRows];
    private readonly TextColorState[] indexColor = new TextColorState[MaxRows];
    private readonly string[] originalNameText = new string[MaxRows];
    private readonly string[] appliedNameText = new string[MaxRows];
    private readonly string[] appliedNameExtra = new string[MaxRows];
    private readonly bool[] nameTextApplied = new bool[MaxRows];
    private readonly byte[,,] originalGaugeFont = new byte[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly byte[,,] appliedGaugeFont = new byte[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly float[,,] originalGaugeX = new float[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly float[,,] appliedGaugeX = new float[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly float[,,] originalGaugeY = new float[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly float[,,] appliedGaugeY = new float[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly bool[,,] gaugeTextApplied = new bool[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly TextColorState[,,] gaugeTextColor = new TextColorState[MaxRows, GaugeCount, GaugeTextSlots];
    private readonly float[] originalHpArrowX = new float[MaxRows];
    private readonly float[] originalHpArrowY = new float[MaxRows];
    private readonly float[] appliedHpArrowX = new float[MaxRows];
    private readonly float[] appliedHpArrowY = new float[MaxRows];
    private readonly bool[] hpArrowApplied = new bool[MaxRows];
    private readonly Vector4[] lastBarColor = new Vector4[MaxRows];

    /// <summary>
    /// Sentinel for "nothing has been written to this node's colour yet". A real colour can
    /// never compare equal to it, so a fresh node always gets its first write - without it a
    /// rebuilt node kept the transparent colour it was created with whenever the colour the
    /// row resolved to happened to match the one written before the rebuild.
    /// </summary>
    private static readonly Vector4 NoColor = new(float.NaN);

    /// <summary>Last line <see cref="LogBarState"/> emitted per row, so it only logs on change.</summary>
    private readonly string[] lastBarTrace = new string[MaxRows];
    private string lastGateTrace = string.Empty;
    private readonly Dictionary<string, CombatantEntry> statsByName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Your own row's stats and the encounter-wide sums, for the header metrics.</summary>
    private CombatantEntry? localPlayerStats;
    private GroupAggregates? encounterAggregates;

    /// <summary>Every player in the encounter, in snapshot order, for the rank stamp.</summary>
    private readonly List<CombatantEntry> rankableCombatants = new();
    private bool ranksNeeded;

    private string originalTotalsText = string.Empty;
    private string appliedTotalsText = string.Empty;
    private string appliedTotalsExtra = string.Empty;
    private bool totalsApplied;

    private byte originalTotalsFont;
    private byte appliedTotalsFont;
    private float originalTotalsX;
    private float originalTotalsY;
    private float appliedTotalsX;
    private float appliedTotalsY;
    private bool totalsStyleApplied;
    private TextColorState totalsColor;


    private double maxDps;
    private PartyListOverlaySettings Settings => config.PartyList;

    private bool encounterActive;
    private DateTime lastEncounterActive = DateTime.MinValue;

    /// <summary>
    /// Whether the parts derived from parse data - the bars, the name metrics and the
    /// header totals - should draw. The restyle is deliberately not gated, so the
    /// rows keep their layout between pulls instead of jumping on every boundary.
    /// </summary>
    private bool MetricsVisible
        => !Settings.HideOutOfCombat
           || encounterActive
           || (DateTime.UtcNow - lastEncounterActive).TotalSeconds < Settings.HideOutOfCombatDelay;

    private DateTime lastCacheRefresh = DateTime.MinValue;
    private bool enabled;
    private bool disposed;

    public PartyListDpsOverlay(DataService dataService, Configuration config)
    {
        this.dataService = dataService;
        this.config = config;

        for (var i = 0; i < MaxRows; i++)
        {
            ClearMetricText(i);
            lastBarWidth[i] = -1f;
            lastBarHeight[i] = -1f;
            lastBarPos[i] = new Vector2(float.NaN, float.NaN);
            lastBarColor[i] = NoColor;
        }

        controller = new AddonController<AddonPartyList>
        {
            AddonName = "_PartyList",
            OnSetup = HandleSetup,
            OnRefresh = HandleRefresh,
            OnUpdate = HandleUpdate,
            OnDraw = HandleDraw,
            OnFinalize = HandleFinalize,
        };
    }

    /// <summary>
    /// Enabling while the party list is already open still creates the nodes, and
    /// disabling while it is open still tears them down - the controller replays
    /// setup/finalize against the live addon.
    /// Must be called on the framework thread; KamiToolKit throws otherwise.
    /// </summary>
    public void SetEnabled(bool value)
    {
        if (disposed || value == enabled)
            return;

        enabled = value;
        if (value)
        {
            controller.Enable();

            // The controller only replays setup for an addon it considers live, but the
            // party list persists while hidden - solo, for instance - so drive it ourselves.
            var existing = LivePartyList();
            if (existing != null && overlayRoot == null)
                HandleSetup(existing);

            // KamiToolKit's OnDraw is a *pre*-draw hook, and the game repositions the glow
            // nodes during its own draw - so anything written before it is overwritten for
            // that frame. This fires afterwards, which is the only place a write survives
            // the frame it is made on.
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "_PartyList", HandlePostDraw);
        }
        else
        {
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostDraw, "_PartyList", HandlePostDraw);
            TearDownNow();
            controller.Disable();
        }
    }

    /// <summary>The party list even when hidden - it stays allocated while you are solo.</summary>
    private static AddonPartyList* LivePartyList()
        => (AddonPartyList*)Svc.GameGui.GetAddonByName("_PartyList").Address;

    /// <summary>
    /// Removes our nodes ourselves rather than waiting for the controller's finalize, which
    /// is skipped when it doesn't consider the addon live. Reloading the plugin while the
    /// party list is hidden would otherwise leave our nodes linked into an addon that
    /// outlives us, and the game crashes walking them on its next update.
    /// </summary>
    private void TearDownNow()
    {
        var addon = LivePartyList();
        if (addon != null)
            HandleFinalize(addon);
    }

    private void HandlePostDraw(AddonEvent type, AddonArgs args)
    {
        if (!Settings.AdjustSelectionGlow)
            return;

        ApplySelectionGlowLayout((AddonPartyList*)args.Addon.Address);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        // Ours come out here, not from the controller's finalize - that is skipped when the
        // addon isn't considered live, which is the case whenever the list is hidden.
        if (enabled)
            TearDownNow();

        if (enabled)
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostDraw, "_PartyList", HandlePostDraw);

        controller.Dispose();

        // Anything still waiting to be handed over was never adopted by a node, so it is ours.
        for (var i = 0; i < MaxRows; i++)
            Interlocked.Exchange(ref pendingBarTexture[i], null)?.Dispose();
    }

    /// <summary>Ours are decoration, so they stay out of the addon's input handling.</summary>
    private static void MakeNonInteractive(KamiToolKit.BaseTypes.NodeBase node)
        => node.RemoveNodeFlags(NodeFlags.RespondToMouse, NodeFlags.EmitsEvents, NodeFlags.HasCollision);

    /// <summary>
    /// Unlinks a node from its parent without freeing it. Used only on nodes left behind by
    /// a previous plugin instance, whose memory we can't release - leaking them is the lesser
    /// evil, since the game walks this tree and dereferences whatever it finds.
    /// </summary>
    private static void UnlinkNode(AtkResNode* parent, AtkResNode* node)
    {
        var previous = node->PrevSiblingNode;
        var next = node->NextSiblingNode;

        if (previous != null)
            previous->NextSiblingNode = next;
        if (next != null)
            next->PrevSiblingNode = previous;

        // ChildNode heads the chain that PrevSiblingNode walks.
        if (parent->ChildNode == node)
            parent->ChildNode = previous;

        node->ParentNode = null;
        node->PrevSiblingNode = null;
        node->NextSiblingNode = null;

        if (parent->ChildCount > 0)
            parent->ChildCount--;
    }

    /// <summary>
    /// Removes nodes carrying our ids that we don't own. A plugin reload leaves the previous
    /// instance's nodes linked into the addon while its memory is gone, and the game's next
    /// tree walk - rebuilding the collision list - dereferences them and crashes.
    /// </summary>
    private static void SweepOrphanedNodes(AtkResNode* parent)
    {
        if (parent == null)
            return;

        var child = parent->ChildNode;
        while (child != null)
        {
            var previous = child->PrevSiblingNode;

            if (child->NodeId >= OwnNodeIdMin && child->NodeId <= OwnNodeIdMax)
            {
                Svc.Log.Debug($"[PartyList] Unlinking orphaned node {child->NodeId:X} from a previous session.");
                UnlinkNode(parent, child);
            }

            child = previous;
        }
    }

    /// <summary>
    /// Puts a container immediately after the party list's backdrop in the root's children,
    /// which is early enough in the draw order to sit behind the rows without prepending -
    /// prepending shifts every node the addon already had and it misaddresses its own.
    /// </summary>
    private bool EnsureBarRoot(AddonPartyList* addon)
    {
        if (barRoot != null)
            return true;

        var root = addon->RootNode;
        var backdrop = addon->BackgroundNineGridNode;
        if (root == null || backdrop == null)
            return false;

        barRoot = new ResNode
        {
            NodeId = BarRootNodeId,
            Position = Vector2.Zero,
            Size = new Vector2(root->Width, root->Height),
            IsVisible = true,
        };

        MakeNonInteractive(barRoot);
        barRoot.AttachNode(&backdrop->AtkResNode, NodePosition.AfterTarget);
        return true;
    }

    /// <summary>
    /// Creates a row's fill node under whichever parent the current setting asks for, and
    /// re-creates it if that setting changed - the parent is fixed at attach time.
    /// </summary>
    private void EnsureBarNode(AddonPartyList* addon, int row)
    {
        var wantBehind = Settings.BarBehindRowContent && EnsureBarRoot(addon);

        if (barNodes[row] != null)
        {
            if (barOnBarRoot[row] == wantBehind)
                return;

            barNodes[row]!.Dispose();
            barNodes[row] = null;
            barTextureApplied[row] = false;
        }

        // Appended, never prepended: inserting at the front shifts every pre-existing node
        // in the row and the game misaddresses its own.
        var bar = new ImGuiImageNode
        {
            NodeId = BarNodeIdBase + (uint)row,
            Size = Vector2.Zero,
            Position = Vector2.Zero,
            Color = new Vector4(1f, 1f, 1f, 0f),
            IsVisible = false,
        };

        MakeNonInteractive(bar);
        bar.AttachNode((AtkResNode*)(wantBehind ? barRoot! : overlayRoot!), NodePosition.AsLastChild);

        barOnBarRoot[row] = wantBehind;
        barNodes[row] = bar;
        lastBarWidth[row] = -1f;
        lastBarHeight[row] = -1f;
        lastBarPos[row] = new Vector2(float.NaN, float.NaN);
        lastBarColor[row] = NoColor;
    }

    /// <summary>
    /// Creates a row's metric text nodes in our own container. They are placed on the row
    /// by projecting its rectangle every frame rather than by living inside it, so a row
    /// changing which component it resolves to costs nothing and the party member
    /// components keep the node lists the game gave them.
    /// </summary>
    private void EnsureMetricNodes(int row)
    {
        if (overlayRoot == null)
            return;

        for (var slot = 0; slot < MetricSlots; slot++)
        {
            if (metricNodes[row, slot] != null)
                continue;

            var metrics = new TextNode
            {
                NodeId = MetricNodeIdBase + (uint)(row * MetricSlots + slot),
                Size = new Vector2(160f, 20f),
                // Overwritten each frame from the name node's font.
                FontSize = 14,
                AlignmentType = AlignmentType.Left,
                TextColor = new Vector4(1f, 1f, 1f, 1f),
                TextOutlineColor = new Vector4(0f, 0f, 0f, 1f),
                TextFlags = TextFlags.Edge,
                IsVisible = false,
            };

            MakeNonInteractive(metrics);
            metrics.AttachNode((AtkResNode*)overlayRoot, NodePosition.AsLastChild);
            metricNodes[row, slot] = metrics;
            lastMetricText[row, slot] = string.Empty;
        }
    }

    private void HandleSetup(AddonPartyList* addon)
    {
        var root = addon->RootNode;
        if (root == null)
            return;

        // A live overlayRoot here means setup arrived without a finalize, which is the case
        // the sweep below silently detaches our own containers in.
        ServiceManager.LogInfo(
            LogChannel.PartyMembership,
            $"[PartyList] Setup addon={(nint)addon:X} root={(nint)root:X} " +
            $"overlayRoot={(overlayRoot == null ? "null" : "LIVE")} barRoot={(barRoot == null ? "null" : "LIVE")}");

        // Anything already carrying our ids is a leftover from a previous instance.
        SweepOrphanedNodes(root);
        for (var i = 0; i < MaxRows; i++)
        {
            SweepOrphanedNodes(addon->PartyMembers[i].TargetGlowContainer);
            SweepOrphanedNodes(addon->TrustMembers[i].TargetGlowContainer);
            SweepOrphanedNodes(RowComponentNode(addon->PartyMembers[i].PartyMemberComponent));
            SweepOrphanedNodes(RowComponentNode(addon->TrustMembers[i].PartyMemberComponent));
        }

        if (overlayRoot == null)
        {
            overlayRoot = new ResNode
            {
                NodeId = OverlayRootNodeId,
                Position = Vector2.Zero,
                Size = new Vector2(root->Width, root->Height),
                IsVisible = true,
            };

            MakeNonInteractive(overlayRoot);
            overlayRoot.AttachNode(root, NodePosition.AsLastChild);
        }

        for (var i = 0; i < MaxRows; i++)
        {
            if (GetRowNode(addon, i) == null)
                continue;

            EnsureBarNode(addon, i);
            EnsureMetricNodes(i);
        }

        // Rebuild so the list matches the tree we just changed.
        addon->UpdateCollisionNodeList(false);

        PositionNodes(addon);
        ApplyRowSpacing(addon);
        ApplyRowContentShift(addon);
        ApplyShieldStyles(addon);
        ApplyGaugeOutlines(addon);
        ApplyCastBarLayout(addon);
        ApplyCastNameLayout(addon);
        ApplyGaugeNumberLayout(addon);
        ApplyNameStyle(addon);
        ApplyNameText(addon);
        ApplyPartyIndexLayout(addon);
        ApplyStatusIconLayout(addon);
        ApplyStatusTimerLayout(addon);
        ApplySelectionGlowLayout(addon);
        ApplyTotalsTextStyle(addon);
        ApplyEncounterTotals(addon);
    }

    private void HandleRefresh(AddonPartyList* addon)
    {
        PositionNodes(addon);
        ApplyRowSpacing(addon);
        ApplyRowContentShift(addon);
        ApplyShieldStyles(addon);
        ApplyGaugeOutlines(addon);
        ApplyCastBarLayout(addon);
        ApplyCastNameLayout(addon);
        ApplyGaugeNumberLayout(addon);
        ApplyNameStyle(addon);
        ApplyNameText(addon);
        ApplyPartyIndexLayout(addon);
        ApplyStatusIconLayout(addon);
        ApplyStatusTimerLayout(addon);
        ApplySelectionGlowLayout(addon);
        ApplyTotalsTextStyle(addon);
        ApplyEncounterTotals(addon);
    }

    /// <summary>
    /// Re-applies the glow styling immediately before the draw. The game re-sets these
    /// nodes from their animation keyframes on the frame a glow appears, which lands after
    /// our update pass - doing it here too stops that one frame drawing unstyled.
    /// </summary>
    private void HandleDraw(AddonPartyList* addon) => ApplySelectionGlowLayout(addon);

    private void HandleFinalize(AddonPartyList* addon)
    {
        ServiceManager.LogInfo(
            LogChannel.PartyMembership,
            $"[PartyList] Finalize addon={(nint)addon:X} overlayRoot={(overlayRoot == null ? "null" : "LIVE")}");

        // Hand the game's own nodes back before ours go away.
        RestoreEncounterTotals(addon);
        RestoreTotalsTextStyle(addon);
        RestoreSelectionGlowLayout(addon);
        RestoreStatusTimerLayout(addon);
        RestoreStatusIconLayout(addon);
        RestorePartyIndexStyle(addon);
        RestoreNameText(addon);
        RestoreNameStyle(addon);
        RestoreGaugeNumberLayout(addon);
        RestoreCastNameLayout(addon);
        RestoreCastBarLayout(addon);
        RestoreGaugeOutlines(addon);
        RestoreShieldStyles(addon);
        RestoreRowContentShift(addon);
        RestoreRowSpacing(addon);

        for (var i = 0; i < MaxRows; i++)
        {
            barNodes[i]?.Dispose();
            barNodes[i] = null;

            for (var slot = 0; slot < MetricSlots; slot++)
            {
                metricNodes[i, slot]?.Dispose();
                metricNodes[i, slot] = null;
            }

            ClearMetricText(i);
            lastBarWidth[i] = -1f;
            lastBarHeight[i] = -1f;
            lastBarPos[i] = new Vector2(float.NaN, float.NaN);
            lastBarColor[i] = NoColor;
            lastBarTrace[i] = string.Empty;
        }

        Array.Clear(barTextureApplied);

        // Our containers go last, once everything inside them has gone.
        overlayRoot?.Dispose();
        overlayRoot = null;
        barRoot?.Dispose();
        barRoot = null;
        Array.Clear(barOnBarRoot);

        // Rebuild after removing ours too, so nothing detached is left in the list.
        if (addon != null)
            addon->UpdateCollisionNodeList(false);
    }

    /// <summary>Keeps our container matched to the addon's root, which moves with the HUD.</summary>
    private void SyncOverlayRoot(AddonPartyList* addon)
    {
        var root = addon->RootNode;
        if (overlayRoot == null || root == null)
            return;

        var size = new Vector2(root->Width, root->Height);
        if (overlayRoot.Size != size)
            overlayRoot.Size = size;

        if (overlayRoot.Position != Vector2.Zero)
            overlayRoot.Position = Vector2.Zero;

        if (barRoot == null)
            return;

        if (barRoot.Size != size)
            barRoot.Size = size;

        if (barRoot.Position != Vector2.Zero)
            barRoot.Position = Vector2.Zero;
    }

    private void HandleUpdate(AddonPartyList* addon)
    {
        // Ahead of the apply pass, so the header totals and the rows agree on whether an
        // encounter is live this frame rather than one frame apart.
        RefreshCacheIfStale();
        StampRanks();

        // Read once a frame rather than once a metric - it is a sheet lookup and a game setting,
        // and every row wants the same answer.
        paletteNameColor = GameUiColors.PartyListName;
        paletteNameOutline = GameUiColors.PartyListNameOutline;

        SyncOverlayRoot(addon);

        if (overlayRoot != null)
            for (var i = 0; i < MaxRows; i++)
                if (GetRowNode(addon, i) != null)
                    EnsureBarNode(addon, i);

        ApplyRowSpacing(addon);
        ApplyRowContentShift(addon);
        ApplyShieldStyles(addon);
        ApplyGaugeOutlines(addon);
        ApplyCastBarLayout(addon);
        ApplyCastNameLayout(addon);
        ApplyGaugeNumberLayout(addon);
        ApplyNameStyle(addon);
        ApplyNameText(addon);
        ApplyPartyIndexLayout(addon);
        ApplyStatusIconLayout(addon);
        ApplyStatusTimerLayout(addon);
        ApplySelectionGlowLayout(addon);
        ApplyTotalsTextStyle(addon);
        ApplyEncounterTotals(addon);

        var agent = AgentHUD.Instance();
        var members = agent == null ? default : agent->PartyMembers;
        var count = agent == null ? 0 : Math.Min(agent->PartyMemberCount, members.Length);

        // Out of combat every row is treated as unmatched, which is already the path that
        // hides the metrics node and zero-widths the bar.
        var showMetrics = MetricsVisible;

        var gate = $"{showMetrics}|{encounterActive}|{count}|{addon->MemberCount}|{addon->TrustCount}|{statsByName.Count > 0}";
        if (lastGateTrace != gate)
        {
            lastGateTrace = gate;
            ServiceManager.LogInfo(
                LogChannel.PartyMembership,
                $"[PartyList] gate showMetrics={showMetrics} encounterActive={encounterActive} " +
                $"hideOutOfCombat={Settings.HideOutOfCombat} showBar={Settings.ShowBar} " +
                $"rows={count} party={addon->MemberCount} trust={addon->TrustCount} " +
                $"parsedNames={statsByName.Count} maxDps={maxDps:F0}");
        }

        for (var i = 0; i < MaxRows; i++)
        {
            CombatantEntry? stats = null;

            if (showMetrics && i < count)
            {
                var name = members[i].Name.ToString();
                if (!string.IsNullOrEmpty(name))
                    statsByName.TryGetValue(name, out stats);
            }

            UpdateBar(addon, i, stats);
            UpdateMetrics(addon, i, stats);
        }
    }

    /// <summary>
    /// The metrics get their own text node so they can be sized independently - a single
    /// Atk text node has one font size, which is why the game itself splits the MP value
    /// across two nodes. Placed by its own offsets from the row's corner, so where a metric
    /// sits is the user's to decide and nothing about the name moves it.
    /// </summary>
    /// <summary>
    /// Copies every property that decides how the name is drawn, so the metrics read as a
    /// continuation of it rather than as a second label. Size and colour are the only things
    /// that can differ, and only by what the metric's own style asks for.
    /// </summary>
    private void CopyNameFont(TextNode node, AtkTextNode* name, NameMetricStyle style)
    {
        var font = (uint)Math.Clamp(name->FontSize + style.FontDelta, 6, 60);
        if (node.FontSize != font)
            node.FontSize = font;

        if (node.FontType != name->FontType)
            node.FontType = name->FontType;

        // The left-aligned member of whichever vertical band the name uses - a metric is
        // positioned by its own offset, so only the vertical part is worth copying.
        var alignment = (AlignmentType)((int)name->AlignmentType / 3 * 3);
        if (node.AlignmentType != alignment)
            node.AlignmentType = alignment;

        var flags = Settings.TintTextOutline
            ? OutlineFlags(name->TextFlags)
            : name->TextFlags;
        if (node.TextFlags != flags)
            node.TextFlags = flags;
        if (node.LineSpacing != name->LineSpacing)
            node.LineSpacing = name->LineSpacing;
        if (node.CharSpacing != name->CharSpacing)
            node.CharSpacing = name->CharSpacing;

        var raw = (AtkTextNode*)node;
        if (raw->SheetType != name->SheetType)
            raw->SheetType = name->SheetType;

        // Inheriting means the colour the game paints a resting name with on this player's UI
        // theme, not whatever the name node holds this frame: the row's timeline moves that
        // colour as the row changes state, which is what dragged the metrics along when a cast
        // bar took the name over. The node is still the fallback if the palette can't be read.
        var textColor = style.UseCustomColor
            ? new Vector4(style.Color.X, style.Color.Y, style.Color.Z, 1f)
            : paletteNameColor ?? ToVector4(name->TextColor);
        if (node.TextColor != textColor)
            node.TextColor = textColor;

        // The metric's own outline wins over the party list wide tint, being the narrower setting.
        var edgeColor = style.UseCustomOutlineColor
            ? new Vector4(style.OutlineColor.X, style.OutlineColor.Y, style.OutlineColor.Z, 1f)
            : paletteNameOutline ?? ToVector4(name->EdgeColor);
        if (!style.UseCustomOutlineColor && Settings.TintTextOutline)
        {
            var tint = Settings.TextOutlineTint;
            edgeColor = new Vector4(tint.X, tint.Y, tint.Z, edgeColor.W);
        }

        if (node.TextOutlineColor != edgeColor)
            node.TextOutlineColor = edgeColor;
    }

    private static Vector4 ToVector4(ByteColor color)
        => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    private static ByteColor ToByteColor(Vector4 color) => new()
    {
        R = (byte)Math.Clamp(color.X * 255f, 0f, 255f),
        G = (byte)Math.Clamp(color.Y * 255f, 0f, 255f),
        B = (byte)Math.Clamp(color.Z * 255f, 0f, 255f),
        A = (byte)Math.Clamp(color.W * 255f, 0f, 255f),
    };

    private static bool SameColor(ByteColor a, ByteColor b)
        => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;

    /// <summary>
    /// One game-owned text node's colour, captured before our first write and handed back on
    /// teardown. Same self-correcting capture as everything else here: a colour that isn't
    /// the one we last wrote belongs to the game again.
    /// </summary>
    private struct TextColorState
    {
        public ByteColor Original;
        public ByteColor Applied;
        public bool Active;
        public ByteColor OriginalEdge;
        public ByteColor AppliedEdge;
        public bool EdgeActive;
        public TextFlags OriginalFlags;
        public TextFlags AppliedFlags;
        public bool FlagsActive;
    }

    /// <summary>
    /// Recolours a text node, keeping the game's own alpha so a dimmed or fading row still
    /// dims. Passing <paramref name="useCustom"/> false hands the colour back. The outline is
    /// styled either way - it is a party list wide setting, not part of the colour override.
    /// </summary>
    private void ApplyTextColor(AtkTextNode* text, bool useCustom, Vector4 color, ref TextColorState state)
    {
        if (text == null)
            return;

        if (useCustom)
        {
            if (!state.Active || !SameColor(text->TextColor, state.Applied))
                state.Original = text->TextColor;

            var target = ToByteColor(color);
            target.A = state.Original.A;

            if (!SameColor(text->TextColor, target))
                text->TextColor = target;

            state.Applied = target;
            state.Active = true;
        }
        else if (state.Active)
        {
            state.Active = false;
            text->TextColor = state.Original;
        }

        ApplyTextOutline(text, ref state);
    }

    /// <summary>
    /// Gives the glyph outline - the edge around the text - the configured colour and weight.
    /// The game's own edge alpha is kept, so text the game fades still fades.
    /// </summary>
    private void ApplyTextOutline(AtkTextNode* text, ref TextColorState state)
    {
        if (!Settings.TintTextOutline)
        {
            RestoreTextOutline(text, ref state);
            return;
        }

        if (!state.EdgeActive || !SameColor(text->EdgeColor, state.AppliedEdge))
            state.OriginalEdge = text->EdgeColor;

        var target = ToByteColor(Settings.TextOutlineTint);
        target.A = state.OriginalEdge.A;

        if (!SameColor(text->EdgeColor, target))
            text->EdgeColor = target;

        state.AppliedEdge = target;
        state.EdgeActive = true;

        if (!state.FlagsActive || text->TextFlags != state.AppliedFlags)
            state.OriginalFlags = text->TextFlags;

        var flags = OutlineFlags(state.OriginalFlags);
        if (text->TextFlags != flags)
            text->TextFlags = flags;

        state.AppliedFlags = flags;
        state.FlagsActive = true;
    }

    /// <summary>
    /// The game has no outline width - it has an edge pass and a wider glare pass, both drawn
    /// in the edge colour - so thickness is which of the two the text is drawn with. The rest
    /// of the text's flags are left alone.
    /// </summary>
    private TextFlags OutlineFlags(TextFlags flags) => Settings.TextOutlineThickness switch
    {
        PartyListOutlineThickness.None => flags & ~(TextFlags.Edge | TextFlags.Glare),
        PartyListOutlineThickness.Thick => flags | TextFlags.Edge | TextFlags.Glare,
        _ => (flags | TextFlags.Edge) & ~TextFlags.Glare,
    };

    private static void RestoreTextColor(AtkTextNode* text, ref TextColorState state)
    {
        RestoreTextOutline(text, ref state);

        if (!state.Active)
            return;

        state.Active = false;
        if (text != null)
            text->TextColor = state.Original;
    }

    private static void RestoreTextOutline(AtkTextNode* text, ref TextColorState state)
    {
        if (state.FlagsActive)
        {
            state.FlagsActive = false;
            if (text != null)
                text->TextFlags = state.OriginalFlags;
        }

        if (!state.EdgeActive)
            return;

        state.EdgeActive = false;
        if (text != null)
            text->EdgeColor = state.OriginalEdge;
    }

    /// <summary>
    /// One game-owned node's colour multiply. Artwork can only be tinted, not recoloured, so
    /// the setting is applied relative to the game's own multiply - which is 100, not 255 -
    /// and white therefore leaves the node exactly as it was.
    /// </summary>
    private struct NodeTintState
    {
        public byte Red;
        public byte Green;
        public byte Blue;
        public bool Active;
    }

    private static void ApplyNodeTint(AtkResNode* node, Vector4 tint, ref NodeTintState state)
    {
        if (node == null)
            return;

        if (!state.Active)
        {
            state.Red = node->MultiplyRed;
            state.Green = node->MultiplyGreen;
            state.Blue = node->MultiplyBlue;
            state.Active = true;
        }

        node->MultiplyRed = (byte)Math.Clamp(state.Red * tint.X, 0f, 255f);
        node->MultiplyGreen = (byte)Math.Clamp(state.Green * tint.Y, 0f, 255f);
        node->MultiplyBlue = (byte)Math.Clamp(state.Blue * tint.Z, 0f, 255f);
    }

    private static void RestoreNodeTint(AtkResNode* node, ref NodeTintState state)
    {
        if (!state.Active)
            return;

        state.Active = false;
        if (node == null)
            return;

        node->MultiplyRed = state.Red;
        node->MultiplyGreen = state.Green;
        node->MultiplyBlue = state.Blue;
    }

    /// <summary>
    /// One game-owned node's alpha as the game left it, captured before our first write.
    /// Self-correcting: the game fades the artwork itself as a row comes and goes, so
    /// anything that isn't the value we last wrote becomes the new original.
    /// </summary>
    private struct NodeAlphaState
    {
        public bool Active;
        public byte Original;
        public byte Applied;
    }

    /// <summary>
    /// Fades a node relative to the alpha the game gave it, so one that is animating in still
    /// fades in - it just tops out lower.
    /// </summary>
    private static void ApplyNodeAlpha(AtkResNode* node, float opacity, ref NodeAlphaState state)
    {
        if (!state.Active || node->Color.A != state.Applied)
        {
            state.Original = node->Color.A;
            state.Active = true;
        }

        var target = (byte)Math.Clamp(state.Original * opacity, 0f, 255f);
        node->Color.A = target;
        state.Applied = target;
    }

    private static void RestoreNodeAlpha(AtkResNode* node, ref NodeAlphaState state)
    {
        if (!state.Active)
            return;

        state.Active = false;
        if (node != null)
            node->Color.A = state.Original;
    }

    /// <summary>
    /// One piece of a gauge bar's own artwork. A tint on the bar's owner node would multiply
    /// down onto everything inside it - MP's digits, which are children of the bar component,
    /// and HP's shield, which is styled separately. Tinting the art nodes individually
    /// colours the bar alone.
    /// </summary>
    private static AtkResNode* GetGaugeArtNode(AddonPartyList* addon, int row, int slot, int art)
    {
        var member = RowMember(addon, row);
        var bar = member == null ? null : slot == HpBarSlot ? member->HPGaugeBar : member->MPGaugeBar;
        if (bar == null)
            return null;

        return art switch
        {
            0 => bar->BackdropImageNode == null ? null : &bar->BackdropImageNode->AtkResNode,
            1 => bar->PrimaryFill.MainFillNode == null ? null : &bar->PrimaryFill.MainFillNode->AtkResNode,
            2 => bar->PrimaryFill.IncreaseFillNode == null ? null : &bar->PrimaryFill.IncreaseFillNode->AtkResNode,
            3 => bar->PrimaryFill.DecreaseFillNode == null ? null : &bar->PrimaryFill.DecreaseFillNode->AtkResNode,
            _ => null,
        };
    }

    private void ApplyGaugeBarTint(AddonPartyList* addon, int row, int slot, RowPartStyle part)
    {
        for (var art = GaugeFillArtFirst; art < GaugeArtSlots; art++)
        {
            var node = GetGaugeArtNode(addon, row, slot, art);
            if (part.UseCustomColor)
                ApplyNodeTint(node, part.Color, ref gaugeArtTint[row, slot, art]);
            else
                RestoreNodeTint(node, ref gaugeArtTint[row, slot, art]);
        }
    }

    private void RestoreGaugeBarTint(AddonPartyList* addon, int row, int slot)
    {
        for (var art = GaugeFillArtFirst; art < GaugeArtSlots; art++)
            RestoreNodeTint(addon == null ? null : GetGaugeArtNode(addon, row, slot, art),
                ref gaugeArtTint[row, slot, art]);
    }

    private GaugeOutlineStyle OutlinePart(int slot)
        => slot == HpBarSlot ? Settings.HpBarOutline : Settings.MpBarOutline;

    /// <summary>
    /// Tints and fades the empty-bar artwork behind each gauge - what reads as the bar's
    /// outline. It is part of the bar's own texture rather than a node of its own, so there
    /// is no width to set. Kept out of the fill's pass so the outline can be styled whether
    /// or not the bar itself is being adjusted.
    /// </summary>
    private void ApplyGaugeOutlines(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        for (var row = 0; row < MaxRows; row++)
        {
            ApplyGaugeOutline(addon, row, HpBarSlot);
            ApplyGaugeOutline(addon, row, MpBarSlot);
        }
    }

    private void ApplyGaugeOutline(AddonPartyList* addon, int row, int slot)
    {
        var style = OutlinePart(slot);
        var node = GetGaugeArtNode(addon, row, slot, GaugeOutlineArt);
        if (node == null)
            return;

        ref var tint = ref gaugeArtTint[row, slot, GaugeOutlineArt];
        if (TryGetOutlineTint(slot, style, out var color))
            ApplyNodeTint(node, color, ref tint);
        else
            RestoreNodeTint(node, ref tint);

        var opacity = style.Hidden ? 0f : Math.Clamp(style.Opacity, 0f, 1f);
        if (opacity < 1f)
            ApplyNodeAlpha(node, opacity, ref gaugeOutlineAlpha[row, slot]);
        else
            RestoreNodeAlpha(node, ref gaugeOutlineAlpha[row, slot]);
    }

    /// <summary>
    /// The colour the outline is tinted with, if any. Following the bar means exactly what
    /// the backdrop did before it was split out - the bar's colour, and only where the bar
    /// is tinting its fill with it.
    /// </summary>
    private bool TryGetOutlineTint(int slot, GaugeOutlineStyle style, out Vector4 tint)
    {
        switch (style.ColorMode)
        {
            case GaugeOutlineColorMode.Custom:
                tint = style.Color;
                return true;

            case GaugeOutlineColorMode.FollowBar:
                var bar = RowPart(slot);
                tint = bar?.Color ?? default;
                return bar is { Enabled: true, UseCustomColor: true };

            default:
                tint = default;
                return false;
        }
    }

    private void RestoreGaugeOutlines(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            RestoreGaugeOutline(addon, row, HpBarSlot);
            RestoreGaugeOutline(addon, row, MpBarSlot);
        }
    }

    private void RestoreGaugeOutline(AddonPartyList* addon, int row, int slot)
    {
        ref var alpha = ref gaugeOutlineAlpha[row, slot];
        ref var tint = ref gaugeArtTint[row, slot, GaugeOutlineArt];
        if (!alpha.Active && !tint.Active)
            return;

        var node = addon == null ? null : GetGaugeArtNode(addon, row, slot, GaugeOutlineArt);
        RestoreNodeAlpha(node, ref alpha);
        RestoreNodeTint(node, ref tint);
    }

    /// <summary>
    /// One shield node's transform and alpha as the game left them, captured before our first
    /// write to it. Position and fade are tracked separately because either can be styled
    /// without the other.
    /// </summary>
    private struct ShieldNodeState
    {
        public bool TransformActive;
        public float OriginalX;
        public float OriginalY;
        public float AppliedX;
        public float AppliedY;
        public float OriginalScale;
        public float AppliedScale;
        public float OriginalOriginX;
        public float OriginalOriginY;

        public NodeAlphaState Alpha;
        public NodeTintState Tint;
    }

    /// <summary>
    /// One node of a shield piece. The fill inside the HP bar is three layers; the overflow
    /// bar is the same three plus the icon the game shows when a shield is too big to draw.
    /// </summary>
    private static AtkResNode* GetShieldNode(AddonPartyList* addon, int row, int group, int index)
    {
        var member = RowMember(addon, row);
        var bar = member == null ? null : member->HPGaugeBar;
        if (bar == null)
            return null;

        if (index == ShieldMaxIconIndex)
            return group == ShieldOverflowGroup && bar->SecondaryOverflowMaxIcon != null
                ? &bar->SecondaryOverflowMaxIcon->AtkResNode
                : null;

        var fill = group == ShieldFillGroup ? bar->SecondaryFill : bar->SecondaryOverflow;
        var node = index switch
        {
            0 => fill.MainFillNode,
            1 => fill.IncreaseFillNode,
            2 => fill.DecreaseFillNode,
            _ => null,
        };

        return node == null ? null : &node->AtkResNode;
    }

    private ShieldStyle ShieldPart(int group)
        => group == ShieldFillGroup ? Settings.ShieldFill : Settings.ShieldOverflow;

    /// <summary>
    /// Moves, scales, tints and fades the shield drawn over each HP bar. The shield isn't one
    /// node but a set of sibling layers inside the HP gauge, so every setting is written to
    /// each layer of the piece. The game rewrites their position and width as HP and the
    /// shield change, so the capture is self-correcting the same way the row shift's is:
    /// anything that isn't the value we last wrote becomes the new original.
    /// </summary>
    private void ApplyShieldStyles(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        for (var row = 0; row < MaxRows; row++)
            for (var group = 0; group < ShieldGroups; group++)
                ApplyShieldGroup(addon, row, group, ShieldPart(group));
    }

    private void ApplyShieldGroup(AddonPartyList* addon, int row, int group, ShieldStyle part)
    {
        var opacity = part.Hidden ? 0f : Math.Clamp(part.Opacity, 0f, 1f);
        var fades = opacity < 1f;

        if (!part.Enabled && !part.UseCustomColor && !fades)
        {
            RestoreShieldGroup(addon, row, group);
            return;
        }

        for (var index = 0; index < ShieldNodeSlots; index++)
        {
            var node = GetShieldNode(addon, row, group, index);
            if (node == null)
                continue;

            ref var state = ref shieldState[row, group, index];

            if (part.Enabled)
                ApplyShieldTransform(node, part, ref state);
            else
                RestoreShieldTransform(node, ref state);

            if (part.UseCustomColor)
                ApplyNodeTint(node, part.Color, ref state.Tint);
            else
                RestoreNodeTint(node, ref state.Tint);

            if (fades)
                ApplyNodeAlpha(node, opacity, ref state.Alpha);
            else
                RestoreNodeAlpha(node, ref state.Alpha);
        }
    }

    private static void ApplyShieldTransform(AtkResNode* node, ShieldStyle part, ref ShieldNodeState state)
    {
        var fresh = !state.TransformActive;
        state.TransformActive = true;

        if (fresh || Math.Abs(node->X - state.AppliedX) > 0.01f)
            state.OriginalX = node->X;
        if (fresh || Math.Abs(node->Y - state.AppliedY) > 0.01f)
            state.OriginalY = node->Y;

        var targetX = state.OriginalX + part.OffsetX;
        var targetY = state.OriginalY + part.OffsetY;
        if (Math.Abs(node->X - targetX) > 0.01f || Math.Abs(node->Y - targetY) > 0.01f)
            node->SetPositionFloat(targetX, targetY);

        state.AppliedX = targetX;
        state.AppliedY = targetY;

        if (fresh || Math.Abs(node->ScaleX - state.AppliedScale) > 0.001f)
        {
            state.OriginalScale = node->ScaleX;
            state.OriginalOriginX = node->OriginX;
            state.OriginalOriginY = node->OriginY;
        }

        var targetScale = state.OriginalScale * Math.Max(0.1f, part.Scale);

        node->OriginX = 0f;
        node->OriginY = 0f;
        if (Math.Abs(node->ScaleX - targetScale) > 0.001f || Math.Abs(node->ScaleY - targetScale) > 0.001f)
            node->SetScale(targetScale, targetScale);

        state.AppliedScale = targetScale;
    }

    private static void RestoreShieldTransform(AtkResNode* node, ref ShieldNodeState state)
    {
        if (!state.TransformActive)
            return;

        state.TransformActive = false;
        if (node == null)
            return;

        node->SetPositionFloat(state.OriginalX, state.OriginalY);
        node->OriginX = state.OriginalOriginX;
        node->OriginY = state.OriginalOriginY;
        node->SetScale(state.OriginalScale, state.OriginalScale);
    }

    private void RestoreShieldStyles(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
            for (var group = 0; group < ShieldGroups; group++)
                RestoreShieldGroup(addon, row, group);
    }

    private void RestoreShieldGroup(AddonPartyList* addon, int row, int group)
    {
        for (var index = 0; index < ShieldNodeSlots; index++)
        {
            ref var state = ref shieldState[row, group, index];
            if (!state.TransformActive && !state.Alpha.Active && !state.Tint.Active)
                continue;

            var node = addon == null ? null : GetShieldNode(addon, row, group, index);
            RestoreShieldTransform(node, ref state);
            RestoreNodeAlpha(node, ref state.Alpha);
            RestoreNodeTint(node, ref state.Tint);
        }
    }

    private void UpdateMetrics(AddonPartyList* addon, int row, CombatantEntry? stats)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return;

        EnsureMetricNodes(row);

        var nameNode = member->Name;
        var rowNode = GetRowNode(addon, row);

        // The row's rectangle, read in our container's space where the nodes live. It is the
        // only thing the offsets are measured from - the name's drawn text is never measured,
        // so a long name no longer pushes the metrics along, and a cast bar taking the name
        // over leaves them where they are.
        Bounds rowRect = default;
        var placeable = stats != null && nameNode != null && rowNode != null && overlayRoot != null
                        && TryProjectRect(addon, (AtkResNode*)rowNode, (AtkResNode*)overlayRoot, out rowRect);

        var metrics = Settings.Metrics;

        for (var slot = 0; slot < MetricSlots; slot++)
        {
            var node = metricNodes[row, slot];
            if (node == null)
                continue;

            // Formatted through the meter's own column value, so a metric reads here exactly
            // as it does in the meter window. No tab to take overrides from, hence null.
            var value = placeable && stats != null && slot < metrics.Count
                ? CombatantBarComponent.GetColumnDisplayValue(stats, metrics[slot], config, null)
                : string.Empty;

            // Carried on the metric rather than drawn on its own node, so a metric that has
            // nothing to show takes its separator with it instead of leaving one stranded.
            var text = value.Length > 0 ? Settings.MetricSeparator + value : string.Empty;

            if (lastMetricText[row, slot] != text)
            {
                lastMetricText[row, slot] = text;
                node.String = text;
            }

            var visible = text.Length > 0;
            if (node.IsVisible != visible)
                node.IsVisible = visible;

            if (!visible)
                continue;

            var style = Settings.Style(metrics[slot]);
            CopyNameFont(node, nameNode, style);

            // The name's box height and vertical alignment band, so a metric left on the name's
            // line sits on it whether the game centres its text in the box or hangs it from the top.
            var size = new Vector2(node.Size.X, Math.Max(1f, nameNode->AtkResNode.Height));
            if (node.Size != size)
                node.Size = size;

            // Placed outright rather than chained: every metric is positioned from the row's
            // corner, so moving one leaves the rest exactly where they are.
            node.Position = new Vector2(rowRect.X + style.OffsetX, rowRect.Y + style.OffsetY);
        }
    }

    private void ClearMetricText(int row)
    {
        for (var slot = 0; slot < MetricSlots; slot++)
            lastMetricText[row, slot] = string.Empty;
    }

    /// <summary>
    /// Applies the cap width, and optionally rounds only the right end. A 9-slice can't
    /// simply drop the left cap - zeroing its inset stretches the texture's curved left
    /// column instead of removing it - so the sampled region is moved past that curve and
    /// the left edge then comes from the flat middle of the art.
    /// <summary>
    /// A flat colour node. Both attempts at a textured bar - borrowing the game's parts list,
    /// then loading our own image - crashed the game on party or duty teardown, so the fill
    /// is drawn as a plain rectangle and its shape is not configurable.
    /// </summary>
    private void UpdateBar(AddonPartyList* addon, int row, CombatantEntry? stats)
    {
        var bar = barNodes[row];
        if (bar == null)
        {
            if (lastBarTrace[row] != "no-node")
            {
                lastBarTrace[row] = "no-node";
                ServiceManager.LogInfo(LogChannel.PartyMembership, $"[PartyList] row {row} no-node");
            }

            return;
        }

        if (!barTextureApplied[row])
            ApplyBarTexture(bar, row);

        if (!TryGetRowAnchor(addon, row, out var anchor))
        {
            LogBarState(row, bar, stats, "no-anchor");
            return;
        }

        var fraction = stats != null && Settings.ShowBar && maxDps > 0
            ? Math.Clamp(stats.EncDps / maxDps, 0d, 1d)
            : 0d;

        // Geometry is derived from the row's own name/bars block, so the fill lines up
        // with the existing gauges and stays correct across HUD scale changes.
        // The bar runs from the job icon's right edge to the end of the row, matching the
        // icon's height and centre line. Falls back to the whole anchor if the icon isn't
        // laid out yet.
        float startX, centerY, height;
        if (TryGetIconMetrics(addon, row, out var icon))
        {
            startX = icon.RightX;
            centerY = icon.CenterY;
            height = icon.Height;
        }
        else
        {
            startX = anchor.X;
            centerY = anchor.Y + (anchor.Height / 2f);
            height = anchor.Height;
        }

        var available = Math.Max(0f, anchor.X + anchor.Width - startX);

        // Cap the span a full bar draws to, not the finished width - clamping the width
        // would flatten the top performers together instead of keeping them proportional.
        if (Settings.BarMaxWidth > 0f)
            available = Math.Min(available, Settings.BarMaxWidth);
        var position = new Vector2(
            startX + Settings.BarOffsetX,
            centerY - (height / 2f) + Settings.BarOffsetY);


        if (Math.Abs(lastBarHeight[row] - height) > 0.01f)
        {
            lastBarHeight[row] = height;
            bar.Height = height;
        }

        if (lastBarPos[row] != position)
        {
            lastBarPos[row] = position;
            bar.Position = position;
        }

        var width = available * (float)fraction;


        if (stats != null)
        {
            // Compared by colour rather than by job - keyed on the job, changing the
            // opacity or the job palette never reached the node.
            var color = ResolveBarColor(stats.Job);
            if (lastBarColor[row] != color)
            {
                lastBarColor[row] = color;
                bar.Color = color;
            }
        }

        // Sub-pixel changes aren't worth a native write plus a dirty flag every frame.
        if (Math.Abs(lastBarWidth[row] - width) >= 0.5f)
        {
            lastBarWidth[row] = width;
            bar.Width = width;
            bar.IsVisible = width >= 1f;
        }

        LogBarState(row, bar, stats, "ok");
    }

    /// <summary>
    /// A row's fill colour under the current mode. Alpha always comes from the bar's own
    /// opacity, so the pickers only decide hue. Only the meter mode dims the colour, which
    /// is what the meter does to its own bars - the other two are used as picked.
    /// </summary>
    private Vector4 ResolveBarColor(string job)
    {
        var alpha = Settings.BarOpacity;

        return Settings.BarColorMode switch
        {
            PartyListBarColorMode.SingleColor => JobColorHelper.WithAlpha(Settings.BarSingleColor, alpha),
            PartyListBarColorMode.OwnPalette => JobColorHelper.WithAlpha(
                JobColorHelper.GetEffectiveJobColor(job, Settings.BarColors), alpha),
            _ => JobColorHelper.GetBarColor(job, alpha, config),
        };
    }

    /// <summary>
    /// Traces why a row's fill is or isn't drawing. Reads the node's own values rather than
    /// what we meant to write, so a skipped write shows up as a mismatch. Emits only when the
    /// row's state changes, so a stuck row logs once instead of every frame.
    /// </summary>
    private void LogBarState(int row, ImGuiImageNode bar, CombatantEntry? stats, string note)
    {
        var node = (AtkResNode*)bar;
        var attached = node != null && node->ParentNode != null;
        var color = bar.Color;

        // Categorical only, so a fight's changing numbers don't re-log every frame - one line
        // per meaningful transition. The exact values go in the message.
        var signature = string.Format(
            CultureInfo.InvariantCulture,
            "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}",
            note,
            barOnBarRoot[row],
            attached,
            barTextureApplied[row],
            stats != null,
            bar.Width >= 1f,
            bar.IsVisible,
            color.W > 0.001f,
            maxDps > 0);

        if (lastBarTrace[row] == signature)
            return;

        lastBarTrace[row] = signature;

        ServiceManager.LogInfo(LogChannel.PartyMembership, string.Format(
            CultureInfo.InvariantCulture,
            "[PartyList] row {0} {1} parent={2} attached={3} tex={4} pend={5} req={6} " +
            "stats={7} maxDps={8:F0} w={9:F1} h={10:F1} pos=({11:F1},{12:F1}) vis={13} " +
            "colour=({14:F2},{15:F2},{16:F2},a={17:F2})",
            row,
            note,
            barOnBarRoot[row] ? "barRoot" : "overlayRoot",
            attached ? 1 : 0,
            barTextureApplied[row] ? 1 : 0,
            pendingBarTexture[row] != null ? 1 : 0,
            barTextureRequested[row] ? 1 : 0,
            stats != null
                ? string.Format(CultureInfo.InvariantCulture, "{0}/{1:F0}dps", stats.Job, stats.EncDps)
                : "none",
            maxDps,
            bar.Width,
            bar.Height,
            bar.X,
            bar.Y,
            bar.IsVisible ? 1 : 0,
            color.X,
            color.Y,
            color.Z,
            color.W));
    }

    private void PositionNodes(AddonPartyList* addon)
    {
        for (var i = 0; i < MaxRows; i++)
        {
            if (GetRowNode(addon, i) == null)
                continue;

            // Force the next update to rewrite geometry against the new row size.
            lastBarWidth[i] = -1f;
            lastBarHeight[i] = -1f;
            lastBarPos[i] = new Vector2(float.NaN, float.NaN);
        }
    }

    /// <summary>
    /// The job icon's right edge, vertical centre and height, expressed in our parent's
    /// local space. The icon hangs off a different node than our bar, so the two are
    /// related through screen coordinates and the addon scale rather than by assuming a
    /// shared parent. The right edge is pulled back slightly so the bar tucks under the
    /// icon and reads as emerging from behind it.
    /// </summary>
    private bool TryGetIconMetrics(AddonPartyList* addon, int row, out IconMetrics metrics)
    {
        metrics = default;

        var member = RowMember(addon, row);
        var icon = member == null ? null : member->ClassJobIcon;
        if (icon == null || icon->Height <= 0)
            return false;

        // Measured against whichever node this row's fill hangs off.
        var parent = BarParent(addon, row);
        if (parent == null)
            return false;

        var scale = addon->Scale > 0f ? addon->Scale : 1f;
        var localX = (icon->ScreenX - parent->ScreenX) / scale;
        var localY = (icon->ScreenY - parent->ScreenY) / scale;

        metrics = new IconMetrics(
            RightX: localX + icon->Width - Settings.IconUnderlap,
            CenterY: localY + (icon->Height / 2f),
            Height: Math.Max(1f, Settings.BarHeightPixels));
        return true;
    }

    private readonly record struct IconMetrics(float RightX, float CenterY, float Height);

    /// <summary>
    /// The row's own nodes that we shift vertically. Each one is a leaf as far as the others
    /// go - the bars are siblings of the name, and the gauge numbers are moved by the gauge
    /// layout instead - so no target ever drags another along.
    /// </summary>
    private static AtkResNode* GetShiftTarget(AddonPartyList* addon, int row, int slot)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return null;

        return slot switch
        {
            0 => member->Name == null ? null : &member->Name->AtkResNode,
            1 => member->HPGaugeBar == null || member->HPGaugeBar->OwnerNode == null
                ? null
                : &member->HPGaugeBar->OwnerNode->AtkResNode,
            2 => member->MPGaugeBar == null || member->MPGaugeBar->OwnerNode == null
                ? null
                : &member->MPGaugeBar->OwnerNode->AtkResNode,

            3 => member->CastingProgressBarBackground == null
                ? null
                : &member->CastingProgressBarBackground->AtkResNode,
            4 => member->CastingProgressBar == null
                ? null
                : &member->CastingProgressBar->AtkResNode,
            _ => null,
        };
    }

    /// <summary>The style behind a row-part slot; the cast bar slots have none of their own.</summary>
    private RowPartStyle? RowPart(int slot) => slot switch
    {
        0 => Settings.NameShift,
        1 => Settings.HpBarShift,
        2 => Settings.MpBarShift,
        _ => null,
    };

    private bool IsShiftEnabled(int slot) => RowPart(slot)?.Enabled ?? Settings.AdjustCastBar;

    private float GetShiftOffset(int slot) => RowPart(slot)?.OffsetY ?? Settings.CastBarShiftY;

    /// <summary>
    /// Moves, scales and tints the row's name and gauges. These are the game's nodes, so
    /// every original is captured before its first write and restored on teardown. The
    /// capture is self-correcting: if a value isn't the one we last wrote, the game or
    /// another plugin owns it now and the current value becomes the new original.
    /// <para>
    /// The name is a text node, so its colour is its text colour and its size is its font -
    /// both handled with the rest of the name. The gauge bars are artwork, so they take a
    /// scale and a colour multiply instead.
    /// </para>
    /// </summary>
    private void ApplyRowContentShift(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        for (var row = 0; row < MaxRows; row++)
        {
            for (var slot = 0; slot < ShiftSlots; slot++)
            {
                var part = RowPart(slot);

                if (!IsShiftEnabled(slot))
                {
                    RestoreShiftSlot(addon, row, slot);
                    continue;
                }

                var node = GetShiftTarget(addon, row, slot);
                if (node == null)
                    continue;

                var fresh = !shiftApplied[row, slot];

                if (fresh || Math.Abs(node->Y - appliedShiftY[row, slot]) > 0.01f)
                    originalShiftY[row, slot] = node->Y;

                var targetY = originalShiftY[row, slot] + GetShiftOffset(slot);

                // The cast bar slots own only the vertical shift - their X and width come
                // from the cast bar layout, which would fight a write from here.
                if (part == null)
                {
                    if (Math.Abs(node->Y - targetY) > 0.01f)
                        node->SetPositionFloat(node->X, targetY);

                    appliedShiftY[row, slot] = targetY;
                    shiftApplied[row, slot] = true;
                    continue;
                }

                if (fresh || Math.Abs(node->X - appliedShiftX[row, slot]) > 0.01f)
                    originalShiftX[row, slot] = node->X;

                var targetX = originalShiftX[row, slot] + part.OffsetX;

                if (Math.Abs(node->X - targetX) > 0.01f || Math.Abs(node->Y - targetY) > 0.01f)
                    node->SetPositionFloat(targetX, targetY);

                // The name takes its size from its font instead, so it is never scaled -
                // scaling it would leave the metrics measuring a width the node no longer draws.
                if (slot != 0)
                {
                    if (fresh || Math.Abs(node->ScaleX - appliedPartScale[row, slot]) > 0.001f)
                    {
                        originalPartScale[row, slot] = node->ScaleX;
                        originalPartOriginX[row, slot] = node->OriginX;
                        originalPartOriginY[row, slot] = node->OriginY;
                    }

                    var targetScale = originalPartScale[row, slot] * Math.Max(0.1f, part.Scale);

                    node->OriginX = 0f;
                    node->OriginY = 0f;
                    if (Math.Abs(node->ScaleX - targetScale) > 0.001f
                        || Math.Abs(node->ScaleY - targetScale) > 0.001f)
                        node->SetScale(targetScale, targetScale);

                    appliedPartScale[row, slot] = targetScale;

                    ApplyGaugeBarTint(addon, row, slot, part);
                }

                appliedShiftX[row, slot] = targetX;
                appliedShiftY[row, slot] = targetY;
                shiftApplied[row, slot] = true;
            }
        }
    }

    /// <summary>
    /// Centres the casting spell name vertically on the cast bar and shrinks it slightly.
    /// Horizontal position, width and alignment are left exactly as the game set them.
    /// Uses the bar *background* rather than the fill, since the fill's width animates
    /// with cast progress.
    /// </summary>
    private void ApplyCastNameLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        if (!Settings.AdjustCastName)
        {
            RestoreCastNameLayout(addon);
            return;
        }

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            if (member == null || member->CastingActionName == null || member->CastingProgressBarBackground == null)
                continue;

            var textRes = &member->CastingActionName->AtkResNode;
            var barRes = &member->CastingProgressBarBackground->AtkResNode;

            // Each property is captured independently: a game-side change to one must not
            // recapture another whose current value is ours, or teardown restores garbage.
            var fresh = !castNameApplied[row];

            if (fresh || Math.Abs(textRes->X - appliedCastNameX[row]) > 0.01f)
                originalCastNameX[row] = textRes->X;
            if (fresh || Math.Abs(textRes->Y - appliedCastNameY[row]) > 0.01f)
                originalCastNameY[row] = textRes->Y;
            if (fresh || textRes->Height != appliedCastNameHeight[row])
                originalCastNameHeight[row] = textRes->Height;
            if (fresh || member->CastingActionName->FontSize != appliedCastNameFont[row])
                originalCastNameFont[row] = member->CastingActionName->FontSize;

            if (!TryProjectRect(addon, barRes, textRes->ParentNode, out var target))
                continue;

            var targetX = originalCastNameX[row] + Settings.CastNameOffsetX;
            var targetY = target.Y + Settings.CastNameOffsetY;
            var height = (ushort)Math.Max(1f, target.Height);
            var font = (byte)Math.Clamp(originalCastNameFont[row] + Settings.CastNameFontDelta, 8, 60);

            // Matching the bar's height is what makes the game's own vertical centring
            // land on the bar. Width and alignment stay as the game set them.
            if (Math.Abs(textRes->X - targetX) > 0.01f || Math.Abs(textRes->Y - targetY) > 0.01f)
                textRes->SetPositionFloat(targetX, targetY);
            if (textRes->Height != height)
                textRes->SetHeight(height);
            if (member->CastingActionName->FontSize != font)
                member->CastingActionName->FontSize = font;

            ApplyTextColor(member->CastingActionName, Settings.CastNameUseCustomColor,
                Settings.CastNameColor, ref castNameColor[row]);

            appliedCastNameX[row] = targetX;
            appliedCastNameY[row] = targetY;
            appliedCastNameHeight[row] = height;
            appliedCastNameFont[row] = font;
            castNameApplied[row] = true;
        }
    }

    /// <summary>
    /// HP's number lives on the wrapper component, not on the gauge bar itself - the same
    /// distinction that left it behind when only the bar was shifted.
    /// </summary>
    /// <summary>
    /// Moves the cast bar's left edge right and narrows it by the same amount, keeping the
    /// right edge put. Uses ScaleX rather than Width because the game rewrites the fill's
    /// width every frame as the cast progresses - a width write would fight the animation,
    /// whereas a scale is a fixed value we can set idempotently and the fill still animates
    /// correctly inside it.
    /// </summary>
    private void ApplyCastBarLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        if (!Settings.AdjustCastBar)
        {
            RestoreCastBarLayout(addon);
            return;
        }

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var background = member == null ? null : member->CastingProgressBarBackground;
            if (background == null)
                continue;

            float fullWidth = background->AtkResNode.Width;
            if (fullWidth <= Settings.CastBarShiftX)
                continue;

            var factor = (fullWidth - Settings.CastBarShiftX) / fullWidth;

            for (var slot = 0; slot < CastBarSlots; slot++)
            {
                var node = GetCastBarNode(addon, row, slot);
                if (node == null)
                    continue;

                var fresh = !castBarApplied[row, slot];

                if (fresh || Math.Abs(node->X - appliedCastBarX[row, slot]) > 0.01f)
                    originalCastBarX[row, slot] = node->X;
                if (fresh || Math.Abs(node->ScaleX - appliedCastBarScaleX[row, slot]) > 0.001f)
                {
                    originalCastBarScaleX[row, slot] = node->ScaleX;
                    originalCastBarOriginX[row, slot] = node->OriginX;
                }

                if (fresh || Math.Abs(node->ScaleY - appliedCastBarScaleY[row, slot]) > 0.001f)
                {
                    originalCastBarScaleY[row, slot] = node->ScaleY;
                    originalCastBarOriginY[row, slot] = node->OriginY;
                }

                var targetX = originalCastBarX[row, slot] + Settings.CastBarShiftX;
                var targetScaleX = originalCastBarScaleX[row, slot] * factor;
                var targetScaleY = originalCastBarScaleY[row, slot] * Math.Max(0.1f, Settings.CastBarScaleY);

                if (Math.Abs(node->X - targetX) > 0.01f)
                    node->SetPositionFloat(targetX, node->Y);

                // Grown from the top-left, so the vertical offset still lands where it says.
                node->OriginX = 0f;
                node->OriginY = 0f;
                if (Math.Abs(node->ScaleX - targetScaleX) > 0.001f
                    || Math.Abs(node->ScaleY - targetScaleY) > 0.001f)
                    node->SetScale(targetScaleX, targetScaleY);

                ApplyNodeTint(node, Settings.CastBarTint, ref castBarTint[row, slot]);

                appliedCastBarX[row, slot] = targetX;
                appliedCastBarScaleX[row, slot] = targetScaleX;
                appliedCastBarScaleY[row, slot] = targetScaleY;
                castBarApplied[row, slot] = true;
            }
        }
    }

    private void RestoreCastBarLayout(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            for (var slot = 0; slot < CastBarSlots; slot++)
            {
                var node = addon == null ? null : GetCastBarNode(addon, row, slot);
                RestoreNodeTint(node, ref castBarTint[row, slot]);

                if (!castBarApplied[row, slot])
                    continue;

                castBarApplied[row, slot] = false;

                if (node == null)
                    continue;

                node->SetPositionFloat(originalCastBarX[row, slot], node->Y);
                node->OriginX = originalCastBarOriginX[row, slot];
                node->OriginY = originalCastBarOriginY[row, slot];
                node->SetScale(originalCastBarScaleX[row, slot], originalCastBarScaleY[row, slot]);
            }
        }
    }

    private static AtkResNode* GetCastBarNode(AddonPartyList* addon, int row, int slot)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return null;

        return slot switch
        {
            0 => member->CastingProgressBarBackground == null
                ? null
                : &member->CastingProgressBarBackground->AtkResNode,
            1 => member->CastingProgressBar == null
                ? null
                : &member->CastingProgressBar->AtkResNode,
            _ => null,
        };
    }

    /// <summary>
    /// The slot number before each name. It has its own text node, so by default it is given
    /// the same size change and vertical shift as the name to keep the two reading as one
    /// line; the override replaces that with values of its own.
    /// </summary>
    private void ApplyPartyIndexLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        var fontDelta = Settings.PartyIndexFontDelta;
        var offsetX = Settings.PartyIndexOffsetX;
        var offsetY = Settings.PartyIndexOffsetY;
        var useCustomColor = Settings.PartyIndexUseCustomColor;
        var color = Settings.PartyIndexColor;

        if (!Settings.AdjustPartyIndex)
        {
            fontDelta = Settings.AdjustNameFont ? Settings.NameFontDelta : 0;
            offsetX = Settings.NameShift.Enabled ? Settings.NameShift.OffsetX : 0f;
            offsetY = Settings.NameShift.Enabled ? Settings.NameShift.OffsetY : 0f;
            useCustomColor = Settings.NameShift.UseCustomColor;
            color = Settings.NameShift.Color;
        }

        // Nothing is being done to the name either, so leave the node's layout alone. Its
        // colour is still handled below, since that is a separate setting.
        var adjustLayout = Settings.AdjustPartyIndex || fontDelta != 0 || offsetX != 0f || offsetY != 0f;
        if (!adjustLayout)
            RestorePartyIndexLayout(addon);

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var index = member == null ? null : member->GroupSlotIndicator;
            if (index == null)
                continue;

            if (adjustLayout)
            {
                var res = &index->AtkResNode;

                // Each property is captured on its own, so a game-side change to one doesn't
                // recapture another whose current value is ours.
                var fresh = !indexApplied[row];

                if (fresh || index->FontSize != appliedIndexFont[row])
                    originalIndexFont[row] = index->FontSize;
                if (fresh || Math.Abs(res->X - appliedIndexX[row]) > 0.01f)
                    originalIndexX[row] = res->X;
                if (fresh || Math.Abs(res->Y - appliedIndexY[row]) > 0.01f)
                    originalIndexY[row] = res->Y;

                var font = (byte)Math.Clamp(originalIndexFont[row] + fontDelta, 8, 60);
                var targetX = originalIndexX[row] + offsetX;
                var targetY = originalIndexY[row] + offsetY;

                if (index->FontSize != font)
                    index->FontSize = font;
                if (Math.Abs(res->X - targetX) > 0.01f || Math.Abs(res->Y - targetY) > 0.01f)
                    res->SetPositionFloat(targetX, targetY);

                appliedIndexFont[row] = font;
                appliedIndexX[row] = targetX;
                appliedIndexY[row] = targetY;
                indexApplied[row] = true;
            }

            ApplyTextColor(index, useCustomColor, color, ref indexColor[row]);
        }
    }

    private void RestorePartyIndexLayout(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            if (!indexApplied[row])
                continue;

            indexApplied[row] = false;

            var member = RowMember(addon, row);
            var index = member == null ? null : member->GroupSlotIndicator;
            if (index == null)
                continue;

            index->FontSize = originalIndexFont[row];
            index->AtkResNode.SetPositionFloat(originalIndexX[row], originalIndexY[row]);
        }
    }

    private void RestorePartyIndexStyle(AddonPartyList* addon)
    {
        RestorePartyIndexLayout(addon);

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            RestoreTextColor(member == null ? null : member->GroupSlotIndicator, ref indexColor[row]);
        }
    }

    /// <summary>
    /// The name's own size and colour. A text node's size is its font, so the name is never
    /// scaled - the metrics measure the width it draws to, and a scale would leave them
    /// placed against a width the node no longer renders at.
    /// </summary>
    private void ApplyNameStyle(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var name = member == null ? null : member->Name;
            if (name == null)
                continue;

            if (Settings.AdjustNameFont)
            {
                if (!nameFontApplied[row] || name->FontSize != appliedNameFont[row])
                    originalNameFont[row] = name->FontSize;

                var font = (byte)Math.Clamp(originalNameFont[row] + Settings.NameFontDelta, 8, 60);
                if (name->FontSize != font)
                    name->FontSize = font;

                appliedNameFont[row] = font;
                nameFontApplied[row] = true;
            }
            else if (nameFontApplied[row])
            {
                nameFontApplied[row] = false;
                name->FontSize = originalNameFont[row];
            }

            var style = Settings.NameShift;
            ApplyTextColor(name, style.UseCustomColor, style.Color, ref nameColor[row]);
        }
    }

    /// <summary>
    /// Rewrites the name text: strips the level (which the game prefixes to the name as
    /// glyphs in the private-use block U+E060..U+E06F rather than drawing it as its own
    /// node). The game's own string is kept and written back when the option is switched off.
    /// </summary>
    private void ApplyNameText(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var name = member == null ? null : member->Name;
            if (name == null)
                continue;

            var current = name->NodeText.ToString();

            if (!nameTextApplied[row])
            {
                // First sight of this row. Clean anything a previous session or an older
                // build left on the string; from here on the game's text is trusted as-is.
                originalNameText[row] = SanitiseCapturedName(current, row);
            }
            else if (current != appliedNameText[row])
            {
                originalNameText[row] = current;
            }

            var body = Settings.HideLevel
                ? StripLevelPrefix(originalNameText[row])
                : originalNameText[row];

            // No-op when there's nothing to strip, so a clean name is never rewritten.
            if (current != body)
                name->SetText(body);

            appliedNameText[row] = body;
            nameTextApplied[row] = true;
        }
    }

    /// <summary>
    /// Forces every row to re-capture its name on the next frame, cleaning off anything
    /// left behind by an earlier build or session.
    /// </summary>
    public void ResyncNameText()
    {
        for (var row = 0; row < MaxRows; row++)
            nameTextApplied[row] = false;
    }

    private void RestoreNameText(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            if (!nameTextApplied[row])
                continue;

            nameTextApplied[row] = false;

            var member = RowMember(addon, row);
            var name = member == null ? null : member->Name;
            if (name == null || string.IsNullOrEmpty(originalNameText[row]))
                continue;

            var current = name->NodeText.ToString();
            var extra = appliedNameExtra[row];

            // Put the game's string back if the text is still ours, or if only our metrics
            // are still clinging to the end of a string the game has since rewritten.
            if (current == appliedNameText[row]
                || (!string.IsNullOrEmpty(extra) && current.EndsWith(extra, StringComparison.Ordinal)))
                name->SetText(originalNameText[row]);

            appliedNameExtra[row] = string.Empty;
        }
    }

    /// <summary>
    /// Cleans a first-time capture of anything a previous session left on the front of the
    /// name. The game always writes the level glyphs first, so text before them can only be
    /// ours; failing that, the authoritative name from the agent marks where the real text
    /// starts. Without this, a stale prefix gets stored as the original and sticks until
    /// the game next rewrites the node.
    /// </summary>
    private string SanitiseCapturedName(string current, int row)
    {
        if (string.IsNullOrEmpty(current))
            return current;

        // Anything before the level glyphs can only be ours - the game always writes them
        // first. This also clears leftovers from when the metrics went in front.
        var glyph = current.IndexOfAny(LevelGlyphRange);
        if (glyph > 0)
            current = current[glyph..];

        // The authoritative name marks where the real text ends; anything past it is ours.
        var agent = AgentHUD.Instance();
        if (agent == null || row >= agent->PartyMembers.Length)
            return current;

        var agentName = agent->PartyMembers[row].Name.ToString();
        if (string.IsNullOrEmpty(agentName))
            return current;

        var index = current.IndexOf(agentName, StringComparison.Ordinal);
        if (index < 0)
            return current;

        var end = index + agentName.Length;
        return end < current.Length ? current[..end] : current;
    }

    private static readonly char[] LevelGlyphRange = BuildLevelGlyphRange();

    private static char[] BuildLevelGlyphRange()
    {
        var range = new char[LevelGlyphLast - LevelGlyphFirst + 1];
        for (var i = 0; i < range.Length; i++)
            range[i] = (char)(LevelGlyphFirst + i);
        return range;
    }

    private static string StripLevelPrefix(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var index = 0;
        while (index < value.Length && value[index] >= LevelGlyphFirst && value[index] <= LevelGlyphLast)
            index++;

        if (index == 0)
            return value;

        while (index < value.Length && value[index] == ' ')
            index++;

        return value[index..];
    }

    private void RestoreNameStyle(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var name = member == null ? null : member->Name;

            if (nameFontApplied[row])
            {
                nameFontApplied[row] = false;
                if (name != null)
                    name->FontSize = originalNameFont[row];
            }

            RestoreTextColor(name, ref nameColor[row]);
        }
    }

    private static AtkComponentBase* GetGaugeComponent(AddonPartyList* addon, int row, int gauge)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return null;

        if (gauge == 0)
            return member->HPGaugeComponent;

        return member->MPGaugeBar == null ? null : &member->MPGaugeBar->AtkComponentBase;
    }

    /// <summary>Gathers a component's text nodes, descending into nested components.</summary>
    private static int CollectComponentTextNodes(AtkComponentBase* component, AtkTextNode** buffer, int capacity, int count, int depth)
    {
        if (component == null || depth > 3 || count >= capacity)
            return count;

        var nodes = component->UldManager.Nodes;
        for (var i = 0; i < nodes.Length && count < capacity; i++)
        {
            var node = nodes[i].Value;
            if (node == null)
                continue;

            if (node->Type == NodeType.Text)
                buffer[count++] = (AtkTextNode*)node;
            else if ((uint)node->Type >= 1000)
                count = CollectComponentTextNodes(((AtkComponentNode*)node)->Component, buffer, capacity, count, depth + 1);
        }

        return count;
    }

    /// <summary>
    /// Lifts both gauges' numbers, and evens out MP's two-node rendering. MP draws its value
    /// across two text nodes - leading digits at one size and the trailing pair smaller -
    /// so the small one is raised to match and re-aligned to the larger node's Y, undoing
    /// the baseline nudge the game applied for the smaller size. HP has a single node, which
    /// this handles as the degenerate case: nothing to resize, just the lift.
    /// </summary>
    private void ApplyGaugeNumberLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        // Refilled per gauge; hoisted so the allocation doesn't sit inside the loops.
        var texts = stackalloc AtkTextNode*[GaugeTextSlots];

        for (var row = 0; row < MaxRows; row++)
        {
            for (var gaugeIndex = 0; gaugeIndex < GaugeCount; gaugeIndex++)
            {
                var style = GaugeStyle(gaugeIndex);
                if (!style.Enabled)
                {
                    RestoreGaugeNumbers(addon, row, gaugeIndex, texts);
                    continue;
                }

                var component = GetGaugeComponent(addon, row, gaugeIndex);
                if (component == null)
                    continue;

                if (gaugeIndex == HpGaugeIndex)
                    ApplyHpArrowLayout(addon, row, style);

                var slotCount = CollectComponentTextNodes(component, texts, GaugeTextSlots, 0, 0);
                if (slotCount == 0)
                    continue;

                // Capture first, so the target size and reference Y come from the game's
                // own values rather than from what we wrote last frame.
                for (var slot = 0; slot < slotCount; slot++)
                {
                    var text = texts[slot];
                    var fresh = !gaugeTextApplied[row, gaugeIndex, slot];

                    if (fresh || text->FontSize != appliedGaugeFont[row, gaugeIndex, slot])
                        originalGaugeFont[row, gaugeIndex, slot] = text->FontSize;
                    if (fresh || Math.Abs(text->AtkResNode.X - appliedGaugeX[row, gaugeIndex, slot]) > 0.01f)
                        originalGaugeX[row, gaugeIndex, slot] = text->AtkResNode.X;
                    if (fresh || Math.Abs(text->AtkResNode.Y - appliedGaugeY[row, gaugeIndex, slot]) > 0.01f)
                        originalGaugeY[row, gaugeIndex, slot] = text->AtkResNode.Y;
                }

                // Identify the trailing-digits node structurally, by node id, rather than by
                // it having the smaller font - once we've written a size, that no longer
                // distinguishes it and the controls silently stop working.
                var trailingSlot = -1;
                if (slotCount > 1)
                {
                    uint highestId = 0;
                    for (var s = 0; s < slotCount; s++)
                    {
                        var id = texts[s]->AtkResNode.NodeId;
                        if (id > highestId)
                        {
                            highestId = id;
                            trailingSlot = s;
                        }
                    }
                }

                byte leadingFont = 0;
                for (var s = 0; s < slotCount; s++)
                {
                    if (s != trailingSlot)
                        leadingFont = Math.Max(leadingFont, originalGaugeFont[row, gaugeIndex, s]);
                }

                if (leadingFont == 0)
                    leadingFont = originalGaugeFont[row, gaugeIndex, 0];

                var referenceY = float.NaN;
                for (var s = 0; s < slotCount && float.IsNaN(referenceY); s++)
                {
                    if (s != trailingSlot)
                        referenceY = originalGaugeY[row, gaugeIndex, s];
                }

                for (var slot = 0; slot < slotCount; slot++)
                {
                    var text = texts[slot];
                    var isTrailing = slot == trailingSlot;

                    // The trailing size is derived from the leading node, so it stays
                    // controllable no matter what size we last wrote to it.
                    var target = isTrailing
                        ? (byte)Math.Clamp(leadingFont + style.FontDelta + Settings.MpTrailingFontDelta, 8, 60)
                        : (byte)Math.Clamp(originalGaugeFont[row, gaugeIndex, slot] + style.FontDelta, 8, 60);

                    // The game's baseline nudge only suits its own smaller size, so only
                    // re-align once the trailing digits are set to something else.
                    var resized = isTrailing && Settings.MpTrailingFontDelta != GameMpTrailingFontDelta;

                    var targetX = originalGaugeX[row, gaugeIndex, slot]
                                  + style.OffsetX
                                  + (resized ? Settings.TrailingDigitsOffsetX : 0f);
                    var baseY = resized && !float.IsNaN(referenceY)
                        ? referenceY + Settings.TrailingDigitsOffsetY
                        : originalGaugeY[row, gaugeIndex, slot];
                    var targetY = baseY + style.OffsetY;

                    if (text->FontSize != target)
                        text->FontSize = target;
                    if (Math.Abs(text->AtkResNode.X - targetX) > 0.01f
                        || Math.Abs(text->AtkResNode.Y - targetY) > 0.01f)
                        text->AtkResNode.SetPositionFloat(targetX, targetY);

                    ApplyTextColor(text, style.UseCustomColor, style.Color,
                        ref gaugeTextColor[row, gaugeIndex, slot]);

                    appliedGaugeFont[row, gaugeIndex, slot] = target;
                    appliedGaugeX[row, gaugeIndex, slot] = targetX;
                    appliedGaugeY[row, gaugeIndex, slot] = targetY;
                    gaugeTextApplied[row, gaugeIndex, slot] = true;
                }
            }
        }
    }

    /// <summary>
    /// The arrow the game shows beside the HP number while HP is down. It hangs off the same
    /// wrapper component as the number, next to the bar's component node, and is the only
    /// plain image there - so it is found by node type rather than by an id that could move.
    /// </summary>
    private static AtkResNode* GetHpArrowNode(AddonPartyList* addon, int row)
    {
        var component = addon == null ? null : GetGaugeComponent(addon, row, HpGaugeIndex);
        if (component == null)
            return null;

        var nodes = component->UldManager.Nodes;
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i].Value;
            if (node != null && node->Type == NodeType.Image)
                return node;
        }

        return null;
    }

    /// <summary>Keeps the arrow with the number by giving it the number's own offset.</summary>
    private void ApplyHpArrowLayout(AddonPartyList* addon, int row, GaugeNumberStyle style)
    {
        var node = GetHpArrowNode(addon, row);
        if (node == null)
            return;

        var fresh = !hpArrowApplied[row];

        if (fresh || Math.Abs(node->X - appliedHpArrowX[row]) > 0.01f)
            originalHpArrowX[row] = node->X;
        if (fresh || Math.Abs(node->Y - appliedHpArrowY[row]) > 0.01f)
            originalHpArrowY[row] = node->Y;

        var targetX = originalHpArrowX[row] + style.OffsetX;
        var targetY = originalHpArrowY[row] + style.OffsetY;

        if (Math.Abs(node->X - targetX) > 0.01f || Math.Abs(node->Y - targetY) > 0.01f)
            node->SetPositionFloat(targetX, targetY);

        appliedHpArrowX[row] = targetX;
        appliedHpArrowY[row] = targetY;
        hpArrowApplied[row] = true;
    }

    private void RestoreHpArrow(AddonPartyList* addon, int row)
    {
        if (!hpArrowApplied[row])
            return;

        hpArrowApplied[row] = false;

        var node = GetHpArrowNode(addon, row);
        if (node != null)
            node->SetPositionFloat(originalHpArrowX[row], originalHpArrowY[row]);
    }

    private GaugeNumberStyle GaugeStyle(int gaugeIndex)
        => gaugeIndex == 0 ? Settings.HpNumbers : Settings.MpNumbers;

    private void RestoreGaugeNumberLayout(AddonPartyList* addon)
    {
        var texts = stackalloc AtkTextNode*[GaugeTextSlots];

        for (var row = 0; row < MaxRows; row++)
            for (var gaugeIndex = 0; gaugeIndex < GaugeCount; gaugeIndex++)
                RestoreGaugeNumbers(addon, row, gaugeIndex, texts);
    }

    /// <summary>
    /// Hands one gauge's numbers back. The caller owns the scratch buffer, so this can sit
    /// inside the per-gauge loops without a stack allocation on every pass.
    /// </summary>
    private void RestoreGaugeNumbers(AddonPartyList* addon, int row, int gaugeIndex, AtkTextNode** texts)
    {
        if (gaugeIndex == HpGaugeIndex)
            RestoreHpArrow(addon, row);

        var component = addon == null ? null : GetGaugeComponent(addon, row, gaugeIndex);
        var slotCount = CollectComponentTextNodes(component, texts, GaugeTextSlots, 0, 0);

        for (var slot = 0; slot < slotCount; slot++)
        {
            RestoreTextColor(texts[slot], ref gaugeTextColor[row, gaugeIndex, slot]);

            if (!gaugeTextApplied[row, gaugeIndex, slot])
                continue;

            var text = texts[slot];
            text->FontSize = originalGaugeFont[row, gaugeIndex, slot];
            text->AtkResNode.SetPositionFloat(
                originalGaugeX[row, gaugeIndex, slot],
                originalGaugeY[row, gaugeIndex, slot]);
        }

        for (var s = 0; s < GaugeTextSlots; s++)
            gaugeTextApplied[row, gaugeIndex, s] = false;
    }

    private void RestoreCastNameLayout(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            var member = RowMember(addon, row);
            var text = member == null ? null : member->CastingActionName;
            RestoreTextColor(text, ref castNameColor[row]);

            if (!castNameApplied[row])
                continue;

            castNameApplied[row] = false;

            if (text == null)
                continue;

            text->AtkResNode.SetPositionFloat(originalCastNameX[row], originalCastNameY[row]);
            text->AtkResNode.SetHeight(originalCastNameHeight[row]);
            text->FontSize = originalCastNameFont[row];
        }
    }

    /// <summary>
    /// Expresses one node's rectangle in another node's parent space. Uses local values
    /// directly when they already share a parent - exact, and no one-frame lag from
    /// screen coordinates that the game hasn't recomputed yet.
    /// </summary>
    private static bool TryProjectRect(AddonPartyList* addon, AtkResNode* source, AtkResNode* targetParent, out Bounds rect)
    {
        rect = default;
        if (source == null)
            return false;

        if (targetParent == null || source->ParentNode == targetParent)
        {
            rect = new Bounds(source->X, source->Y, source->Width, source->Height);
            return true;
        }

        var scale = addon->Scale > 0f ? addon->Scale : 1f;
        rect = new Bounds(
            (source->ScreenX - targetParent->ScreenX) / scale,
            (source->ScreenY - targetParent->ScreenY) / scale,
            source->Width,
            source->Height);
        return true;
    }

    /// <summary>
    /// Spreads the rows out by pushing each one down by a gap for every row above it. A row's
    /// index in the array it comes from is not where the list draws it - the local player
    /// heads the party array wherever they sit in the list - so the order is taken from the
    /// positions the game itself gave the rows. Everything in a row hangs off the node moved
    /// here - the game's own name, gauges, glows and cast bar as children of it, our fill and
    /// metrics through the bounds we measure off it - so the whole row travels with it. The
    /// chocobo and pet rows sit below the party, so they take the full stack rather than a
    /// slot's worth.
    /// </summary>
    private void ApplyRowSpacing(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        var spacing = Settings.RowSpacing;
        if (spacing <= 0f)
        {
            RestoreRowSpacing(addon);
            return;
        }

        var party = Math.Clamp(addon->MemberCount, 0, MaxRows);
        var drawn = RowsInUse(addon);

        // Every row's starting position has to be read before any of them moves, or a row
        // already pushed down would be taken as the baseline for the ones after it.
        var rows = 0;
        for (var i = 0; i < drawn; i++)
        {
            var slot = DrawnSlot(i, party);
            var node = SpacingTarget(addon, slot);
            if (node == null)
                continue;

            CaptureSpacing(slot, node);
            rows = AddRowY(originalSpacingY[slot], rows);
        }

        for (var i = 0; i < drawn; i++)
        {
            var slot = DrawnSlot(i, party);
            var node = SpacingTarget(addon, slot);
            if (node != null)
                MoveSpacing(slot, node, RowsAbove(originalSpacingY[slot], rows) * spacing);
        }

        var below = drawn * spacing;
        ShiftBySpacing(addon, ChocoboSpacingSlot, below);
        ShiftBySpacing(addon, PetSpacingSlot, below);

        // How far the bottom row moved is how much taller the list has become. A chocobo or
        // pet row sits one slot below the party, so its presence adds a row's worth.
        var hasCompanion = addon->ChocoboCount > 0 || addon->PetCount > 0;
        ApplyBackdropHeight(addon, hasCompanion ? below : Math.Max(0f, below - spacing));
    }

    /// <summary>Rows the game is drawing for party members and duty support / trust NPCs.</summary>
    private static int RowsInUse(AddonPartyList* addon)
        => Math.Clamp(addon->MemberCount + addon->TrustCount, 0, MaxRows);

    /// <summary>Duty support and trust rows are drawn where the party's own rows end.</summary>
    private static int DrawnSlot(int index, int party)
        => index < party ? index : TrustSpacingSlot + index - party;

    /// <summary>
    /// Adds a row's starting position to the sorted list of them. Two slots that share a node
    /// - the party array keeps pointing at rows the trust array also names - land on the same
    /// entry, so they get the same place in the stack rather than one each.
    /// </summary>
    private int AddRowY(float y, int count)
    {
        var at = 0;
        while (at < count && spacingRowY[at] < y - 0.01f)
            at++;

        if (at < count && Math.Abs(spacingRowY[at] - y) <= 0.01f)
            return count;

        for (var i = count; i > at; i--)
            spacingRowY[i] = spacingRowY[i - 1];

        spacingRowY[at] = y;
        return count + 1;
    }

    /// <summary>How many rows the game drew above the one starting at this position.</summary>
    private int RowsAbove(float y, int count)
    {
        var above = 0;
        while (above < count && spacingRowY[above] < y - 0.01f)
            above++;

        return above;
    }

    private void ShiftBySpacing(AddonPartyList* addon, int slot, float offset)
    {
        var node = SpacingTarget(addon, slot);
        if (node == null)
            return;

        CaptureSpacing(slot, node);
        MoveSpacing(slot, node, offset);
    }

    /// <summary>Re-reads a row's own position whenever the game has moved the row itself.</summary>
    private void CaptureSpacing(int slot, AtkResNode* node)
    {
        if (!spacingApplied[slot] || Math.Abs(node->Y - appliedSpacingY[slot]) > 0.01f)
            originalSpacingY[slot] = node->Y;
    }

    private void MoveSpacing(int slot, AtkResNode* node, float offset)
    {
        var targetY = originalSpacingY[slot] + offset;
        if (Math.Abs(node->Y - targetY) > 0.01f)
            node->SetPositionFloat(node->X, targetY);

        appliedSpacingY[slot] = targetY;
        spacingApplied[slot] = true;
    }

    /// <summary>
    /// Grows the party list's backdrop by however far the bottom row was pushed down, so it
    /// still covers the rows once they are spread out.
    /// </summary>
    private void ApplyBackdropHeight(AddonPartyList* addon, float extra)
    {
        var backdrop = addon->BackgroundNineGridNode;
        if (backdrop == null)
            return;

        var node = &backdrop->AtkResNode;

        if (!backdropHeightApplied || node->Height != appliedBackdropHeight)
            originalBackdropHeight = node->Height;

        var target = (ushort)Math.Clamp(originalBackdropHeight + (int)MathF.Round(extra), 0, ushort.MaxValue);
        if (node->Height != target)
            node->SetHeight(target);

        appliedBackdropHeight = target;
        backdropHeightApplied = true;
    }

    /// <summary>The member behind a spacing slot, read straight from the array that slot names.</summary>
    private static AddonPartyList.PartyListMemberStruct* SpacingMember(AddonPartyList* addon, int slot)
    {
        if (slot == ChocoboSpacingSlot)
            return &addon->Chocobo;

        if (slot == PetSpacingSlot)
            return &addon->Pet;

        if (slot >= TrustSpacingSlot)
        {
            fixed (AddonPartyList.PartyListMemberStruct* members = addon->TrustMembers)
                return members + (slot - TrustSpacingSlot);
        }

        fixed (AddonPartyList.PartyListMemberStruct* members = addon->PartyMembers)
            return members + slot;
    }

    private static AtkResNode* SpacingTarget(AddonPartyList* addon, int slot)
    {
        var member = SpacingMember(addon, slot);
        var component = member == null ? null : member->PartyMemberComponent;
        var owner = component == null ? null : component->OwnerNode;
        return owner == null ? null : &owner->AtkResNode;
    }

    private void RestoreRowSpacing(AddonPartyList* addon)
    {
        for (var slot = 0; slot < SpacingSlots; slot++)
        {
            if (!spacingApplied[slot])
                continue;

            spacingApplied[slot] = false;

            var node = addon == null ? null : SpacingTarget(addon, slot);
            if (node != null)
                node->SetPositionFloat(node->X, originalSpacingY[slot]);
        }

        if (!backdropHeightApplied)
            return;

        backdropHeightApplied = false;

        var backdrop = addon == null ? null : addon->BackgroundNineGridNode;
        if (backdrop != null)
            backdrop->AtkResNode.SetHeight(originalBackdropHeight);
    }

    private void RestoreRowContentShift(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
            for (var slot = 0; slot < ShiftSlots; slot++)
                RestoreShiftSlot(addon, row, slot);
    }

    private void RestoreShiftSlot(AddonPartyList* addon, int row, int slot)
    {
        if (!shiftApplied[row, slot])
            return;

        shiftApplied[row, slot] = false;

        if (slot == HpBarSlot || slot == MpBarSlot)
            RestoreGaugeBarTint(addon, row, slot);

        var node = addon == null ? null : GetShiftTarget(addon, row, slot);
        if (node == null)
            return;

        if (slot >= RowPartSlots)
        {
            node->SetPositionFloat(node->X, originalShiftY[row, slot]);
            return;
        }

        node->SetPositionFloat(originalShiftX[row, slot], originalShiftY[row, slot]);

        if (slot == 0)
            return;

        node->OriginX = originalPartOriginX[row, slot];
        node->OriginY = originalPartOriginY[row, slot];
        node->SetScale(originalPartScale[row, slot], originalPartScale[row, slot]);
    }

    /// <summary>
    /// The rectangle the fill aligns to, in whatever coordinate space our bar's parent
    /// uses. Parented to the glow container the bar covers it exactly, which is the
    /// hover/select footprint; otherwise it aligns to the name/bars block.
    /// </summary>
    private bool TryGetRowAnchor(AddonPartyList* addon, int row, out Bounds anchor)
    {
        anchor = default;
        if (addon == null)
            return false;

        // The row's rectangle expressed in the bar's parent space.
        var owner = GetRowNode(addon, row);
        var parent = BarParent(addon, row);

        if (owner != null && parent != null)
        {
            var scale = addon->Scale > 0f ? addon->Scale : 1f;

            anchor = new Bounds(
                (owner->AtkResNode.ScreenX - parent->ScreenX) / scale,
                (owner->AtkResNode.ScreenY - parent->ScreenY) / scale,
                owner->AtkResNode.Width,
                owner->AtkResNode.Height);
            return true;
        }

        if (owner == null)
            return false;

        anchor = new Bounds(0f, 0f, owner->AtkResNode.Width, owner->AtkResNode.Height);
        return true;
    }

    private readonly record struct Bounds(float X, float Y, float Width, float Height);

    private static AtkResNode* GetStatusIconNode(AddonPartyList* addon, int row, int index)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return null;

        var icons = member->StatusIcons;
        if (index >= icons.Length)
            return null;

        var icon = icons[index].Value;
        return icon == null || icon->OwnerNode == null ? null : &icon->OwnerNode->AtkResNode;
    }

    /// <summary>
    /// Picks the slot each icon takes its coordinates from. Normally that's its own slot.
    /// The game fills its icon grid from the left and hides the slots it doesn't need, so a
    /// member with three buffs leaves the rest of the row empty; right aligning re-seats the
    /// visible icons on the trailing slots instead. The source slot's Y comes along with its
    /// X, so a row the game wraps onto a second line still lands where that line sits.
    /// </summary>
    private void BuildStatusSlotSource(AddonPartyList* addon, int row, Span<int> source)
    {
        for (var i = 0; i < StatusIconSlots; i++)
            source[i] = i;

        if (!Settings.StatusRightAlign)
            return;

        Span<int> filled = stackalloc int[StatusIconSlots];
        Span<int> visible = stackalloc int[StatusIconSlots];
        var filledCount = 0;
        var visibleCount = 0;

        for (var i = 0; i < StatusIconSlots; i++)
        {
            var node = GetStatusIconNode(addon, row, i);
            if (node == null)
                continue;

            filled[filledCount++] = i;
            if (IsNodeVisible(node))
                visible[visibleCount++] = i;
        }

        var shift = filledCount - visibleCount;
        for (var i = 0; i < visibleCount; i++)
            source[visible[i]] = filled[shift + i];
    }

    /// <summary>
    /// Moves and scales the status icons. Each icon is handled individually rather than via
    /// a shared parent, because the icons hang directly off the row. Scaling an icon alone
    /// would change its size but not the gaps between icons, so positions are scaled about
    /// the first icon's corner - that keeps the list evenly spaced at any scale.
    /// </summary>
    private void ApplyStatusIconLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        if (!Settings.AdjustStatusIcons)
        {
            RestoreStatusIconLayout(addon);
            return;
        }

        var scale = Math.Max(0.1f, Settings.StatusScale);
        Span<int> slotSource = stackalloc int[StatusIconSlots];

        for (var row = 0; row < MaxRows; row++)
        {
            var anchorX = float.MaxValue;
            var anchorY = float.MaxValue;

            for (var i = 0; i < StatusIconSlots; i++)
            {
                var node = GetStatusIconNode(addon, row, i);
                if (node == null)
                    continue;

                var fresh = !statusApplied[row, i];

                if (fresh
                    || Math.Abs(node->X - appliedStatusX[row, i]) > 0.01f
                    || Math.Abs(node->Y - appliedStatusY[row, i]) > 0.01f)
                {
                    originalStatusX[row, i] = node->X;
                    originalStatusY[row, i] = node->Y;
                }

                if (fresh || Math.Abs(node->ScaleX - appliedStatusScale[row, i]) > 0.001f)
                {
                    originalStatusScale[row, i] = node->ScaleX;
                    originalStatusOriginX[row, i] = node->OriginX;
                    originalStatusOriginY[row, i] = node->OriginY;
                }

                anchorX = Math.Min(anchorX, originalStatusX[row, i]);
                anchorY = Math.Min(anchorY, originalStatusY[row, i]);
            }

            if (anchorX == float.MaxValue)
                continue;

            BuildStatusSlotSource(addon, row, slotSource);

            for (var i = 0; i < StatusIconSlots; i++)
            {
                var node = GetStatusIconNode(addon, row, i);
                if (node == null)
                    continue;

                // A slot the row is not showing goes back to the game. Left on ours it keeps
                // our coordinates while the game re-lays the grid out - a member leaving, an
                // instance swap - and because the node is then exactly where we put it, the
                // capture above reads our own placement back as the game's original, so the
                // icon returns in the wrong spot the next time the row needs that slot.
                if (!IsNodeVisible(node))
                {
                    RestoreStatusSlot(row, i, node);
                    ApplyNodeTint(node, Settings.StatusTint, ref statusTint[row, i]);
                    continue;
                }

                var source = slotSource[i];
                var targetX = anchorX + ((originalStatusX[row, source] - anchorX) * scale) + Settings.StatusOffsetX;
                var targetY = anchorY + ((originalStatusY[row, source] - anchorY) * scale) + Settings.StatusOffsetY;
                var targetScale = originalStatusScale[row, i] * scale;

                if (Math.Abs(node->X - targetX) > 0.01f || Math.Abs(node->Y - targetY) > 0.01f)
                    node->SetPositionFloat(targetX, targetY);

                node->OriginX = 0f;
                node->OriginY = 0f;
                if (Math.Abs(node->ScaleX - targetScale) > 0.001f || Math.Abs(node->ScaleY - targetScale) > 0.001f)
                    node->SetScale(targetScale, targetScale);

                ApplyNodeTint(node, Settings.StatusTint, ref statusTint[row, i]);

                appliedStatusX[row, i] = targetX;
                appliedStatusY[row, i] = targetY;
                appliedStatusScale[row, i] = targetScale;
                statusApplied[row, i] = true;
            }
        }
    }

    /// <summary>
    /// Appends encounter totals to the party list's header text ("Party", "Light Party"...).
    /// The game's own label is captured and restored, same as the row names.
    /// </summary>
    private void ApplyEncounterTotals(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        var node = addon->PartyTypeTextNode;
        if (node == null)
            return;

        if (!Settings.ShowEncounterTotals && !Settings.HidePartyTypeLabel)
        {
            RestoreEncounterTotals(addon);
            return;
        }

        var current = node->NodeText.ToString();

        if (!totalsApplied)
        {
            originalTotalsText = current;
        }
        else if (current != appliedTotalsText)
        {
            originalTotalsText = !string.IsNullOrEmpty(appliedTotalsExtra)
                                 && current.EndsWith(appliedTotalsExtra, StringComparison.Ordinal)
                ? current[..^appliedTotalsExtra.Length]
                : current;
        }

        // Hiding the label just means dropping the game's own text and keeping whatever we
        // append, so the original is still captured and restored when this is turned off.
        var body = Settings.HidePartyTypeLabel ? string.Empty : originalTotalsText;
        var extra = Settings.ShowEncounterTotals ? BuildEncounterTotals() : string.Empty;

        // Nothing to show either because the metrics are hidden or because no encounter is
        // active - either way the user's own text stands in for them.
        if (Settings.ShowEncounterTotals && extra.Length == 0 && Settings.TotalsHiddenText.Length > 0)
            extra = TotalsSeparator + Settings.TotalsHiddenText;

        // With the game's label gone the separator has nothing to separate, so drop just that -
        // trimming the whole start would also eat any spaces the user put in front of their text.
        if (body.Length == 0 && extra.StartsWith(TotalsSeparator, StringComparison.Ordinal))
            extra = extra[TotalsSeparator.Length..];

        var target = body + extra;

        if (current != target)
            node->SetText(target);

        appliedTotalsText = target;
        appliedTotalsExtra = extra;
        totalsApplied = true;
    }

    /// <summary>
    /// The header text node itself - size, position and colour - as opposed to the string
    /// written into it. Independent of the totals, so the game's own label can be restyled
    /// with nothing appended to it.
    /// </summary>
    private void ApplyTotalsTextStyle(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        var node = addon->PartyTypeTextNode;
        if (node == null)
            return;

        if (!Settings.AdjustTotalsText)
        {
            RestoreTotalsTextStyle(addon);
            return;
        }

        var res = &node->AtkResNode;
        var fresh = !totalsStyleApplied;

        if (fresh || node->FontSize != appliedTotalsFont)
            originalTotalsFont = node->FontSize;
        if (fresh || Math.Abs(res->X - appliedTotalsX) > 0.01f)
            originalTotalsX = res->X;
        if (fresh || Math.Abs(res->Y - appliedTotalsY) > 0.01f)
            originalTotalsY = res->Y;

        var font = (byte)Math.Clamp(originalTotalsFont + Settings.TotalsFontDelta, 8, 60);
        var targetX = originalTotalsX + Settings.TotalsOffsetX;
        var targetY = originalTotalsY + Settings.TotalsOffsetY;

        if (node->FontSize != font)
            node->FontSize = font;
        if (Math.Abs(res->X - targetX) > 0.01f || Math.Abs(res->Y - targetY) > 0.01f)
            res->SetPositionFloat(targetX, targetY);

        ApplyTextColor(node, Settings.TotalsUseCustomColor, Settings.TotalsColor, ref totalsColor);

        appliedTotalsFont = font;
        appliedTotalsX = targetX;
        appliedTotalsY = targetY;
        totalsStyleApplied = true;
    }

    private void RestoreTotalsTextStyle(AddonPartyList* addon)
    {
        var node = addon == null ? null : addon->PartyTypeTextNode;
        RestoreTextColor(node, ref totalsColor);

        if (!totalsStyleApplied)
            return;

        totalsStyleApplied = false;

        if (node == null)
            return;

        node->FontSize = originalTotalsFont;
        node->AtkResNode.SetPositionFloat(originalTotalsX, originalTotalsY);
    }

    private void RestoreEncounterTotals(AddonPartyList* addon)
    {
        if (!totalsApplied)
            return;

        totalsApplied = false;

        var node = addon == null ? null : addon->PartyTypeTextNode;
        if (node == null || string.IsNullOrEmpty(originalTotalsText))
            return;

        var current = node->NodeText.ToString();
        if (current == appliedTotalsText
            || (!string.IsNullOrEmpty(appliedTotalsExtra) && current.EndsWith(appliedTotalsExtra, StringComparison.Ordinal)))
            node->SetText(originalTotalsText);

        appliedTotalsExtra = string.Empty;
    }

    /// <summary>Separates the totals from the game's label, and each other.</summary>
    private const string TotalsSeparator = "  ";

    private string BuildEncounterTotals()
    {
        if (!MetricsVisible)
            return string.Empty;

        CombatEncounter? encounter = null;
        try
        {
            encounter = dataService.Store.ActiveEncounter?.Encounter;
        }
        catch (Exception ex)
        {
            ServiceManager.LogDebug(LogChannel.PartyMembership, $"Encounter totals read failed: {ex.Message}");
        }

        if (encounter == null)
            return string.Empty;

        var metrics = Settings.TotalsMetrics;
        var parts = new List<string>(metrics.Count + 2);

        if (Settings.TotalsShowTitle && !string.IsNullOrEmpty(encounter.Title))
            parts.Add(encounter.Title);
        if (Settings.TotalsShowDuration && !string.IsNullOrEmpty(encounter.Duration))
            parts.Add(encounter.Duration);

        var local = localPlayerStats;
        var group = encounterAggregates;

        foreach (var col in metrics)
        {
            string value;

            // Formatted through the meter's own column value, so a metric reads here exactly
            // as it does in the meter window. No tab to take overrides from, hence null.
            // A metric with nothing behind it is left out rather than shown as a zero, so
            // the header doesn't fill up before you've hit anything.
            if (CombatantBarComponent.IsGroupColumn(col))
            {
                if (group == null)
                    continue;
                value = CombatantBarComponent.GetGroupColumnDisplayValue(col, config, null, group);
            }
            else
            {
                if (local == null)
                    continue;
                value = CombatantBarComponent.GetColumnDisplayValue(local, col, config, null);
            }

            if (string.IsNullOrEmpty(value))
                continue;

            parts.Add(Settings.TotalsShowLabels ? $"{value} {Settings.TotalsLabel(col)}" : value);
        }

        return parts.Count == 0 ? string.Empty : TotalsSeparator + string.Join(TotalsSeparator, parts);
    }

    /// <summary>
    /// The two glow nodes we style, one per state slot: the row highlight (TargetGlow) and
    /// the ring around the job icon (the container's image node).
    /// <para>
    /// ClickFlash is deliberately left alone. It is the click effect rather than the hover
    /// or selection background - it never showed while hovering - and because it is also a
    /// nine-grid it would share the row slot with TargetGlow, leaving the two to re-capture
    /// their "original" from each other's values and drift a little further every frame.
    /// </para>
    /// </summary>
    /// <summary>
    /// Slot 0 is the glow *container*, not the glow itself. The game rewrites TargetGlow's
    /// position and scale every frame for the length of its fade-in, after our update pass
    /// and before the transform is computed, so a write to that node is reverted within the
    /// frame and only takes hold once the animation ends. The container is never touched by
    /// the game, so moving it applies instantly - at the cost of carrying its other children
    /// along, which the icon glow and our DPS fill compensate for.
    /// Slot 1 is the ring around the job icon.
    /// </summary>
    private static AtkResNode* GetGlowNode(AddonPartyList* addon, int row, int slot)
    {
        var member = RowMember(addon, row);
        var container = member == null ? null : member->TargetGlowContainer;
        if (container == null)
            return null;

        if (slot == GlowStateRow)
            return container;

        if (slot != GlowStateIcon)
            return null;

        for (var child = container->ChildNode; child != null; child = child->PrevSiblingNode)
        {
            if (child->NodeId >= OwnNodeIdMin && child->NodeId <= OwnNodeIdMax)
                continue;

            if (child->Type == NodeType.Image)
                return child;
        }

        return null;
    }

    /// <summary>
    /// Offsets, scales and tints the hover / selection glow. Applied to the glow nodes
    /// themselves rather than their container, because our own DPS fill hangs off that
    /// container and would otherwise move and scale along with them.
    /// </summary>
    private void ApplySelectionGlowLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        if (!Settings.AdjustSelectionGlow)
        {
            RestoreSelectionGlowLayout(addon);
            return;
        }

        for (var row = 0; row < MaxRows; row++)
        {
            // A selected row can keep its own look while the mouse is over it, since the
            // shared node can only show one of the two.
            var targeted = addon->TargetedIndex == row;
            var hovered = addon->HoveredIndex == row
                          && !(targeted && Settings.SelectionOverridesHover);

            for (var slot = 0; slot < GlowGroups; slot++)
            {
                var node = GetGlowNode(addon, row, slot);
                if (node == null)
                    continue;

                // The slot index *is* the node, so a capture can never re-base on a different
                // node's values. Which settings feed it is a separate question, so switching
                // between hover and selection can't re-base it either.
                var group = slot;
                var isIcon = group == GlowStateIcon;

                var rowOffsetX = hovered ? Settings.HoverOffsetX : Settings.SelectionOffsetX;
                var rowOffsetY = hovered ? Settings.HoverOffsetY : Settings.SelectionOffsetY;
                var rowScale = Math.Max(0.1f, hovered ? Settings.HoverScale : Settings.SelectionScale);

                // The icon glow lives inside the container we just moved, so its own offset
                // has to undo that transform to end up where the settings actually ask for.
                var offsetX = isIcon ? Settings.IconGlowOffsetX - rowOffsetX : rowOffsetX;
                var offsetY = isIcon ? Settings.IconGlowOffsetY - rowOffsetY : rowOffsetY;
                // The container is moved but never scaled - scaling it would resize our own
                // fill, which hangs off it. The row glow's scale goes on the glow node
                // instead, further down.
                var scale = isIcon ? Math.Max(0.1f, Settings.IconGlowScale) : 1f;
                var tint = isIcon ? Settings.IconGlowTint
                    : hovered ? Settings.HoverTint : Settings.SelectionTint;

                if (!isIcon)
                {
                }

                var multiplyR = (byte)Math.Clamp(tint.X * 255f, 0f, 255f);
                var multiplyG = (byte)Math.Clamp(tint.Y * 255f, 0f, 255f);
                var multiplyB = (byte)Math.Clamp(tint.Z * 255f, 0f, 255f);

                var fresh = !glowApplied[row, group];

                if (fresh
                    || Math.Abs(node->X - appliedGlowX[row, group]) > 0.01f
                    || Math.Abs(node->Y - appliedGlowY[row, group]) > 0.01f)
                {
                    originalGlowX[row, group] = node->X;
                    originalGlowY[row, group] = node->Y;
                }

                if (fresh || Math.Abs(node->ScaleX - appliedGlowScale[row, group]) > 0.001f)
                {
                    originalGlowScale[row, group] = node->ScaleX;
                    originalGlowOriginX[row, group] = node->OriginX;
                    originalGlowOriginY[row, group] = node->OriginY;
                }

                if (fresh)
                {
                    originalGlowMultiply[row, group, 0] = node->MultiplyRed;
                    originalGlowMultiply[row, group, 1] = node->MultiplyGreen;
                    originalGlowMultiply[row, group, 2] = node->MultiplyBlue;

                    if (node->Timeline != null)
                        originalGlowMask[row, group] = node->Timeline->Mask;
                }

                // The timeline re-drives these every frame while the glow animates in, which
                // is what overwrote our values for the first frames of a hover. Clearing just
                // those bits leaves Alpha animated, so the fade still plays.
                if (node->Timeline != null)
                {
                    node->Timeline->Mask = Settings.FreezeGlowTransform
                        ? originalGlowMask[row, group] & ~(AtkTimelineMask.Position | AtkTimelineMask.Scale | AtkTimelineMask.NodeTint)
                        : originalGlowMask[row, group];
                }

                var targetX = originalGlowX[row, group] + offsetX;
                var targetY = originalGlowY[row, group] + offsetY;
                var targetScale = originalGlowScale[row, group] * scale;


                if (Math.Abs(node->X - targetX) > 0.01f || Math.Abs(node->Y - targetY) > 0.01f)
                    node->SetPositionFloat(targetX, targetY);

                node->OriginX = 0f;
                node->OriginY = 0f;
                if (Math.Abs(node->ScaleX - targetScale) > 0.001f || Math.Abs(node->ScaleY - targetScale) > 0.001f)
                    node->SetScale(targetScale, targetScale);

                // Never tint the container - its multiply cascades to every child, which
                // includes our own DPS fill. Only the icon glow is tinted here; the row
                // glow's tint goes on the glow node itself, below.
                // Our fill is appended after the icon glow in the container, so it draws over
                // it. This lifts the icon glow above its siblings without reordering the
                // child list - the operation that corrupted the addon earlier.
                if (isIcon)
                {
                    if (!iconGlowOnTopApplied[row])
                    {
                        originalIconGlowOnTop[row] = node->IsRenderedOnTop;
                        iconGlowOnTopApplied[row] = true;
                    }

                    if (!node->IsRenderedOnTop)
                        node->IsRenderedOnTop = true;
                }

                if (isIcon)
                {
                    // Relative to the game's own multiply, which is 100 rather than 255 -
                    // writing an absolute value brightened the node instead of leaving a
                    // white tint neutral.
                    node->MultiplyRed = (byte)Math.Clamp(originalGlowMultiply[row, group, 0] * tint.X, 0f, 255f);
                    node->MultiplyGreen = (byte)Math.Clamp(originalGlowMultiply[row, group, 1] * tint.Y, 0f, 255f);
                    node->MultiplyBlue = (byte)Math.Clamp(originalGlowMultiply[row, group, 2] * tint.Z, 0f, 255f);
                }

                appliedGlowX[row, group] = targetX;
                appliedGlowY[row, group] = targetY;
                appliedGlowScale[row, group] = targetScale;
                glowApplied[row, group] = true;
            }

            // The row glow's own node still carries its tint. Position and scale can't live
            // here - the game rewrites those during the fade - but colour survives.
            var rowMember = RowMember(addon, row);
            var glowNode = rowMember == null || rowMember->TargetGlow == null
                ? null
                : &rowMember->TargetGlow->AtkResNode;
            if (glowNode != null)
            {
                if (!rowGlowTintApplied[row])
                {
                    originalRowGlowMultiply[row, 0] = glowNode->MultiplyRed;
                    originalRowGlowMultiply[row, 1] = glowNode->MultiplyGreen;
                    originalRowGlowMultiply[row, 2] = glowNode->MultiplyBlue;
                    rowGlowTintApplied[row] = true;
                }

                var rowTint = hovered ? Settings.HoverTint : Settings.SelectionTint;
                glowNode->MultiplyRed = (byte)Math.Clamp(originalRowGlowMultiply[row, 0] * rowTint.X, 0f, 255f);
                glowNode->MultiplyGreen = (byte)Math.Clamp(originalRowGlowMultiply[row, 1] * rowTint.Y, 0f, 255f);
                glowNode->MultiplyBlue = (byte)Math.Clamp(originalRowGlowMultiply[row, 2] * rowTint.Z, 0f, 255f);

                // Scale stays on the glow node rather than the container, so our fill isn't
                // resized with it. The game drives this during the fade-in, so it takes hold
                // once that finishes - unlike position, which the container applies at once.
                var rowScaleSetting = Math.Max(0.1f, hovered ? Settings.HoverScale : Settings.SelectionScale);
                glowNode->OriginX = 0f;
                glowNode->OriginY = 0f;
                if (Math.Abs(glowNode->ScaleX - rowScaleSetting) > 0.001f)
                    glowNode->SetScale(rowScaleSetting, rowScaleSetting);
            }
        }
    }

    /// <summary>
    /// Which settings a glow node uses. The ring around the job icon is an image and always
    /// its own group. The row glow is a single node shared by hover and selection - the game
    /// only swaps the animation label on it - so those two can't be told apart by node and
    /// come from the row's state instead. A row that is both uses hover.
    /// </summary>

    private void RestoreSelectionGlowLayout(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            for (var slot = 0; slot < GlowGroups; slot++)
            {
                var node = addon == null ? null : GetGlowNode(addon, row, slot);
                if (node == null)
                    continue;

                var group = slot;
                if (!glowApplied[row, group])
                    continue;

                node->SetPositionFloat(originalGlowX[row, group], originalGlowY[row, group]);
                node->OriginX = originalGlowOriginX[row, group];
                node->OriginY = originalGlowOriginY[row, group];
                node->SetScale(originalGlowScale[row, group], originalGlowScale[row, group]);
                node->MultiplyRed = originalGlowMultiply[row, group, 0];
                node->MultiplyGreen = originalGlowMultiply[row, group, 1];
                node->MultiplyBlue = originalGlowMultiply[row, group, 2];

                if (node->Timeline != null)
                    node->Timeline->Mask = originalGlowMask[row, group];

                if (group == GlowStateIcon && iconGlowOnTopApplied[row])
                {
                    node->IsRenderedOnTop = originalIconGlowOnTop[row];
                    iconGlowOnTopApplied[row] = false;
                }
            }

            if (rowGlowTintApplied[row])
            {
                rowGlowTintApplied[row] = false;

                var rowMember = RowMember(addon, row);
                var glowNode = rowMember == null || rowMember->TargetGlow == null
                    ? null
                    : &rowMember->TargetGlow->AtkResNode;

                if (glowNode != null)
                {
                    glowNode->MultiplyRed = originalRowGlowMultiply[row, 0];
                    glowNode->MultiplyGreen = originalRowGlowMultiply[row, 1];
                    glowNode->MultiplyBlue = originalRowGlowMultiply[row, 2];
                }
            }

            for (var group = 0; group < GlowGroups; group++)
                glowApplied[row, group] = false;
        }
    }

    private static AtkComponentBase* GetStatusIconComponent(AddonPartyList* addon, int row, int index)
    {
        var member = RowMember(addon, row);
        if (member == null)
            return null;

        var icons = member->StatusIcons;
        if (index >= icons.Length)
            return null;

        var icon = icons[index].Value;
        return icon == null ? null : &icon->AtkComponentBase;
    }

    /// <summary>
    /// Sizes and positions the timer text inside each status icon. The text is a child of
    /// the icon, so it already inherits whatever scale the icon has - these adjustments sit
    /// on top of that, in the icon's own coordinate space.
    /// </summary>
    private void ApplyStatusTimerLayout(AddonPartyList* addon)
    {
        if (addon == null)
            return;

        if (!Settings.AdjustStatusTimers)
        {
            RestoreStatusTimerLayout(addon);
            return;
        }

        var texts = stackalloc AtkTextNode*[1];

        for (var row = 0; row < MaxRows; row++)
        {
            for (var i = 0; i < StatusIconSlots; i++)
            {
                var component = GetStatusIconComponent(addon, row, i);
                if (component == null)
                    continue;

                if (CollectComponentTextNodes(component, texts, 1, 0, 0) == 0)
                    continue;

                var text = texts[0];
                var fresh = !timerApplied[row, i];

                if (fresh || text->FontSize != appliedTimerFont[row, i])
                    originalTimerFont[row, i] = text->FontSize;
                if (fresh
                    || Math.Abs(text->AtkResNode.X - appliedTimerX[row, i]) > 0.01f
                    || Math.Abs(text->AtkResNode.Y - appliedTimerY[row, i]) > 0.01f)
                {
                    originalTimerX[row, i] = text->AtkResNode.X;
                    originalTimerY[row, i] = text->AtkResNode.Y;
                }

                var font = (byte)Math.Clamp(originalTimerFont[row, i] + Settings.StatusTimerFontDelta, 6, 60);
                var targetX = originalTimerX[row, i] + Settings.StatusTimerOffsetX;
                var targetY = originalTimerY[row, i] + Settings.StatusTimerOffsetY;

                if (text->FontSize != font)
                    text->FontSize = font;
                if (Math.Abs(text->AtkResNode.X - targetX) > 0.01f
                    || Math.Abs(text->AtkResNode.Y - targetY) > 0.01f)
                    text->AtkResNode.SetPositionFloat(targetX, targetY);

                ApplyTextColor(text, Settings.StatusTimerUseCustomColor, Settings.StatusTimerColor,
                    ref timerColor[row, i]);

                appliedTimerFont[row, i] = font;
                appliedTimerX[row, i] = targetX;
                appliedTimerY[row, i] = targetY;
                timerApplied[row, i] = true;
            }
        }
    }

    private void RestoreStatusTimerLayout(AddonPartyList* addon)
    {
        var texts = stackalloc AtkTextNode*[1];

        for (var row = 0; row < MaxRows; row++)
        {
            for (var i = 0; i < StatusIconSlots; i++)
            {
                var component = addon == null ? null : GetStatusIconComponent(addon, row, i);
                var text = component != null && CollectComponentTextNodes(component, texts, 1, 0, 0) > 0
                    ? texts[0]
                    : null;

                RestoreTextColor(text, ref timerColor[row, i]);

                if (!timerApplied[row, i])
                    continue;

                timerApplied[row, i] = false;

                if (text == null)
                    continue;

                text->FontSize = originalTimerFont[row, i];
                text->AtkResNode.SetPositionFloat(originalTimerX[row, i], originalTimerY[row, i]);
            }
        }
    }

    private void RestoreStatusIconLayout(AddonPartyList* addon)
    {
        for (var row = 0; row < MaxRows; row++)
        {
            for (var i = 0; i < StatusIconSlots; i++)
            {
                var node = addon == null ? null : GetStatusIconNode(addon, row, i);
                RestoreNodeTint(node, ref statusTint[row, i]);
                RestoreStatusSlot(row, i, node);
            }
        }
    }

    /// <summary>Hands one status icon slot back to the game, if we ever moved it.</summary>
    private void RestoreStatusSlot(int row, int slot, AtkResNode* node)
    {
        if (!statusApplied[row, slot])
            return;

        statusApplied[row, slot] = false;

        if (node == null)
            return;

        node->SetPositionFloat(originalStatusX[row, slot], originalStatusY[row, slot]);
        node->OriginX = originalStatusOriginX[row, slot];
        node->OriginY = originalStatusOriginY[row, slot];
        node->SetScale(originalStatusScale[row, slot], originalStatusScale[row, slot]);
    }


    private static bool IsNodeVisible(AtkResNode* node)
        => node != null && (node->NodeFlags & NodeFlags.Visible) != 0;

    /// <summary>The node this row's fill is attached to, which its coordinates are relative to.</summary>
    private AtkResNode* BarParent(AddonPartyList* addon, int row)
    {
        if (barOnBarRoot[row] && barRoot != null)
            return (AtkResNode*)barRoot;

        return overlayRoot == null ? null : (AtkResNode*)overlayRoot;
    }

    /// <summary>
    /// The member data behind a row. Duty support and trust NPCs are not party members -
    /// the addon draws them from a second array of its own, below the real party rows - so
    /// a row past the party's count resolves there and a row index always means the same
    /// row on screen whatever the content is.
    /// </summary>
    private static AddonPartyList.PartyListMemberStruct* RowMember(AddonPartyList* addon, int row)
    {
        if (addon == null || row < 0 || row >= MaxRows)
            return null;

        var trust = row - Math.Clamp(addon->MemberCount, 0, MaxRows);
        if (trust >= 0 && trust < addon->TrustCount)
        {
            fixed (AddonPartyList.PartyListMemberStruct* members = addon->TrustMembers)
                return members + trust;
        }

        fixed (AddonPartyList.PartyListMemberStruct* members = addon->PartyMembers)
            return members + row;
    }

    private static AtkComponentNode* GetRowNode(AddonPartyList* addon, int index)
    {
        if (addon == null)
            return null;

        var member = RowMember(addon, index);
        var component = member == null ? null : member->PartyMemberComponent;
        return component == null ? null : component->OwnerNode;
    }

    private static AtkResNode* RowComponentNode(AtkComponentBase* component)
        => component == null ? null : (AtkResNode*)component->OwnerNode;

    private static bool IsRankColumn(BarColumn col) => col is BarColumn.DpsRank or BarColumn.HpsRank;

    /// <summary>
    /// Ranks live on the combatant, stamped by whichever meter window last rendered them, so
    /// with no meter open they keep whatever that last render left behind. Re-stamped every
    /// update, immediately before the rows read them, rather than on the cache's slower beat
    /// where an open meter's own stamp would show through in between.
    /// </summary>
    private void StampRanks()
    {
        if (!ranksNeeded || rankableCombatants.Count == 0)
            return;

        try
        {
            MeterWindowHelper.StampRanks(rankableCombatants);
        }
        catch (Exception ex)
        {
            // Sorting on values the data thread is still writing can trip the comparer.
            ServiceManager.LogDebug(LogChannel.PartyMembership, $"Party list rank stamp failed: {ex.Message}");
        }
    }

    private void RefreshCacheIfStale()
    {
        var now = DateTime.UtcNow;
        if (now - lastCacheRefresh < CacheTtl)
            return;
        lastCacheRefresh = now;

        statsByName.Clear();
        rankableCombatants.Clear();
        localPlayerStats = null;
        encounterAggregates = null;
        maxDps = 0;
        ranksNeeded = Settings.Metrics.Any(IsRankColumn) || Settings.TotalsMetrics.Any(IsRankColumn);

        try
        {
            // Read the snapshot once - the property takes the store's lock on every access.
            var snapshot = dataService.Store.ActiveEncounter;

            // Sample data counts as live, so the config window and the wizard can be tuned
            // in town where no real encounter is running.
            encounterActive = (snapshot?.Encounter.IsActive ?? false) || dataService.Store.IsSampleDataActive;
            if (encounterActive)
                lastEncounterActive = now;

            var combatants = snapshot?.Combatants;
            if (combatants == null)
                return;

            foreach (var combatant in combatants)
            {
                // The same set the meter ranks over, minus its tab filter - the party list
                // has no tab, so a rank here reads against every player in the encounter.
                if (ranksNeeded && JobRegistry.GetRole(combatant.Job) != JobRole.Default)
                    rankableCombatants.Add(combatant);

                if (string.IsNullOrEmpty(combatant.Name))
                    continue;

                statsByName[combatant.Name] = combatant;

                if (combatant.IsLocalPlayer)
                    localPlayerStats = combatant;

                // AgentHUD reports bare names, the parser may report "Name@World".
                var at = combatant.Name.IndexOf('@');
                if (at > 0)
                    statsByName[combatant.Name[..at]] = combatant;

                // Duty support NPCs are reported against whoever brought them - the parser
                // names them "Alisaie (YOU)" where the party list row says just "Alisaie".
                // Added without displacing a combatant that owns the bare name outright.
                var owner = combatant.Name.LastIndexOf(" (", StringComparison.Ordinal);
                if (owner > 0 && combatant.Name.EndsWith(')'))
                    statsByName.TryAdd(combatant.Name[..owner], combatant);

                if (combatant.EncDps > maxDps)
                    maxDps = combatant.EncDps;
            }

            // Summed over everyone in the encounter rather than a tab's filtered set, since
            // the header has no tab. Skipped outright when no header metric asks for it.
            if (Settings.TotalsMetrics.Any(CombatantBarComponent.IsGroupColumn))
                encounterAggregates = GroupAggregates.Compute(combatants);
        }
        catch (Exception ex)
        {
            // The combatant list is owned by the data thread and can mutate mid-enumeration.
            ServiceManager.LogDebug(LogChannel.PartyMembership, $"Party list DPS cache refresh failed: {ex.Message}");
        }
    }
}
