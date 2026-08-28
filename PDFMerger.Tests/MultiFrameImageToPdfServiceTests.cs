using PDFMerger.Services;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Tests.Services;
public sealed class MultiFrameImageToPdfServiceTests
{
    [Fact]
    public void GetFrameCount_NullImage_ThrowsArgumentNullException()
    {
        var service = new MultiFrameImageToPdfService();

        Assert.Throws<ArgumentNullException>(() =>
            service.GetFrameCount(null!));
    }

    [Fact]
    public void GetFrameCount_SingleFrameImage_ReturnsOne()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);

        int frameCount = service.GetFrameCount(image);

        Assert.Equal(1, frameCount);
    }

    [Fact]
    public void GetFrameCount_MultiFrameImage_ReturnsCorrectCount()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);

        image.Frames.AddFrame(image.Frames.RootFrame);
        image.Frames.AddFrame(image.Frames.RootFrame);

        int frameCount = service.GetFrameCount(image);

        Assert.Equal(3, frameCount);
    }

    [Fact]
    public void ConvertImageToPngStream_NullImage_ThrowsArgumentNullException()
    {
        var service = new MultiFrameImageToPdfService();

        Assert.Throws<ArgumentNullException>(() =>
            service.ConvertImageToPngStream(null!));
    }

    [Fact]
    public void ConvertImageToPngStream_ReturnsStreamPositionedAtBeginning()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 80);

        using var stream = service.ConvertImageToPngStream(image);

        Assert.Equal(0, stream.Position);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void ConvertImageToPngStream_ReturnsValidPngData()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 80);

        using var stream = service.ConvertImageToPngStream(image);

        Assert.Equal(0x89, stream.ReadByte());
        Assert.Equal((byte)'P', (byte)stream.ReadByte());
        Assert.Equal((byte)'N', (byte)stream.ReadByte());
        Assert.Equal((byte)'G', (byte)stream.ReadByte());
    }


    [Fact]
    public void AddImagePagesToDocument_SingleFrameImage_AddsOnePage()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);
        using var document = new PdfDocument();

        service.AddImagePagesToDocument(
            image,
            document,
            595,
            842);

        Assert.Single(document.Pages);
    }

    [Fact]
    public void AddImagePagesToDocument_MultiFrameImage_AddsOnePagePerFrame()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);

        image.Frames.AddFrame(image.Frames.RootFrame);
        image.Frames.AddFrame(image.Frames.RootFrame);

        using var document = new PdfDocument();

        service.AddImagePagesToDocument(
            image,
            document,
            595,
            842);

        Assert.Equal(3, document.Pages.Count);
    }

    [Fact]
    public void AddImagePagesToDocument_NullImage_ThrowsArgumentNullException()
    {
        var service = new MultiFrameImageToPdfService();

        using var document = new PdfDocument();

        Assert.Throws<ArgumentNullException>(() =>
            service.AddImagePagesToDocument(
                null!,
                document,
                595,
                842));
    }

    [Fact]
    public void AddImagePagesToDocument_NullDocument_ThrowsArgumentNullException()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);

        Assert.Throws<ArgumentNullException>(() =>
            service.AddImagePagesToDocument(
                image,
                null!,
                595,
                842));
    }

    [Fact]
    public void AddImagePagesToDocument_PreservesExistingPages()
    {
        var service = new MultiFrameImageToPdfService();

        using var image = new Image<Rgba32>(100, 100);

        image.Frames.AddFrame(image.Frames.RootFrame);

        using var document = new PdfDocument();

        // Existing page
        document.AddPage();

        service.AddImagePagesToDocument(
            image,
            document,
            595,
            842);

        Assert.Equal(3, document.Pages.Count);
    }
}
