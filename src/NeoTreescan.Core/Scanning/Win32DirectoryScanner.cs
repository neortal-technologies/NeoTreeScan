using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NeoTreescan.Core.Interop;
using NeoTreescan.Core.Models;

namespace NeoTreescan.Core.Scanning;

public sealed class Win32DirectoryScanner : IDirectoryScanner
{
    public string Name => "Win32";

    private sealed class ScanState
    {
        public long FilesSeen;
        public long FoldersSeen;
        public long BytesSeen;
        public string CurrentPath = string.Empty;
        public readonly ScanResult Result;
        public readonly CancellationToken Ct;
        public ScanState(ScanResult r, CancellationToken ct) { Result = r; Ct = ct; }
    }

    public Task<ScanResult> ScanAsync(string rootPath, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var normalized = Path.TrimEndingDirectorySeparator(rootPath.Replace('/', '\\'));
        var root = new FolderNode
        {
            Name = GetDisplayName(normalized),
            FullPath = normalized,
        };
        var result = new ScanResult { Root = root, Started = started };

        // When elevated, enable backup privilege so protected dirs (System Volume Information,
        // $Recycle.Bin, per-user profiles, etc.) can be enumerated.
        NeoTreescan.Core.Interop.Privilege.TryEnableBackupPrivilege();

        return Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            var state = new ScanState(result, cancellationToken) { CurrentPath = normalized };

            using var _ = progress is null ? null : new Timer(__ =>
            {
                progress.Report(new ScanProgress(
                    Interlocked.Read(ref state.FilesSeen),
                    Interlocked.Read(ref state.FoldersSeen),
                    Interlocked.Read(ref state.BytesSeen),
                    Volatile.Read(ref state.CurrentPath) ?? normalized,
                    sw.ElapsedMilliseconds));
            }, null, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200));

            try
            {
                ScanRecursive(root, state);
            }
            catch (OperationCanceledException)
            {
                result.Cancelled = true;
            }

            result.Finished = DateTime.UtcNow;
            progress?.Report(new ScanProgress(
                state.FilesSeen, state.FoldersSeen, state.BytesSeen, normalized, sw.ElapsedMilliseconds));
            return result;
        }, cancellationToken);
    }

    private static string GetDisplayName(string path)
    {
        var name = Path.GetFileName(path);
        return string.IsNullOrEmpty(name) ? path : name;
    }

    private static void ScanRecursive(FolderNode node, ScanState state)
    {
        state.Ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref state.FoldersSeen);
        Volatile.Write(ref state.CurrentPath, node.FullPath);

        var searchPath = LongPath.CombineNative(LongPath.Prefix(node.FullPath), "*");
        using var handle = NativeMethods.FindFirstFileExW(
            searchPath,
            NativeMethods.FINDEX_INFO_LEVELS.FindExInfoBasic,
            out var data,
            NativeMethods.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
            IntPtr.Zero,
            NativeMethods.FIND_FIRST_EX_LARGE_FETCH);

        if (handle.IsInvalid)
        {
            int err = Marshal.GetLastWin32Error();
            if (err != NativeMethods.ERROR_NO_MORE_FILES)
            {
                node.HadErrors = true;
                node.ErrorMessage = $"FindFirstFile failed ({err})";
                lock (state.Result.Errors)
                    state.Result.Errors.Add(new ScanError(node.FullPath, node.ErrorMessage));
            }
            RollupSelf(node);
            return;
        }

        var subdirs = new List<FolderNode>();
        do
        {
            if (data.cFileName == "." || data.cFileName == "..") continue;

            bool isDir = (data.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0;
            bool isReparse = (data.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0;
            long size = ((long)data.nFileSizeHigh << 32) | data.nFileSizeLow;
            var childPath = LongPath.CombineNative(node.FullPath, data.cFileName);
            var lastWrite = FileTimeToUtc(data.ftLastWriteTime);

            if (isDir)
            {
                var child = new FolderNode
                {
                    Name = data.cFileName,
                    FullPath = childPath,
                    Parent = node,
                    IsReparsePoint = isReparse,
                    LastWriteUtc = lastWrite,
                };
                if (isReparse)
                {
                    node.Subfolders.Add(child);
                }
                else
                {
                    subdirs.Add(child);
                }
            }
            else
            {
                long allocated = RoundUpToBlock(size, 4096);
                var file = new FileEntry(
                    data.cFileName, childPath, size, allocated, lastWrite, (FileAttributes)data.dwFileAttributes);
                node.Files.Add(file);
                node.DirectSize += size;
                node.DirectAllocated += allocated;
                node.DirectFileCount++;
                Interlocked.Add(ref state.BytesSeen, size);
                Interlocked.Increment(ref state.FilesSeen);
            }
        } while (NativeMethods.FindNextFileW(handle, out data));

        // Parallelize only the top two levels; deeper recursion serial (avoids thread explosion on deep trees)
        if (subdirs.Count > 1 && node.Depth < 2)
        {
            Parallel.ForEach(subdirs, new ParallelOptions
            {
                CancellationToken = state.Ct,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, sub => ScanRecursive(sub, state));
        }
        else
        {
            foreach (var sub in subdirs) ScanRecursive(sub, state);
        }
        foreach (var sub in subdirs) node.Subfolders.Add(sub);

        RollupSelf(node);
    }

    private static void RollupSelf(FolderNode node)
    {
        node.TotalSize = node.DirectSize;
        node.TotalAllocated = node.DirectAllocated;
        node.TotalFileCount = node.DirectFileCount;
        node.TotalFolderCount = node.Subfolders.Count;
        foreach (var sub in node.Subfolders)
        {
            node.TotalSize += sub.TotalSize;
            node.TotalAllocated += sub.TotalAllocated;
            node.TotalFileCount += sub.TotalFileCount;
            node.TotalFolderCount += sub.TotalFolderCount;
        }
    }

    private static long RoundUpToBlock(long bytes, long block) =>
        bytes == 0 ? 0 : ((bytes + block - 1) / block) * block;

    private static DateTime FileTimeToUtc(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        try
        {
            long raw = ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
            if (raw <= 0) return DateTime.MinValue;
            return DateTime.FromFileTimeUtc(raw);
        }
        catch { return DateTime.MinValue; }
    }
}
