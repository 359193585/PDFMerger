using PDFMerger.Infrastructure;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace PDFMerger.Services;
public class PdfPageNumberService
{
    public void AddPageNumbers(PdfDocument document)
    {
        int totalPages = document.PageCount;
        // Use standard Helvetica font
        GlobalFontSettings.FontResolver = new CustomFontResolver();
        XFont font = new XFont("Helvetica", 12, XFontStyleEx.Regular);
        XBrush brush = XBrushes.Black;

        for (int i = 0; i < totalPages; i++)
        {
            PdfPage page = document.Pages[i];
            // Open the page in append mode for drawing
            using (XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append))
            {
                string text = $"{i + 1} / {totalPages}";
                XSize size = gfx.MeasureString(text, font);
                // Centered at the bottom, 20 points from the bottom
                double pageWidth = page.Width.Point;
                double pageHeight = page.Height.Point;
                double x = (pageWidth - size.Width) / 2;
                double y = pageHeight - 20;
                gfx.DrawString(text, font, brush, x, y);
            }
        }
    }

}
