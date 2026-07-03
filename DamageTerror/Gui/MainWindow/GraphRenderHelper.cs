using ImPlot = Dalamud.Bindings.ImPlot.ImPlot;

namespace DamageTerror.Gui.MainWindow;

internal struct GraphSettings
{
    public Vector4 BackgroundColor;
    public Vector4 GridColor;
    public bool ShowLegend;
    public bool ShowGrid;
    public bool ShowXAxisLabels;
    public bool ShowYAxisLabels;
    public bool AutoScroll;
    public float AutoScrollWindow;
    public float AutoScrollSmoothing;
    public float XAxisPadding;
    public bool XAxisMinSec;
    public float YAxisHeadroom;
    public int YAxisTickCount;
    public float MouseTextOpacity;

    public static GraphSettings From(Configuration config, bool isGraphView)
    {
        return isGraphView
            ? new GraphSettings
            {
                BackgroundColor = config.GraphViewBackgroundColor,
                GridColor = config.GraphViewGridColor,
                ShowLegend = config.GraphViewShowLegend,
                ShowGrid = config.GraphViewShowGrid,
                ShowXAxisLabels = config.GraphViewShowXAxisLabels,
                ShowYAxisLabels = config.GraphViewShowYAxisLabels,
                AutoScroll = config.GraphViewAutoScroll,
                AutoScrollWindow = config.GraphViewAutoScrollWindow,
                AutoScrollSmoothing = config.GraphViewAutoScrollSmoothing,
                XAxisPadding = config.GraphViewXAxisPadding,
                XAxisMinSec = config.GraphViewXAxisMinSec,
                YAxisHeadroom = config.GraphViewYAxisHeadroom,
                YAxisTickCount = config.GraphViewYAxisTickCount,
                MouseTextOpacity = config.GraphViewMouseTextOpacity,
            }
            : new GraphSettings
            {
                BackgroundColor = config.GraphBackgroundColor,
                GridColor = config.GraphGridColor,
                ShowLegend = config.GraphShowLegend,
                ShowGrid = config.GraphShowGrid,
                ShowXAxisLabels = config.GraphShowXAxisLabels,
                ShowYAxisLabels = config.GraphShowYAxisLabels,
                AutoScroll = config.GraphAutoScroll,
                AutoScrollWindow = config.GraphAutoScrollWindow,
                AutoScrollSmoothing = config.GraphAutoScrollSmoothing,
                XAxisPadding = config.GraphXAxisPadding,
                XAxisMinSec = config.GraphXAxisMinSec,
                YAxisHeadroom = config.GraphYAxisHeadroom,
                YAxisTickCount = config.GraphYAxisTickCount,
                MouseTextOpacity = config.GraphMouseTextOpacity,
            };
    }
}

internal static class GraphRenderHelper
{
    public static void PushGraphStyles(in GraphSettings settings)
    {
        ImPlot.PushStyleColor(ImPlotCol.Bg, settings.BackgroundColor);
        ImPlot.PushStyleColor(ImPlotCol.FrameBg, new Vector4(0, 0, 0, 0));
        if (settings.ShowGrid)
            ImPlot.PushStyleColor(ImPlotCol.AxisGrid, settings.GridColor);
    }

    public static void PopGraphStyles(bool showGrid)
    {
        if (showGrid)
            ImPlot.PopStyleColor();
        ImPlot.PopStyleColor(2);
    }

    public static (ImPlotFlags plot, ImPlotAxisFlags x, ImPlotAxisFlags y) ComputePlotFlags(in GraphSettings settings, float maxVal)
    {
        var plotFlags = ImPlotFlags.NoMouseText;
        if (!settings.ShowLegend) plotFlags |= ImPlotFlags.NoLegend;

        var xAxisFlags = ImPlotAxisFlags.None;
        if (!settings.ShowGrid) xAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!settings.ShowXAxisLabels) xAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        var yAxisFlags = maxVal > 0f ? ImPlotAxisFlags.AutoFit : ImPlotAxisFlags.None;
        if (!settings.ShowGrid) yAxisFlags |= ImPlotAxisFlags.NoGridLines;
        if (!settings.ShowYAxisLabels) yAxisFlags |= ImPlotAxisFlags.NoTickLabels;

        return (plotFlags, xAxisFlags, yAxisFlags);
    }

    public static void SetupGraphAxes(
        in GraphSettings settings,
        ImPlotFlags plotFlags,
        ImPlotAxisFlags xAxisFlags,
        ImPlotAxisFlags yAxisFlags,
        float maxTime,
        float maxVal,
        bool isLive,
        bool encounterActive,
        ref bool wasActivelyUpdating,
        ref double scrollXMin,
        ref double scrollXMax)
    {
        ImPlot.SetupAxes("", "", xAxisFlags, yAxisFlags);
        if (!plotFlags.HasFlag(ImPlotFlags.NoLegend))
            ImPlot.SetupLegend(ImPlotLocation.NorthWest, (ImPlotLegendFlags)2);  // NoButtons — our manual tracking controls both lines and markers

        var isActivelyUpdating = isLive && encounterActive;
        var justEnded = wasActivelyUpdating && !isActivelyUpdating;
        wasActivelyUpdating = isActivelyUpdating;
        var axisLimitCond = (isActivelyUpdating || justEnded) ? ImPlotCond.Always : ImPlotCond.Once;

        if (settings.AutoScroll && isActivelyUpdating && maxTime > settings.AutoScrollWindow)
        {
            var windowSec = settings.AutoScrollWindow;
            var targetMin = maxTime - windowSec;
            var targetMax = maxTime + windowSec * (settings.XAxisPadding - 1f);
            var dt = ImGui.GetIO().DeltaTime;
            var speed = settings.AutoScrollSmoothing;
            var t = (float)Math.Min(1.0, speed * dt);
            if (double.IsNaN(scrollXMin) || double.IsNaN(scrollXMax))
            {
                scrollXMin = targetMin;
                scrollXMax = targetMax;
            }
            else
            {
                scrollXMin += (targetMin - scrollXMin) * t;
                scrollXMax += (targetMax - scrollXMax) * t;
            }
            ImPlot.SetupAxisLimits(ImAxis.X1, scrollXMin, scrollXMax, axisLimitCond);
        }
        else
        {
            if (!double.IsNaN(scrollXMin)) { scrollXMin = double.NaN; scrollXMax = double.NaN; }
            ImPlot.SetupAxisLimits(ImAxis.X1, 0, maxTime * settings.XAxisPadding, axisLimitCond);
        }
        ImPlot.SetupAxisLimitsConstraints(ImAxis.X1, 0, double.MaxValue);

        if (settings.XAxisMinSec) SetupMinSecXTicks(maxTime, settings);

        if (maxVal > 0f) SetupAbbreviatedYTicks(maxVal, settings.YAxisHeadroom, settings.YAxisTickCount);
        else ImPlot.SetupAxisLimits(ImAxis.Y1, 0, 1, ImPlotCond.Always);
    }

    public static void DrawMousePositionText(float opacity, bool minSecFormat = false)
    {
        if (!ImPlot.IsPlotHovered()) return;
        var pos = ImPlot.GetPlotMousePos();
        var plotRect = ImPlot.GetPlotPos();
        var plotSize = ImPlot.GetPlotSize();
        var text = minSecFormat ? FormatMinSec((float)pos.X) : $"{pos.X:F1}s";
        var textSize = ImGui.CalcTextSize(text);
        var drawList = ImPlot.GetPlotDrawList();
        drawList.AddText(
            new Vector2(plotRect.X + plotSize.X - textSize.X - 4, plotRect.Y + plotSize.Y - textSize.Y - 4),
            ImGui.GetColorU32(new Vector4(1, 1, 1, opacity)),
            text);
    }

    private static string FormatMinSec(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        var mins = (int)(seconds / 60f);
        var secs = seconds - mins * 60f;
        return $"{mins}:{secs:00.0}";
    }

    private static void SetupMinSecXTicks(float maxTime, in GraphSettings settings)
    {
        var visibleMax = settings.AutoScroll ? settings.AutoScrollWindow * settings.XAxisPadding : maxTime * settings.XAxisPadding;
        if (visibleMax <= 0f) return;

        float step;
        if (visibleMax <= 30f) step = 5f;
        else if (visibleMax <= 60f) step = 10f;
        else if (visibleMax <= 180f) step = 15f;
        else if (visibleMax <= 600f) step = 30f;
        else step = 60f;

        var ticks = new List<double>();
        var labels = new List<string>();
        for (var v = 0f; v <= maxTime * settings.XAxisPadding + step; v += step)
        {
            ticks.Add(v);
            var m = (int)(v / 60f);
            var s = (int)(v - m * 60f);
            labels.Add($"{m}:{s:D2}");
        }

        if (ticks.Count > 0)
        {
            var tickArr = ticks.ToArray();
            var labelArr = labels.ToArray();
            ImPlot.SetupAxisTicks(ImAxis.X1, ref tickArr[0], ticks.Count, labelArr, false);
        }
    }

    public static float InterpolateValue(float[] times, float[]? values, float t)
    {
        if (values == null || values.Length == 0) return 0f;
        if (values.Length == 1) return values[0];
        if (t <= times[0]) return values[0];
        if (t >= times[^1]) return values[^1];

        for (var i = 1; i < times.Length; i++)
        {
            if (t <= times[i])
            {
                var frac = (t - times[i - 1]) / (times[i] - times[i - 1]);
                return values[i - 1] + frac * (values[i] - values[i - 1]);
            }
        }

        return values[^1];
    }

    public static string FormatValue(float val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000:F1}M";
        if (val >= 10_000) return $"{val / 1_000:F1}K";
        return $"{val:F0}";
    }

    public static string FormatValueLong(long val)
    {
        if (val >= 1_000_000) return $"{val / 1_000_000.0:F1}M";
        if (val >= 10_000) return $"{val / 1_000.0:F1}K";
        return val.ToString();
    }

    public static void SetupAbbreviatedYTicks(float maxVal, float headroomMultiplier, int targetTickCount)
    {
        var headroom = maxVal * headroomMultiplier;
        var step = headroom / targetTickCount;

        var magnitude = MathF.Pow(10f, MathF.Floor(MathF.Log10(step)));
        var normalized = step / magnitude;
        float niceStep;
        if (normalized <= 1f) niceStep = magnitude;
        else if (normalized <= 2f) niceStep = 2f * magnitude;
        else if (normalized <= 5f) niceStep = 5f * magnitude;
        else niceStep = 10f * magnitude;

        var ticks = new List<double>();
        var labels = new List<string>();
        for (var v = niceStep; v <= headroom; v += niceStep)
        {
            ticks.Add(v);
            if (v >= 1_000_000) labels.Add($"{v / 1_000_000:F1}M");
            else if (v >= 1_000) labels.Add($"{v / 1_000:G4}K");
            else labels.Add($"{v:F0}");
        }

        if (ticks.Count > 0)
        {
            var tickArr = ticks.ToArray();
            var labelArr = labels.ToArray();
            ImPlot.SetupAxisTicks(ImAxis.Y1, ref tickArr[0], ticks.Count, labelArr, false);
        }
    }

    public static void PlotScatterSubset(float[] allX, float[] allY, List<int> indices, string label, Vector4 color, float? markerSize = null)
    {
        if (indices.Count == 0) return;
        var sx = new float[indices.Count];
        var sy = new float[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            sx[i] = allX[indices[i]];
            sy[i] = allY[indices[i]];
        }
        if (markerSize.HasValue)
            ImPlot.PushStyleVar(ImPlotStyleVar.MarkerSize, markerSize.Value);
        ImPlot.PushStyleColor(ImPlotCol.MarkerFill, color);
        ImPlot.PushStyleColor(ImPlotCol.MarkerOutline, color);
        ImPlot.PlotScatter(label, ref sx[0], ref sy[0], indices.Count);
        ImPlot.PopStyleColor(2);
        if (markerSize.HasValue)
            ImPlot.PopStyleVar();
    }

    public static void PlotSkillMarkers(List<SkillUseEvent> events, float[] times, float[] values, string idPrefix, SkillMarkerConfig mc)
    {
        var mX = new float[events.Count];
        var mY = new float[events.Count];
        for (var ei = 0; ei < events.Count; ei++)
        {
            mX[ei] = events[ei].TimeSec;
            mY[ei] = InterpolateValue(times, values, events[ei].TimeSec);
        }

        List<int> dotTickIdx = [], dotAppIdx = [], regularIdx = [];
        for (var ei = 0; ei < events.Count; ei++)
        {
            var ev = events[ei];
            if (ev.IsDoTTick || ev.IsHoTTick)
                dotTickIdx.Add(ei);
            else if (ev.IsDoTApplication || ev.IsHoTApplication)
                dotAppIdx.Add(ei);
            else
                regularIdx.Add(ei);
        }

        if (mc.ShowDoTTickMarkers && dotTickIdx.Count > 0)
            PlotScatterSubset(mX, mY, dotTickIdx, $"##{idPrefix}_skills_dot", mc.DoTTickColor, mc.DoTTickMarkerSize);

        if (mc.ShowDoTApplicationMarkers && dotAppIdx.Count > 0)
            PlotScatterSubset(mX, mY, dotAppIdx, $"##{idPrefix}_skills_dotapp", mc.DoTApplicationColor, mc.DoTApplicationMarkerSize);

        if (regularIdx.Count > 0)
        {
            ImPlot.PushStyleVar(ImPlotStyleVar.MarkerSize, mc.MarkerSize);

            if (mc.ShowCritMarkers)
            {
                List<int> normalIdx = [], critIdx = [], dhIdx = [], cdhIdx = [];
                foreach (var ei in regularIdx)
                {
                    var ev = events[ei];
                    if (ev.IsCrit && ev.IsDirectHit) cdhIdx.Add(ei);
                    else if (ev.IsDirectHit) dhIdx.Add(ei);
                    else if (ev.IsCrit) critIdx.Add(ei);
                    else normalIdx.Add(ei);
                }

                PlotScatterSubset(mX, mY, normalIdx, $"##{idPrefix}_skills_n", mc.MarkerColor);
                PlotScatterSubset(mX, mY, critIdx, $"##{idPrefix}_skills_c", mc.CritMarkerColor);
                PlotScatterSubset(mX, mY, dhIdx, $"##{idPrefix}_skills_dh", mc.DirectHitMarkerColor);
                PlotScatterSubset(mX, mY, cdhIdx, $"##{idPrefix}_skills_cdh", mc.CritDirectHitMarkerColor);
            }
            else
            {
                PlotScatterSubset(mX, mY, regularIdx, $"##{idPrefix}_skills", mc.MarkerColor);
            }

            ImPlot.PopStyleVar();
        }
    }

    public static List<SkillUseEvent> GetSourceEvents(
        bool isLive, string combatantName,
        Services.SkillTracker skillTracker, EncounterSnapshot? snapshot)
    {
        if (isLive)
        {
            var events = skillTracker.GetSkillEvents(combatantName);
            if (events.Count == 0
                && snapshot?.SkillEvents != null
                && snapshot.SkillEvents.TryGetValue(combatantName, out var fallback))
            {
                return fallback;
            }
            return events;
        }

        if (snapshot?.SkillEvents != null
            && snapshot.SkillEvents.TryGetValue(combatantName, out var saved))
        {
            return saved;
        }

        return [];
    }

    public static List<SkillUseEvent> GetDamageTakenEvents(
        bool isLive, string combatantName,
        Services.SkillTracker skillTracker, EncounterSnapshot? snapshot)
    {
        if (isLive)
        {
            var events = skillTracker.GetDamageTakenEvents(combatantName);
            if (events.Count == 0
                && snapshot?.DamageTakenEvents != null
                && snapshot.DamageTakenEvents.TryGetValue(combatantName, out var fallback))
            {
                return fallback;
            }
            return events;
        }

        if (snapshot?.DamageTakenEvents != null
            && snapshot.DamageTakenEvents.TryGetValue(combatantName, out var saved))
        {
            return saved;
        }

        return [];
    }

    public static List<GraphSample> GetSamples(
        bool isLive, string combatantName,
        GraphDataTracker graphTracker, EncounterSnapshot? snapshot)
    {
        if (isLive)
        {
            var samples = graphTracker.GetSamples(combatantName);
            // Fall back to stored data when live tracker is empty
            // (e.g. encounter restored from history after plugin reload)
            if (samples.Count == 0
                && snapshot?.GraphData != null
                && snapshot.GraphData.TryGetValue(combatantName, out var fallback)
                && fallback.Count > 0)
            {
                return fallback;
            }
            return samples;
        }

        if (snapshot?.GraphData != null
            && snapshot.GraphData.TryGetValue(combatantName, out var saved)
            && saved.Count > 0)
        {
            return saved;
        }

        return [];
    }

    public static void DrawGraphContextMenu(Configuration config, bool isGraphView, string idSuffix)
    {
        var popupId = isGraphView ? $"##GraphViewCtx{idSuffix}" : $"##DetailGraphCtx{idSuffix}";
        if (!ImGui.BeginPopupContextItem(popupId, ImGuiPopupFlags.MouseButtonMiddle))
            return;

        var sliderSuffix = isGraphView ? "gvctx" : "dgctx";

        ImGui.TextDisabled("DamageTerror");
        ImGui.Separator();

        var autoScroll = isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll;
        if (ImGui.Checkbox("Auto-scroll", ref autoScroll))
        {
            if (isGraphView) config.GraphViewAutoScroll = autoScroll;
            else config.GraphAutoScroll = autoScroll;
        }

        if (isGraphView ? config.GraphViewAutoScroll : config.GraphAutoScroll)
        {
            ImGui.SetNextItemWidth(150);
            var scrollWin = isGraphView ? config.GraphViewAutoScrollWindow : config.GraphAutoScrollWindow;
            if (ImGui.SliderFloat($"Window##{sliderSuffix}", ref scrollWin, 15f, 300f, "%.0f sec"))
            {
                if (isGraphView) config.GraphViewAutoScrollWindow = scrollWin;
                else config.GraphAutoScrollWindow = scrollWin;
            }

            ImGui.SetNextItemWidth(150);
            var scrollSmooth = isGraphView ? config.GraphViewAutoScrollSmoothing : config.GraphAutoScrollSmoothing;
            if (ImGui.SliderFloat($"Smoothing##{sliderSuffix}", ref scrollSmooth, 1f, 30f, "%.1f"))
            {
                if (isGraphView) config.GraphViewAutoScrollSmoothing = scrollSmooth;
                else config.GraphAutoScrollSmoothing = scrollSmooth;
            }
        }

        ImGui.Separator();

        var showLegend = isGraphView ? config.GraphViewShowLegend : config.GraphShowLegend;
        if (ImGui.Checkbox("Show Legend", ref showLegend))
        {
            if (isGraphView) config.GraphViewShowLegend = showLegend;
            else config.GraphShowLegend = showLegend;
        }

        var showGrid = isGraphView ? config.GraphViewShowGrid : config.GraphShowGrid;
        if (ImGui.Checkbox("Show Grid", ref showGrid))
        {
            if (isGraphView) config.GraphViewShowGrid = showGrid;
            else config.GraphShowGrid = showGrid;
        }

        var showLabels = isGraphView ? config.GraphViewShowLabels : config.GraphShowLabels;
        if (ImGui.Checkbox("Show Value Labels", ref showLabels))
        {
            if (isGraphView) config.GraphViewShowLabels = showLabels;
            else config.GraphShowLabels = showLabels;
        }

        var minSec = isGraphView ? config.GraphViewXAxisMinSec : config.GraphXAxisMinSec;
        if (ImGui.Checkbox("X-Axis min:sec", ref minSec))
        {
            if (isGraphView) config.GraphViewXAxisMinSec = minSec;
            else config.GraphXAxisMinSec = minSec;
        }

        ImGui.EndPopup();
    }
}
