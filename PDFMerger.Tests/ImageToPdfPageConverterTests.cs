using PDFMerger.Services;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Tests.Services;

public class ImageToPdfPageConverterTests
{
    #region ConvertImageToPdfDocument - File

    [Fact]
    public void ConvertImageToPdfDocument_FromFile_CreatesOnePage()
    {
        var filePath = CreatePngFile(800, 600);

        try
        {
            var converter = new ImageToPdfPageConverter();

            using var document =
                converter.ConvertImageToPdfDocument(filePath);

            Assert.Single(document.Pages);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ConvertImageToPdfDocument_FromMissingFile_ThrowsFileNotFoundException()
    {
        var filePath =
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        var converter = new ImageToPdfPageConverter();

        var exception = Assert.Throws<FileNotFoundException>(() =>
            converter.ConvertImageToPdfDocument(filePath));

        Assert.Equal(filePath, exception.FileName);
    }

    #endregion


    #region ConvertImageToPdfDocument - byte[]

    [Fact]
    public void ConvertImageToPdfDocument_FromBytes_CreatesOnePage()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(imageData);

        Assert.Single(document.Pages);
    }

    [Fact]
    public void ConvertImageToPdfDocument_FromNullBytes_ThrowsArgumentException()
    {
        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument((byte[])null!));
    }

    [Fact]
    public void ConvertImageToPdfDocument_FromEmptyBytes_ThrowsArgumentException()
    {
        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument(Array.Empty<byte>()));
    }

    #endregion


    #region ConvertImageToPdfDocument - Stream

    [Fact]
    public void ConvertImageToPdfDocument_FromStream_CreatesOnePage()
    {
        var imageData = CreatePngBytes(800, 600);

        using var stream =
            new MemoryStream(imageData);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(stream);

        Assert.Single(document.Pages);
    }

    [Fact]
    public void ConvertImageToPdfDocument_FromNullStream_ThrowsArgumentException()
    {
        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument((Stream)null!));
    }

    [Fact]
    public void ConvertImageToPdfDocument_FromUnreadableStream_ThrowsArgumentException()
    {
        var converter = new ImageToPdfPageConverter();

        using var stream = new NonReadableStream();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument(stream));
    }

    #endregion


    #region AddImagePageToDocument

    [Fact]
    public void AddImagePageToDocument_AddsOnePage()
    {
        var filePath = CreatePngFile(800, 600);

        try
        {
            using var document = new PdfDocument();

            var converter = new ImageToPdfPageConverter();

            var addedPages =
                converter.AddImagePageToDocument(
                    filePath,
                    document);

            Assert.Equal(1, addedPages);
            Assert.Single(document.Pages);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void AddImagePageToDocument_AddsPageToExistingDocument()
    {
        var filePath = CreatePngFile(800, 600);

        try
        {
            using var document = new PdfDocument();

            document.AddPage();

            var converter = new ImageToPdfPageConverter();

            var addedPages =
                converter.AddImagePageToDocument(
                    filePath,
                    document);

            Assert.Equal(1, addedPages);
            Assert.Equal(2, document.PageCount);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void AddImagePageToDocument_WithNullDocument_ThrowsArgumentNullException()
    {
        var filePath = CreatePngFile(800, 600);

        try
        {
            var converter = new ImageToPdfPageConverter();

            Assert.Throws<ArgumentNullException>(() =>
                converter.AddImagePageToDocument(
                    filePath,
                    null!));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void AddImagePageToDocument_WithMissingFile_ThrowsFileNotFoundException()
    {
        var filePath =
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");

        using var document = new PdfDocument();

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<FileNotFoundException>(() =>
            converter.AddImagePageToDocument(
                filePath,
                document));
    }

    #endregion


    #region AddImageStreamToDocument

    [Fact]
    public void AddImageStreamToDocument_AddsOnePage()
    {
        var imageData = CreatePngBytes(800, 600);

        using var stream = new MemoryStream(imageData);
        using var document = new PdfDocument();

        var converter = new ImageToPdfPageConverter();

        var addedPages =
            converter.AddImageStreamToDocument(
                stream,
                document,
                ImageToPdfPageConverter.PageSizeMode.FitImage);

        Assert.Equal(1, addedPages);
        Assert.Single(document.Pages);
    }

    [Fact]
    public void AddImageStreamToDocument_WithNullDocument_ThrowsArgumentNullException()
    {
        var imageData = CreatePngBytes(800, 600);

        using var stream = new MemoryStream(imageData);

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentNullException>(() =>
            converter.AddImageStreamToDocument(
                stream,
                null!,
                ImageToPdfPageConverter.PageSizeMode.FitImage));
    }

    [Fact]
    public void AddImageStreamToDocument_WithNullStream_ThrowsArgumentException()
    {
        using var document = new PdfDocument();

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.AddImageStreamToDocument(
                null!,
                document,
                ImageToPdfPageConverter.PageSizeMode.FitImage));
    }

    [Fact]
    public void AddImageStreamToDocument_WithUnreadableStream_ThrowsArgumentException()
    {
        using var document = new PdfDocument();

        using var stream = new NonReadableStream();

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.AddImageStreamToDocument(
                stream,
                document,
                ImageToPdfPageConverter.PageSizeMode.FitImage));
    }

    #endregion


    #region PageSizeMode

    [Fact]
    public void FitImage_UsesImagePhysicalSize()
    {
        var imageData =
            CreatePngBytes(
                width: 960,
                height: 480,
                horizontalDpi: 96,
                verticalDpi: 96);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.FitImage);

        var page = document.Pages[0];

        // 960 / 96 * 72 = 720
        // 480 / 96 * 72 = 360
        Assert.Equal(720, page.Width.Point, 0.1);
        Assert.Equal(360, page.Height.Point, 0.1);
    }

    [Fact]
    public void A4_CreatesA4Page()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.A4);

        var page = document.Pages[0];

        Assert.Equal(595, page.Width.Point, 3);
        Assert.Equal(842, page.Height.Point, 3);
    }

    [Fact]
    public void Custom_CreatesSpecifiedPageSize()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: 400,
                customHeight: 300);

        var page = document.Pages[0];

        Assert.Equal(400, page.Width.Point, 3);
        Assert.Equal(300, page.Height.Point, 3);
    }

    #endregion


    #region Custom Page Size Validation

    [Fact]
    public void Custom_WithoutWidth_ThrowsArgumentException()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: null,
                customHeight: 300));
    }

    [Fact]
    public void Custom_WithoutHeight_ThrowsArgumentException()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentException>(() =>
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: 400,
                customHeight: null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Custom_WithInvalidWidth_ThrowsArgumentOutOfRangeException(
        double width)
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: width,
                customHeight: 300));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Custom_WithInvalidHeight_ThrowsArgumentOutOfRangeException(
        double height)
    {
        var imageData = CreatePngBytes(800, 600);

        var converter = new ImageToPdfPageConverter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: 400,
                customHeight: height));
    }

    #endregion


    #region Default Settings

    [Fact]
    public void DefaultMode_IsFitImage()
    {
        var imageData = CreatePngBytes(960, 480, 96, 96);
        var converter = new ImageToPdfPageConverter();
        using var document = converter.ConvertImageToPdfDocument(imageData);
        var page = document.Pages[0];

        Assert.Equal(720, page.Width.Point, 0.1);
        Assert.Equal(360, page.Height.Point, 0.1);
    }

    [Fact]
    public void Constructor_DefaultMode_IsUsed()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter =
            new ImageToPdfPageConverter(
                ImageToPdfPageConverter.PageSizeMode.A4);

        using var document =
            converter.ConvertImageToPdfDocument(imageData);

        var page = document.Pages[0];

        Assert.Equal(595, page.Width.Point, 3);
        Assert.Equal(842, page.Height.Point, 3);
    }

    [Fact]
    public void ExplicitMode_OverridesDefaultMode()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter =
            new ImageToPdfPageConverter(
                ImageToPdfPageConverter.PageSizeMode.A4);

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.Custom,
                customWidth: 400,
                customHeight: 300);

        var page = document.Pages[0];

        Assert.Equal(400, page.Width.Point, 3);
        Assert.Equal(300, page.Height.Point, 3);
    }

    [Fact]
    public void DefaultCustomPageSize_IsUsed()
    {
        var imageData = CreatePngBytes(800, 600);

        var converter =
            new ImageToPdfPageConverter(
                ImageToPdfPageConverter.PageSizeMode.Custom,
                defaultCustomWidth: 500,
                defaultCustomHeight: 700);

        using var document =
            converter.ConvertImageToPdfDocument(imageData);

        var page = document.Pages[0];

        Assert.Equal(500, page.Width.Point, 3);
        Assert.Equal(700, page.Height.Point, 3);
    }

    #endregion


    #region DPI

    [Fact]
    public void FitImage_WithDifferentHorizontalAndVerticalDpi_UsesBothDpiValues()
    {
        var imageData =
            CreatePngBytes(
                width: 1200,
                height: 600,
                horizontalDpi: 120,
                verticalDpi: 60);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.FitImage);

        var page = document.Pages[0];

        // 1200 / 120 * 72 = 720
        // 600 / 60 * 72 = 720
        Assert.Equal(720, page.Width.Point, 0.1);
        Assert.Equal(720, page.Height.Point, 0.1);
    }

    #endregion


    #region Aspect Ratio

    [Fact]
    public void A4_WideImage_PreservesAspectRatio()
    {
        var imageData = CreatePngBytes(1600, 800);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.A4);

        var page = document.Pages[0];

        Assert.Equal(595, page.Width.Point, 3);
        Assert.Equal(842, page.Height.Point, 3);
    }

    [Fact]
    public void A4_TallImage_PreservesAspectRatio()
    {
        var imageData = CreatePngBytes(800, 1600);

        var converter = new ImageToPdfPageConverter();

        using var document =
            converter.ConvertImageToPdfDocument(
                imageData,
                ImageToPdfPageConverter.PageSizeMode.A4);

        var page = document.Pages[0];

        Assert.Equal(595, page.Width.Point, 3);
        Assert.Equal(842, page.Height.Point, 3);
    }

    #endregion


    #region Multiple Images

    [Fact]
    public void AddImageStreamToDocument_CanAddMultipleImages()
    {
        var imageData1 = CreatePngBytes(800, 600);
        var imageData2 = CreatePngBytes(600, 800);

        using var stream1 = new MemoryStream(imageData1);
        using var stream2 = new MemoryStream(imageData2);
        using var document = new PdfDocument();

        var converter = new ImageToPdfPageConverter();

        var first =
            converter.AddImageStreamToDocument(
                stream1,
                document,
                ImageToPdfPageConverter.PageSizeMode.A4);

        var second =
            converter.AddImageStreamToDocument(
                stream2,
                document,
                ImageToPdfPageConverter.PageSizeMode.A4);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(2, document.PageCount);
    }

    #endregion


    #region Helpers 

    private static byte[] CreatePngBytes(
        int width,
        int height,
        double horizontalDpi = 96,
        double verticalDpi = 96)
    {
        using var image = new Image<Rgba32>(width, height);

        image.Metadata.HorizontalResolution = horizontalDpi;

        image.Metadata.VerticalResolution = verticalDpi;

        using var stream = new MemoryStream();

        image.Save(stream, new PngEncoder());

        return stream.ToArray();
    }

    private static string CreatePngFile(int width, int height)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
        File.WriteAllBytes(path, CreatePngBytes(width, height));
        return path;
    }

    private sealed class NonReadableStream : MemoryStream
    {
        public override bool CanRead => false;
    }

    #endregion
}
