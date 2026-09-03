using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PDFMerger.Tests.TestData;

public static class SyntheticPdfGenerator
{
    public static void Create(string filePath, long targetSizeBytes, int pageCount = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (targetSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetSizeBytes));

        if (pageCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageCount));

        string? directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        WritePdf(stream, targetSizeBytes, pageCount);
    }

    private static void WritePdf(
        Stream stream,
        long targetSizeBytes,
        int pageCount)
    {
        var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false),
            bufferSize: 4096,
            leaveOpen: true);

        writer.NewLine = "\n";

        // ------------------------------------------------------------
        // PDF objects
        //
        // 1  Catalog
        // 2  Pages
        // 3  Page 1
        // 4  Content stream
        // 5  Padding stream
        //
        // The PDF is deliberately simple.
        // ------------------------------------------------------------

        writer.WriteLine("%PDF-1.7");
        writer.WriteLine("%\u00E2\u00E3\u00CF\u00D3");

        writer.Flush();

        var offsets = new List<long>
        {
            0
        };

        // Object 1: Catalog
        offsets.Add(stream.Position);
        WriteObject(
            writer,
            1,
            """
            <<
            /Type /Catalog
            /Pages 2 0 R
            >>
            """);

        // Object 2: Pages
        offsets.Add(stream.Position);

        string kids = string.Join(
            " ",
            Enumerable.Range(0, pageCount)
                .Select(i => $"{3 + i * 2} 0 R"));

        WriteObject(
            writer,
            2,
            $"""
            <<
            /Type /Pages
            /Count {pageCount}
            /Kids [{kids}]
            >>
            """);

        // ------------------------------------------------------------
        // Pages
        // ------------------------------------------------------------

        for (int i = 0; i < pageCount; i++)
        {
            int pageObjectNumber = 3 + i * 2;
            int contentObjectNumber = pageObjectNumber + 1;

            offsets.Add(stream.Position);

            WriteObject(
                writer,
                pageObjectNumber,
                $"""
                <<
                /Type /Page
                /Parent 2 0 R
                /MediaBox [0 0 595 842]
                /Contents {contentObjectNumber} 0 R
                >>
                """);

            string content =
                $"BT /F1 12 Tf 50 780 Td (Synthetic test page {i + 1}) Tj ET";

            offsets.Add(stream.Position);

            WriteStreamObject(
                writer,
                contentObjectNumber,
                Encoding.ASCII.GetBytes(content));
        }

        writer.Flush();

        // ------------------------------------------------------------
        // Padding
        //
        // Add a large stream so the file reaches approximately the
        // requested target size.
        // ------------------------------------------------------------

        long currentSize = stream.Position;

        long paddingBytes =
            Math.Max(0, targetSizeBytes - currentSize);

        if (paddingBytes > 0)
        {
            long paddingObjectOffset = stream.Position;

            // We deliberately do not create a valid huge stream object
            // here because the purpose is to create a synthetic boundary
            // condition rather than a realistic PDF workload.
            //
            // Keep the padding outside the referenced PDF objects.
            WritePadding(stream, paddingBytes);

            _ = paddingObjectOffset;
        }

        writer.Flush();
    }

    private static void WriteObject(
        StreamWriter writer,
        int objectNumber,
        string content)
    {
        writer.WriteLine($"{objectNumber} 0 obj");
        writer.WriteLine(content);
        writer.WriteLine("endobj");
    }

    private static void WriteStreamObject(
        StreamWriter writer,
        int objectNumber,
        byte[] data)
    {
        writer.WriteLine($"{objectNumber} 0 obj");
        writer.WriteLine($"<< /Length {data.Length} >>");
        writer.WriteLine("stream");
        writer.Flush();

        writer.BaseStream.Write(data);

        writer.WriteLine();
        writer.WriteLine("endstream");
        writer.WriteLine("endobj");
    }

    private static void WritePadding(
        Stream stream,
        long bytes)
    {
        const int bufferSize = 1024 * 1024;

        byte[] buffer = new byte[bufferSize];

        // Use deterministic non-zero data rather than zero-filled
        // sparse-looking content.
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)('A' + (i % 26));
        }

        long remaining = bytes;

        while (remaining > 0)
        {
            int count = (int)Math.Min(
                remaining,
                buffer.Length);

            stream.Write(
                buffer,
                0,
                count);

            remaining -= count;
        }
    }
}

public static class PdfSharpIssueTestDataGenerator
{
    public static void CreateLargePdf(string filePath, long targetSize)
    {
        var tempFile = filePath + ".base";
        using (var document = CreateDocument(pageCount: 1))
        {
            document.Save(tempFile);
        }

        var original = File.ReadAllBytes(tempFile);
        const string eofMarker = "%%EOF";
        var eofBytes = Encoding.ASCII.GetBytes(eofMarker);

        int eofIndex = FindBytes(original, eofBytes);

        if (eofIndex < 0)
        {
            throw new InvalidOperationException(
                "Could not find %%EOF in the PDF generated by PDFsharp.");
        }

        long baseSizeWithoutEof = eofIndex;

        if (targetSize <= baseSizeWithoutEof + eofBytes.Length)
        {
            File.Copy(tempFile, filePath, overwrite: true);
            File.Delete(tempFile);
            return;
        }

        long paddingSize =
            targetSize - baseSizeWithoutEof - eofBytes.Length;

        using (var output = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read))
        {
            // 写入 PDFsharp 生成的全部内容，但暂时不写 %%EOF。
            output.Write(original, 0, eofIndex);

            // 写入巨大的 PDF 注释。
            WritePadding(output, paddingSize);

            // 最后写回真正的 EOF。
            output.Write(eofBytes, 0, eofBytes.Length);

            output.Flush();
        }

        File.Delete(tempFile);
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
    private static void WritePadding(FileStream stream, long paddingSize)
    {
        const int chunkSize = 1024 * 1024; // 1 MiB

        var buffer = new byte[chunkSize];

        // PDF comment.
        buffer[0] = (byte)'%';

        // Fill the rest with harmless comment data.
        Array.Fill(buffer, (byte)'A', 1, buffer.Length - 1);

        long remaining = paddingSize;

        while (remaining > 0)
        {
            int bytesToWrite = (int)Math.Min(
                remaining,
                buffer.Length);

            stream.Write(buffer, 0, bytesToWrite);
            remaining -= bytesToWrite;
        }
    }
    private static int FindBytes(byte[] source, byte[] pattern)
    {
        for (int i = 0; i <= source.Length - pattern.Length; i++)
        {
            bool matched = true;

            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return i;
            }
        }

        return -1;
    }

}
