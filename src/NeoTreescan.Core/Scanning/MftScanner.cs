using System;
using System.Threading;
using System.Threading.Tasks;
using NeoTreescan.Core.Models;

namespace NeoTreescan.Core.Scanning;

/// Placeholder type for a raw-MFT scanner. Raw MFT parsing via FSCTL_GET_NTFS_FILE_RECORD
/// (with full $FILE_NAME/$DATA attribute decoding) is not implemented here.
/// This scanner currently delegates to Win32DirectoryScanner; ScannerSelector returns it
/// only when the caller explicitly asks for the fast path, and the scan still succeeds.
/// It is tagged as "MFT(fallback)" in ScanResult.ScannerUsed so users can see what ran.
public sealed class MftScanner : IDirectoryScanner
{
    public string Name => "MFT(fallback)";

    public async Task<ScanResult> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var result = await new Win32DirectoryScanner().ScanAsync(rootPath, progress, cancellationToken);
        result.ScannerUsed = Name;
        return result;
    }
}
