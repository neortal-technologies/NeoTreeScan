// One-off tool: renders the NeoTreescan logo into a multi-resolution .ico file.
// Usage: dotnet run --project tools/iconbuilder -- <output.ico>
//
// Design: blue rounded-square badge with a stylized pie/treemap mark - four wedges
// in different sizes to suggest "disk space broken down by folder".
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var outPath = args.Length > 0 ? args[0] : "NeoTreescan.ico";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);

        var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
        var pngs = new List<byte[]>();
        foreach (var s in sizes)
        {
            var bmp = RenderLogo(s);
            pngs.Add(EncodePng(bmp));
        }

        WriteIco(outPath, sizes, pngs);
        Console.WriteLine($"Wrote {outPath} ({new FileInfo(outPath).Length:N0} bytes, {sizes.Length} sizes).");
        return 0;
    }

    private static BitmapSource RenderLogo(int size)
    {
        var visual = new System.Windows.Media.DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            double w = size, h = size;
            double radius = w * 0.22;

            // Rounded-square background with a subtle top-to-bottom gradient for depth.
            var bg = new LinearGradientBrush(
                Color.FromRgb(0x5F, 0xA8, 0xFF),
                Color.FromRgb(0x3A, 0x82, 0xE6),
                new Point(0, 0), new Point(0, 1));
            dc.DrawRoundedRectangle(bg, null, new Rect(0, 0, w, h), radius, radius);

            // Pie/treemap mark centered.
            double cx = w / 2.0, cy = h / 2.0;
            double r = w * 0.30;
            DrawWedge(dc, cx, cy, r, startDeg: -90,  sweepDeg: 150, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.95);  // big
            DrawWedge(dc, cx, cy, r, startDeg:  60,  sweepDeg: 110, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.72);  // medium
            DrawWedge(dc, cx, cy, r, startDeg: 170,  sweepDeg:  60, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.50);  // small
            DrawWedge(dc, cx, cy, r, startDeg: 230,  sweepDeg:  40, Color.FromRgb(0xFF, 0xFF, 0xFF), 0.30);  // smaller

            // Thin ring around the pie for definition at large sizes.
            if (size >= 32)
            {
                dc.DrawEllipse(null,
                    new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), Math.Max(1, w * 0.018)),
                    new Point(cx, cy), r * 1.02, r * 1.02);
            }
        }

        var rtb = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);
        return rtb;
    }

    private static void DrawWedge(DrawingContext dc, double cx, double cy, double r, double startDeg, double sweepDeg, Color color, double alpha)
    {
        double start = startDeg * Math.PI / 180.0;
        double end = (startDeg + sweepDeg) * Math.PI / 180.0;
        var p0 = new Point(cx, cy);
        var p1 = new Point(cx + Math.Cos(start) * r, cy + Math.Sin(start) * r);
        var p2 = new Point(cx + Math.Cos(end) * r,   cy + Math.Sin(end) * r);
        bool largeArc = sweepDeg > 180;

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(p0, isFilled: true, isClosed: true);
            ctx.LineTo(p1, false, false);
            ctx.ArcTo(p2, new Size(r, r), 0, largeArc, SweepDirection.Clockwise, false, false);
        }
        geom.Freeze();

        var brush = new SolidColorBrush(Color.FromArgb((byte)(255 * alpha), color.R, color.G, color.B));
        dc.DrawGeometry(brush, null, geom);
    }

    private static byte[] EncodePng(BitmapSource src)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(src));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    // ICO format: ICONDIR (6 bytes) + N × ICONDIRENTRY (16 bytes each) + image data (PNG for modern icons).
    private static void WriteIco(string path, int[] sizes, List<byte[]> pngs)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        bw.Write((ushort)0);        // reserved
        bw.Write((ushort)1);        // type = 1 (ICO)
        bw.Write((ushort)sizes.Length);

        int headerSize = 6 + 16 * sizes.Length;
        int offset = headerSize;
        for (int i = 0; i < sizes.Length; i++)
        {
            int s = sizes[i];
            bw.Write((byte)(s >= 256 ? 0 : s)); // width (0 = 256)
            bw.Write((byte)(s >= 256 ? 0 : s)); // height
            bw.Write((byte)0);                  // color palette
            bw.Write((byte)0);                  // reserved
            bw.Write((ushort)1);                // planes
            bw.Write((ushort)32);               // bpp
            bw.Write((uint)pngs[i].Length);     // size in bytes
            bw.Write((uint)offset);             // offset
            offset += pngs[i].Length;
        }
        foreach (var png in pngs) bw.Write(png);
    }
}
