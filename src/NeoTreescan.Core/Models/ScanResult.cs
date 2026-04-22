using System;
using System.Collections.Generic;

namespace NeoTreescan.Core.Models;

public sealed class ScanResult
{
    public required FolderNode Root { get; init; }
    public required DateTime Started { get; init; }
    public DateTime Finished { get; set; }
    public string ScannerUsed { get; set; } = "Win32";
    public List<ScanError> Errors { get; } = new();
    public bool Cancelled { get; set; }

    public TimeSpan Elapsed => Finished - Started;
    public long TotalFiles => Root.TotalFileCount;
    public long TotalFolders => Root.TotalFolderCount;
    public long TotalBytes => Root.TotalSize;
    public long TotalAllocated => Root.TotalAllocated;
}

public readonly record struct ScanError(string Path, string Message);
