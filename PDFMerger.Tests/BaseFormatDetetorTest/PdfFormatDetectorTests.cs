using PdfSharp.Pdf;

namespace PDFMerger.Tests.BaseFormatDetetorTest;
public class PdfFormatDetectorTests : IDisposable
{
    private readonly string _tempDirectory;

    public PdfFormatDetectorTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "PDFMergerTests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_tempDirectory);
    }
    [Fact]
    public void Detect_ValidPdf_ReturnsCorrectInspectionResult()
    {
        var filePath = CreatePdf(
            pageCount: 3,
            author: "Test Author");

        var detector = new PdfFormatDetector();

        var result = detector.Detect(filePath);

        var expectedFileSize = new FileInfo(filePath).Length;

        Assert.True(result.IsSupported);
        Assert.False(result.IsEncrypted);
        Assert.Equal(3, result.PageCount);
        Assert.Equal("Test Author", result.Author);
        Assert.Equal(expectedFileSize, result.FileSize);
    }
    [Fact]
    public void Detect_PdfWithoutAuthor_ReturnsEmptyAuthor()
    {
        var filePath = CreatePdf(pageCount: 1);

        var detector = new PdfFormatDetector();

        var result = detector.Detect(filePath);

        Assert.True(result.IsSupported);
        Assert.False(result.IsEncrypted);
        Assert.Equal(1, result.PageCount);
        Assert.Equal("", result.Author);
    }
    [Fact]
    public void Detect_InvalidPdf_ReturnsNotSupported()
    {
        var filePath = Path.Combine(_tempDirectory, "invalid.pdf");

        File.WriteAllText(filePath, "This is not a valid PDF.");

        var detector = new PdfFormatDetector();

        var result = detector.Detect(filePath);

        Assert.False(result.IsSupported);
        Assert.False(result.IsEncrypted);
    }
    [Fact]
    public void Detect_EncryptedPdf_ReturnsEncrypted()
    {
        var filePath = CreatePdf(password: "test-password");

        var detector = new PdfFormatDetector();

        var result = detector.Detect(filePath);

        Assert.False(result.IsSupported);
        Assert.True(result.IsEncrypted);
    }

    private string CreatePdf(int pageCount = 1, string? author = null, string? password = null)
    {
        var filePath = Path.Combine(
            _tempDirectory,
            Guid.NewGuid() + ".pdf");

        using var document = new PdfDocument();

        if (author != null)
        {
            document.Info.Author = author;
        }

        if (password != null)
        {
            document.SecuritySettings.UserPassword = password;
            document.SecuritySettings.OwnerPassword = password;
        }

        for (var i = 0; i < pageCount; i++)
        {
            document.AddPage();
        }

        document.Save(filePath);

        return filePath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch
        {
        }
    }
}

