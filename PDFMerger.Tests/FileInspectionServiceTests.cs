using PDFMerger.Models;
using PDFMerger.Services;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Tests.Services;

public sealed class FileInspectionServiceTests : IDisposable
{
    private readonly FileInspectionService _service;
    private readonly string _testDirectory;

    public FileInspectionServiceTests()
    {
        _service = new FileInspectionService();

        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "PDFMergerTests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Fact]
    public void Inspect_NullPath_ReturnsInvalidPath()
    {
        var result = _service.Inspect(null!);

        Assert.False(result.IsSupported);
        Assert.Equal("InvalidPath", result.ErrorCode);
    }

    [Fact]
    public void Inspect_EmptyPath_ReturnsInvalidPath()
    {
        var result = _service.Inspect(string.Empty);

        Assert.False(result.IsSupported);
        Assert.Equal("InvalidPath", result.ErrorCode);
    }

    [Fact]
    public void Inspect_UnsupportedExtension_ReturnsUnsupportedFormat()
    {
        var filePath = CreateTextFile(
            "test.txt",
            "This is not a supported file.");

        var result = _service.Inspect(filePath);

        Assert.False(result.IsSupported);
        Assert.Equal("UnsupportedFormat", result.ErrorCode);
    }

    [Fact]
    public void Inspect_ValidPdf_ReturnsPdfInformation()
    {
        var filePath = CreatePdfFile(
            "test.pdf",
            pageCount: 3);

        var result = _service.Inspect(filePath);

        Assert.True(result.IsSupported);
        Assert.Equal(FileType.Pdf, result.Type);
        Assert.Equal(3, result.PageCount);

        var expectedSize = new FileInfo(filePath).Length;

        Assert.True(expectedSize > 0);
        Assert.Equal(expectedSize, result.FileSize);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Inspect_PdfExtension_IsCaseInsensitive()
    {
        var filePath = CreatePdfFile(
            "test.PDF",
            pageCount: 2);

        var result = _service.Inspect(filePath);

        Assert.True(result.IsSupported);
        Assert.Equal(FileType.Pdf, result.Type);
        Assert.Equal(2, result.PageCount);
    }

    [Fact]
    public void Inspect_ImageExtension_IsCaseInsensitive()
    {
        var filePath = CreatePngFile("test.PNG");

        var result = _service.Inspect(filePath);

        Assert.True(result.IsSupported);
        Assert.Equal(FileType.Image, result.Type);
        Assert.Equal(1, result.PageCount);

        var expectedSize = new FileInfo(filePath).Length;

        Assert.True(expectedSize > 0);
        Assert.Equal(expectedSize, result.FileSize);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Inspect_PngWithInvalidContent_ReturnsUnsupportedImage()
    {
        // The extension says PNG, but the actual content is not a PNG.
        var filePath = CreateTextFile(
            "invalid.png",
            "This is definitely not a PNG file.");

        var result = _service.Inspect(filePath);

        Assert.False(result.IsSupported);
        Assert.Equal(FileType.Image, result.Type);
        Assert.Equal(
            new FileInfo(filePath).Length,
            result.FileSize);

        Assert.NotNull(result.ErrorCode);
    }

    [Fact]
    public void Inspect_Image_ReturnsFrameCount()
    {
        var filePath = CreatePngFile("single-frame.png");

        var result = _service.Inspect(filePath);

        Assert.True(result.IsSupported);
        Assert.Equal(FileType.Image, result.Type);
        Assert.Equal(1, result.PageCount);
    }

    [Fact]
    public void Inspect_EmptyPdfFile_ReturnsFailure()
    {
        var filePath = CreateTextFile(
            "empty.pdf",
            string.Empty);

        var result = _service.Inspect(filePath);

        Assert.False(result.IsSupported);
    }

    private string CreateTextFile(string fileName, string content)
    {
        var filePath = Path.Combine(_testDirectory, fileName);

        File.WriteAllText(filePath, content);

        return filePath;
    }

    private string CreatePdfFile(string fileName, int pageCount)
    {
        var filePath = Path.Combine(_testDirectory, fileName);

        using var document = new PdfDocument();

        for (var i = 0; i < pageCount; i++)
        {
            document.AddPage();
        }

        document.Save(filePath);

        return filePath;
    }

    private string CreatePngFile(string fileName)
    {
        var filePath = Path.Combine(_testDirectory, fileName);

        using var image = new Image<Rgba32>(10, 10);
        image.SaveAsPng(filePath);

        return filePath;
    }
}
