using System;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PDFMerger.Infrastructure;

namespace PDFMerger.Services;
public sealed class PdfPageNumberService
{
    // PDF uses points. 1 inch = 72 points.
    // 96 DPI is used as the reference when defining the desired
    // visual size in pixels.
    private const double Dpi = 96.0;

    // Desired page-number size range, expressed in pixels.
    private const double MinFontSizePixels = 8.0;
    private const double MaxFontSizePixels = 16.0;

    // Font size is calculated from the shortest page dimension.
    // This prevents very large pages from producing excessively
    // large page numbers.
    private const double FontSizeRatio = 0.035;

    // Distance from the bottom edge, relative to the font size.
    private const double BottomMarginRatio = 1.5;

    // Minimum horizontal margin, expressed in multiples of font size.
    private const double HorizontalMarginRatio = 1.0;

    private const string FontName = "Helvetica";

    /// <summary>
    /// Adds page numbers to all pages of the specified PDF document.
    /// </summary>
    /// <param name="document">The PDF document to modify.</param>
    public void AddPageNumbers(PdfDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        int totalPages = document.PageCount;

        if (totalPages == 0)
        {
            return;
        }

        GlobalFontSettings.FontResolver = new CustomFontResolver();

        for (int i = 0; i < totalPages; i++)
        {
            PdfPage page = document.Pages[i];

            if (page.Width.Point <= 0 || page.Height.Point <= 0)
            {
                continue;
            }

            string text = $"{i + 1} / {totalPages}";

            using XGraphics gfx = XGraphics.FromPdfPage(
                page,
                XGraphicsPdfPageOptions.Append);

            double fontSize = CalculateFontSize(page);

            XFont font = new XFont(
                FontName,
                fontSize,
                XFontStyleEx.Regular);

            // Ensure the page-number text actually fits on the page.
            fontSize = AdjustFontSizeToFit(
                gfx,
                text,
                font,
                fontSize,
                page.Width.Point);

            // The font may have been reduced, so recreate it.
            if (Math.Abs(fontSize - font.Size) > 0.01)
            {
                font = new XFont(
                    FontName,
                    fontSize,
                    XFontStyleEx.Regular);
            }

            XSize textSize = gfx.MeasureString(text, font);

            double horizontalMargin = fontSize * HorizontalMarginRatio;

            double x = (page.Width.Point - textSize.Width) / 2;

            // Keep the text above the bottom edge while maintaining
            // a reasonable visual distance from it.
            double bottomMargin = fontSize * BottomMarginRatio;
            double y = page.Height.Point - bottomMargin;

            // Protect against extremely small pages.
            x = Math.Max(horizontalMargin, x);
            y = Math.Max(textSize.Height, y);

            // If the page is too small to satisfy the ideal margins,
            // center the text vertically within the available lower area.
            if (x + textSize.Width > page.Width.Point)
            {
                x = Math.Max(
                    0,
                    (page.Width.Point - textSize.Width) / 2);
            }

            gfx.DrawString(
                text,
                font,
                XBrushes.Black,
                x,
                y);
        }
    }

    /// <summary>
    /// Calculates a font size based on the shortest page dimension.
    /// </summary>
    private static double CalculateFontSize(PdfPage page)
    {
        double shortSide = Math.Min(
            page.Width.Point,
            page.Height.Point);

        double calculatedSize = shortSide * FontSizeRatio;

        double minFontSize = PixelsToPoints(MinFontSizePixels);
        double maxFontSize = PixelsToPoints(MaxFontSizePixels);

        return Math.Clamp(
            calculatedSize,
            minFontSize,
            maxFontSize);
    }

    /// <summary>
    /// Reduces the font size if the page-number text is too wide
    /// for the current page.
    /// </summary>
    private static double AdjustFontSizeToFit(
        XGraphics graphics,
        string text,
        XFont font,
        double fontSize,
        double pageWidth)
    {
        double minFontSize = PixelsToPoints(MinFontSizePixels);
        double horizontalMargin = fontSize * HorizontalMarginRatio;
        double availableWidth = pageWidth - horizontalMargin * 2;

        if (availableWidth <= 0)
        {
            return minFontSize;
        }

        XSize size = graphics.MeasureString(text, font);

        if (size.Width <= availableWidth)
        {
            return fontSize;
        }

        // Font width is approximately proportional to font size,
        // so calculate a first estimate instead of repeatedly
        // reducing the font one step at a time.
        double adjustedSize =
            fontSize * availableWidth / size.Width;

        return Math.Max(
            minFontSize,
            adjustedSize);
    }

    /// <summary>
    /// Converts a reference pixel size at 96 DPI to PDF points.
    /// </summary>
    private static double PixelsToPoints(double pixels)
    {
        return pixels * 72.0 / Dpi;
    }
}
