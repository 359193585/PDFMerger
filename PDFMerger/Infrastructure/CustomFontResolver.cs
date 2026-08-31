//CustomFontResolver.cs
using System;
using System.IO;
using Avalonia.Platform;
using PdfSharp.Fonts;

namespace PDFMerger.Infrastructure;

public class CustomFontResolver : IFontResolver
{
    private const string localFontFilename = "NotoSans-SemiBold.ttf";
    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // here simplify processing: for Helvetica requests, always return the same font file
        if (string.Equals(familyName, "Helvetica", StringComparison.OrdinalIgnoreCase))
        {
            return new FontResolverInfo("NotoSans");
        }
        return null;
    }

    public byte[]? GetFont(string faceName)
    {
        if (faceName != "NotoSans") return null;

        try
        {
            var uri = new Uri($"avares://PDFMerger/Assets/{localFontFilename}");
            using var stream = AssetLoader.Open(uri);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch
        {
            string fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", localFontFilename);
            if (File.Exists(fontPath))
                return File.ReadAllBytes(fontPath);
        }

        return null;
    }
}
public static class PdfSharpInitializer
{
    public static void Initialize()
    {
        GlobalFontSettings.FontResolver = new CustomFontResolver();
    }
}
