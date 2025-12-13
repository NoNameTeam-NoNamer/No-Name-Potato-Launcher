using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Copilot.SettingsRecord.Helpers
{
    // Simple settings store that serializes a dictionary to a JSON file.
    // This avoids manual parsing of INI files and supports storing typed data.
    public static class SettingsStore
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static Dictionary<string, JsonElement> Load(string fileLocation)
        {
            try
            {
                if (!File.Exists(fileLocation))
                    return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

                var txt = File.ReadAllText(fileLocation);
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
            catch
            {
                return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void Save(string fileLocation, Dictionary<string, JsonElement> dict)
        {
            try
            {
                using var fs = File.Open(fileLocation, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
                writer.WriteStartObject();
                foreach (var kv in dict)
                {
                    writer.WritePropertyName(kv.Key);
                    kv.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
                writer.Flush();
            }
            catch
            {
                // best-effort
            }
        }

        // ReadSetting returns the raw JSON string of the value, or null if not present.
        public static string? ReadSetting(string fileLocation = "./settings.json", string itemName = null)
        {
            if (itemName is null)
                throw new ArgumentNullException(nameof(itemName));

            var dict = Load(fileLocation);
            if (!dict.TryGetValue(itemName, out var elem))
                return null;

            // Return the raw JSON text for the element so callers can parse typed values if needed.
            return elem.GetRawText();
        }

        // ReadSetting<T> will deserialize the stored JSON value back to the requested type T.
        public static T? ReadSetting<T>(string fileLocation = "./settings.json", string itemName = null)
        {
            if (itemName is null)
                throw new ArgumentNullException(nameof(itemName));

            var dict = Load(fileLocation);
            if (!dict.TryGetValue(itemName, out var elem))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(elem.GetRawText(), jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        // WriteSetting writes the provided object as JSON under the given itemName.
        public static void WriteSetting(string fileLocation = "./settings.json", string itemName = null, object itemData = null)
        {
            if (itemName is null)
                throw new ArgumentNullException(nameof(itemName));

            var dict = Load(fileLocation);
            JsonElement elem;
            if (itemData == null)
            {
                // store null
                using var doc = JsonDocument.Parse("null");
                elem = doc.RootElement.Clone();
            }
            else
            {
                // serialize the data to JSON and parse back to JsonElement
                var bytes = JsonSerializer.SerializeToUtf8Bytes(itemData, jsonOptions);
                using var doc = JsonDocument.Parse(bytes);
                elem = doc.RootElement.Clone();
            }

            dict[itemName] = elem;
            Save(fileLocation, dict);
        }
    }
}
