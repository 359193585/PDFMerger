using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDFMerger.Contracts;
using PDFMerger.Services;

namespace PDFMerger.Tests.Services;
public class SimpleBookmarkGeneratorTests
{
    [Fact]
    public void GenerateBookmarks_WithSingleEntry_CreatesOneBookmark()
    {
        var fileEntries = new List<FileBookmarkInfo>
        {
            new()
            {
                FileNameWithoutExtension = "Report",
                StartPageNumber = 8
            }
        };

        var generator = new SimpleBookmarkGenerator();

        var result = generator.GenerateBookmarks(fileEntries);

        var bookmark = Assert.Single(result);

        Assert.Equal("Report", bookmark.Title);
        Assert.Equal(8, bookmark.PageNumber);
    }
    [Fact]
    public void GenerateBookmarks_CreatesBookmarksFromFileEntries()
    {
        var fileEntries = new List<FileBookmarkInfo>
        {
            new()
            {
                FileNameWithoutExtension = "Document1",
                StartPageNumber = 1
            },
            new()
            {
                FileNameWithoutExtension = "Document2",
                StartPageNumber = 5
            },
            new()
            {
                FileNameWithoutExtension = "Document3",
                StartPageNumber = 12
            }
        };

        var generator = new SimpleBookmarkGenerator();

        var result = generator.GenerateBookmarks(fileEntries);

        Assert.Equal(3, result.Count);

        Assert.Equal("Document1", result[0].Title);
        Assert.Equal(1, result[0].PageNumber);

        Assert.Equal("Document2", result[1].Title);
        Assert.Equal(5, result[1].PageNumber);

        Assert.Equal("Document3", result[2].Title);
        Assert.Equal(12, result[2].PageNumber);
    }

    [Fact]
    public void GenerateBookmarks_WithEmptyList_ReturnsEmptyList()
    {
        var generator = new SimpleBookmarkGenerator();

        var result = generator.GenerateBookmarks(new List<FileBookmarkInfo>());

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
