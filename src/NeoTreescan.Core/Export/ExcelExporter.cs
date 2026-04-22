using System;
using System.IO;
using ClosedXML.Excel;
using NeoTreescan.Core.Analysis;
using NeoTreescan.Core.Models;

namespace NeoTreescan.Core.Export;

public static class ExcelExporter
{
    public static void Export(ScanResult result, string outputPath)
    {
        using var wb = new XLWorkbook();
        WriteSummary(wb, result);
        WriteFolders(wb, result);
        WriteFileTypes(wb, result);
        WriteAgeBuckets(wb, result);
        WriteSizeBuckets(wb, result);
        WriteTopFiles(wb, result);

        EnsureDirectory(outputPath);
        wb.SaveAs(outputPath);
    }

    private static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void WriteSummary(XLWorkbook wb, ScanResult r)
    {
        var ws = wb.Worksheets.Add("Summary");
        ws.Cell(1, 1).Value = "NeoTreescan Scan Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;

        int row = 3;
        void KV(string k, object v)
        {
            ws.Cell(row, 1).Value = k;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = v switch
            {
                string s => s,
                DateTime dt => dt,
                long l => l,
                double d => d,
                int i => i,
                TimeSpan ts => ts.ToString(@"hh\:mm\:ss"),
                _ => v.ToString() ?? string.Empty
            };
            row++;
        }

        KV("Root path", r.Root.FullPath);
        KV("Scanner used", r.ScannerUsed);
        KV("Started (UTC)", r.Started);
        KV("Finished (UTC)", r.Finished);
        KV("Elapsed", r.Elapsed);
        KV("Cancelled", r.Cancelled ? "Yes" : "No");
        KV("Total files", r.TotalFiles);
        KV("Total folders", r.TotalFolders);
        KV("Total size", SizeFormat.Format(r.TotalBytes));
        KV("Total allocated", SizeFormat.Format(r.TotalAllocated));
        KV("Errors", r.Errors.Count);

        ws.Columns().AdjustToContents();
    }

    private static void WriteFolders(XLWorkbook wb, ScanResult r)
    {
        var ws = wb.Worksheets.Add("Folders");
        string[] headers = { "Level", "Path", "Name", "Size", "Allocated", "Files", "Subfolders", "% of Parent", "% of Root", "Last Modified (UTC)" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        long rootSize = Math.Max(r.TotalBytes, 1);
        int row = 2;
        WriteFolderRow(ws, r.Root, ref row, rootSize);

        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();
    }

    private static void WriteFolderRow(IXLWorksheet ws, FolderNode node, ref int row, long rootSize)
    {
        ws.Cell(row, 1).Value  = node.Depth;
        ws.Cell(row, 2).Value  = node.FullPath;
        ws.Cell(row, 3).Value  = node.Name;
        ws.Cell(row, 4).Value  = SizeFormat.Format(node.TotalSize);
        ws.Cell(row, 5).Value  = SizeFormat.Format(node.TotalAllocated);
        ws.Cell(row, 6).Value  = node.TotalFileCount;
        ws.Cell(row, 7).Value  = node.TotalFolderCount;
        ws.Cell(row, 8).Value  = Math.Round(node.PercentOfParent, 2);
        ws.Cell(row, 9).Value  = Math.Round((double)node.TotalSize / rootSize * 100.0, 2);
        ws.Cell(row, 10).Value = node.LastWriteUtc == DateTime.MinValue ? null : (DateTime?)node.LastWriteUtc;
        row++;
        foreach (var sub in node.Subfolders)
            WriteFolderRow(ws, sub, ref row, rootSize);
    }

    private static void WriteFileTypes(XLWorkbook wb, ScanResult r)
    {
        var ws = wb.Worksheets.Add("File Types");
        var stats = Aggregators.ByFileType(r.Root);
        long total = Math.Max(r.TotalBytes, 1);

        ws.Cell(1, 1).Value = "Extension";
        ws.Cell(1, 2).Value = "File Count";
        ws.Cell(1, 3).Value = "Size";
        ws.Cell(1, 4).Value = "% of Total";
        ws.Row(1).Style.Font.Bold = true;

        for (int i = 0; i < stats.Count; i++)
        {
            var s = stats[i];
            ws.Cell(i + 2, 1).Value = s.Extension;
            ws.Cell(i + 2, 2).Value = s.FileCount;
            ws.Cell(i + 2, 3).Value = SizeFormat.Format(s.TotalSize);
            ws.Cell(i + 2, 4).Value = Math.Round((double)s.TotalSize / total * 100.0, 2);
        }
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();
    }

    private static void WriteBuckets(XLWorkbook wb, string sheetName, System.Collections.Generic.List<BucketStat> buckets, long total)
    {
        var ws = wb.Worksheets.Add(sheetName);
        ws.Cell(1, 1).Value = "Bucket";
        ws.Cell(1, 2).Value = "File Count";
        ws.Cell(1, 3).Value = "Size";
        ws.Cell(1, 4).Value = "% of Total";
        ws.Row(1).Style.Font.Bold = true;
        for (int i = 0; i < buckets.Count; i++)
        {
            var b = buckets[i];
            ws.Cell(i + 2, 1).Value = b.Label;
            ws.Cell(i + 2, 2).Value = b.FileCount;
            ws.Cell(i + 2, 3).Value = SizeFormat.Format(b.TotalSize);
            ws.Cell(i + 2, 4).Value = total == 0 ? 0 : Math.Round((double)b.TotalSize / total * 100.0, 2);
        }
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void WriteAgeBuckets(XLWorkbook wb, ScanResult r) =>
        WriteBuckets(wb, "Age Buckets", Aggregators.ByAge(r.Root), Math.Max(r.TotalBytes, 1));

    private static void WriteSizeBuckets(XLWorkbook wb, ScanResult r) =>
        WriteBuckets(wb, "Size Buckets", Aggregators.BySize(r.Root), Math.Max(r.TotalBytes, 1));

    private static void WriteTopFiles(XLWorkbook wb, ScanResult r)
    {
        var ws = wb.Worksheets.Add("Top Files");
        ws.Cell(1, 1).Value = "Path";
        ws.Cell(1, 2).Value = "Size";
        ws.Cell(1, 3).Value = "Extension";
        ws.Cell(1, 4).Value = "Last Modified (UTC)";
        ws.Row(1).Style.Font.Bold = true;

        var top = Aggregators.TopFiles(r.Root, 1000);
        for (int i = 0; i < top.Count; i++)
        {
            var f = top[i];
            ws.Cell(i + 2, 1).Value = f.Path;
            ws.Cell(i + 2, 2).Value = SizeFormat.Format(f.Size);
            ws.Cell(i + 2, 3).Value = f.Extension;
            ws.Cell(i + 2, 4).Value = f.LastWriteUtc == DateTime.MinValue ? null : (DateTime?)f.LastWriteUtc;
        }
        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents();
    }
}
