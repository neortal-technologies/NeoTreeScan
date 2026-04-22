using System.Windows;

namespace NeoTreescan.App;

public partial class App : Application
{
    public static string? InitialRootPath { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
        {
            // Windows paths cannot legally contain quotes; strip any that sneaked in
            // from an upstream quoting bug so the path box renders a clean value.
            InitialRootPath = e.Args[0].Trim().Trim('"');
        }
    }
}
