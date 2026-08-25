using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PDFMerger.Infrastructure;

public static class I18n
{
    private static Dictionary<string, string>? _resources;
    public static void Initialize(string cultureName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "I18n", $"{cultureName}.json");
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "I18n", "en-US.json");
        var json = File.ReadAllText(path);
        _resources = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }
    public static string GetString(string key)
    {
        if (_resources is null)
        {
            throw new InvalidOperationException("I18n resources not initialized. Call Initialize() first.");
        }
        return _resources.TryGetValue(key, out var value) ? value : key;
    }
}
