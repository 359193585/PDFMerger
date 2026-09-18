using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Security;

namespace PDFMerger.Tests.StressTest;


public class LargePdfEncryptionTests
{
    [Fact]
    public void EncryptLargePdf_ShouldCreatePasswordProtectedFile()
    {
        // 测试目标：对一个接近 2GB 的 PDF 文件进行加密，生成一个带密码保护的 PDF 文件。
        // 注意：此测试需要一个实际存在的接近 2GB 的 PDF 文件，路径请根据实际情况修改。
        string sourcePath = @"E:\temp\TestPdfFile\testpdf-nearly-2G.pdf";      // 原始 1GB PDF
        string destPath = @"E:\temp\TestPdfFile\testpdf-nearly-2G-encrypted(密码1111）.pdf";
        string userPassword = "1111";
        string ownerPassword = "2222";

        Assert.True(File.Exists(sourcePath), "测试源 PDF 文件不存在");

        // 1. 打开源 PDF（如果源文件本身没有加密，密码参数可省略或传空）
        //    注意：对于 1GB 文件，PdfReader.Open 会加载整个文档，这一步可能耗时较长。
        using (PdfDocument document = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Modify))
        {
            // 2. 获取安全设置对象
            PdfSecuritySettings securitySettings = document.SecuritySettings;

            // 3. 设置密码 —— 设置任一密码会自动启用 128 位 AES 加密
            securitySettings.UserPassword = userPassword;
            securitySettings.OwnerPassword = ownerPassword;

            // 4. （可选）设置权限限制，例如禁止打印
            // securitySettings.PermitPrint = false;
            // securitySettings.PermitExtractContent = false;

            // 5. 保存为加密文件
            //    这一步会重写整个 1GB 文件，IO 和加密计算的开销都很大。
            document.Save(destPath);
        }

        // Assert
        Assert.True(File.Exists(destPath), "加密后的文件未生成");

        // 验证：用用户密码可以打开加密文件
        using (PdfDocument encryptedDoc = PdfReader.Open(destPath, userPassword, PdfDocumentOpenMode.Import))
        {
            Assert.NotNull(encryptedDoc);
            // 可以用页数做一个简单校验
            Assert.True(encryptedDoc.PageCount > 0);
        }

        // 验证：不提供密码打开会抛出异常
        Assert.Throws<PdfReaderException>(() =>
        {
            PdfReader.Open(destPath, PdfDocumentOpenMode.Import);
        });
    }
}
