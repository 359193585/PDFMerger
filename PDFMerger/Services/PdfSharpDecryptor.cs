using System;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMerger.Services;

public class PdfDecryptor
{
    public PdfDocument OpenDecrypted(string filePath, string password)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("PDF file not found.", filePath);
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password), "password is null");

        using (PdfDocument source = PdfReader.Open(filePath, password, PdfDocumentOpenMode.Import))
        {
            PdfDocument decrypted = new PdfDocument();

            foreach (PdfPage page in source.Pages)
            {
                decrypted.AddPage(page);
            }
            return decrypted;
        }
    }

    public void DecryptAndSave(string sourcePath, string destPath, string password)
    {
        using (PdfDocument doc = OpenDecrypted(sourcePath, password))
        {
            doc.Save(destPath);
        }
    }
}
