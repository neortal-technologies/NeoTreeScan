using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using NeoTreescan.App.ViewModels;
using NeoTreescan.Core.Models;

namespace NeoTreescan.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowTheme.ApplyDarkTitleBar(this);
        DataContextChanged += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RequestPathFocus += (_, _) => PathBox.Focus();
        };
        if (DataContext is MainViewModel v0)
            v0.RequestPathFocus += (_, _) => PathBox.Focus();
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm && e.NewValue is FolderNodeViewModel node)
            vm.SelectedNode = node;
    }

    private void Treemap_NodeClicked(object? sender, FolderNode node)
    {
        if (DataContext is not MainViewModel vm || vm.RootNode is null) return;
        var targetVm = FindVm(vm.RootNode, node);
        if (targetVm is null) return;
        vm.NavigateTo(targetVm);
    }

    private static FolderNodeViewModel? FindVm(FolderNodeViewModel start, FolderNode target)
    {
        if (start.Model == target) return start;
        // Force lazy expansion so children exist
        if (!start.IsExpanded && start.Children.Count == 1 && start.Children[0] is null)
            start.IsExpanded = true;
        foreach (var c in start.Children)
        {
            if (c is null) continue;
            var hit = FindVm(c, target);
            if (hit is not null) return hit;
        }
        return null;
    }

    private FolderNodeViewModel? SelectedTreeNode =>
        (DataContext as MainViewModel)?.SelectedNode;

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        var node = SelectedTreeNode;
        if (node is null) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + node.FullPath + "\"",
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* ignore */ }
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        var node = SelectedTreeNode;
        if (node is null) return;
        try { Clipboard.SetText(node.FullPath); }
        catch { /* ignore */ }
    }

    private void SetAsRoot_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var node = SelectedTreeNode;
        if (node is null) return;
        vm.RootPath = node.FullPath;
    }

    private void FocusTreemap_Click(object sender, RoutedEventArgs e)
    {
        // Find the TabControl and select the first tab (Treemap)
        if (FindDescendant<TabControl>(this) is { } tc && tc.Items.Count > 0)
            tc.SelectedIndex = 0;
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var c = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (c is T hit) return hit;
            var deeper = FindDescendant<T>(c);
            if (deeper is not null) return deeper;
        }
        return null;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = TryGetDroppedFolder(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedFolder(e, out var path)) return;
        if (DataContext is not MainViewModel vm) return;
        vm.RootPath = path!;
        if (vm.ScanCommand.CanExecute(null)) vm.ScanCommand.Execute(null);
    }

    private static bool TryGetDroppedFolder(DragEventArgs e, out string? folder)
    {
        folder = null;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (files is null || files.Length == 0) return false;
        var f = files[0];
        if (Directory.Exists(f)) { folder = f; return true; }
        // If a file was dropped, scan its parent folder
        if (File.Exists(f))
        {
            var parent = Path.GetDirectoryName(f);
            if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) { folder = parent; return true; }
        }
        return false;
    }
}
