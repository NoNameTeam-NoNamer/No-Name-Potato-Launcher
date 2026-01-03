using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using No_Namer.SettingsRecord.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace No_Name_Potato_Launcher;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ColorTextWindow : Window
{
    private readonly Microsoft.UI.Windowing.AppWindow m_AppWindow;
    private Microsoft.UI.Windowing.AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        Microsoft.UI.WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);
    }
    private readonly Scighost.WinUILib.Helpers.SystemBackdrop backdropHelper;
    public ColorTextWindow()
    {
        InitializeComponent();
        this.Title = "某个不想起名的土豆烘烤器";
        m_AppWindow = GetAppWindowForCurrentWindow();
        this.SetTitleBar(AppTitleBar);
        CompactOverlayPresenter presenter = CompactOverlayPresenter.Create();
        presenter.InitialSize = CompactOverlaySize.Small;
        AppWindow.SetPresenter(presenter);
        var titleBar = m_AppWindow.TitleBar;
        // Hide system title bar.
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        MainFrame.Navigate(typeof(ColorTextPage), true);
        backdropHelper = new Scighost.WinUILib.Helpers.SystemBackdrop(this);
        backdropHelper.TrySetAcrylic(type:true);
    }
}
