using System;
using System.Globalization;
using System.Windows.Data;

namespace NeoTreescan.App.Converters;

public sealed class PercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double pct = value switch
        {
            double d => d,
            float f  => f,
            int i    => i,
            long l   => l,
            _        => double.NaN,
        };

        if (double.IsNaN(pct)) return "-";

        // If a numeric parameter is supplied, return a scaled pixel width (0 .. parameter).
        if (parameter is not null)
        {
            double max = parameter switch
            {
                double d => d,
                string s => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0,
                int i    => i,
                _        => 0,
            };
            if (max > 0)
            {
                var clamped = Math.Max(0, Math.Min(100, pct));
                return clamped / 100.0 * max;
            }
        }

        return $"{pct:0.0}%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
