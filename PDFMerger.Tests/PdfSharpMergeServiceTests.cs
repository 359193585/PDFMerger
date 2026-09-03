// PdfSharpMergeServiceTests.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PDFMerger.Models;
using PDFMerger.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace PDFMerger.Tests.Services;

public class PdfSharpMergeServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PdfSharpMergeService _service;

    public PdfSharpMergeServiceTests()
    {
        _service = new PdfSharpMergeService();

        _testDirectory = Path.Combine(Path.GetTempPath(), "PDFMergerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }
    #region Tests null and empty inputs
    [Fact]
    public async Task MergeAsync_NullFilePaths_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.MergeAsync(
                null!,
                GetOutputPath(),
                new MergeOptions()));
    }

    [Fact]
    public async Task MergeAsync_NullOutputPath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.MergeAsync(
                Array.Empty<string>(),
                null!,
                new MergeOptions()));
    }

    [Fact]
    public async Task MergeAsync_NullOptions_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.MergeAsync(
                Array.Empty<string>(),
                GetOutputPath(),
                null!));
    }

    [Fact]
    public async Task MergeAsync_EmptyFilePaths_ReturnsFailure()
    {
        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            Array.Empty<string>(),
            outputPath,
            new MergeOptions());

        Assert.False(result.Success);
        Assert.Equal(outputPath, result.OutputPath);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task MergeAsync_NoExistingFiles_ReturnsFailure()
    {
        var outputPath = GetOutputPath();

        var missingFile = Path.Combine(
            _testDirectory,
            "missing.pdf");

        var result = await _service.MergeAsync(
            new[] { missingFile },
            outputPath,
            new MergeOptions());

        Assert.False(result.Success);
        Assert.Equal(outputPath, result.OutputPath);
        Assert.NotNull(result.Error);
    }
    #endregion

    [Fact]
    public async Task MergeAsync_TwoPdfFiles_MergesAllPages()
    {
        var pdf1 = CreatePdf("first.pdf", 2);
        var pdf2 = CreatePdf("second.pdf", 3);

        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { pdf1, pdf2 },
            outputPath,
            new MergeOptions());

        Assert.True(result.Success);
        Assert.Equal(5, result.TotalPages);
        Assert.Equal(outputPath, result.OutputPath);

        Assert.True(File.Exists(outputPath));

        using var outputDocument =
            PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);

        Assert.Equal(5, outputDocument.PageCount);
    }

    [Fact]
    public async Task MergeAsync_PreservesFileOrder()
    {
        var first = CreatePdf("first.pdf", 1);
        var second = CreatePdf("second.pdf", 2);

        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { first, second },
            outputPath,
            new MergeOptions());

        Assert.True(result.Success);

        Assert.Equal(
            new[] { first, second },
            result.MergedFiles);
    }

    [Fact]
    public async Task MergeAsync_ResultContainsMergedFiles()
    {
        var first = CreatePdf("first.pdf", 1);
        var second = CreatePdf("second.pdf", 1);

        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { first, second },
            outputPath,
            new MergeOptions());

        Assert.True(result.Success);

        Assert.Equal(2, result.MergedFiles.Count);
        Assert.Equal(
            Path.GetFullPath(first),
            result.MergedFiles[0]);

        Assert.Equal(
            Path.GetFullPath(second),
            result.MergedFiles[1]);

        Assert.Empty(result.DuplicatedFiles);
    }

    [Fact]
    public async Task MergeAsync_IgnoreDuplicates_RemovesDuplicateFiles()
    {
        var pdf = CreatePdf("duplicate.pdf", 2);

        var outputPath = GetOutputPath();

        var options = new MergeOptions
        {
            IgnoreDuplicates = true
        };

        var result = await _service.MergeAsync(
            new[] { pdf, pdf },
            outputPath,
            options);

        Assert.True(result.Success);

        Assert.Equal(2, result.TotalPages);

        Assert.Single(result.MergedFiles);
        Assert.Single(result.DuplicatedFiles);

        Assert.Equal(
            Path.GetFullPath(pdf),
            result.MergedFiles[0]);

        Assert.Equal(
            Path.GetFullPath(pdf),
            result.DuplicatedFiles[0]);
    }

    [Fact]
    public async Task MergeAsync_IgnoreDuplicatesFalse_MergesDuplicateFiles()
    {
        var pdf = CreatePdf("duplicate.pdf", 2);

        var outputPath = GetOutputPath();

        var options = new MergeOptions
        {
            IgnoreDuplicates = false
        };

        var result = await _service.MergeAsync(
            new[] { pdf, pdf },
            outputPath,
            options);

        Assert.True(result.Success);
        Assert.Equal(4, result.TotalPages);

        Assert.Equal(2, result.MergedFiles.Count);
        Assert.Empty(result.DuplicatedFiles);

        using var outputDocument =
            PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);

        Assert.Equal(4, outputDocument.PageCount);
    }

    [Fact]
    public async Task MergeAsync_SetsDocumentMetadata()
    {
        var pdf = CreatePdf("source.pdf", 1);
        var outputPath = GetOutputPath();

        var options = new MergeOptions
        {
            Title = "Test Title",
            Author = "Test Author",
            Subject = "Test Subject",
            Creator = "Test Creator"
        };

        var result = await _service.MergeAsync(
            new[] { pdf },
            outputPath,
            options);

        Assert.True(result.Success);

        using var document =
            PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);

        Assert.Equal("Test Title", document.Info.Title);
        Assert.Equal("Test Author", document.Info.Author);
        Assert.Equal("Test Subject", document.Info.Subject);
        Assert.Equal("Test Creator", document.Info.Creator);
    }

    [Fact]
    public async Task MergeAsync_DefaultMetadata_IsApplied()
    {
        var pdf = CreatePdf("source.pdf", 1);
        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { pdf },
            outputPath,
            new MergeOptions());

        Assert.True(result.Success);

        using var document =
            PdfReader.Open(outputPath, PdfDocumentOpenMode.Import);

        Assert.Equal("MergedFiles", document.Info.Title);
        Assert.Equal("User of PDFMerger", document.Info.Author);
        Assert.Equal("", document.Info.Subject);
        Assert.Equal("PDFMerger", document.Info.Creator);
    }

    [Fact]
    public async Task MergeAsync_ReportsProgressForEachFile()
    {
        var first = CreatePdf("first.pdf", 2);
        var second = CreatePdf("second.pdf", 3);

        var outputPath = GetOutputPath();

        var progressValues = new List<MergeProgress>();

        var options = new MergeOptions
        {
            Progress = new Progress<MergeProgress>(
                progress => progressValues.Add(progress))
        };

        var result = await _service.MergeAsync(
            new[] { first, second },
            outputPath,
            options);

        Assert.True(result.Success);

        Assert.Equal(3, progressValues.Count);

        Assert.False(progressValues[0].IsComplete);
        Assert.False(progressValues[1].IsComplete);
        Assert.True(progressValues[2].IsComplete);

        Assert.Equal(2, progressValues[0].PageCount);
        Assert.Equal(3, progressValues[1].PageCount);

        Assert.Equal(5, progressValues[2].TotalPagesProcessed);
        Assert.Equal(2, progressValues[2].FileIndex);
        Assert.Equal(2, progressValues[2].TotalFiles);
    }

    [Fact]
    public async Task MergeAsync_AlreadyCanceled_ThrowsOperationCanceledException()
    {
        var pdf = CreatePdf("source.pdf", 1);
        var outputPath = GetOutputPath();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _service.MergeAsync(
                new[] { pdf },
                outputPath,
                new MergeOptions(),
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task MergeAsync_InvalidPdf_ReturnsFailure()
    {
        var invalidPdf = Path.Combine(
            _testDirectory,
            "invalid.pdf");

        File.WriteAllText(
            invalidPdf,
            "This is not a valid PDF.");

        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { invalidPdf },
            outputPath,
            new MergeOptions());

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Equal(
            invalidPdf,
            result.Error.FilePath);
    }

    [Fact]
    public async Task MergeAsync_UnsupportedFile_ReturnFalse()
    {
        var textFile = Path.Combine(
            _testDirectory,
            "test.txt");

        File.WriteAllText(
            textFile,
            "This is not a PDF or image.");

        var outputPath = GetOutputPath();

        var result = await _service.MergeAsync(
            new[] { textFile },
            outputPath,
            new MergeOptions());

        Assert.False(result.Success);
        Assert.Equal(outputPath, result.OutputPath);
        Assert.NotNull(result.Error);
    }

    #region Helpers & Cleanup

    private string CreatePdf(string fileName, int pageCount)
    {
        var path = Path.Combine(
            _testDirectory,
            fileName);

        using var document = new PdfDocument();

        for (int i = 0; i < pageCount; i++)
        {
            document.AddPage();
        }

        document.Save(path);

        return path;
    }

    private string GetOutputPath()
    {
        return Path.Combine(
            _testDirectory,
            $"merged-{Guid.NewGuid():N}.pdf");
    }

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

    #endregion
}
