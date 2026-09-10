// PdfFormatDetector.cs
using System;
using System.IO;
using PDFMerger.Models;
using PdfSharp.Pdf.IO;

public sealed class PdfFormatDetector
{
    public FileInspectionResult Detect(string filePath)
    {
        try
        {
            using var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            return new FileInspectionResult
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                IsEncrypted = false,
                IsSupported = true,
                PageCount = doc.PageCount,
                Author = doc.Info.Author ?? "",
            };
        }
        catch (PdfReaderException ex)
        {
            if (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
            {
                return new FileInspectionResult
                {
                    FileName = Path.GetFileName(filePath),
                    FileSize = new FileInfo(filePath).Length,
                    IsSupported = true,
                    IsEncrypted = true
                };
            }
            else
            {
                return new FileInspectionResult
                {
                    IsSupported = false
                };
            }
        }
        catch (Exception)
        {
            return new FileInspectionResult
            {
                IsSupported = false
            };

        }
    }
    public FileInspectionResult Detect(string filePath, string pdfPassword)
    {
        try
        {
            using var doc = PdfReader.Open(filePath, pdfPassword, PdfDocumentOpenMode.Import);
            return new FileInspectionResult
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                IsEncrypted = true,
                IsSupported = true,
                PageCount = doc.PageCount,
                Author = doc.Info.Author ?? "",
                Password = pdfPassword
            };
        }
        catch (Exception)
        {
            return new FileInspectionResult
            {
                IsSupported = false
            };
        }
    }
}
