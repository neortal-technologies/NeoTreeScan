using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NeoTreescan.Core.Models;

namespace NeoTreescan.App.ViewModels;

public sealed partial class FolderNodeViewModel : ObservableObject
{
    public FolderNode Model { get; }

    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _isSelected;

    private bool _childrenLoaded;
    public ObservableCollection<FolderNodeViewModel> Children { get; } = new();

    public string Name => Model.Name;
    public string FullPath => Model.FullPath;
    public long TotalSize => Model.TotalSize;
    public long TotalAllocated => Model.TotalAllocated;
    public long TotalFileCount => Model.TotalFileCount;
    public long TotalFolderCount => Model.TotalFolderCount;
    public double PercentOfParent => Model.PercentOfParent;
    public bool IsReparsePoint => Model.IsReparsePoint;
    public bool HadErrors => Model.HadErrors;

    public FolderNodeViewModel(FolderNode model)
    {
        Model = model;
        if (model.Subfolders.Count > 0)
            Children.Add(null!); // placeholder triggers expander arrow
    }

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value || _childrenLoaded) return;
        _childrenLoaded = true;
        Children.Clear();
        foreach (var sub in Model.Subfolders)
            Children.Add(new FolderNodeViewModel(sub));
        // Sort largest first
        var sorted = new System.Collections.Generic.List<FolderNodeViewModel>(Children);
        sorted.Sort((a, b) => b.TotalSize.CompareTo(a.TotalSize));
        Children.Clear();
        foreach (var v in sorted) Children.Add(v);
    }
}
