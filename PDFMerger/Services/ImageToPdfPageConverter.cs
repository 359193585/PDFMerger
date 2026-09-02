// ImageToPdfPageConverter.cs

using System;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PDFMerger.Services
{
    /// <summary>
    /// Converts images into PDF pages.
    /// Supports multiple input sources and page size modes.
    /// </summary>
    public class ImageToPdfPageConverter
    {
        public enum PageSizeMode
        {
            FitImage,
            A4,
            Custom
        }

        // Keep the original maximum dimension limitation for fallback normalization.
        private const int MAX_IMAGE_DIMENSION = 2560;

        private readonly MultiFrameImageToPdfService _multiFrameImageToPdfService;

        public PageSizeMode DefaultMode { get; set; } = PageSizeMode.FitImage;

        public double? DefaultCustomWidth { get; set; }

        public double? DefaultCustomHeight { get; set; }

        public ImageToPdfPageConverter(
            PageSizeMode defaultMode = PageSizeMode.FitImage,
            double? defaultCustomWidth = null,
            double? defaultCustomHeight = null)
        {
            DefaultMode = defaultMode;
            DefaultCustomWidth = defaultCustomWidth;
            DefaultCustomHeight = defaultCustomHeight;

            _multiFrameImageToPdfService = new MultiFrameImageToPdfService();
        }

        public PdfDocument ConvertImageToPdfDocument(string imagePath)
            => ConvertImageToPdfDocument(
                imagePath,
                DefaultMode,
                DefaultCustomWidth,
                DefaultCustomHeight);

        public PdfDocument ConvertImageToPdfDocument(
            string imagePath,
            PageSizeMode mode,
            double? customWidth = null,
            double? customHeight = null)
        {
            if (!File.Exists(imagePath)) throw new FileNotFoundException("Image file not found", imagePath);
            var doc = new PdfDocument();
            AddImagePageToDocument(imagePath, doc, mode, customWidth, customHeight);
            return doc;
        }

        public PdfDocument ConvertImageToPdfDocument(byte[] imageData)
            => ConvertImageToPdfDocument(
                imageData,
                DefaultMode,
                DefaultCustomWidth,
                DefaultCustomHeight);

        public PdfDocument ConvertImageToPdfDocument(
            byte[] imageData,
            PageSizeMode mode,
            double? customWidth = null,
            double? customHeight = null)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            using var ms = new MemoryStream(imageData);
            return ConvertImageToPdfDocument(
                ms,
                mode,
                customWidth,
                customHeight);
        }
        public PdfDocument ConvertImageToPdfDocument(Stream imageStream)
            => ConvertImageToPdfDocument(
                imageStream,
                DefaultMode,
                DefaultCustomWidth,
                DefaultCustomHeight);

        public PdfDocument ConvertImageToPdfDocument(
            Stream imageStream,
            PageSizeMode mode,
            double? customWidth = null,
            double? customHeight = null)
        {
            var targetDoc = new PdfDocument();
            AddImageStreamToDocument(imageStream, targetDoc, mode, customWidth, customHeight);
            return targetDoc;
        }


        public int AddImagePageToDocument(
            string imagePath,
            PdfDocument targetDoc,
            PageSizeMode mode = PageSizeMode.FitImage,
            double? customWidth = null,
            double? customHeight = null)
        {
            ArgumentNullException.ThrowIfNull(targetDoc);
            if (!File.Exists(imagePath)) throw new FileNotFoundException("Image file not found", imagePath);

            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            int addedPages = AddImageStreamToDocument(stream, targetDoc, mode, customWidth, customHeight);
            return addedPages;
        }

        public int AddImageStreamToDocument(
            Stream imageStream,
            PdfDocument targetDoc,
            PageSizeMode mode,
            double? customWidth = null,
            double? customHeight = null)
        {
            ArgumentNullException.ThrowIfNull(targetDoc);
            if (imageStream == null || !imageStream.CanRead)
                throw new ArgumentException("Invalid image stream", nameof(imageStream));

            using var image = LoadImageSharpImage(imageStream);
            if (image.Frames.Count > 1)
            {
                AddMultiFrameImageToDocument(image, targetDoc, mode, customWidth, customHeight);
                return image.Frames.Count;
            }


            // For single-frame images, we can use PdfSharp's XImage directly.
            XImage? xImage = null;
            MemoryStream? normalizedStream = null;
            try
            {
                if (imageStream.CanSeek) imageStream.Position = 0;
                xImage = XImage.FromStream(imageStream);
            }
            catch (Exception ex)
            {
                try
                {
                    // if XImage.FromStream fails, we normalize the image and convert it to PNG format.
                    normalizedStream = NormalizeImageToPng(image);
                    xImage = XImage.FromStream(normalizedStream);
                }
                catch (Exception fallbackEx)
                {
                    throw new InvalidOperationException(
                        "Failed to load the image directly and also failed after normalization to PNG.",
                        new AggregateException(ex, fallbackEx));
                }
            }

            using (xImage)
            using (normalizedStream)
            {
                var pageSize = ResolvePageSize(xImage, mode, customWidth, customHeight);
                AddSingleImagePage(xImage, targetDoc, pageSize.width, pageSize.height);
            }

            return image.Frames.Count; 
        }

        private void AddMultiFrameImageToDocument(
           Image image,
           PdfDocument targetDoc,
           PageSizeMode mode,
           double? customWidth,
           double? customHeight)
        {
            var (pageWidth, pageHeight) = ResolvePageSize(image, mode, customWidth, customHeight);
            _multiFrameImageToPdfService.AddImagePagesToDocument(
                image,
                targetDoc,
                pageWidth,
                pageHeight);
        }

        private static Image LoadImageSharpImage(Stream imageStream)
        {
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            return Image.Load<Rgba32>(imageStream);
        }

        private static (double width, double height) ResolvePageSize(
            XImage image,
            PageSizeMode mode,
            double? customWidth,
            double? customHeight)
        {
            switch (mode)
            {
                case PageSizeMode.FitImage:
                    return (
                        image.PointWidth,
                        image.PointHeight);

                case PageSizeMode.A4:
                    return (
                        595.0,
                        842.0);

                case PageSizeMode.Custom:
                    ValidateCustomPageSize(
                        customWidth,
                        customHeight);

                    return (
                        customWidth!.Value,
                        customHeight!.Value);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        "Unsupported page size mode.");
            }
        }

        private static (double width, double height) ResolvePageSize(
            Image image,
            PageSizeMode mode,
            double? customWidth,
            double? customHeight)
        {
            switch (mode)
            {
                case PageSizeMode.FitImage:
                    return GetImagePhysicalSizeInPoints(image);

                case PageSizeMode.A4:
                    return (
                        595.0,
                        842.0);

                case PageSizeMode.Custom:
                    ValidateCustomPageSize(
                        customWidth,
                        customHeight);

                    return (
                        customWidth!.Value,
                        customHeight!.Value);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        "Unsupported page size mode.");
            }
        }

        private static void ValidateCustomPageSize(
            double? customWidth,
            double? customHeight)
        {
            if (!customWidth.HasValue ||
                !customHeight.HasValue)
            {
                throw new ArgumentException(
                    "Custom mode requires specifying customWidth and customHeight.");
            }

            ValidatePageDimension(
                customWidth.Value,
                nameof(customWidth));

            ValidatePageDimension(
                customHeight.Value,
                nameof(customHeight));
        }

        private static void ValidatePageDimension(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Page dimension must be a finite value greater than zero.");
            }
        }

        private static (double width, double height) GetImagePhysicalSizeInPoints(Image image)
        {
            const double pointsPerInch = 72.0;

            /*
             * ImageSharp normally provides the image resolution.
             *
             * 96 DPI is used as the fallback when resolution metadata
             * is missing or invalid.
             */
            double horizontalDpi = image.Metadata.HorizontalResolution;

            double verticalDpi = image.Metadata.VerticalResolution;

            if (horizontalDpi <= 0 ||
                double.IsNaN(horizontalDpi) ||
                double.IsInfinity(horizontalDpi))
            {
                horizontalDpi = 96.0;
            }

            if (verticalDpi <= 0 ||
                double.IsNaN(verticalDpi) ||
                double.IsInfinity(verticalDpi))
            {
                verticalDpi = 96.0;
            }

            double width = image.Width / horizontalDpi * pointsPerInch;

            double height = image.Height / verticalDpi * pointsPerInch;

            ValidatePageDimension(width, nameof(width));

            ValidatePageDimension(height, nameof(height));

            return (width, height);
        }

        private static void AddSingleImagePage(
            XImage image,
            PdfDocument targetDoc,
            double pageWidth,
            double pageHeight)
        {
            if (image.PointWidth <= 0 ||
                image.PointHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Image has invalid dimensions.");
            }

            ValidatePageDimension(
                pageWidth,
                nameof(pageWidth));

            ValidatePageDimension(
                pageHeight,
                nameof(pageHeight));

            PdfPage page = targetDoc.AddPage();

            page.Width =
                XUnit.FromPoint(pageWidth);

            page.Height =
                XUnit.FromPoint(pageHeight);

            double scaleX =
                pageWidth / image.PointWidth;

            double scaleY =
                pageHeight / image.PointHeight;

            double scale =
                Math.Min(scaleX, scaleY);

            double drawWidth =
                image.PointWidth * scale;

            double drawHeight =
                image.PointHeight * scale;

            double x =
                (pageWidth - drawWidth) / 2.0;

            double y =
                (pageHeight - drawHeight) / 2.0;

            using var gfx =
                XGraphics.FromPdfPage(page);

            gfx.DrawImage(
                image,
                x,
                y,
                drawWidth,
                drawHeight);
        }

        private static MemoryStream NormalizeImageToPng(Image image)
        {
            // Keep the original EXIF orientation handling.
            image.Mutate(x => x.AutoOrient());

            // Keep the original maximum dimension limitation.
            if (image.Width > MAX_IMAGE_DIMENSION || image.Height > MAX_IMAGE_DIMENSION)
            {
                image.Mutate(x =>
                    x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(
                            MAX_IMAGE_DIMENSION,
                            MAX_IMAGE_DIMENSION)
                    }));
            }

            var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            stream.Position = 0;
            return stream;
        }

    }
}
