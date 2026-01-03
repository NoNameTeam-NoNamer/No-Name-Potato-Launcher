using Microsoft.UI;
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;
using Color = Windows.UI.Color;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace No_Name_Potato_Launcher;

public class ColorTheme
{
    [JsonIgnore]
    public string name = "新主题";

    [JsonPropertyName("stops")]
    public List<GradientStop> Stops { get; set; } = [];
}

public class GradientStop
{
    // 使用转换器
    [JsonConverter(typeof(ColorJsonConverter))]
    public Color Color { get; set; } = Colors.Black;

    [JsonPropertyName("offset")]
    public double Offset { get; set; } = 0.0;

    [JsonIgnore]
    public string ColorHex
    {
        get => $"#{Color.A:X2}{Color.R:X2}{Color.G:X2}{Color.B:X2}";
    }
}

// 颜色转换器：在序列化时使用字符串，反序列化时转换回Color
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var colorString = reader.GetString();
        return ColorFromString(colorString);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ColorToString(value));
    }

    private static string ColorToString(Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static Color ColorFromString(string colorString)
    {
        try
        {
            colorString = colorString.TrimStart('#');

            if (colorString.Length == 6)
            {
                // RGB格式，添加不透明度 FF
                colorString = "FF" + colorString;
            }
            else if (colorString.Length != 8)
            {
                throw new ArgumentException("颜色格式不正确，应为#RRGGBB或#AARRGGBB格式");
            }

            var a = Convert.ToByte(colorString[..2], 16);
            var r = Convert.ToByte(colorString.Substring(2, 2), 16);
            var g = Convert.ToByte(colorString.Substring(4, 2), 16);
            var b = Convert.ToByte(colorString.Substring(6, 2), 16);

            return Color.FromArgb(a, r, g, b);
        }
        catch
        {
            return Colors.Black;
        }
    }
}

// 定义颜色格式转换的接口（所有格式都要实现这个方法）
public interface IColorFormatter
{
    // 入参：Color对象；出参：对应格式的字符串
    string Format(Color color, char c);
}

// 前端颜色转换接口
public partial class ListToGradientStopCollectionConverter : IValueConverter
{
    // 将List<GradientStop>转为GradientStopCollection
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IEnumerable<GradientStop> stopsList)
        {
            var gradientStops = new GradientStopCollection();
            foreach (var stop in stopsList)
            {
                gradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
                {
                    Color = stop.Color,
                    Offset = stop.Offset
                });
            }
            return gradientStops;
        }
        return new GradientStopCollection(); // 空集合兜底
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

// 前端Color转SolidColorBrush的转换器
public partial class ColorToBrushConverter : IValueConverter
{
    // Color → SolidColorBrush
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Windows.UI.Color color) // 注意：WinUI 3用Microsoft.UI.Color，UWP用Windows.UI.Color
        {
            return new SolidColorBrush(color);
        }
        // 兜底返回黑色画笔
        return new SolidColorBrush(Microsoft.UI.Colors.Black);
    }

    // 反向转换（Brush→Color，可选实现）
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }
        return Microsoft.UI.Colors.Black;
    }
}
// Hex格式（#AARRGGBB）
public class HexArgbColorFormatter : IColorFormatter
{
    public string Format(Color color, char c)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}{c}";
    }
}
// Hex格式（#RRGGBB）
public class HexRgbColorFormatter : IColorFormatter
{
    public string Format(Color color, char c)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}{c}";
    }
}
// MC格式（&#RRGGBB）
public class MCRgbColorFormatter : IColorFormatter
{
    public string Format(Color color, char c)
    {
        return $"&#{color.R:X2}{color.G:X2}{color.B:X2}{c}";
    }
}
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ColorTextPage : Page
{
    private readonly ObservableCollection<ColorTheme> _themes = [];

    public ObservableCollection<ColorTheme> Themes
    {
        get { return this._themes; }
    }

    ColorTheme currentTheme = new();

    public ColorTextPage()
    {
        InitializeComponent();
        WindowButton.Visibility = true ? Visibility.Collapsed : Visibility.Visible;
        currentTheme = SettingsStore.ReadSetting<ColorTheme>(fileLocation: "./ColorThemes.json", itemName:SettingsStore.ReadSetting<string>(fileLocation: "./ColorThemes.json", itemName: "currentTheme")) ?? new ColorTheme()
        {
            Stops =
            [
                new() { Color = Color.FromArgb(255,238,119,0), Offset = 0.0 },
                new() { Color = Color.FromArgb(255,0,187,238), Offset = 1.0 }
            ]
        };
        currentTheme.name = SettingsStore.ReadSetting<ColorTheme>(fileLocation: "./ColorThemes.json", itemName: SettingsStore.ReadSetting<string>(fileLocation: "./ColorThemes.json", itemName: "currentTheme")) != null ?
            SettingsStore.ReadSetting<string>(fileLocation: "./ColorThemes.json", itemName: "currentTheme") ?? "默认主题" : "默认主题";
        ApplyGradientToButton(PreviewButton, currentTheme);
        foreach (string ctn in SettingsStore.ReadSetting<List<string>>(fileLocation: "./ColorThemes.json", itemName: "Themes") ?? Enumerable.Empty<string>())
        { 
            ColorTheme? theme = SettingsStore.ReadSetting<ColorTheme>(fileLocation: "./ColorThemes.json", itemName: ctn);
            if (theme != null)
            {
                theme.name = ctn;
                Themes.Add(theme);
            }
        }
        FormatChoose.SelectedIndex = SettingsStore.ReadSetting<int>(fileLocation: "./ColorThemes.json", itemName: "Format");
    }

    // 在OnNavigatedTo中接收参数
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        WindowButton.Visibility = e.Parameter is true?Visibility.Collapsed : Visibility.Visible;
        UIPanel.VerticalAlignment = e.Parameter is true ? VerticalAlignment.Bottom : VerticalAlignment.Center;
    }

    private void OpenInWindow(object sender, RoutedEventArgs e)
    {
        new ColorTextWindow().Activate();
    }
    /*
    // 获取线性渐变画笔在指定位置的颜色
    public static Color GetColorAtPosition(LinearGradientBrush brush, double position)
    {
        if (brush == null || brush.GradientStops.Count == 0)
            return Colors.Transparent;

        // 确保位置在 [0,1] 范围内
        position = Math.Clamp(position, 0.0, 1.0);

        var gradientStops = brush.GradientStops.OrderBy(g => g.Offset).ToList();

        // 如果位置小于第一个偏移量，返回第一个颜色
        if (position <= gradientStops[0].Offset)
            return gradientStops[0].Color;

        // 如果位置大于最后一个偏移量，返回最后一个颜色
        if (position >= gradientStops.Last().Offset)
            return gradientStops.Last().Color;

        // 找到位置所在的两个渐变点
        Microsoft.UI.Xaml.Media.GradientStop lowerStop = null;
        Microsoft.UI.Xaml.Media.GradientStop upperStop = null;

        for (int i = 0; i < gradientStops.Count - 1; i++)
        {
            if (position >= gradientStops[i].Offset &&
                position <= gradientStops[i + 1].Offset)
            {
                lowerStop = gradientStops[i];
                upperStop = gradientStops[i + 1];
                break;
            }
        }

        if (lowerStop == null || upperStop == null)
            return Colors.Transparent;

        // 计算插值比例
        double range = upperStop.Offset - lowerStop.Offset;
        double ratio = (position - lowerStop.Offset) / range;

        // 对每个颜色通道进行线性插值
        return InterpolateColor(lowerStop.Color, upperStop.Color, ratio);
    }
    // 获取颜色中的线性插值方法
    private static Color InterpolateColor(Color color1, Color color2, double ratio)
    {
        ratio = Math.Clamp(ratio, 0.0, 1.0);

        byte a = (byte)(color1.A + (color2.A - color1.A) * ratio);
        byte r = (byte)(color1.R + (color2.R - color1.R) * ratio);
        byte g = (byte)(color1.G + (color2.G - color1.G) * ratio);
        byte b = (byte)(color1.B + (color2.B - color1.B) * ratio);

        return Color.FromArgb(a, r, g, b);
    }
    */
    // 修改后的方法（线程安全）
    public static Color GetColorAtPosition(List<(Color Color, double Offset)> gradientData, double position)
    {
        if (gradientData == null || gradientData.Count == 0) return Color.FromArgb(255, 255, 255, 255);

        // 边界处理
        if (position <= gradientData[0].Offset) return gradientData[0].Color;
        if (position >= gradientData.Last().Offset) return gradientData.Last().Color;

        // 插值计算中间颜色（纯数据操作，无UI对象）
        for (int i = 0; i < gradientData.Count - 1; i++)
        {
            var current = gradientData[i];
            var next = gradientData[i + 1];
            if (position >= current.Offset && position <= next.Offset)
            {
                double ratio = (position - current.Offset) / (next.Offset - current.Offset);
                byte r = (byte)(current.Color.R + ratio * (next.Color.R - current.Color.R));
                byte g = (byte)(current.Color.G + ratio * (next.Color.G - current.Color.G));
                byte b = (byte)(current.Color.B + ratio * (next.Color.B - current.Color.B));
                byte a = (byte)(current.Color.A + ratio * (next.Color.A - current.Color.A));
                return Color.FromArgb(a, r, g, b);
            }
        }
        return Color.FromArgb(255,255,255,255);
    }
    private void SaveThemesData()
    {
        // 用LINQ投影直接生成List<string>，处理Themes为null的情况
        List<string> names = Themes?.Select(ct => ct.name).ToList() ?? [];
        foreach (ColorTheme ct in Themes ?? Enumerable.Empty<ColorTheme>())
            SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: ct.name, itemData: ct);
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: "Themes", itemData: names);
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: "currentTheme", itemData: currentTheme.name);
    }

    public static void ApplyGradientToButton(Button button, ColorTheme theme)
    {
        if (button == null || theme == null)
            return;

        try
        {
            // 创建LinearGradientBrush
            var gradientBrush = new LinearGradientBrush();

            // 添加渐变停止点
            foreach (var stop in theme.Stops.OrderBy(s => s.Offset))
            {
                gradientBrush.GradientStops.Add(new Microsoft.UI.Xaml.Media.GradientStop
                {
                    Color = stop.Color,
                    Offset = stop.Offset
                });
            }

            // 应用到按钮
            button.Background = gradientBrush;
            button.Content = theme.name;
            Debug.WriteLine($"已将渐变主题 '{theme.name}' 应用到按钮");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"应用渐变失败: {ex.Message}");
        }
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessOutput.ShowError = false;
        ProcessOutput.Value = 100;
        // 内部将sender转换为Button（确保类型安全）
        Button btn = sender as Button;
        if (btn == null) return; // 防止非Button类型调用
        if (currentTheme == null || string.IsNullOrEmpty(InputBox.Text)){ return; }
        if (InputBox.Text.Length > 100000)
        {
            ContentDialog InvaildReadyTime_TooBig = new()
            {
                XamlRoot = this.XamlRoot,
                Title = "字数警告！",
                Content = $"当前字数（{InputBox.Text.Length}字）已超十万字！\n将其生成文本写入剪切板和从剪切板中读取生成的字会耗费极其多的资源，可能导致系统卡死！\n\n您仍要生成吗？",
                PrimaryButtonText = "我知道我在做什么！",
                CloseButtonText = "跑路了兄弟！"
            };
            if (await InvaildReadyTime_TooBig.ShowAsync() != ContentDialogResult.Primary) return;
        }
        GenerateButton.IsEnabled = false;
        var gradientData = new List<(Color Color, double Offset)>();
        foreach (var stop in currentTheme.Stops.OrderBy(s => s.Offset))
        {
            gradientData.Add((stop.Color, stop.Offset)); // 提取颜色和偏移，转成元组
        }
        try
        {
            string inputText = InputBox.Text;
            int l = inputText.Length;
            int r = (int)Math.Ceiling(((double)inputText.Length) / 75);
            byte a = 0;
            IColorFormatter colorFormatter = FormatChoose.SelectedIndex switch
            {
                0 => new HexArgbColorFormatter(),
                1 => new HexRgbColorFormatter(),
                2 => new MCRgbColorFormatter(),
                _ => new HexArgbColorFormatter(),
            };
            string output = await Task.Run(() =>
            {
                StringBuilder sb = new(); // 替换+=，提升性能
                int i = 0;
                foreach (char c in inputText)
                {
                    if (i % r == 0)
                    {
                        // 3. 进度更新需要回到UI线程（通过Progress<T>）
                        btn.DispatcherQueue.TryEnqueue(() =>
                        {
                            ProcessOutput.Value = a;
                            StatusTextBlock.Text = $"生成中...（{i}/{l}）";
                        });
                        a++;
                    }
                    sb.Append(colorFormatter.Format(GetColorAtPosition(gradientData, (double)i / l),c));
                    i++;
                }
                return sb.ToString();
            });
            ProcessOutput.Value = 75;
            StatusTextBlock.Text = "写入剪切板...";
            var package = new DataPackage();
            package.SetText(output);
            Clipboard.SetContent(package);
            Clipboard.Flush();
            StatusTextBlock.Text = $"已生成至剪切板！（{InputBox.Text.Length}字->{output.Length}字）";
        }
        catch (Exception ex)
        {
            // 异常处理
            StatusTextBlock.Text = $"生成失败：{ex.Message}";
            GenerateButton.IsEnabled = true;
            ProcessOutput.ShowError = true;
        }
        finally
        {
            // 恢复按钮状态
            GenerateButton.IsEnabled = true;
            ProcessOutput.Value = 100;
        }
    }

    private void ThemeChoose_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeChoose.SelectedItem != null)
        {
            currentTheme = Themes[ThemeChoose.SelectedIndex];
            ApplyGradientToButton(PreviewButton, currentTheme);
            SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: "currentTheme", itemData: currentTheme.name);
        }
    }

    private void AddColorTheme(object sender, RoutedEventArgs e)
    {
        string newName = "新主题";
        int i = 0;
        // 先把Themes转为非null集合，避免每次Any都判断null（减少重复计算）
        var themes = Themes ?? Enumerable.Empty<ColorTheme>();
        while (themes?.Any(ct => ct.name == newName) ?? false)
        { 
            i++;
            newName = $"新主题{i}";
        }
        ColorTheme newTheme = new()
        {
            name = newName,
            Stops =
            [
                new() { Color = Color.FromArgb(255,238,119,0), Offset = 0.0 },
                new() { Color = Color.FromArgb(255,0,187,238), Offset = 1.0 }
            ]
        };
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: newTheme.name, itemData: newTheme);
        Themes.Insert(ThemeChoose.SelectedIndex+1,newTheme);
        currentTheme= newTheme;
        SaveThemesData();
        ThemeChoose.SelectedIndex += 1;
        ApplyGradientToButton(PreviewButton, currentTheme);
        FlyoutBase.ShowAttachedFlyout(SetThemeFlyoutBasePanel);
    }

    private void EditColorTheme(object sender, RoutedEventArgs e)
    {
        if (ThemeChoose.SelectedItem != null)
        FlyoutBase.ShowAttachedFlyout(SetThemeFlyoutBasePanel);
    }

    private void DeleteColorTheme(object sender, RoutedEventArgs e)
    {
        if (ThemeChoose.SelectedItem != null && Themes.Count >= 2) 
        {
            Themes.RemoveAt(ThemeChoose.SelectedIndex);
            SettingsStore.RemoveSetting(fileLocation:"./ColorThemes.json", itemName:currentTheme.name);
            SaveThemesData();
        }
    }

    private void InitialEditPanel(object sender, object e)
    {
        ThemeEditor.ItemsSource = currentTheme.Stops;
        ThemeNameBox.Text = currentTheme.name;
    }

    private void ChangeThemeName(object sender, RoutedEventArgs e)
    {
        if (ThemeNameBox.Text == "" || ThemeNameBox.Text == "currentTheme" || ThemeNameBox.Text == "Themes" 
            || ThemeNameBox.Text == "Format" || (Themes?.Any(ct => ct.name == ThemeNameBox.Text) ?? false))
        {
            ThemeNameBox.Text = currentTheme.name;
            return;
        }
        int p = Themes.IndexOf(currentTheme);
        Themes.RemoveAt(p);
        SettingsStore.RemoveSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name);
        currentTheme.name = ThemeNameBox.Text;
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name, itemData: currentTheme);
        Themes.Insert(p, currentTheme);
        SaveThemesData();
        ThemeChoose.SelectedIndex = p;
    }

    private void DeleteColor(object sender, RoutedEventArgs e)
    {
        if (ThemeEditor.SelectedItem == null||currentTheme.Stops.Count <=2) return;
        int p = ThemeEditor.SelectedIndex;
        currentTheme.Stops.RemoveAt(ThemeEditor.SelectedIndex);
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name, itemData: currentTheme);
        ThemeEditor.ItemsSource = null;
        ThemeEditor.ItemsSource = currentTheme.Stops;
        ThemeEditor.SelectedIndex = p - 1;
        p = ThemeChoose.SelectedIndex;
        ThemeChoose.ItemsSource = null;
        ThemeChoose.ItemsSource = Themes;
        ThemeChoose.SelectedIndex = p;
    }

    private void EditColor(object sender, RoutedEventArgs e)
    {
        if (ThemeEditor.SelectedItem != null)
            FlyoutBase.ShowAttachedFlyout(SetColorFlyoutBasePanel);
    }

    private void AddColor(object sender, RoutedEventArgs e)
    {
        int p = ThemeEditor.SelectedIndex;
        currentTheme.Stops.Insert(ThemeEditor.SelectedIndex+1, new() { Color = Color.FromArgb(255, 0, 187, 238), Offset = 1.0 });
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name, itemData: currentTheme);
        ThemeEditor.ItemsSource = null;
        ThemeEditor.ItemsSource = currentTheme.Stops;
        ThemeEditor.SelectedIndex = p + 1;
        FlyoutBase.ShowAttachedFlyout(SetColorFlyoutBasePanel);
        p = ThemeChoose.SelectedIndex;
        ThemeChoose.ItemsSource = null;
        ThemeChoose.ItemsSource = Themes;
        ThemeChoose.SelectedIndex = p;
    }

    private void InitialEditColorPanel(object sender, object e)
    {
        StopColorEditor.Color = currentTheme.Stops[ThemeEditor.SelectedIndex].Color;
        StopValueEditor.Value = currentTheme.Stops[ThemeEditor.SelectedIndex].Offset;
        StopValueEditor.Foreground = new SolidColorBrush(currentTheme.Stops[ThemeEditor.SelectedIndex].Color);
        StopValueEditor.Background = StopValueEditor.Foreground;
    }

    private void ChangeColor(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (ThemeEditor.SelectedItem == null) return;
        int p = ThemeEditor.SelectedIndex;
        currentTheme.Stops[ThemeEditor.SelectedIndex].Color = args.NewColor;
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name, itemData: currentTheme);
        ThemeEditor.ItemsSource = null;
        ThemeEditor.ItemsSource = currentTheme.Stops;
        ThemeEditor.SelectedIndex = p;
        FlyoutBase.ShowAttachedFlyout(SetColorFlyoutBasePanel);
        p = ThemeChoose.SelectedIndex;
        ThemeChoose.ItemsSource = null;
        ThemeChoose.ItemsSource = Themes;
        ThemeChoose.SelectedIndex = p;
    }

    private void ChangeValue(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (ThemeEditor.SelectedItem == null) return;
        int p = ThemeEditor.SelectedIndex;
        currentTheme.Stops[ThemeEditor.SelectedIndex].Offset = e.NewValue;
        SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: currentTheme.name, itemData: currentTheme);
        ThemeEditor.ItemsSource = null;
        ThemeEditor.ItemsSource = currentTheme.Stops;
        ThemeEditor.SelectedIndex = p;
        FlyoutBase.ShowAttachedFlyout(SetColorFlyoutBasePanel);
        p = ThemeChoose.SelectedIndex;
        ThemeChoose.ItemsSource = null;
        ThemeChoose.ItemsSource = Themes;
        ThemeChoose.SelectedIndex = p;
    }

    private void ChangeFormat(object sender, SelectionChangedEventArgs e)
    {
        if (FormatChoose.SelectedIndex!=-1)SettingsStore.WriteSetting(fileLocation: "./ColorThemes.json", itemName: "Format", itemData: FormatChoose.SelectedIndex);
    }
}