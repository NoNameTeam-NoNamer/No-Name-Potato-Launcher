using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using No_Namer.SettingsRecord.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace No_Name_Potato_Launcher;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        BackgroundPathInput.Text = SettingsStore.ReadSetting<string>(itemName: "BackgroundLocation");
        BackgroundOpacityInput.Value = SettingsStore.ReadSetting<double>(itemName: "BackgroundOpacity");
    }

    private async void BackgroundPathInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        SettingsStore.WriteSetting(itemName: "BackgroundLocation", itemData: BackgroundPathInput.Text);
    }
    private async void PickBackgroundFile(object sender, RoutedEventArgs e)
    {
        var filePicker = new FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow.current);
        WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);
        filePicker.FileTypeFilter.Add(".jpg");
        filePicker.FileTypeFilter.Add(".jpeg");
        filePicker.FileTypeFilter.Add(".png");
        filePicker.FileTypeFilter.Add(".bmp");
        filePicker.FileTypeFilter.Add(".gif");
        filePicker.FileTypeFilter.Add(".tiff");
        filePicker.FileTypeFilter.Add(".ico");
        filePicker.FileTypeFilter.Add(".jxr");
        var file = await filePicker.PickSingleFileAsync();
        if (file != null)
        {
            BackgroundPathInput.Text = file.Path;
            await Task.Delay(100);
            MainWindow.ReadBG();
        }
    }

    private async void BackgroundOpacityInput_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        SettingsStore.WriteSetting(itemName: "BackgroundOpacity", itemData: BackgroundOpacityInput.Value);
        await Task.Delay(100);
        MainWindow.ReadBG();
    }
}
