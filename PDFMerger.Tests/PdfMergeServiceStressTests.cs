using System.Text;
using PDFMerger.Models;
using PDFMerger.Services;

namespace PDFMerger.Tests;
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

    [Fact]
    [Trait("Category", "Stress")]
    public async Task MergeAsync_ShouldSupport_PdfLargerThan10GiB()
    {
        // Arrange
        const long targetSize = 11L * 1024 * 1024 * 1024; // 10 GiB
        var sourcePdf = Path.Combine(_testDirectory, "large-source.pdf");
        var outputPdf = Path.Combine(_testDirectory, "merged-output.pdf");

        CreateLargePdf(sourcePdf, targetSize);
        var sourceSize = new FileInfo(sourcePdf).Length;

        // Assert
        Assert.True(
            sourceSize > targetSize,
            $"Test PDF must be larger than 2 GiB. Actual size: {sourceSize:N0} bytes");

        // Act
        var result = await _pdfMergeService.MergeAsync(
            new[] { sourcePdf }, outputPdf, new MergeOptions());

        // Assert
        Assert.NotNull(result);
        Assert.True(
            result.Success,
            $"Merge failed: {result.Error?.TechnicalDetail}");

        Assert.True(File.Exists(outputPdf));

        Assert.Equal(1, result.TotalPages);
    }


    private static void CreateLargePdf(string filePath, long targetSize)
    {
        //const int paddingChunkSize = 1024 * 1024; // 1 MiB

        // A minimal valid PDF:
        // %PDF-1.7
        // 1 0 obj
        // << /Type /Catalog /Pages 2 0 R >>
        // endobj
        //
        // 2 0 obj
        // << /Type /Pages /Kids [3 0 R] /Count 1 >>
        // endobj
        //
        // 3 0 obj
        // << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>
        // endobj

        using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            bufferSize: 64 * 1024,
            leaveOpen: true);

        writer.Write("%PDF-1.7\n");

        var offsets = new long[4];

        offsets[1] = stream.Position;

        writer.Write("1 0 obj\n");
        writer.Write("<< /Type /Catalog /Pages 2 0 R >>\n");
        writer.Write("endobj\n");

        offsets[2] = stream.Position;

        writer.Write("2 0 obj\n");
        writer.Write("<< /Type /Pages /Kids [3 0 R] /Count 1 >>\n");
        writer.Write("endobj\n");

        offsets[3] = stream.Position;

        writer.Write("3 0 obj\n");
        writer.Write("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>\n");
        writer.Write("endobj\n");

        writer.Flush();

        // Put the large padding BEFORE xref.
        // PDF comments start with '%' and continue until end-of-line.
        //
        // This makes the actual PDF file larger than 2 GiB while
        // keeping the PDF object structure tiny.
        WritePadding(stream, targetSize);

        var xrefOffset = stream.Position;

        writer.Write("xref\n");
        writer.Write("0 4\n");

        writer.Write("0000000000 65535 f \n");
        writer.Write($"{offsets[1]:D10} 00000 n \n");
        writer.Write($"{offsets[2]:D10} 00000 n \n");
        writer.Write($"{offsets[3]:D10} 00000 n \n");

        writer.Write("trailer\n");
        writer.Write("<< /Size 4 /Root 1 0 R >>\n");

        writer.Write("startxref\n");
        writer.Write(xrefOffset);
        writer.Write("\n");

        writer.Write("%%EOF\n");

        writer.Flush();
    }

    private static void WritePadding(FileStream stream, long targetSize)
    {
        const int chunkSize = 1024 * 1024; // 1 MiB

        var buffer = new byte[chunkSize];

        // Make the entire buffer a PDF comment.
        Array.Fill(buffer, (byte)'A');
        buffer[0] = (byte)'%';

        while (stream.Position + buffer.Length < targetSize)
        {
            stream.Write(buffer, 0, buffer.Length);
        }

        var remaining = targetSize - stream.Position;

        if (remaining > 0)
        {
            buffer[0] = (byte)'%';

            stream.Write(
                buffer,
                0,
                (int)remaining);
        }

        // Make sure the xref starts on a new line.
        stream.WriteByte((byte)'\n');
    }

    public void Dispose()
    {
        TryDeleteDirectory();

    }

    private void TryDeleteDirectory()
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
