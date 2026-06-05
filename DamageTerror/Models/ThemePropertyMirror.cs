using System.Diagnostics;
using System.Reflection;
using Dalamud.Plugin.Services;

namespace DamageTerror.Models;

internal static class ThemePropertyMirror
{
    // Properties on ThemePreset that are metadata about the preset itself,
    // not theme attributes. They don't exist on Configuration, so the
    // name-match would skip them anyway — listing them here documents intent
    // and protects against future Configuration additions accidentally matching.
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.Ordinal)
    {
        nameof(ThemePreset.Name),
        nameof(ThemePreset.Description),
        nameof(ThemePreset.IsBuiltIn),
    };

    private static readonly Lazy<MirroredProperty[]> mirrored = new(BuildMirroredProperties);

    /// <summary>Copies all mirrored properties from <paramref name="preset"/> into <paramref name="config"/>.</summary>
    public static void ApplyTo(ThemePreset preset, Configuration config)
    {
        foreach (var p in mirrored.Value)
            p.Apply(preset, config);
    }

    /// <summary>Captures all mirrored properties from <paramref name="config"/> into <paramref name="preset"/>.</summary>
    public static void CaptureFrom(ThemePreset preset, Configuration config)
    {
        foreach (var p in mirrored.Value)
            p.Capture(preset, config);
    }

    /// <summary>
    /// DEBUG-only round-trip check: applies <paramref name="reference"/> to a temp config, captures back into a fresh preset,
    /// and asserts every mirrored property matches. Throws on first mismatch with the property name in the message.
    /// </summary>
    [Conditional("DEBUG")]
    public static void SelfCheckOrThrow(ThemePreset reference, IPluginLog log)
    {
        var tempConfig = new Configuration();
        ApplyTo(reference, tempConfig);
        var roundTrip = new ThemePreset
        {
            Name = reference.Name,
            Description = reference.Description,
            IsBuiltIn = reference.IsBuiltIn,
        };
        CaptureFrom(roundTrip, tempConfig);
        foreach (var p in mirrored.Value)
        {
            var a = p.GetPresetValue(reference);
            var b = p.GetPresetValue(roundTrip);
            if (!ValuesEqual(a, b))
                throw new InvalidOperationException(
                    $"ThemePropertyMirror round-trip failed for property '{p.Name}': '{a}' vs '{b}'");
        }
        log.Debug($"ThemePropertyMirror: {mirrored.Value.Length} properties mirrored OK");
    }

    private static MirroredProperty[] BuildMirroredProperties()
    {
        var presetType = typeof(ThemePreset);
        var configType = typeof(Configuration);
        var presetProps = presetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = new List<MirroredProperty>();

        foreach (var pp in presetProps)
        {
            if (!pp.CanRead || !pp.CanWrite) continue;
            if (ExcludedProperties.Contains(pp.Name)) continue;

            var cp = configType.GetProperty(pp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (cp == null || !cp.CanRead || !cp.CanWrite) continue;
            if (cp.PropertyType != pp.PropertyType) continue;

            result.Add(new MirroredProperty(pp, cp));
        }

        return result.ToArray();
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        // Dictionary<string, Vector4> deep equality (JobColors).
        if (a is Dictionary<string, Vector4> da && b is Dictionary<string, Vector4> db)
        {
            if (da.Count != db.Count) return false;
            foreach (var kv in da)
                if (!db.TryGetValue(kv.Key, out var bv) || !bv.Equals(kv.Value)) return false;
            return true;
        }
        // Dictionary<MetricType, SkillMarkerConfig> deep equality (DetailMarkers, GraphViewMarkers).
        if (a is Dictionary<MetricType, SkillMarkerConfig> mda &&
            b is Dictionary<MetricType, SkillMarkerConfig> mdb)
        {
            if (mda.Count != mdb.Count) return false;
            foreach (var kv in mda)
            {
                if (!mdb.TryGetValue(kv.Key, out var other)) return false;
                if (!ValuesEqual(kv.Value, other)) return false;  // recurses into SkillMarkerConfig branch
            }
            return true;
        }

        // SkillMarkerConfig: two distinct instances with equal field values count as equal.
        if (a is SkillMarkerConfig sa && b is SkillMarkerConfig sb)
        {
            return sa.ShowMarkers == sb.ShowMarkers
                && sa.MarkerColor.Equals(sb.MarkerColor)
                && sa.MarkerSize == sb.MarkerSize
                && sa.ShowCritMarkers == sb.ShowCritMarkers
                && sa.CritMarkerColor.Equals(sb.CritMarkerColor)
                && sa.DirectHitMarkerColor.Equals(sb.DirectHitMarkerColor)
                && sa.CritDirectHitMarkerColor.Equals(sb.CritDirectHitMarkerColor)
                && sa.ShowDoTTickMarkers == sb.ShowDoTTickMarkers
                && sa.DoTTickColor.Equals(sb.DoTTickColor)
                && sa.DoTTickMarkerSize == sb.DoTTickMarkerSize
                && sa.ShowDoTApplicationMarkers == sb.ShowDoTApplicationMarkers
                && sa.DoTApplicationColor.Equals(sb.DoTApplicationColor)
                && sa.DoTApplicationMarkerSize == sb.DoTApplicationMarkerSize;
        }
        return a.Equals(b);
    }

    private sealed class MirroredProperty
    {
        public string Name { get; }

        private readonly Func<ThemePreset, object?> getPreset;
        private readonly Action<ThemePreset, object?> setPreset;
        private readonly Func<Configuration, object?> getConfig;
        private readonly Action<Configuration, object?> setConfig;
        private readonly CopyStrategy strategy;

        public MirroredProperty(PropertyInfo presetProp, PropertyInfo configProp)
        {
            Name = presetProp.Name;
            getPreset = BuildGetter<ThemePreset>(presetProp);
            setPreset = BuildSetter<ThemePreset>(presetProp);
            getConfig = BuildGetter<Configuration>(configProp);
            setConfig = BuildSetter<Configuration>(configProp);
            strategy = SelectStrategy(presetProp);
        }

        public void Apply(ThemePreset preset, Configuration config)
            => setConfig(config, strategy.CopyForApply(getPreset(preset)));

        public void Capture(ThemePreset preset, Configuration config)
            => setPreset(preset, strategy.CopyForCapture(getConfig(config)));

        public object? GetPresetValue(ThemePreset preset) => getPreset(preset);

        private static CopyStrategy SelectStrategy(PropertyInfo presetProp)
        {
            if (presetProp.Name == nameof(ThemePreset.JobColors)) return JobColorsStrategy.Instance;
            if (presetProp.PropertyType == typeof(Dictionary<MetricType, SkillMarkerConfig>))
                return MarkerDictionaryStrategy.Instance;
            return ScalarStrategy.Instance;
        }

        private static Func<TOwner, object?> BuildGetter<TOwner>(PropertyInfo prop)
        {
            // Boxed-return delegate: TOwner -> object?
            // We use MethodInfo.Invoke as a minor compromise to keep this readable and avoid
            // per-type Expression compilation. The cost is one delegate dispatch + one virtual
            // PropertyInfo.GetValue call per property per ApplyTo/CaptureFrom. Negligible at
            // ~175 properties × rare invocation rate.
            var getter = prop.GetGetMethod(nonPublic: false)
                ?? throw new InvalidOperationException($"Property '{prop.Name}' has no public getter");
            return owner => getter.Invoke(owner, null);
        }

        private static Action<TOwner, object?> BuildSetter<TOwner>(PropertyInfo prop)
        {
            var setter = prop.GetSetMethod(nonPublic: false)
                ?? throw new InvalidOperationException($"Property '{prop.Name}' has no public setter");
            return (owner, value) => setter.Invoke(owner, [value]);
        }
    }

    private abstract class CopyStrategy
    {
        public abstract object? CopyForApply(object? value);
        public abstract object? CopyForCapture(object? value);
    }

    /// <summary>Pass-through for primitives, structs, enums, strings.</summary>
    private sealed class ScalarStrategy : CopyStrategy
    {
        public static readonly ScalarStrategy Instance = new();
        public override object? CopyForApply(object? value) => value;
        public override object? CopyForCapture(object? value) => value;
    }

    /// <summary>
    /// Asymmetric semantics matching the original manual code:
    /// - ApplyTo: preset null → empty dict on config; non-null → fresh copy on config.
    /// - CaptureFrom: empty dict on config → null on preset; non-empty → fresh copy on preset.
    /// </summary>
    private sealed class JobColorsStrategy : CopyStrategy
    {
        public static readonly JobColorsStrategy Instance = new();

        public override object? CopyForApply(object? value)
        {
            if (value is Dictionary<string, Vector4> source)
                return new Dictionary<string, Vector4>(source);
            return new Dictionary<string, Vector4>();
        }

        public override object? CopyForCapture(object? value)
        {
            if (value is Dictionary<string, Vector4> source && source.Count > 0)
                return new Dictionary<string, Vector4>(source);
            return null;
        }
    }

    /// <summary>
    /// Deep-clones Dictionary&lt;MetricType, SkillMarkerConfig&gt; by cloning each
    /// value entry. Used for DetailMarkers and GraphViewMarkers properties.
    /// </summary>
    private sealed class MarkerDictionaryStrategy : CopyStrategy
    {
        public static readonly MarkerDictionaryStrategy Instance = new();

        public override object? CopyForApply(object? value) => Copy(value);
        public override object? CopyForCapture(object? value) => Copy(value);

        private static Dictionary<MetricType, SkillMarkerConfig> Copy(object? value)
        {
            var src = value as Dictionary<MetricType, SkillMarkerConfig> ?? new();
            var dst = new Dictionary<MetricType, SkillMarkerConfig>(src.Count);
            foreach (var kv in src)
                dst[kv.Key] = kv.Value.Clone();
            return dst;
        }
    }
}
