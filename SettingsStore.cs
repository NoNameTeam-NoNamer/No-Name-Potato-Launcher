using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Windows.Storage;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace No_Namer.SettingsRecord.Helpers
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    // Simple settings store that serializes a dictionary to a JSON file.
    // This avoids manual parsing of INI files and supports storing typed data.
    public static class SettingsStore
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true
        };

        private static string GetDefaultSettingsPath()
        {
            string baseDir;

            // 总是使用 ApplicationData.Current.LocalFolder，这是WinUI3推荐的方式
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                baseDir = localFolder.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get LocalFolder: {ex}");
                // 如果失败，回退到传统的 LocalApplicationData
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = AppContext.BaseDirectory;

            try
            {
                Directory.CreateDirectory(baseDir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create settings directory '{baseDir}': {ex}");
            }

            return Path.Combine(baseDir, "settings.json");
        }

        private static string GetSettingsBaseDirectory()
        {
            var defaultSettingsPath = GetDefaultSettingsPath();
            return Path.GetDirectoryName(defaultSettingsPath);
        }

        private static string ResolvePath(string fileLocation)
        {
            if (string.IsNullOrWhiteSpace(fileLocation) || fileLocation == "./settings.json" || fileLocation == "settings.json")
            {
                var path = GetDefaultSettingsPath();
                Debug.WriteLine($"返回默认路径: {path}");
                return path;
            }

            try
            {
                // 获取settings.json所在的基础目录
                var settingsBaseDir = GetSettingsBaseDirectory();

                // 如果是相对路径（以 ./ 或 ../ 开头或没有根目录），则相对于settings.json所在目录
                if (fileLocation.StartsWith("./") || fileLocation.StartsWith("../") || !Path.IsPathRooted(fileLocation))
                {
                    // 去除开头的 "./" 或 "."（如果有）
                    var relativePath = fileLocation;
                    if (relativePath.StartsWith("./"))
                        relativePath = relativePath[2..];
                    else if (relativePath.StartsWith('.'))
                        relativePath = relativePath[1..];

                    // 将相对路径转换为基于settings.json目录的完整路径
                    var resolved = Path.GetFullPath(Path.Combine(settingsBaseDir, relativePath));

                    Debug.WriteLine($"解析相对路径: '{fileLocation}' -> '{resolved}'");
                    Debug.WriteLine($"基础目录: {settingsBaseDir}");

                    return resolved;
                }

                // 如果是绝对路径，直接使用
                var absolutePath = Path.GetFullPath(fileLocation);
                Debug.WriteLine($"解析绝对路径: '{fileLocation}' -> '{absolutePath}'");
                return absolutePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"路径解析异常: {ex}");
                return GetDefaultSettingsPath();
            }
        }

        private static Dictionary<string, JsonElement> Load(string fileLocation)
        {
            try
            {
                var resolved = ResolvePath(fileLocation);
                Debug.WriteLine($"加载设置从: {resolved}");

                if (!File.Exists(resolved))
                {
                    Debug.WriteLine($"设置文件不存在: {resolved}");
                    return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                }

                var txt = File.ReadAllText(resolved);
                if (string.IsNullOrWhiteSpace(txt))
                    return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

                var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;
                var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.Clone();
                    }
                }
                return dict;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载设置失败: {ex}");
                return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void Save(string fileLocation, Dictionary<string, JsonElement> dict)
        {
            try
            {
                var resolved = ResolvePath(fileLocation);
                Debug.WriteLine($"保存设置到: {resolved}");

                var dir = Path.GetDirectoryName(resolved);
                if (!string.IsNullOrWhiteSpace(dir))
                    Directory.CreateDirectory(dir);

                using var fs = File.Open(resolved, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();
                foreach (var kv in dict)
                {
                    writer.WritePropertyName(kv.Key);
                    kv.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
                writer.Flush();

                Debug.WriteLine($"设置保存成功到: {resolved}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设置失败: {ex}");
            }
        }

        // ReadSetting returns the raw JSON string of the value, or null if not present.
        public static string? ReadSetting(string fileLocation = "./settings.json", string itemName = null)
        {
            if (itemName is null) { return null;}

            var dict = Load(fileLocation);
            if (!dict.TryGetValue(itemName, out var elem))
                return null;

            return elem.GetRawText();
        }

        // ReadSetting<T> will deserialize the stored JSON value back to the requested type T.
        public static T? ReadSetting<T>(string fileLocation = "./settings.json", string itemName = null)
        {
            if (itemName is null) { return default;}

            var dict = Load(fileLocation);
            if (!dict.TryGetValue(itemName, out var elem))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(elem.GetRawText(), jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"反序列化设置失败 '{itemName}': {ex}");
                return default;
            }
        }

        // RemoveSetting removes the specified item from the settings file.
        public static void RemoveSetting(string fileLocation = "./settings.json", string itemName = null)
        {
            ArgumentNullException.ThrowIfNull(itemName);
            try
            {
                var dict = Load(fileLocation);
                Debug.WriteLine($"尝试移除设置项: {itemName}");

                if (dict.Remove(itemName))
                {
                    Debug.WriteLine($"成功移除设置项: {itemName}");
                    Save(fileLocation, dict);
                }
                else
                {
                    Debug.WriteLine($"设置项不存在: {itemName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"移除设置失败 '{itemName}': {ex}");
            }
        }

        // WriteSetting writes the provided object as JSON under the given itemName.
        public static void WriteSetting(string fileLocation = "./settings.json", string itemName = null, object itemData = null)
        {
            ArgumentNullException.ThrowIfNull(itemName);

            var dict = Load(fileLocation);
            JsonElement elem;
            if (itemData == null)
            {
                using var doc = JsonDocument.Parse("null");
                elem = doc.RootElement.Clone();
            }
            else
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(itemData, jsonOptions);
                using var doc = JsonDocument.Parse(bytes);
                elem = doc.RootElement.Clone();
            }

            dict[itemName] = elem;
            Save(fileLocation, dict);
        }

        // 新增：获取所有设置键名
        public static List<string> GetAllSettingKeys(string fileLocation = "./settings.json")
        {
            try
            {
                var dict = Load(fileLocation);
                return [.. dict.Keys];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取所有设置键名失败: {ex}");
                return [];
            }
        }

        // 新增：检查设置项是否存在
        public static bool SettingExists(string fileLocation = "./settings.json", string itemName = null)
        {
            if (itemName is null)
                return false;
            try
            {
                var dict = Load(fileLocation);
                return dict.ContainsKey(itemName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查设置项存在失败: {ex}");
                return false;
            }
        }

        // 新增：清空所有设置
        public static void ClearAllSettings(string fileLocation = "./settings.json")
        {
            try
            {
                Debug.WriteLine($"清空所有设置");
                var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                Save(fileLocation, dict);
                Debug.WriteLine($"所有设置已清空");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清空所有设置失败: {ex}");
            }
        }

        // 调试方法：获取当前 LocalFolder 路径
        public static string GetLocalFolderPath()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                return localFolder.Path;
            }
            catch
            {
                return "无法获取 LocalFolder";
            }
        }

        // 调试方法：打开配置文件所在目录
        public static async void OpenSettingsDirectory(string fileLocation = "./settings.json",bool createWarningFile = false)
        {
            try
            {
                if (createWarningFile) {
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var debugFile = await localFolder.CreateFileAsync("请读我！.txt",
                        CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteTextAsync(debugFile,
                        "请注意！这里是配置文件目录！非专业人员勿动！否则可能导致您的数据丢失或损坏！");

                    Debug.WriteLine($"调试文件已创建: {debugFile.Path}");
                }
                var resolved = ResolvePath(fileLocation);
                Debug.WriteLine($"打开目录 - 完整路径: {resolved}");
                // 如果文件存在，使用 /select 参数选中文件
                // 如果文件不存在，直接打开目录
                if (File.Exists(resolved))
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select, \"{resolved}\""
                    };
                    Process.Start(processInfo);
                    Debug.WriteLine($"已打开目录并选中文件");
                }
                else
                {
                    var dir = Path.GetDirectoryName(resolved);
                    if (Directory.Exists(dir))
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{dir}\""
                        };
                        Process.Start(processInfo);
                        Debug.WriteLine($"已打开目录: {dir}");
                    }
                    else
                    {
                        Debug.WriteLine($"目录不存在: {dir}");
                        // 创建目录并再次尝试
                        Directory.CreateDirectory(dir);
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{dir}\""
                        };
                        Process.Start(processInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"打开目录失败: {ex}");
            }
        }
    }
}