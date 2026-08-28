// FileInspectionService.cs
using System;
using System.IO;
using System.Linq;
using PDFMerger.Models;

namespace PDFMerger.Services;

public sealed class FileInspectionService
{
    private readonly ImageFormatDetector _imageFormatDetector;
    private readonly PdfFormatDetector _pdfFormatDetector;
    public FileInspectionService()
    {
        _imageFormatDetector = new ImageFormatDetector();
        _pdfFormatDetector = new PdfFormatDetector();
    }

    public FileInspectionResult Inspect(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return CreateFailure("InvalidPath");
        }

        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists)
        {
            return CreateFailure("FileNotFound");
        }

        var extension = fileInfo.Extension;

        if (FileExtensions.PdfExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            return _pdfFormatDetector.Detect(filePath);
        }

        if (FileExtensions.ImageExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            return InspectImage(filePath, fileInfo);
        }

        return CreateFailure("UnsupportedFormat");
    }

    private FileInspectionResult InspectImage(string filePath, FileInfo fileInfo)
    {
        var imageFormatInfo = _imageFormatDetector.Detect(filePath);

        if (!imageFormatInfo.IsSupported)
        {
            return new FileInspectionResult
            {
                IsSupported = false,
                Type = FileType.Image,
                FileSize = fileInfo.Length,
                ErrorCode = imageFormatInfo.Format.ToString()
            };
        }

        return new FileInspectionResult
        {
            IsSupported = true,
            Type = FileType.Image,
            PageCount = imageFormatInfo.FrameCount,
            FileSize = fileInfo.Length
        };
    }

    private static FileInspectionResult CreateFailure(string errorCode)
    {
        return new FileInspectionResult
        {
            IsSupported = false,
            ErrorCode = errorCode
        };
    }
}


