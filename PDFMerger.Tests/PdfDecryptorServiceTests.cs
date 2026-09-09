using PDFMerger.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMerger.Tests.Services;

public sealed class PdfDecryptorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _tempEncryptedFile;
    private readonly string _tempDecryptedFile;
    private const string UserPassword = "user123";
    private const string OwnerPassword = "owner456";
    private const int PageCount = 3;

    private readonly PdfDecryptor _service;

    public PdfDecryptorTests()
    {
        _service = new PdfDecryptor();
        _testDirectory = Path.Combine(Path.GetTempPath(), "PDFMergerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _tempEncryptedFile = Path.Combine(_testDirectory, "encrypted.pdf");
        _tempDecryptedFile = Path.Combine(_testDirectory, "decrypted.pdf");

        CreateEncryptedPdf(_tempEncryptedFile, UserPassword, OwnerPassword, PageCount);
    }



    [Fact]
    public void OpenDecrypted_WithCorrectPassword_ReturnsDecryptedDocument()
    {
        var password = UserPassword;
        using (var decryptedDoc = _service.OpenDecrypted(_tempEncryptedFile, password))
        {
            Assert.NotNull(decryptedDoc);
            Assert.Equal(PageCount, decryptedDoc.PageCount);
        }
    }

    [Fact]
    public void OpenDecrypted_WithOwnerPassword_AlsoWorks()
    {
        var password = UserPassword;
        using (var decryptedDoc = _service.OpenDecrypted(_tempEncryptedFile, password))
        {
            Assert.NotNull(decryptedDoc);
            Assert.Equal(PageCount, decryptedDoc.PageCount);
        }
    }

    [Fact]
    public void OpenDecrypted_WithWrongPassword_ThrowsPdfReaderException()
    {
        var wrongPassword = "wrongPasswd";

        Assert.Throws<PdfReaderException>(() =>
        {
            using (var doc = _service.OpenDecrypted(_tempEncryptedFile, wrongPassword))
            {
            }
        });
    }

    [Fact]
    public void OpenDecrypted_WhenFileNotFound_ThrowsFileNotFoundException()
    {
        var nonExistentFile = @"C:\this_file_should_not_exist.pdf";

        var ex = Assert.Throws<FileNotFoundException>(() =>
            _service.OpenDecrypted(nonExistentFile, UserPassword));
        Assert.Contains("not found.", ex.Message);
    }

    [Fact]
    public void OpenDecrypted_WithEmptyFilePath_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.OpenDecrypted("", UserPassword));
        Assert.Equal("filePath", ex.ParamName);
    }

    [Fact]
    public void OpenDecrypted_WithEmptyPassword_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            _service.OpenDecrypted(_tempEncryptedFile, ""));
        Assert.Equal("password", ex.ParamName);
    }

    [Fact]
    public void DecryptAndSave_ProducesUnencryptedFile()
    {
        _service.DecryptAndSave(_tempEncryptedFile, _tempDecryptedFile, UserPassword);

        using (var doc = PdfReader.Open(_tempDecryptedFile, PdfDocumentOpenMode.Import))
        {
            Assert.Equal(PageCount, doc.PageCount);
        }
    }

    [Fact]
    public void DecryptAndSave_WithWrongPassword_ThrowsPdfReaderException()
    {
        Assert.Throws<PdfReaderException>(() =>
            _service.DecryptAndSave(_tempEncryptedFile, _tempDecryptedFile, "wrongPasswd"));
    }

    #region Helpers
    private static void CreateEncryptedPdf(string filePath, string userPwd, string ownerPwd, int pageCount)
    {
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pageCount; i++)
            {
                doc.AddPage();
            }
            doc.SecuritySettings.UserPassword = userPwd;
            doc.SecuritySettings.OwnerPassword = ownerPwd;
            doc.Save(filePath);
        }
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
