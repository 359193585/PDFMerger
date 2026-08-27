using System.Collections.Generic;
using System.Linq;

namespace PDFMerger.Models
{
    public static class FileExtensions
    {
        public static readonly HashSet<string> PdfExtensions = new()
        {
            ".pdf"
        };

        public static readonly HashSet<string> ImageExtensions = new()
        {
            // Common raster formats
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp",
            // Modern image formats
            ".avif", ".heif", ".heic", ".jxl",
            // Other raster/image container formats
            ".ico", ".tga", ".psd", ".qoi",
            // Vector image
            ".svg"
        };

        public static string[] PdfPatterns => PdfExtensions.Select(ext => $"*{ext}").ToArray();
        public static string[] ImagePatterns => ImageExtensions.Select(ext => $"*{ext}").ToArray();

    }
    public enum FileType
    {
        Pdf,
        Image
    }
}
