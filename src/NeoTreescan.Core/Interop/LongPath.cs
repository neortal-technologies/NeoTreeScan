using System;

namespace NeoTreescan.Core.Interop;

public static class LongPath
{
    private const string LocalPrefix = @"\\?\";
    private const string UncPrefix = @"\\?\UNC\";

    public static string Prefix(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        // The \\?\ prefix disables path normalization, so we must hand Win32 backslashes only.
        var normalized = path.Replace('/', '\\');
        if (normalized.StartsWith(LocalPrefix, StringComparison.Ordinal)) return normalized;

        if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return UncPrefix + normalized.Substring(2);
        }
        return LocalPrefix + normalized;
    }

    public static string Unprefix(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith(UncPrefix, StringComparison.Ordinal))
            return @"\\" + path.Substring(UncPrefix.Length);
        if (path.StartsWith(LocalPrefix, StringComparison.Ordinal))
            return path.Substring(LocalPrefix.Length);
        return path;
    }

    public static bool IsUnc(string path) =>
        path.StartsWith(@"\\", StringComparison.Ordinal) &&
        !path.StartsWith(LocalPrefix, StringComparison.Ordinal);

    public static string CombineNative(string parent, string child)
    {
        if (parent.EndsWith('\\')) return parent + child;
        return parent + "\\" + child;
    }
}
