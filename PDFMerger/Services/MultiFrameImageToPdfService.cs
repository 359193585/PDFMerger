// MultiFrameImageToPdfService.cs
using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace PDFMerger.Services
{
    /// <summary>
    /// Converts ImageSharp images, including multi-frame images,
    /// into pages of a PdfSharp PDF document.
    ///
    /// This service is responsible only for bridging ImageSharp image
    /// frames to PdfSharp. PDF page layout decisions such as A4,
    /// FitImage, or Custom are handled by the caller.
    /// </summary>
    public sealed class MultiFrameImageToPdfService
    {

        /// <summary>
        /// Adds all frames of an ImageSharp image to a PdfSharp document.
        ///
        /// Each frame becomes one PDF page.
        /// </summary>
        /// <param name="image">
        /// The ImageSharp image. It may contain one or multiple frames.
        /// </param>
        /// <param name="targetDocument">
        /// The PdfSharp document receiving the generated pages.
        /// </param>
        /// <param name="pageWidth">
        /// PDF page width in points.
        /// </param>
        /// <param name="pageHeight">
        /// PDF page height in points.
        /// </param>
        public void AddImagePagesToDocument(Image image, PdfDocument targetDocument, double pageWidth, double pageHeight)
        {
            ArgumentNullException.ThrowIfNull(image);
            ArgumentNullException.ThrowIfNull(targetDocument);

            ValidatePageSize(pageWidth, pageHeight);

            for (int frameIndex = 0; frameIndex < image.Frames.Count; frameIndex++)
            {
                using var frameImage = image.Frames.CloneFrame(frameIndex);

                AddSingleImagePage(
                    frameImage,
                    targetDocument,
                    pageWidth,
                    pageHeight);
            }
        }

        /// <summary>
        /// Creates a PNG representation of one ImageSharp image.
        ///
        /// The returned stream is positioned at the beginning and remains
        /// owned by the caller.
        /// </summary>
        public MemoryStream ConvertImageToPngStream(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            var stream = new MemoryStream();

            image.Save(stream, new PngEncoder());

            stream.Position = 0;

            return stream;
        }

        private void AddSingleImagePage(Image image, PdfDocument targetDocument, double pageWidth, double pageHeight)
        {
            using var imageStream = ConvertImageToPngStream(image);
            using var xImage = XImage.FromStream(imageStream);

            var page = targetDocument.AddPage();

            page.Width = XUnit.FromPoint(pageWidth);
            page.Height = XUnit.FromPoint(pageHeight);

            DrawImageCentered(
                page,
                xImage,
                pageWidth,
                pageHeight);
        }

        private static void DrawImageCentered(PdfPage page, XImage image, double pageWidth, double pageHeight)
        {
            if (image.PointWidth <= 0 || image.PointHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Image has invalid dimensions.");
            }

            double scaleX = pageWidth / image.PointWidth;
            double scaleY = pageHeight / image.PointHeight;
            double scale = Math.Min(scaleX, scaleY);

            double drawWidth = image.PointWidth * scale;
            double drawHeight = image.PointHeight * scale;

            double x = (pageWidth - drawWidth) / 2.0;
            double y = (pageHeight - drawHeight) / 2.0;

            using var graphics = XGraphics.FromPdfPage(page);

            graphics.DrawImage(
                image,
                x,
                y,
                drawWidth,
                drawHeight);
        }

        private static void ValidatePageSize(double pageWidth, double pageHeight)
        {
            if (double.IsNaN(pageWidth) ||
                double.IsInfinity(pageWidth) ||
                pageWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageWidth),
                    "Page width must be a finite value greater than zero.");
            }

            if (double.IsNaN(pageHeight) ||
                double.IsInfinity(pageHeight) ||
                pageHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageHeight),
                    "Page height must be a finite value greater than zero.");
            }
        }

        /// <summary>
        /// Gets the number of frames contained in an ImageSharp image.
        /// </summary>
        public int GetFrameCount(Image image)
        {
            ArgumentNullException.ThrowIfNull(image);

            return image.Frames.Count;
        }
    }
}
