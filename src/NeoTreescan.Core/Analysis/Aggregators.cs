using System;
using System.Collections.Generic;
using System.Linq;
using NeoTreescan.Core.Models;

namespace NeoTreescan.Core.Analysis;

public readonly record struct TypeStat(string Extension, long FileCount, long TotalSize);
public readonly record struct BucketStat(string Label, long FileCount, long TotalSize);
public readonly record struct TopFile(string Path, long Size, DateTime LastWriteUtc, string Extension);

public static class Aggregators
{
    public static List<TypeStat> ByFileType(FolderNode root)
    {
        var map = new Dictionary<string, (long Count, long Size)>(StringComparer.OrdinalIgnoreCase);
        Walk(root, f =>
        {
            var ext = f.Extension;
            map.TryGetValue(ext, out var cur);
            map[ext] = (cur.Count + 1, cur.Size + f.Size);
        });
        return map
            .Select(kv => new TypeStat(kv.Key, kv.Value.Count, kv.Value.Size))
            .OrderByDescending(s => s.TotalSize)
            .ToList();
    }

    private static readonly (string Label, TimeSpan Max)[] AgeBuckets =
    {
        ("< 7 days",       TimeSpan.FromDays(7)),
        ("7 - 30 days",    TimeSpan.FromDays(30)),
        ("30 - 90 days",   TimeSpan.FromDays(90)),
        ("90 - 365 days",  TimeSpan.FromDays(365)),
        ("1 - 2 years",    TimeSpan.FromDays(730)),
        ("> 2 years",      TimeSpan.MaxValue),
    };

    public static List<BucketStat> ByAge(FolderNode root)
    {
        var counts = new long[AgeBuckets.Length];
        var sizes  = new long[AgeBuckets.Length];
        var now = DateTime.UtcNow;
        Walk(root, f =>
        {
            var age = f.LastWriteUtc == DateTime.MinValue ? TimeSpan.Zero : now - f.LastWriteUtc;
            for (int i = 0; i < AgeBuckets.Length; i++)
            {
                if (age <= AgeBuckets[i].Max)
                {
                    counts[i]++;
                    sizes[i] += f.Size;
                    return;
                }
            }
        });
        var list = new List<BucketStat>(AgeBuckets.Length);
        for (int i = 0; i < AgeBuckets.Length; i++)
            list.Add(new BucketStat(AgeBuckets[i].Label, counts[i], sizes[i]));
        return list;
    }

    private static readonly (string Label, long MaxBytes)[] SizeBuckets =
    {
        ("< 4 KB",          4L * 1024),
        ("4 KB - 1 MB",     1L * 1024 * 1024),
        ("1 MB - 100 MB",   100L * 1024 * 1024),
        ("100 MB - 1 GB",   1L * 1024 * 1024 * 1024),
        ("1 GB - 10 GB",    10L * 1024 * 1024 * 1024),
        ("> 10 GB",         long.MaxValue),
    };

    public static List<BucketStat> BySize(FolderNode root)
    {
        var counts = new long[SizeBuckets.Length];
        var sizes  = new long[SizeBuckets.Length];
        Walk(root, f =>
        {
            for (int i = 0; i < SizeBuckets.Length; i++)
            {
                if (f.Size <= SizeBuckets[i].MaxBytes)
                {
                    counts[i]++;
                    sizes[i] += f.Size;
                    return;
                }
            }
        });
        var list = new List<BucketStat>(SizeBuckets.Length);
        for (int i = 0; i < SizeBuckets.Length; i++)
            list.Add(new BucketStat(SizeBuckets[i].Label, counts[i], sizes[i]));
        return list;
    }

    public static List<TopFile> TopFiles(FolderNode root, int count = 1000)
    {
        // Use a min-heap of fixed size to avoid full sort on huge sets.
        var heap = new SortedSet<(long Size, string Path, DateTime LastWrite, string Ext)>(
            Comparer<(long, string, DateTime, string)>.Create((a, b) =>
            {
                int c = a.Item1.CompareTo(b.Item1);
                if (c != 0) return c;
                return string.Compare(a.Item2, b.Item2, StringComparison.Ordinal);
            }));
        Walk(root, f =>
        {
            if (heap.Count < count)
            {
                heap.Add((f.Size, f.FullPath, f.LastWriteUtc, f.Extension));
            }
            else if (f.Size > heap.Min.Item1)
            {
                heap.Remove(heap.Min);
                heap.Add((f.Size, f.FullPath, f.LastWriteUtc, f.Extension));
            }
        });
        return heap
            .Reverse()
            .Select(t => new TopFile(t.Path, t.Size, t.LastWrite, t.Ext))
            .ToList();
    }

    private static void Walk(FolderNode node, Action<FileEntry> onFile)
    {
        foreach (var f in node.Files) onFile(f);
        foreach (var sub in node.Subfolders) Walk(sub, onFile);
    }
}
