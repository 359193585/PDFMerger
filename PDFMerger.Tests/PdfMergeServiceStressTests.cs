using System.Text;
using PDFMerger.Models;
using PDFMerger.Services;
using PDFMerger.Tests.TestData;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMerger.Tests.StressTest;

public class PdfMergeServiceStressTests : IDisposable
{
    private readonly PdfSharpMergeService _pdfMergeService;
    private readonly string _testDirectory;
    public PdfMergeServiceStressTests()
    {
        _pdfMergeService = new PdfSharpMergeService();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PDFMergerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    #region  Specially generate PDFsharp parser boundary/bug test data
    [Theory]
    [InlineData(1L)]
    //[InlineData(2L)]
    //[InlineData(5L)]
    //[InlineData(10L)]
    [Trait("Category", "Stress")]
    public async Task MergeAsync_ShouldSupport_LargePdf(long sizeInGiB)
    {
        var targetSize = sizeInGiB * 1024L * 1024L * 1024L; 
        var sourcePdf = Path.Combine(_testDirectory, $"large-source-{sizeInGiB}GiB.pdf");
        var outputPdf = Path.Combine(_testDirectory, $"merged-output-{sizeInGiB}GiB.pdf");

        PdfSharpIssueTestDataGenerator.CreateLargePdf(sourcePdf, targetSize);
        var sourceSize = new FileInfo(sourcePdf).Length;

        Assert.True(
            sourceSize >= targetSize,
            $"Test PDF must be at least {sizeInGiB} GiB. " +
            $"Actual size: {sourceSize:N0} bytes.");

        var result = await _pdfMergeService.MergeAsync(
            new[] { sourcePdf }, outputPdf, new MergeOptions());

        Assert.NotNull(result);
        Assert.True(
            result.Success,
            $"Merge of {sizeInGiB} GiB PDF failed: " +
            $"{result.Error?.TechnicalDetail}");

        Assert.True(File.Exists(outputPdf));

        Assert.Equal(1, result.TotalPages);
    }
    #endregion


    #region Create multi-page realistic PDF test data using large images with content
    [Fact]
    public void RealImagePdfGenerator_CreatesLargeMultiPagePdf()
    {
        // Arrange
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"PDFMerger-Test-{Guid.NewGuid():N}.pdf");

        const int width = 8192;
        const int height = 8192;
        const int pageCount = 10;
        const int jpegQuality = 90;

        try
        {
            // Act
            RealImagePdfGenerator.Create(
                filePath,
                width,
                height,
                pageCount,
                jpegQuality);

            // Assert
            Assert.True(
                File.Exists(filePath),
                "The generated PDF file should exist.");

            var fileInfo = new FileInfo(filePath);

            Assert.True(
                fileInfo.Length > 10 * 1024 * 1024,
                $"The generated PDF should be reasonably large. " +
                $"Actual size: {fileInfo.Length:N0} bytes.");

            using var document =
                PdfReader.Open(
                    filePath,
                    PdfDocumentOpenMode.Import);

            Assert.Equal(pageCount, document.PageCount);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
    #endregion


    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }

}
