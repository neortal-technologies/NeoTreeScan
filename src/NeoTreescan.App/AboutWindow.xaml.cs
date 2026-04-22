using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using NeoTreescan.Core.Interop;

namespace NeoTreescan.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        WindowTheme.ApplyDarkTitleBar(this);

        Title              = "About " + Branding.ProductName;
        ProductText.Text   = Branding.ProductName;
        TaglineText.Text   = Branding.Tagline;
        VersionPill.Text   = "v" + Branding.Version;
        DescriptionText.Text = Branding.Description;

        VersionText.Text   = Branding.Version;
        RuntimeText.Text   = $".NET {Environment.Version.Major}.{Environment.Version.Minor}   ({System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription})";
        ArchText.Text      = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
        OsText.Text        = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

        bool admin = Privilege.IsAdministrator();
        PrivilegeText.Text = admin ? "Administrator (full disk access)" : "Standard user (protected paths unavailable)";
        PrivilegeText.Foreground = admin
            ? new SolidColorBrush(Color.FromRgb(0x5C, 0xC8, 0x8A))
            : new SolidColorBrush(Color.FromRgb(0xF2, 0xA3, 0x4C));
        PrivilegeIcon.Foreground = PrivilegeText.Foreground;

        CompanyText.Text   = Branding.Company;
        WebsiteLink.Inlines.Clear();
        WebsiteLink.Inlines.Add(new Run(Branding.Website));
        WebsiteLink.NavigateUri = TryMakeUri(Branding.Website);

        LicenseText.Text   = Branding.License;
        CopyrightText.Text = Branding.Copyright;
    }

    private static Uri? TryMakeUri(string s)
    {
        try
        {
            if (!s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) s = "https://" + s;
            return new Uri(s);
        }
        catch { return null; }
    }

    private void WebsiteLink_Click(object sender, RoutedEventArgs e)
    {
        var uri = WebsiteLink.NavigateUri;
        if (uri is null) return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
        }
        catch { /* ignore */ }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
