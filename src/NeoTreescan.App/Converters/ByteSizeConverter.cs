using System;
using System.Globalization;
using System.Windows.Data;
using NeoTreescan.Core.Analysis;

namespace NeoTreescan.App.Converters;

public sealed class ByteSizeConverter : IValueConverter
{
    public static string Format(long bytes) => SizeFormat.Format(bytes);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long l) return Format(l);
        if (value is int i)  return Format(i);
        if (value is double d) return Format((long)d);
        return "-";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
