using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Tests.TestData;

/// <summary>
/// Creates realistic large PDF files consisting of high-resolution
/// raster images embedded into PDF pages.
/// 
/// Unlike SyntheticPdfGenerator, this class creates a structurally
/// realistic PDF workload suitable for testing large image-heavy PDFs.
/// </summary>
public static class RealImagePdfGenerator
{
    /// <summary>
    /// Creates a PDF containing one high-resolution random image per page.
    /// </summary>
    /// <param name="filePath">Output PDF path.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="pageCount">Number of PDF pages.</param>
    /// <param name="jpegQuality">JPEG quality from 1 to 100.</param>
    /// <param name="seed">Base random seed.</param>
    public static void Create(
        string filePath,
        int width,
        int height,
        int pageCount,
        int jpegQuality = 90,
        int seed = 123456789)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));

        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));

        if (jpegQuality is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(jpegQuality));

        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var document = new PdfDocument();

        document.Info.Title = "Large Real Image PDF";
        document.Info.Author = "PDFMerger.Tests";
        document.Info.Subject = "Large PDF stress test";
        document.Info.Creator = "RealImagePdfGenerator";

        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "PDFMerger-RealImagePdfGenerator",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            for (int pageNumber = 1;
                 pageNumber <= pageCount;
                 pageNumber++)
            {
                string imagePath = Path.Combine(
                    temporaryDirectory,
                    $"page-{pageNumber:D5}.jpg");

                try
                {
                    CreateRandomJpeg(
                        imagePath,
                        width,
                        height,
                        pageNumber,
                        jpegQuality,
                        seed);

                    AddImagePage(
                        document,
                        imagePath);
                }
                finally
                {
                    TryDelete(imagePath);
                }
            }

            document.Save(filePath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    private static void CreateRandomJpeg(
        string filePath,
        int width,
        int height,
        int pageNumber,
        int jpegQuality,
        int seed)
    {
        // Every page gets a deterministic but different sequence.
        int pageSeed = HashCode.Combine(
            seed,
            pageNumber);

        var random = new Random(pageSeed);

        using var image = new Image<Rgb24>(
            width,
            height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                Span<Rgb24> row =
                    accessor.GetRowSpan(y);

                for (int x = 0; x < width; x++)
                {
                    row[x] = new Rgb24(
                        (byte)random.Next(256),
                        (byte)random.Next(256),
                        (byte)random.Next(256));
                }
            }
        });

        image.Save(
            filePath,
            new JpegEncoder
            {
                Quality = jpegQuality
            });
    }

    private static void AddImagePage(
        PdfDocument document,
        string imagePath)
    {
        using var image = XImage.FromFile(imagePath);

        var page = document.AddPage();

        // A4 landscape.
        page.Width = XUnit.FromMillimeter(297);
        page.Height = XUnit.FromMillimeter(210);

        using var graphics =
            XGraphics.FromPdfPage(page);

        const double margin = 10;

        double availableWidth =
            page.Width.Point - margin * 2;

        double availableHeight =
            page.Height.Point - margin * 2;

        double scale = Math.Min(
            availableWidth / image.PixelWidth,
            availableHeight / image.PixelHeight);

        double drawWidth =
            image.PixelWidth * scale;

        double drawHeight =
            image.PixelHeight * scale;

        double x =
            (page.Width.Point - drawWidth) / 2;

        double y =
            (page.Height.Point - drawHeight) / 2;

        graphics.DrawImage(
            image,
            x,
            y,
            drawWidth,
            drawHeight);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Test cleanup should not hide the original test failure.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(
                    path,
                    recursive: true);
            }
        }
        catch
        {
            // Test cleanup should not hide the original test failure.
        }
    }
}
