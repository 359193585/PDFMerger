using System;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PDFMerger.Services;
public sealed class PdfPageNumberService
{
    private const double MinFontSize = 8;
    private const double MaxFontSize = 48;
    private const double FontSizeRatio = 0.035;

    // Distance from the bottom edge, relative to font size.
    private const double BottomMarginRatio = 1.5;

    // Minimum horizontal safe margin, relative to font size.
    private const double HorizontalMarginRatio = 1.0;

    // Underline dimensions.
    private const double UnderlineThicknessRatio = 0.08;
    private const double UnderlineOffsetRatio = 0.15;

    // Pill dimensions.
    private const double PillHorizontalPaddingRatio = 0.65;
    private const double PillVerticalPaddingRatio = 0.35;

    private const string FontName = "Helvetica";

    public void AddPageNumbers(PdfDocument document)
    {
        AddPageNumbers(document, PageNumberStyle.Plain, XColors.Black);
    }

    public void AddPageNumbers(PdfDocument document, PageNumberStyle style, XColor color)
    {
        ArgumentNullException.ThrowIfNull(document);

        int totalPages = document.PageCount;
        if (totalPages == 0) return;

        for (int i = 0; i < totalPages; i++)
        {
            PdfPage page = document.Pages[i];

            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;

            if (pageWidth <= 0 || pageHeight <= 0) continue;

            string text = $"{i + 1} / {totalPages}";

            using XGraphics gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

            double fontSize = CalculateFontSize(page);

            XFont font = new XFont(FontName, fontSize, XFontStyleEx.Regular);

            // Ensure the complete page-number element fits within the page.
            fontSize = AdjustFontSizeToFit(gfx, text, font, fontSize, pageWidth, style);

            if (Math.Abs(fontSize - font.Size) > 0.01)
            {
                font = new XFont(FontName, fontSize, XFontStyleEx.Regular);
            }

            XSize textSize = gfx.MeasureString(text, font);

            PageNumberLayout layout = CalculateLayout(
                pageWidth,
                pageHeight,
                textSize,
                fontSize,
                style);

            DrawPageNumber(
                gfx,
                text,
                font,
                color,
                style,
                layout);
        }
    }

    private static double CalculateFontSize(PdfPage page)
    {
        double shortSide = Math.Min(page.Width.Point, page.Height.Point);
        double fontSize = shortSide * FontSizeRatio;

        return Math.Clamp(fontSize, MinFontSize, MaxFontSize);
    }

    private static double AdjustFontSizeToFit(
        XGraphics graphics,
        string text,
        XFont font,
        double fontSize,
        double pageWidth,
        PageNumberStyle style)
    {
        XSize textSize = graphics.MeasureString(text, font);

        double horizontalMargin = fontSize * HorizontalMarginRatio;

        double requiredWidth = CalculateRequiredWidth(
            textSize.Width,
            fontSize,
            style);

        double availableWidth = Math.Max(
            0,
            pageWidth - horizontalMargin * 2);

        if (requiredWidth <= availableWidth) return fontSize;

        double scale = availableWidth / requiredWidth;

        return Math.Max(MinFontSize, fontSize * scale);
    }

    private static double CalculateRequiredWidth(
        double textWidth,
        double fontSize,
        PageNumberStyle style)
    {
        return style switch
        {
            PageNumberStyle.Plain =>
                textWidth,

            PageNumberStyle.Underline =>
                textWidth,

            PageNumberStyle.Pill =>
                textWidth + fontSize * PillHorizontalPaddingRatio * 2,

            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }

    private static PageNumberLayout CalculateLayout(
        double pageWidth,
        double pageHeight,
        XSize textSize,
        double fontSize,
        PageNumberStyle style)
    {
        double horizontalMargin = fontSize * HorizontalMarginRatio;

        double elementWidth = CalculateRequiredWidth(
            textSize.Width,
            fontSize,
            style);

        double elementHeight = CalculateElementHeight(
            textSize.Height,
            fontSize,
            style);

        // Keep the entire element horizontally inside the page.
        double elementX = (pageWidth - elementWidth) / 2;

        elementX = Math.Clamp(
            elementX,
            0,
            Math.Max(0, pageWidth - elementWidth));

        // Leave a proportional distance from the bottom.
        double bottomMargin = fontSize * BottomMarginRatio;

        double elementY = pageHeight - bottomMargin - elementHeight;

        // If the page is too small, move the element as close
        // to the bottom edge as possible without clipping it.
        elementY = Math.Clamp(
            elementY,
            0,
            Math.Max(0, pageHeight - elementHeight));

        double textX = elementX;

        double textY = elementY + textSize.Height;

        if (style == PageNumberStyle.Pill)
        {
            double paddingX = fontSize * PillHorizontalPaddingRatio;
            double paddingY = fontSize * PillVerticalPaddingRatio;

            textX = elementX + paddingX;
            textY = elementY + paddingY + textSize.Height;
        }

        return new PageNumberLayout
        {
            ElementX = elementX,
            ElementY = elementY,
            ElementWidth = elementWidth,
            ElementHeight = elementHeight,
            TextX = textX,
            TextY = textY
        };
    }

    private static double CalculateElementHeight(
        double textHeight,
        double fontSize,
        PageNumberStyle style)
    {
        return style switch
        {
            PageNumberStyle.Plain =>
                textHeight,

            PageNumberStyle.Underline =>
                textHeight + fontSize * UnderlineOffsetRatio,

            PageNumberStyle.Pill =>
                textHeight + fontSize * PillVerticalPaddingRatio * 2,

            _ => throw new ArgumentOutOfRangeException(nameof(style), style, null)
        };
    }

    private static void DrawPageNumber(
        XGraphics graphics,
        string text,
        XFont font,
        XColor color,
        PageNumberStyle style,
        PageNumberLayout layout)
    {
        switch (style)
        {
            case PageNumberStyle.Plain:
                graphics.DrawString(
                    text,
                    font,
                    XBrushes.Black,
                    layout.TextX,
                    layout.TextY);
                break;

            case PageNumberStyle.Underline:
                graphics.DrawString(
                    text,
                    font,
                    XBrushes.Black,
                    layout.TextX,
                    layout.TextY);

                double underlineThickness = Math.Max(
                    0.8,
                    font.Size * UnderlineThicknessRatio);

                double underlineY =
                    layout.TextY + font.Size * UnderlineOffsetRatio;

                var pen = new XPen(color, underlineThickness);

                graphics.DrawLine(
                    pen,
                    layout.TextX,
                    underlineY,
                    layout.TextX + layout.ElementWidth,
                    underlineY);

                break;

            case PageNumberStyle.Pill:
                double radius = layout.ElementHeight / 2;

                var brush = new XSolidBrush(color);

                graphics.DrawRoundedRectangle(
                    brush,
                    layout.ElementX,
                    layout.ElementY,
                    layout.ElementWidth,
                    layout.ElementHeight,
                    radius,
                    radius);

                graphics.DrawString(
                    text,
                    font,
                    XBrushes.Black,
                    layout.TextX,
                    layout.TextY);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(style), style, null);
        }
    }

    private sealed class PageNumberLayout
    {
        public double ElementX { get; init; }
        public double ElementY { get; init; }
        public double ElementWidth { get; init; }
        public double ElementHeight { get; init; }
        public double TextX { get; init; }
        public double TextY { get; init; }
    }
    public enum PageNumberStyle
    {
        Plain,
        Underline,
        Pill
    }
}
