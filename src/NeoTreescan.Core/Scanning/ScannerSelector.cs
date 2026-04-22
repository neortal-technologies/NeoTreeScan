using System;
using System.IO;
using System.Text;
using NeoTreescan.Core.Interop;

namespace NeoTreescan.Core.Scanning;

public static class ScannerSelector
{
    public static IDirectoryScanner Select(string rootPath, bool preferFastPath)
    {
        if (!preferFastPath) return new Win32DirectoryScanner();
        if (LongPath.IsUnc(rootPath)) return new Win32DirectoryScanner();
        if (!Privilege.IsAdministrator()) return new Win32DirectoryScanner();

        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(rootPath));
            if (string.IsNullOrEmpty(root)) return new Win32DirectoryScanner();
            if (!root.EndsWith('\\')) root += "\\";

            var fsName = new StringBuilder(32);
            if (!NativeMethods.GetVolumeInformationW(
                    root, null, 0, out _, out _, out _, fsName, fsName.Capacity))
            {
                return new Win32DirectoryScanner();
            }
            if (!string.Equals(fsName.ToString(), "NTFS", StringComparison.OrdinalIgnoreCase))
                return new Win32DirectoryScanner();

            // MFT scanner is only a win when scanning the entire volume.
            // For subdirectory scans, use Win32 (faster than filtering MFT records).
            var normalizedRoot = Path.TrimEndingDirectorySeparator(root);
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
            if (!string.Equals(normalizedRoot, normalizedPath, StringComparison.OrdinalIgnoreCase))
                return new Win32DirectoryScanner();

            return new MftScanner();
        }
        catch
        {
            return new Win32DirectoryScanner();
        }
    }
}
