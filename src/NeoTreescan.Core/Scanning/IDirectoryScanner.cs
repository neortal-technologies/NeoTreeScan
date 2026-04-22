using System;
using System.Threading;
using System.Threading.Tasks;
using NeoTreescan.Core.Models;

namespace NeoTreescan.Core.Scanning;

public interface IDirectoryScanner
{
    string Name { get; }
    Task<ScanResult> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
