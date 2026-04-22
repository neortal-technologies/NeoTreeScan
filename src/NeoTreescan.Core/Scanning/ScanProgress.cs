namespace NeoTreescan.Core.Scanning;

public readonly record struct ScanProgress(
    long FilesSeen,
    long FoldersSeen,
    long BytesSeen,
    string CurrentPath,
    long ElapsedMs);
