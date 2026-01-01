using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BookTranslator;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        
        var gitHubButton = this.FindControl<Button>("GitHubButton");
        if (gitHubButton != null)
        {
            gitHubButton.Click += GitHubButton_Click;
        }
    }

    private void GitHubButton_Click(object? sender, RoutedEventArgs e)
    {
        var url = "https://github.com/hudsonventura";
        OpenUrl(url);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
            // Silently fail if we can't open the URL
        }
    }
}
