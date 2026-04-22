namespace NeoTreescan.Core.Analysis;

/// Human-readable size formatting. MB/GB/TB only - per product decision that the UI and
/// the Excel export should always speak in these units (no bytes, no KB).
public static class SizeFormat
{
    private const double MB = 1024d * 1024d;
    private const double GB = MB * 1024d;
    private const double TB = GB * 1024d;

    public static string Format(long bytes)
    {
        if (bytes < 0) return "-";
        if (bytes == 0) return "0 MB";
        double mb = bytes / MB;
        if (mb < 0.01) return "< 0.01 MB";
        if (bytes < GB) return $"{mb:0.##} MB";
        if (bytes < TB) return $"{bytes / GB:0.##} GB";
        return $"{bytes / TB:0.##} TB";
    }
}
