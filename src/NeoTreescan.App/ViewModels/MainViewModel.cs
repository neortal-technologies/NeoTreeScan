using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeoTreescan.App.Converters;
using NeoTreescan.Core.Analysis;
using NeoTreescan.Core.Export;
using NeoTreescan.Core.Interop;
using NeoTreescan.Core.Models;
using NeoTreescan.Core.Scanning;
using Microsoft.Win32;

namespace NeoTreescan.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _rootPath = App.InitialRootPath ?? "C:\\";
    [ObservableProperty] private string _statusText = "Ready.";
    [ObservableProperty] private double _progressBytes;
    [ObservableProperty] private double _progressMax = 1;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _useFastPath = true;
    [ObservableProperty] private ScanResult? _lastResult;
    [ObservableProperty] private FolderNodeViewModel? _rootNode;
    [ObservableProperty] private FolderNodeViewModel? _selectedNode;
    [ObservableProperty] private string _scannerUsed = "-";
    [ObservableProperty] private string _totalsText = "";

    public bool IsAdministrator { get; } = Privilege.IsAdministrator();
    public bool IsNotAdministrator => !IsAdministrator;
    public string BrandName    => Branding.ProductName;
    public string BrandTagline => Branding.Tagline;
    public string BrandCompany => Branding.Company;

    public ObservableCollection<FolderNodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<FileTypeRow> FileTypes { get; } = new();
    public ObservableCollection<BucketRow> AgeBuckets { get; } = new();
    public ObservableCollection<BucketRow> SizeBuckets { get; } = new();
    public ObservableCollection<TopFile> TopFiles { get; } = new();
    public ObservableCollection<CrumbSegment> Breadcrumb { get; } = new();

    private CancellationTokenSource? _cts;

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    public async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(RootPath) || !Directory.Exists(RootPath))
        {
            StatusText = $"Path does not exist: {RootPath}";
            return;
        }

        IsScanning = true;
        ScanCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        ExportExcelCommand.NotifyCanExecuteChanged();

        _cts = new CancellationTokenSource();
        StatusText = $"Scanning {RootPath}…";
        ProgressBytes = 0;
        RootNodes.Clear();
        FileTypes.Clear();
        AgeBuckets.Clear();
        SizeBuckets.Clear();
        TopFiles.Clear();

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusText = $"Scanning: {p.CurrentPath}   |   {p.FilesSeen:N0} files   |   {ByteSizeConverter.Format(p.BytesSeen)}   |   {TimeSpan.FromMilliseconds(p.ElapsedMs):hh\\:mm\\:ss}";
            ProgressBytes = p.BytesSeen;
            ProgressMax = Math.Max(ProgressMax, p.BytesSeen + 1);
        });

        try
        {
            var scanner = ScannerSelector.Select(RootPath, UseFastPath);
            var result = await scanner.ScanAsync(RootPath, progress, _cts.Token);
            result.ScannerUsed = scanner.Name;
            LastResult = result;

            RootNode = new FolderNodeViewModel(result.Root) { IsExpanded = true, IsSelected = true };
            RootNodes.Add(RootNode);
            SelectedNode = RootNode;
            PopulateAnalysis(result);
            ScannerUsed = result.ScannerUsed;
            TotalsText = $"{result.TotalFiles:N0} files, {result.TotalFolders:N0} folders, {ByteSizeConverter.Format(result.TotalBytes)}";
            StatusText = result.Cancelled
                ? "Scan cancelled."
                : $"Scan complete in {result.Elapsed:hh\\:mm\\:ss} · {result.Errors.Count} errors";
        }
        catch (Exception ex)
        {
            StatusText = "Scan failed: " + ex.Message;
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
            ScanCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
            ExportExcelCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanStartScan() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    public void Cancel() => _cts?.Cancel();
    private bool CanCancel() => IsScanning;

    [RelayCommand(CanExecute = nameof(CanExport))]
    public void ExportExcel()
    {
        if (LastResult is null) return;
        var dlg = new SaveFileDialog
        {
            Title = "Export scan as Excel",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            FileName = $"NeoTreescan_{System.IO.Path.GetFileName(LastResult.Root.FullPath.TrimEnd('\\', '/'))}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ExcelExporter.Export(LastResult, dlg.FileName);
            StatusText = "Exported: " + dlg.FileName;
        }
        catch (Exception ex)
        {
            StatusText = "Export failed: " + ex.Message;
        }
    }
    private bool CanExport() => !IsScanning && LastResult is not null;

    [RelayCommand]
    public void BrowseFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Choose folder to scan" };
        if (dlg.ShowDialog() == true) RootPath = dlg.FolderName;
    }

    [RelayCommand]
    public void RestartAsAdmin()
    {
        try
        {
            var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                Arguments = EscapeArg(RootPath),
            };
            System.Diagnostics.Process.Start(psi);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusText = "Elevation cancelled: " + ex.Message;
        }
    }

    // Windows CommandLineToArgvW escape rules: backslashes before a quote must be doubled;
    // trailing backslashes before the closing quote must also be doubled. Without this,
    // `"C:\"` parses as argv[0]="C:\"" (literal quote) and corrupts the path.
    private static string EscapeArg(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        bool needsQuotes = arg.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0 || arg.EndsWith('\\');
        if (!needsQuotes) return arg;

        var sb = new System.Text.StringBuilder();
        sb.Append('"');
        int backslashes = 0;
        foreach (var c in arg)
        {
            if (c == '\\') { backslashes++; continue; }
            if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1);
                sb.Append('"');
            }
            else
            {
                if (backslashes > 0) sb.Append('\\', backslashes);
                sb.Append(c);
            }
            backslashes = 0;
        }
        if (backslashes > 0) sb.Append('\\', backslashes * 2);
        sb.Append('"');
        return sb.ToString();
    }

    [RelayCommand]
    public void ShowAbout()
    {
        var w = new AboutWindow { Owner = System.Windows.Application.Current.MainWindow };
        w.ShowDialog();
    }

    private void PopulateAnalysis(ScanResult result)
    {
        var types = Aggregators.ByFileType(result.Root);
        long typeMax = types.Count > 0 ? types[0].TotalSize : 1;
        foreach (var t in types)
            FileTypes.Add(new FileTypeRow(t.Extension, t.FileCount, t.TotalSize,
                typeMax == 0 ? 0 : (double)t.TotalSize / typeMax * 100.0));

        var ages = Aggregators.ByAge(result.Root);
        long ageMax = 1;
        foreach (var b in ages) if (b.TotalSize > ageMax) ageMax = b.TotalSize;
        foreach (var b in ages)
            AgeBuckets.Add(new BucketRow(b.Label, b.FileCount, b.TotalSize, (double)b.TotalSize / ageMax * 100.0));

        var sizes = Aggregators.BySize(result.Root);
        long sizeMax = 1;
        foreach (var b in sizes) if (b.TotalSize > sizeMax) sizeMax = b.TotalSize;
        foreach (var b in sizes)
            SizeBuckets.Add(new BucketRow(b.Label, b.FileCount, b.TotalSize, (double)b.TotalSize / sizeMax * 100.0));

        foreach (var f in Aggregators.TopFiles(result.Root, 1000)) TopFiles.Add(f);
    }

    // Called by CommunityToolkit source-generator when SelectedNode changes.
    partial void OnSelectedNodeChanged(FolderNodeViewModel? value) => RebuildBreadcrumb(value);

    private void RebuildBreadcrumb(FolderNodeViewModel? node)
    {
        Breadcrumb.Clear();
        if (node is null || RootNode is null) return;

        // Walk from node up to RootNode, collecting VMs in reverse.
        var chain = new System.Collections.Generic.List<FolderNodeViewModel>();
        var cur = node;
        while (cur is not null)
        {
            chain.Add(cur);
            if (cur == RootNode) break;
            // Find parent VM by matching Model.Parent
            cur = FindVmForModel(RootNode, cur.Model.Parent);
            if (cur is null) break;
        }
        chain.Reverse();
        for (int i = 0; i < chain.Count; i++)
            Breadcrumb.Add(new CrumbSegment { Label = chain[i].Name, Node = chain[i], HasMore = i < chain.Count - 1 });
    }

    private static FolderNodeViewModel? FindVmForModel(FolderNodeViewModel start, FolderNode? target)
    {
        if (target is null) return null;
        if (start.Model == target) return start;
        foreach (var c in start.Children)
        {
            if (c is null) continue;
            var hit = FindVmForModel(c, target);
            if (hit is not null) return hit;
        }
        return null;
    }

    [RelayCommand]
    public void NavigateTo(FolderNodeViewModel? node)
    {
        if (node is null) return;
        // Walking up to the node - expand ancestors so the tree row is visible.
        var cur = node.Model.Parent;
        while (cur is not null)
        {
            var vm = RootNode is null ? null : FindVmForModel(RootNode, cur);
            if (vm is not null) vm.IsExpanded = true;
            cur = cur.Parent;
        }
        node.IsSelected = true;
        SelectedNode = node;
    }

    public event System.EventHandler? RequestPathFocus;
    [RelayCommand]
    public void FocusPath() => RequestPathFocus?.Invoke(this, System.EventArgs.Empty);
}
