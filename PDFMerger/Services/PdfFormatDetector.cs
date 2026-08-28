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
                IsSupported = true,
                PageCount = doc.PageCount,
                Author = doc.Info.Author ?? "",
                IsEncrypted = false,
                FileSize = new FileInfo(filePath).Length
            };
        }
        catch (PdfReaderException ex)
        {
            if (ex.Message.Contains("password") || ex.Message.Contains("encrypted"))
            {
                return new FileInspectionResult
                {
                    IsSupported = false,
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
}
