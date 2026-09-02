using PDFMerger.Services;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PDFMerger.Infrastructure;

using static PDFMerger.Services.PdfPageNumberService;

namespace PDFMerger.Tests.Services;

public class PdfPageNumberServiceTests
{
    public PdfPageNumberServiceTests()
    {
        PdfSharpInitializer.Initialize();
    }

    private readonly PdfPageNumberService _service = new();

    [Fact]
    public void AddPageNumbers_NullDocument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _service.AddPageNumbers(null!));
    }

    [Fact]
    public void AddPageNumbers_EmptyDocument_DoesNotThrow()
    {
        using var document = new PdfDocument();

        _service.AddPageNumbers(document);

        Assert.Equal(0, document.PageCount);
    }

    [Fact]
    public void AddPageNumbers_SinglePage_PreservesPageCount()
    {
        using var document = CreateDocument(1);

        _service.AddPageNumbers(document);

        Assert.Equal(1, document.PageCount);
    }

    [Fact]
    public void AddPageNumbers_MultiplePages_PreservesPageCount()
    {
        using var document = CreateDocument(5);

        _service.AddPageNumbers(document);

        Assert.Equal(5, document.PageCount);
    }

    [Fact]
    public void AddPageNumbers_DefaultStyle_CreatesValidPdf()
    {
        using var document = CreateDocument(3);

        _service.AddPageNumbers(document);

        AssertValidPdf(document, 3);
    }

    [Fact]
    public void AddPageNumbers_UnderlineStyle_CreatesValidPdf()
    {
        using var document = CreateDocument(3);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Underline,
            XColors.DarkGray);

        AssertValidPdf(document, 3);
    }

    [Fact]
    public void AddPageNumbers_PillStyle_CreatesValidPdf()
    {
        using var document = CreateDocument(3);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 3);
    }

    [Fact]
    public void AddPageNumbers_AllStyles_CanBeApplied()
    {
        using var document = CreateDocument(3);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Plain,
            XColors.Black);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Underline,
            XColors.DarkGray);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 3);
    }

    [Fact]
    public void AddPageNumbers_CustomColor_CreatesValidPdf()
    {
        using var document = CreateDocument(2);

        XColor color = XColors.Red;

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            color);

        AssertValidPdf(document, 2);
    }

    [Fact]
    public void AddPageNumbers_TinyPages_CreatesValidPdf()
    {
        using var document = CreateDocument(
            pageCount: 3,
            width: 20,
            height: 20);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 3);
    }

    [Fact]
    public void AddPageNumbers_VeryWidePage_CreatesValidPdf()
    {
        using var document = CreateDocument(
            pageCount: 2,
            width: 2000,
            height: 100);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Underline,
            XColors.DarkGray);

        AssertValidPdf(document, 2);
    }

    [Fact]
    public void AddPageNumbers_VeryTallPage_CreatesValidPdf()
    {
        using var document = CreateDocument(
            pageCount: 2,
            width: 100,
            height: 2000);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 2);
    }

    [Fact]
    public void AddPageNumbers_LargePageCount_CreatesValidPdf()
    {
        using var document = CreateDocument(100);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 100);
    }

    [Fact]
    public void AddPageNumbers_CanBeCalledMultipleTimes()
    {
        using var document = CreateDocument(3);

        _service.AddPageNumbers(document);
        _service.AddPageNumbers(
            document,
            PageNumberStyle.Underline,
            XColors.DarkGray);

        _service.AddPageNumbers(
            document,
            PageNumberStyle.Pill,
            XColors.LightGray);

        AssertValidPdf(document, 3);
    }

    private static PdfDocument CreateDocument(
        int pageCount,
        double width = 595.28,
        double height = 841.89)
    {
        var document = new PdfDocument();

        for (int i = 0; i < pageCount; i++)
        {
            PdfPage page = document.AddPage();
            page.Width = XUnit.FromPoint(width);
            page.Height = XUnit.FromPoint(height);
        }

        return document;
    }

    private static void AssertValidPdf(
        PdfDocument document,
        int expectedPageCount)
    {
        using var stream = new MemoryStream();

        document.Save(stream, false);

        Assert.True(stream.Length > 0);

        stream.Position = 0;

        using PdfDocument reopened =
            PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        Assert.Equal(expectedPageCount, reopened.PageCount);
    }
}
