namespace NeoTreescan.App.ViewModels;

public readonly record struct FileTypeRow(string Extension, long FileCount, long TotalSize, double BarPercent);
public readonly record struct BucketRow(string Label, long FileCount, long TotalSize, double BarPercent);

public sealed class CrumbSegment
{
    public required string Label { get; init; }
    public required FolderNodeViewModel Node { get; init; }
    public bool HasMore { get; init; }
}
