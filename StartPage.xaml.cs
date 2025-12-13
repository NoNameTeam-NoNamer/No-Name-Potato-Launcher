using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Copilot.SettingsRecord.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace No_Name_Potato_Launcher;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StartPage : Page
{
    // Keep shared terminal instances so they survive navigation and can be reparented into the new page.
    //private static System.Collections.Generic.List<Copilot.EmbeddedTerminalControl.Helpers.EmbeddedTerminalControl> sharedTerminals = new();

    public StartPage()
    {
        InitializeComponent();
        /*InitializeTerminals();
        this.Loaded += StartPage_Loaded;
        this.Unloaded += StartPage_Unloaded;*/

    }
    /*
    private void InitializeTerminals()
    {
        // Create terminals once and keep them in a static list so they survive page navigation.
        if (sharedTerminals.Count == 0)
        {
            sharedTerminals.Add(new Copilot.EmbeddedTerminalControl.Helpers.EmbeddedTerminalControl());
            sharedTerminals.Add(new Copilot.EmbeddedTerminalControl.Helpers.EmbeddedTerminalControl());
        }

        // Reparent shared terminals into this page's TerminalHost.
        foreach (var term in sharedTerminals)
        {
            try
            {
                // If already hosted somewhere, remove from previous parent first.
                if (term.Parent is Panel p)
                {
                    p.Children.Remove(term);
                }
                TerminalHost.Children.Add(term);
            }
            catch
            {
                // best-effort: ignore reparent errors
            }
        }
    }

    private async System.Threading.Tasks.Task<string?> HandleCommandAsync(string cmd)
    {
        // Simple demo handler: implement some commands, else echo.
        await System.Threading.Tasks.Task.Yield();
        if (string.Equals(cmd, "time", StringComparison.OrdinalIgnoreCase))
        {
            return DateTime.Now.ToString("O");
        }
        if (cmd.StartsWith("get ", StringComparison.OrdinalIgnoreCase))
        {
            var key = cmd.Substring(4).Trim();
            var val = SettingsStore.ReadSetting<string>("./settings.json", key);
            return val ?? "(null)";
        }
        if (cmd.StartsWith("set ", StringComparison.OrdinalIgnoreCase))
        {
            var rest = cmd.Substring(4).Split(' ', 2);
            if (rest.Length == 2)
            {
                SettingsStore.WriteSetting("./settings.json", rest[0], rest[1]);
                return "OK";
            }
            return "Usage: set <key> <value>";
        }
        return "Echo: " + cmd;
    }

    private void StartPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sharedTerminals.Count > 0)
            {
                var term = sharedTerminals[0];
                // ensure CmdHost size changes will adjust embedding
                CmdHost.SizeChanged += CmdHost_SizeChanged;
                EmbedTerminalToHost(term);
            }
        }
        catch { }
    }

    private void StartPage_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sharedTerminals.Count > 0)
            {
                var term = sharedTerminals[0];
                term.SafeUnembed();
                CmdHost.SizeChanged -= CmdHost_SizeChanged;
            }
        }
        catch { }
    }

    private void CmdHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            if (sharedTerminals.Count == 0) return;
            EmbedTerminalToHost(sharedTerminals[0]);
        }
        catch { }
    }

    private void EmbedTerminalToHost(Copilot.EmbeddedTerminalControl.Helpers.EmbeddedTerminalControl term)
    {
        try
        {
            // compute position of CmdHost relative to this Page (client area)
            // TransformToVisual(null) gives coordinates relative to the XAML root (window client area)
            var transform = CmdHost.TransformToVisual(null);
            var pt = transform.TransformPoint(new Point(0, 0));
            int x = (int)Math.Round(pt.X);
            int y = (int)Math.Round(pt.Y);
            int w = (int)CmdHost.ActualWidth;
            int h = (int)CmdHost.ActualHeight;
            if (w <= 0 || h <= 0) return;
            term.SafeUnembed();
            term.EmbedToMainWindowRect(x, y, w, h);
        }
        catch { }
    }
    */
}
