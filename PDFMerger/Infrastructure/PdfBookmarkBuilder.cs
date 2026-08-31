using System.Collections.Generic;
using System.Linq;
using PDFMerger.Models;
using PdfSharp.Pdf;

namespace PDFMerger.Infrastructure;
public class PdfBookmarkBuilder
{
    public void GenerateBookmarks(PdfDocument outputDocument, List<FileMergeInfo> fileInfos)
    {
        foreach (var fileInfo in fileInfos)
        {
            // Create top-level bookmark (use file name)
            int firstPageIndex = fileInfo.StartPageNumber - 1;
            if (firstPageIndex >= 0 && firstPageIndex < outputDocument.PageCount)
            {
                var destPage = outputDocument.Pages[firstPageIndex];
                var fileOutline = outputDocument.Outlines.Add(fileInfo.FileNameWithoutExtension, destPage, false);

                // If the file has original bookmarks, add them as child bookmarks
                if (fileInfo.OutlineNodes.Any())
                {
                    foreach (var rootNode in fileInfo.OutlineNodes)
                    {
                        AddOutlineNode(rootNode, fileOutline, fileInfo.StartPageNumber - 1, outputDocument);
                    }
                }
            }
        }
    }
    public List<OutlineNode> ExtractOutlineNodes(PdfOutlineCollection outlines, Dictionary<PdfPage, int> pageIndexMap)
    {
        var list = new List<OutlineNode>();
        if (outlines == null) return list;
        foreach (PdfOutline outline in outlines)
        {
            list.Add(ExtractOutlineNode(outline, pageIndexMap)); // Pass the mapping
        }
        return list;
    }

    private void AddOutlineNode(OutlineNode node, PdfOutline parent, int pageOffset, PdfDocument outputDoc)
    {
        int destPageIndex = node.PageIndex + pageOffset;
        if (destPageIndex < 0 || destPageIndex >= outputDoc.PageCount)
            return; // Skip invalid page numbers

        var destPage = outputDoc.Pages[destPageIndex];
        // Create outline node (expanded state depends on whether it has child nodes)
        var newOutline = parent.Outlines.Add(node.Title, destPage, node.Children.Any());
        // Recursively add child nodes
        foreach (var child in node.Children)
        {
            AddOutlineNode(child, newOutline, pageOffset, outputDoc);
        }
    }

    private OutlineNode ExtractOutlineNode(PdfOutline outline, Dictionary<PdfPage, int> pageIndexMap)
    {
        var node = new OutlineNode
        {
            Title = outline.Title,
            PageIndex = outline.DestinationPage != null && pageIndexMap.TryGetValue(outline.DestinationPage, out int idx)
                ? idx
                : -1
        };
        foreach (PdfOutline child in outline.Outlines)
        {
            node.Children.Add(ExtractOutlineNode(child, pageIndexMap)); // Recursively pass
        }
        return node;
    }
    }
