namespace DamageTerror.Helpers;

public static class ValueFormatter
{
    public static string Format(double value, ValueDisplayFormat format)
    {
        switch (format)
        {
            case ValueDisplayFormat.Commas:
                return ((long)Math.Round(value)).ToString("N0");
            case ValueDisplayFormat.Raw:
                return $"{value:F1}";
            default:
                if (value >= 1_000_000)
                    return $"{value / 1_000_000:F2}M";
                if (value >= 10_000)
                    return $"{value / 1_000:F1}K";
                return $"{value:F1}";
        }
    }
}
