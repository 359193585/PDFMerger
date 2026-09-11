using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace PDFMerger.Infrastructure;

public static class I18n
{
    private static Dictionary<string, string>? _resources;
    private static readonly Dictionary<string, string> _fallback = new(StringComparer.Ordinal);
    public static void Initialize(string cultureName)
    {
        _resources = LoadFromDisk(cultureName);
        _resources ??= LoadFromDisk("en-US");
        _resources ??= LoadEmbedded("en-US");
        _resources ??= new Dictionary<string, string>(StringComparer.Ordinal);
        var fallbackDict = LoadFromDisk("en-US") ?? LoadEmbedded("en-US");
        if (fallbackDict is not null)
        {
            foreach (var kv in fallbackDict)
                _fallback[kv.Key] = kv.Value;
        }
    }
    private static Dictionary<string, string>? LoadFromDisk(string cultureName)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "I18n", $"{cultureName}.json");
            if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "I18n", "en-US.json");
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null; 
        }
    }
    public static string GetString(string key)
    {
        if (_resources is null)
        {
            throw new InvalidOperationException("I18n resources not initialized. Call Initialize() first.");
        }
        if (_resources.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
            return v;

        if (_fallback.TryGetValue(key, out var fb) && !string.IsNullOrEmpty(fb))
            return fb;

        return key;
    }
    private static Dictionary<string, string>? LoadEmbedded(string cultureName)
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = $"I18n.{cultureName}.json";   
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null) return null;
            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        }
        catch
        {
            return null;
        }
    }

}
