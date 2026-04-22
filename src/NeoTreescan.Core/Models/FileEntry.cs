using System;
using System.IO;

namespace NeoTreescan.Core.Models;

public readonly record struct FileEntry(
    string Name,
    string FullPath,
    long Size,
    long Allocated,
    DateTime LastWriteUtc,
    FileAttributes Attributes)
{
    public string Extension
    {
        get
        {
            var name = Name;
            int dot = name.LastIndexOf('.');
            if (dot <= 0 || dot == name.Length - 1) return "(none)";
            return name[dot..].ToLowerInvariant();
        }
    }
}
