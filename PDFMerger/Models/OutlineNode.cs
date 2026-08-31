using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFMerger.Models;

public class OutlineNode
{
    public string Title { get; set; } = string.Empty;
    public int PageIndex { get; set; } // 0-based within source
    public List<OutlineNode> Children { get; set; } = new List<OutlineNode>();
}
