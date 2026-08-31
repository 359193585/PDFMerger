using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDFMerger.Services;

namespace PDFMerger.Models;
public class FileMergeInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileNameWithoutExtension { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int StartPageNumber { get; set; } // 1-based
    public int PageCount { get; set; }
    public List<OutlineNode> OutlineNodes { get; set; } = new List<OutlineNode>();
}
