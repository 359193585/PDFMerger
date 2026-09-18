using PDFMerger.Models;
using PDFMerger.Tests.Fixtures;
using PDFMerger.ViewModels;

namespace PDFMerger.Tests.MainWindowsVMTests;

public sealed class MainWindowViewModelTests : IClassFixture<I18nFixture>, IDisposable
{
    //private readonly FakeMergeService _fakeMerge;
    //private readonly FakeInspectionService _fakeInspection;
    private readonly MainWindowViewModel _vm;
    private readonly string _tempDir;

    public MainWindowViewModelTests(I18nFixture fixture)
    {
        //_fakeMerge = new FakeMergeService();
        //_fakeInspection = new FakeInspectionService();
        _vm = new MainWindowViewModel(/*_fakeMerge, _fakeInspection*/);

        _tempDir = Path.Combine(Path.GetTempPath(), "PdfMergerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    #region Helpers
    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { }
        }
    }

    private FileItem MakeItem(string fileName, bool encrypted = false, string? password = null)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, "%PDF-1.4 dummy");
        return new FileItem
        {
            FilePath = filePath,
            FileName = fileName,
            PageCount = 1,
            IsEncrypted = encrypted,
            Password = password,
            Type = FileType.Pdf
        };
    }

    #endregion

    #region ResolveUniqueOutputPath Tests
    [Fact]
    public void ResolveUniqueOutputPath_FileExists_AppendsCounter()
    {
        var existing = Path.Combine(_tempDir, "out.pdf");
        File.WriteAllText(existing, "x");
        _vm.OutputPath = existing;

        _vm.ResolveUniqueOutputPath();

        Assert.Equal(Path.Combine(_tempDir, "out_1.pdf"), _vm.OutputPath);
    }

    [Theory]
    [InlineData("out.pdf", "out_1.pdf", "out_2.pdf")]
    [InlineData("out_1.pdf", "out_2.pdf", "out_1_1.pdf")]
    [InlineData("out_9.pdf", "out_9_1.pdf", "out_9_2.pdf")]
    [InlineData("out_99.pdf", "out_9.pdf", "out_99_1.pdf")]
    [InlineData("out_999.pdf", "out_1000.pdf", "out_999_1.pdf")]
    public void ResolveUniqueOutputPath_MultipleConflicts_KeepsIncrementing(
        string initial1, string initial2, string expected)
    {
        var p0 = Path.Combine(_tempDir, initial1);
        var p1 = Path.Combine(_tempDir, initial2);
        File.WriteAllText(p0, "x");
        File.WriteAllText(p1, "x");
        _vm.OutputPath = p0;

        _vm.ResolveUniqueOutputPath();

        Assert.Equal(Path.Combine(_tempDir, expected), _vm.OutputPath);
    }

    [Fact]
    public void ResolveUniqueOutputPath_NoConflict_KeepsOriginal()
    {
        var path = Path.Combine(_tempDir, "out.pdf");
        _vm.OutputPath = path;

        _vm.ResolveUniqueOutputPath();

        Assert.Equal(path, _vm.OutputPath);
    }
    #endregion

    #region Items move delete clear tests
    [Fact]
    public void MoveUp_MiddleItem_SwapsWithPrevious()
    {
        var a = MakeItem("a.pdf");
        var b = MakeItem("b.pdf");
        var c = MakeItem("c.pdf");
        _vm.FileItems.Add(a);
        _vm.FileItems.Add(b);
        _vm.FileItems.Add(c);
        _vm.SelectedItem = b;

        _vm.MoveUpCommand.Execute(null);

        Assert.Equal(new[] { b, a, c }, _vm.FileItems.ToArray());
        Assert.Same(b, _vm.SelectedItem);
    }
    [Fact]
    public void MoveUp_FirstItem_DoesNothing()
    {
        var a = MakeItem("a.pdf");
        var b = MakeItem("b.pdf");
        _vm.FileItems.Add(a);
        _vm.FileItems.Add(b);
        _vm.SelectedItem = a;

        _vm.MoveUpCommand.Execute(null);

        Assert.Equal(new[] { a, b }, _vm.FileItems.ToArray());
        Assert.Same(a, _vm.SelectedItem);
    }

    [Fact]
    public void MoveDown_MiddleItem_SwapsWithNext()
    {
        var a = MakeItem("a.pdf");
        var b = MakeItem("b.pdf");
        var c = MakeItem("c.pdf");
        _vm.FileItems.Add(a);
        _vm.FileItems.Add(b);
        _vm.FileItems.Add(c);
        _vm.SelectedItem = b;

        _vm.MoveDownCommand.Execute(null);

        Assert.Equal(new[] { a, c, b }, _vm.FileItems.ToArray());
    }

    [Fact]
    public void MoveDown_LastItem_DoesNothing()
    {
        var a = MakeItem("a.pdf");
        var b = MakeItem("b.pdf");
        _vm.FileItems.Add(a);
        _vm.FileItems.Add(b);
        _vm.SelectedItem = b;

        _vm.MoveDownCommand.Execute(null);

        Assert.Equal(new[] { a, b }, _vm.FileItems.ToArray());
        Assert.Same(b, _vm.SelectedItem);
    }

    [Fact]
    public void RemoveSelected_RemovesAndClearsSelection()
    {
        var a = MakeItem("a.pdf");
        var b = MakeItem("b.pdf");
        _vm.FileItems.Add(a);
        _vm.FileItems.Add(b);
        _vm.SelectedItem = a;

        _vm.RemoveSelectedCommand.Execute(null);

        Assert.Equal(new[] { b }, _vm.FileItems.ToArray());
        Assert.Null(_vm.SelectedItem);
    }
    [Fact]
    public void ClearList_EmptiesItemsAndOutputPath()
    {
        _vm.FileItems.Add(MakeItem("a.pdf"));
        _vm.OutputPath = "some/path.pdf";

        _vm.ClearListCommand.Execute(null);

        Assert.Empty(_vm.FileItems);
        Assert.Equal("", _vm.OutputPath);
        Assert.Equal("列表为空", _vm.StatusMessage);
    }

    #endregion
}
