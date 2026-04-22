using System.Collections.Generic;

namespace NeoTreescan.Core.Models;

public sealed class FolderNode
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public FolderNode? Parent { get; set; }
    public List<FolderNode> Subfolders { get; } = new();
    public List<FileEntry> Files { get; } = new();

    public long DirectSize { get; set; }
    public long DirectAllocated { get; set; }
    public int DirectFileCount { get; set; }

    public long TotalSize { get; set; }
    public long TotalAllocated { get; set; }
    public long TotalFileCount { get; set; }
    public long TotalFolderCount { get; set; }

    public System.DateTime LastWriteUtc { get; set; }
    public bool IsReparsePoint { get; set; }
    public bool HadErrors { get; set; }
    public string? ErrorMessage { get; set; }

    public int Depth
    {
        get
        {
            int depth = 0;
            var n = Parent;
            while (n is not null) { depth++; n = n.Parent; }
            return depth;
        }
    }

    public double PercentOfParent =>
        Parent is null || Parent.TotalSize == 0 ? 100.0 :
        (double)TotalSize / Parent.TotalSize * 100.0;
}
