namespace DamageTerror.Helpers;

public static class ValueFormatter
{
    public static string Format(double value, ValueDisplayFormat format, int decimals)
        => Format(value, format, decimals, 10_000, 1_000_000);

    public static string Format(double value, Configuration config)
    {
        var decimals = config.ValueDisplayFormat == ValueDisplayFormat.Abbreviated
            ? config.AbbreviatedDecimalPlaces
            : config.RawDecimalPlaces;
        return Format(value, config.ValueDisplayFormat, decimals, config.AbbreviatedKThreshold, config.AbbreviatedMThreshold);
    }

    public static string Format(double value, ValueDisplayFormat format, int decimals, double kThreshold, double mThreshold)
    {
        decimals = Math.Clamp(decimals, 0, 2);
        switch (format)
        {
            case ValueDisplayFormat.Commas:
                return ((long)Math.Round(value)).ToString("N0");
            case ValueDisplayFormat.Raw:
                return value.ToString($"F{decimals}");
            default:
                if (mThreshold > 0 && value >= mThreshold)
                    return (value / 1_000_000).ToString($"F{Math.Min(decimals + 1, 2)}") + "M";
                if (kThreshold > 0 && value >= kThreshold)
                    return (value / 1_000).ToString($"F{decimals}") + "K";
                return ((long)Math.Round(value)).ToString("N0");
        }
    }

    public static string FormatPercent(double value, int decimals)
    {
        decimals = Math.Clamp(decimals, 0, 2);
        return value.ToString($"F{decimals}") + "%";
    }

    public static string AbbreviateSkillName(string name, int maxLength, bool truncate = false)
    {
        if (maxLength <= 0 || string.IsNullOrEmpty(name) || name.Length <= maxLength)
            return name;

        if (truncate)
            return name[..maxLength] + "...";

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
            return name;

        return string.Concat(words.Select(w => w[0]));
    }

    public static string FormatColumn(double value, Configuration config, BarColumn column, MeterTab? activeTab)
    {
        if (activeTab?.ColumnFormatOverrides.TryGetValue(column, out var ov) == true)
        {
            var dec = ov.ValueDisplayFormat == ValueDisplayFormat.Abbreviated
                ? ov.AbbreviatedDecimalPlaces
                : ov.RawDecimalPlaces;
            return Format(value, ov.ValueDisplayFormat, dec, ov.AbbreviatedKThreshold, ov.AbbreviatedMThreshold);
        }
        return Format(value, config);
    }

    public static string FormatPercentColumn(double value, Configuration config, BarColumn column, MeterTab? activeTab)
    {
        if (activeTab?.ColumnFormatOverrides.TryGetValue(column, out var ov) == true)
            return FormatPercent(value, ov.PercentDecimalPlaces);
        return FormatPercent(value, config.PercentDecimalPlaces);
    }
}
