//PdfSharpMergeService.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PDFMerger.Contracts;
using PDFMerger.Infrastructure;
using PDFMerger.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMerger.Services;

/// <summary>
/// PDF merging service implemented with PDFsharp, supporting retention of original bookmarks and using file names as first-level directories
/// </summary>
public class PdfSharpMergeService
{
    public Task<MergeResult> MergeAsync(
        string[] filePaths,
        string outputPath,
        MergeOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        ArgumentNullException.ThrowIfNull(outputPath);
        ArgumentNullException.ThrowIfNull(options);

        return Task.Run(() => MergeInternal(filePaths, outputPath, options, cancellationToken), cancellationToken);
    }

    private MergeResult MergeInternal(
        string[] filePaths,
        string outputPath,
        MergeOptions options,
        CancellationToken cancellationToken)
    {
        var result = new MergeResult { OutputPath = outputPath };
        string? currentFilePath = null;

        try
        {
            var finalPaths = DetermineFinalMergePaths(filePaths, options, result);

            using (var outputDocument = new PdfDocument())
            {
                outputDocument.Info.Title = options.Title ?? "MergedFiles";
                outputDocument.Info.Author = options.Author ?? "User of PDFMerger";
                outputDocument.Info.Subject = options.Subject ?? "";
                outputDocument.Info.Creator = options.Creator ?? "PDFMerger";

                var context = new MergeContext(outputDocument, finalPaths, options);
                var imageConverter = new ImageToPdfPageConverter();

                foreach (var pathName in finalPaths)
                {
                    currentFilePath = pathName;
                    cancellationToken.ThrowIfCancellationRequested();

                    ImageFormatInfo imageFormatInfo = new ImageFormatDetector().Detect(pathName);
                    string ext = System.IO.Path.GetExtension(pathName).ToLowerInvariant();

                    if (imageFormatInfo.IsRaster || imageFormatInfo.IsVector)
                    {
                        ProcessSingleImageFile(context, pathName, imageConverter);
                    }
                    else if (string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessSinglePdfFile(context, pathName, cancellationToken);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Report completion progress
                options.Progress?.Report(new MergeProgress
                {
                    FileIndex = finalPaths.Count,
                    TotalFiles = finalPaths.Count,
                    IsComplete = true,
                    TotalPagesProcessed = context.TotalPages
                });


                result.TotalPages = context.TotalPages;

                //  Generate bookmarks (if a generator is provided or the original document has bookmarks)
                if (options.BookmarkGenerator != null || context.FileInfos.Any(f => f.OutlineNodes.Any()))
                {
                    _pdfBookmarkBuilder.GenerateBookmarks(outputDocument, context.FileInfos);
                }

                // After all pages are added, check if page numbers need to be added
                if (options.AddPageNumbers && result.TotalPages > 0)
                {
                    _pdfPageNumberService.AddPageNumbers(outputDocument);
                }

                cancellationToken.ThrowIfCancellationRequested();
                outputDocument.Save(outputPath);
                result.Success = true;
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            TryDeleteIncompleteOutputFile(outputPath);
            throw;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = new MergeError
            {
                Code = MergeErrorCode.Unknown,
                TechnicalDetail = ex.ToString(),
                FilePath = currentFilePath
            };
            return result;
        }
    }

    private PdfBookmarkBuilder _pdfBookmarkBuilder = new PdfBookmarkBuilder();
    private PdfPageNumberService _pdfPageNumberService = new PdfPageNumberService();
    private void TryDeleteIncompleteOutputFile(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            try
            {
                File.Delete(path);
                break;
            }
            catch (IOException)
            {
                System.Threading.Thread.Sleep(50);
            }
        }
    }
    private class MergeContext
    {
        public MergeContext(PdfDocument outputDocument, List<string> finalPaths, MergeOptions options)
        {
            OutputDocument = outputDocument ?? throw new ArgumentNullException(nameof(outputDocument));
            FinalPaths = finalPaths ?? throw new ArgumentNullException(nameof(finalPaths));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            FileInfos = new List<FileMergeInfo>();
        }
        public PdfDocument OutputDocument { get; }
        public List<FileMergeInfo> FileInfos { get; }
        public List<string> FinalPaths { get; }
        public MergeOptions Options { get; }
        public int TotalPages { get; set; }= 0;
        public int FileIndex { get; set; }= 0;
    }

    
    private void ProcessSingleImageFile(MergeContext context, string imagePath, ImageToPdfPageConverter converter
        , CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int startPage = context.TotalPages + 1;
        int addedPages = converter.AddImagePageToDocument(imagePath, context.OutputDocument, ImageToPdfPageConverter.PageSizeMode.A4);

        var fileInfo = new FileMergeInfo
        {
            FilePath = imagePath,
            FileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(imagePath),
            StartPageNumber = startPage,
            PageCount = addedPages,
            FileSize = new FileInfo(imagePath).Length,
            OutlineNodes = new List<OutlineNode>()
        };
        context.FileInfos.Add(fileInfo);

        context.TotalPages += addedPages;
        context.Options.Progress?.Report(new MergeProgress
        {
            FileIndex = context.FileIndex,
            TotalFiles = context.FinalPaths.Count,
            FileName = System.IO.Path.GetFileName(imagePath),
            PageCount = addedPages,
            TotalPagesProcessed = context.TotalPages,
            IsComplete = false
        });

        context.FileIndex++;
    }

    private void ProcessSinglePdfFile(MergeContext context, string path, CancellationToken cancellationToken)
    {

        using var inputDocument = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        var pageIndexMap = new Dictionary<PdfPage, int>();
        for (int i = 0; i < inputDocument.PageCount; i++)
        {
            pageIndexMap[inputDocument.Pages[i]] = i;
        }

        int pageCount = inputDocument.PageCount;

        var outlineNodes = _pdfBookmarkBuilder.ExtractOutlineNodes(inputDocument.Outlines, pageIndexMap);

        var pages = inputDocument.Pages.Cast<PdfPage>();
        cancellationToken.ThrowIfCancellationRequested();
        ProcessPages(context, path, pages, pageCount, outlineNodes, cancellationToken);
    }

    private void ProcessPages(
                MergeContext context,
                string filePath,
                IEnumerable<PdfPage> pages,
                int pageCount,
                List<OutlineNode>? outlineNodes,
                CancellationToken cancellationToken)
    {
        int startPage = context.TotalPages + 1;

        // Record file information (used for bookmarks)
        var fileInfo = new FileMergeInfo
        {
            FilePath = filePath,
            FileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath),
            StartPageNumber = startPage,
            PageCount = pageCount,
            FileSize = new FileInfo(filePath).Length,
            OutlineNodes = outlineNodes ?? new List<OutlineNode>()
        };
        context.FileInfos.Add(fileInfo);

        // report progress but not complete yet
        context.Options.Progress?.Report(new MergeProgress
        {
            FileIndex = context.FileIndex,
            TotalFiles = context.FinalPaths.Count,
            FileName = System.IO.Path.GetFileName(filePath),
            PageCount = pageCount,
            TotalPagesProcessed = context.TotalPages,
            IsComplete = false
        });

        // Copy pages to the output document
        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.OutputDocument.AddPage(page);
        }

        context.TotalPages += pageCount;
        context.FileIndex++;
    }
    private List<string> DetermineFinalMergePaths(string[] filePaths, MergeOptions options, MergeResult result)
    {
        if (filePaths == null || filePaths.Length == 0)
            throw new ArgumentException("Please provide at least one file path.");

        var existingPaths = filePaths.Where(File.Exists).ToList();
        if (!existingPaths.Any())
            throw new FileNotFoundException("No valid PDF or Image files were found.");

        List<string> finalPaths;
        List<string>? duplicatedFiles = null;
        if (options.IgnoreDuplicates)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            finalPaths = new List<string>();
            duplicatedFiles = new List<string>();
            foreach (var p in existingPaths)
            {
                var normalizedPath = Path.GetFullPath(p); // Normalize path
                if (seen.Add(normalizedPath))
                    finalPaths.Add(normalizedPath);
                else
                    duplicatedFiles.Add(normalizedPath);
            }
        }
        else
        {
            finalPaths = existingPaths;
        }

        result.DuplicatedFiles = duplicatedFiles ?? new List<string>();
        result.MergedFiles = finalPaths;
        return finalPaths;
    }
}


