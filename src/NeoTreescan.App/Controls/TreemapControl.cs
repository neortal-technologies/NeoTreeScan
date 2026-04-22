using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NeoTreescan.Core.Models;

namespace NeoTreescan.App.Controls;

public sealed class TreemapControl : FrameworkElement
{
    public static readonly DependencyProperty RootProperty = DependencyProperty.Register(
        nameof(Root), typeof(FolderNode), typeof(TreemapControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public FolderNode? Root
    {
        get => (FolderNode?)GetValue(RootProperty);
        set => SetValue(RootProperty, value);
    }

    /// Fired when the user clicks a folder block - caller zooms in by updating SelectedNode.
    public event EventHandler<FolderNode>? NodeClicked;

    private abstract record HitItem(Rect Rect)
    {
        public abstract string TooltipPath { get; }
        public abstract long Size { get; }
    }
    private sealed record FolderHit(Rect Rect, FolderNode Node) : HitItem(Rect)
    {
        public override string TooltipPath => Node.FullPath;
        public override long Size => Node.TotalSize;
    }
    private sealed record FileHit(Rect Rect, FileEntry File) : HitItem(Rect)
    {
        public override string TooltipPath => File.FullPath;
        public override long Size => File.Size;
    }

    private readonly List<HitItem> _hits = new();

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
            return new Size(600, 400);
        return availableSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        _hits.Clear();
        if (Root is null || ActualWidth <= 2 || ActualHeight <= 2) return;

        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x18)), null, new Rect(0, 0, ActualWidth, ActualHeight));

        // Root fills entire canvas - we don't draw the root label so children use the whole area.
        var bounds = new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4));
        LayoutContainer(dc, Root, bounds, depth: 0, maxDepth: 4, drawOwnLabel: false);
    }

    private void LayoutContainer(DrawingContext dc, FolderNode node, Rect rect, int depth, int maxDepth, bool drawOwnLabel)
    {
        if (rect.Width < 2 || rect.Height < 2) return;

        if (drawOwnLabel)
        {
            var fill = FolderFill(node, depth);
            var stroke = new Pen(new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)), 0.5);
            dc.DrawRectangle(fill, stroke, rect);
            _hits.Add(new FolderHit(rect, node));
            DrawLabel(dc, node.Name, node.TotalSize, rect, Brushes.White);
        }

        if (depth >= maxDepth) return;

        // Collect children - folders + files - proportionate by total size.
        var items = BuildItems(node);
        if (items.Count == 0) return;

        long total = items.Sum(i => i.Size);
        if (total <= 0) return;

        // Inner rect: leave header space only if we drew the label
        double headerHeight = drawOwnLabel && rect.Height > 28 ? 16 : 0;
        var inner = new Rect(
            rect.X + (drawOwnLabel ? 2 : 0),
            rect.Y + headerHeight + (drawOwnLabel ? 2 : 0),
            Math.Max(0, rect.Width - (drawOwnLabel ? 4 : 0)),
            Math.Max(0, rect.Height - headerHeight - (drawOwnLabel ? 4 : 0)));
        if (inner.Width < 3 || inner.Height < 3) return;

        Squarify(dc, items, total, inner, depth + 1, maxDepth);
    }

    private abstract record Item(long Size, object Payload);
    private sealed record FolderItem(FolderNode Node) : Item(Node.TotalSize, Node);
    private sealed record FileItem(FileEntry File) : Item(File.Size, File);

    private static List<Item> BuildItems(FolderNode node)
    {
        var list = new List<Item>();
        foreach (var sub in node.Subfolders)
            if (sub.TotalSize > 0) list.Add(new FolderItem(sub));
        foreach (var f in node.Files)
            if (f.Size > 0) list.Add(new FileItem(f));
        list.Sort((a, b) => b.Size.CompareTo(a.Size));
        return list;
    }

    private static readonly Color[] FolderPalette =
    {
        Color.FromRgb(0x4C, 0x9A, 0xFF), // blue
        Color.FromRgb(0x5C, 0xC8, 0x8A), // green
        Color.FromRgb(0xF2, 0xA3, 0x4C), // orange
        Color.FromRgb(0xEB, 0x6F, 0x92), // rose
        Color.FromRgb(0xA5, 0x78, 0xE6), // violet
        Color.FromRgb(0x36, 0xB8, 0xC6), // teal
        Color.FromRgb(0xE6, 0xCC, 0x4C), // yellow
        Color.FromRgb(0xE3, 0x5D, 0x6A), // red
        Color.FromRgb(0x7A, 0xD0, 0xB4), // mint
        Color.FromRgb(0xFF, 0x95, 0x58), // coral
    };

    private Brush FolderFill(FolderNode node, int depth)
    {
        unchecked
        {
            int h = (node.Name + node.FullPath).GetHashCode();
            var c = FolderPalette[((h % FolderPalette.Length) + FolderPalette.Length) % FolderPalette.Length];
            double factor = Math.Max(0.55, 1.0 - depth * 0.12);
            return new SolidColorBrush(Color.FromRgb((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor)));
        }
    }

    private Brush FileFill(FileEntry f)
    {
        // File blocks are color-coded by extension so repeated extensions visually cluster.
        unchecked
        {
            int h = f.Extension.GetHashCode();
            var c = FolderPalette[((h % FolderPalette.Length) + FolderPalette.Length) % FolderPalette.Length];
            // Desaturate slightly so files don't compete with folder headers.
            byte r = (byte)(c.R * 0.7);
            byte g = (byte)(c.G * 0.7);
            byte b = (byte)(c.B * 0.7);
            return new SolidColorBrush(Color.FromRgb(r, g, b));
        }
    }

    private static readonly Pen ItemStroke = new(new SolidColorBrush(Color.FromArgb(130, 0, 0, 0)), 0.5);

    // Squarified treemap (Bruls/Huijing/van Wijk 2000).
    private void Squarify(DrawingContext dc, List<Item> items, long total, Rect rect, int depth, int maxDepth)
    {
        int i = 0;
        var remaining = rect;
        double remainingTotal = total;
        while (i < items.Count && remaining.Width > 0 && remaining.Height > 0)
        {
            double shortSide = Math.Min(remaining.Width, remaining.Height);
            var row = new List<Item> { items[i] };
            double rowSum = items[i].Size;
            int j = i + 1;
            while (j < items.Count)
            {
                double withNext = rowSum + items[j].Size;
                if (Worst(row, rowSum, shortSide, remainingTotal, remaining) >=
                    Worst(AppendTmp(row, items[j]), withNext, shortSide, remainingTotal, remaining))
                {
                    row.Add(items[j]);
                    rowSum = withNext;
                    j++;
                }
                else break;
            }
            DrawRow(dc, row, rowSum, remaining, remainingTotal, depth, maxDepth);
            if (remaining.Width <= remaining.Height)
            {
                double h = remaining.Height * (rowSum / remainingTotal);
                remaining = new Rect(remaining.X, remaining.Y + h, remaining.Width, Math.Max(0, remaining.Height - h));
            }
            else
            {
                double w = remaining.Width * (rowSum / remainingTotal);
                remaining = new Rect(remaining.X + w, remaining.Y, Math.Max(0, remaining.Width - w), remaining.Height);
            }
            remainingTotal -= rowSum;
            i = j;
        }
    }

    private static List<Item> AppendTmp(List<Item> row, Item extra)
    {
        var copy = new List<Item>(row.Count + 1);
        copy.AddRange(row);
        copy.Add(extra);
        return copy;
    }

    private static double Worst(List<Item> row, double rowSum, double shortSide, double remainingTotal, Rect remaining)
    {
        if (row.Count == 0 || rowSum <= 0 || remainingTotal <= 0) return double.PositiveInfinity;
        double area = (rowSum / remainingTotal) * remaining.Width * remaining.Height;
        double max = 0;
        foreach (var it in row)
        {
            double nArea = (it.Size / rowSum) * area;
            if (nArea <= 0) continue;
            double ratio = Math.Max((shortSide * shortSide) * (it.Size / rowSum) / area,
                                    area / ((shortSide * shortSide) * (it.Size / rowSum)));
            if (ratio > max) max = ratio;
        }
        return max;
    }

    private void DrawRow(DrawingContext dc, List<Item> row, double rowSum, Rect remaining, double remainingTotal, int depth, int maxDepth)
    {
        bool horizontal = remaining.Width > remaining.Height;
        if (horizontal)
        {
            double w = remaining.Width * (rowSum / remainingTotal);
            double y = remaining.Y;
            foreach (var it in row)
            {
                double h = remaining.Height * (it.Size / rowSum);
                DrawItem(dc, it, new Rect(remaining.X, y, w, h), depth, maxDepth);
                y += h;
            }
        }
        else
        {
            double h = remaining.Height * (rowSum / remainingTotal);
            double x = remaining.X;
            foreach (var it in row)
            {
                double w = remaining.Width * (it.Size / rowSum);
                DrawItem(dc, it, new Rect(x, remaining.Y, w, h), depth, maxDepth);
                x += w;
            }
        }
    }

    private void DrawItem(DrawingContext dc, Item it, Rect rect, int depth, int maxDepth)
    {
        if (rect.Width < 2 || rect.Height < 2) return;

        if (it is FolderItem fi)
        {
            // Draw folder with label + recurse.
            LayoutContainer(dc, fi.Node, rect, depth, maxDepth, drawOwnLabel: true);
        }
        else if (it is FileItem xi)
        {
            dc.DrawRectangle(FileFill(xi.File), ItemStroke, rect);
            _hits.Add(new FileHit(rect, xi.File));
            DrawLabel(dc, xi.File.Name, xi.File.Size, rect, new SolidColorBrush(Color.FromArgb(240, 250, 250, 250)));
        }
    }

    private void DrawLabel(DrawingContext dc, string name, long size, Rect rect, Brush textBrush)
    {
        if (rect.Width < 42 || rect.Height < 14) return;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var nameText = new FormattedText(
            name,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            11.5, textBrush, dpi)
        { MaxTextWidth = Math.Max(8, rect.Width - 6), MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
        dc.DrawText(nameText, new Point(rect.X + 4, rect.Y + 2));

        if (rect.Width > 80 && rect.Height > 32)
        {
            var sizeText = new FormattedText(
                Converters.ByteSizeConverter.Format(size),
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                10.5, new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)), dpi)
            { MaxTextWidth = Math.Max(8, rect.Width - 6), MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
            dc.DrawText(sizeText, new Point(rect.X + 4, rect.Y + 17));
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        var p = e.GetPosition(this);
        for (int i = _hits.Count - 1; i >= 0; i--)
        {
            if (_hits[i].Rect.Contains(p) && _hits[i] is FolderHit fh)
            {
                NodeClicked?.Invoke(this, fh.Node);
                return;
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        for (int i = _hits.Count - 1; i >= 0; i--)
        {
            if (_hits[i].Rect.Contains(p))
            {
                ToolTip = $"{_hits[i].TooltipPath}\n{Converters.ByteSizeConverter.Format(_hits[i].Size)}";
                return;
            }
        }
        ToolTip = null;
    }
}
