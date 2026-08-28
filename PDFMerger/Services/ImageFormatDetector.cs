// ImageFormatDetector.cs
using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Services;

public enum ImageFormat
{
    Unknown,

    // Common raster formats
    Jpeg,
    Png,
    Bmp,
    Gif,
    Tiff,
    WebP,

    // Modern image formats
    Avif,
    Heif,
    Heic,
    JpegXl,

    // Other raster/image container formats
    Ico,
    Tga,
    Psd,
    Qoi,

    // Vector image
    Svg
}

public sealed class ImageFormatInfo
{
    public ImageFormat Format { get; init; }
    public bool IsMultiFrameCandidate { get; init; }
    public bool IsRaster => Format switch
    {
        ImageFormat.Jpeg => true,
        ImageFormat.Png => true,
        ImageFormat.Bmp => true,
        ImageFormat.Gif => true,
        ImageFormat.Tiff => true,
        ImageFormat.WebP => true,
        ImageFormat.Avif => true,
        ImageFormat.Heif => true,
        ImageFormat.Heic => true,
        ImageFormat.JpegXl => true,
        ImageFormat.Ico => true,
        ImageFormat.Tga => true,
        ImageFormat.Psd => true,
        ImageFormat.Qoi => true,
        _ => false
    };

    public bool IsVector => Format switch
    {
        ImageFormat.Svg => true,
        _ => false
    };

    public bool IsSupported => Format != ImageFormat.Unknown;
    public bool IsMultiFrame => IsMultiFrameCandidate && IsRaster;
    public bool IsSingleFrame => !IsMultiFrameCandidate && IsRaster;
    public int FrameCount { get; internal set; }
}

public sealed class ImageFormatDetector
{
    public ImageFormatInfo Detect(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException(
                "Image path cannot be null or empty.",
                nameof(imagePath));
        }

        using var stream = new FileStream(
            imagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        return Detect(stream);
    }



    public ImageFormatInfo Detect(Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Image stream must be readable.", nameof(stream));

        long originalPosition = 0;

        if (stream.CanSeek)
        {
            originalPosition = stream.Position;
        }

        try
        {
            Span<byte> header = stackalloc byte[64];

            int bytesRead = ReadAtLeast(stream, header);
            ImageFormatInfo imageFormatInfo = DetectFormat(header[..bytesRead]);
            imageFormatInfo.FrameCount = GetFrameCount(stream);
            return imageFormatInfo;
        }
        finally
        {
            if (stream.CanSeek)
                stream.Position = originalPosition;
        }
    }

    private static ImageFormatInfo DetectFormat(ReadOnlySpan<byte> header)
    {
        // ------------------------------------------------------------
        // JPEG
        // FF D8 FF
        // ------------------------------------------------------------
        if (header.Length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return Info(
                ImageFormat.Jpeg,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // PNG
        // 89 50 4E 47 0D 0A 1A 0A
        // ------------------------------------------------------------
        if (StartsWith(
                header,
                0x89, 0x50, 0x4E, 0x47,
                0x0D, 0x0A, 0x1A, 0x0A))
        {
            return Info(
                ImageFormat.Png,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // GIF
        // GIF87a / GIF89a
        // ------------------------------------------------------------
        if (StartsWithAscii(header, "GIF87a") ||
            StartsWithAscii(header, "GIF89a"))
        {
            return Info(
                ImageFormat.Gif,
                multiFrame: true);
        }

        // ------------------------------------------------------------
        // BMP
        // BM
        // ------------------------------------------------------------
        if (StartsWith(
                header,
                0x42, 0x4D))
        {
            return Info(
                ImageFormat.Bmp,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // TIFF
        //
        // Little endian:
        // 49 49 2A 00
        //
        // Big endian:
        // 4D 4D 00 2A
        // ------------------------------------------------------------
        if (StartsWith(
                header,
                0x49, 0x49, 0x2A, 0x00) ||
            StartsWith(
                header,
                0x4D, 0x4D, 0x00, 0x2A))
        {
            return Info(
                ImageFormat.Tiff,
                multiFrame: true);
        }

        // BigTIFF
        //
        // Little endian:
        // 49 49 2B 00
        //
        // Big endian:
        // 4D 4D 00 2B
        if (StartsWith(
                header,
                0x49, 0x49, 0x2B, 0x00) ||
            StartsWith(
                header,
                0x4D, 0x4D, 0x00, 0x2B))
        {
            return Info(
                ImageFormat.Tiff,
                multiFrame: true);
        }

        // ------------------------------------------------------------
        // WebP
        //
        // RIFF .... WEBP
        //
        // 52 49 46 46 xx xx xx xx
        // 57 45 42 50
        // ------------------------------------------------------------
        if (header.Length >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
        {
            return Info(
                ImageFormat.WebP,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // JPEG XL
        //
        // Container format:
        // 00 00 00 0C 4A 58 4C 20 0D 0A 87 0A
        //
        // Codestream:
        // FF 0A
        // ------------------------------------------------------------
        if (StartsWith(
                header,
                0x00, 0x00, 0x00, 0x0C,
                0x4A, 0x58, 0x4C, 0x20,
                0x0D, 0x0A, 0x87, 0x0A) ||
            StartsWith(
                header,
                0xFF, 0x0A))
        {
            return Info(
                ImageFormat.JpegXl,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // QOI
        //
        // q o i f
        // ------------------------------------------------------------
        if (StartsWithAscii(header, "qoif"))
        {
            return Info(
                ImageFormat.Qoi,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // PSD
        //
        // 8BPS
        // ------------------------------------------------------------
        if (StartsWithAscii(header, "8BPS"))
        {
            return Info(
                ImageFormat.Psd,
                multiFrame: false);
        }

        // ------------------------------------------------------------
        // ICO / CUR
        //
        // Reserved = 0
        // Type = 1 (ICO) or 2 (CUR)
        // ------------------------------------------------------------
        if (header.Length >= 4 &&
            header[0] == 0x00 &&
            header[1] == 0x00 &&
            (header[2] == 0x01 || header[2] == 0x02) &&
            header[3] == 0x00)
        {
            return Info(
                ImageFormat.Ico,
                multiFrame: true);
        }

        // ------------------------------------------------------------
        // TGA
        //
        // TGA has no universal magic number.
        // Do not aggressively classify arbitrary files as TGA here.
        // It is intentionally handled as Unknown unless a stronger
        // signature is available.
        // ------------------------------------------------------------

        // ------------------------------------------------------------
        // ISO Base Media File Format
        //
        // HEIF / HEIC / AVIF use an ftyp box.
        //
        // Offset 4:
        // "ftyp"
        //
        // Offset 8:
        // brand such as:
        // heic
        // heix
        // hevc
        // hevx
        // mif1
        // msf1
        // avif
        // avis
        // ------------------------------------------------------------
        if (IsIsoBaseMediaFile(header))
        {
            ImageFormat format = DetectIsoBaseMediaFormat(header);

            if (format != ImageFormat.Unknown)
            {
                return Info(
                    format,
                    multiFrame: true);
            }
        }

        // ------------------------------------------------------------
        // SVG
        //
        // SVG has no fixed binary magic number.
        // XML declaration or <svg can appear at the beginning.
        //
        // This is deliberately conservative.
        // ------------------------------------------------------------
        if (LooksLikeSvg(header))
        {
            return Info(
                ImageFormat.Svg,
                multiFrame: false);
        }

        return Info(
            ImageFormat.Unknown,
            multiFrame: false);
    }

    private static ImageFormat DetectIsoBaseMediaFormat(
        ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
            return ImageFormat.Unknown;

        // ftyp must start at byte 4.
        if (!EqualsAscii(
                header.Slice(4, 4),
                "ftyp"))
        {
            return ImageFormat.Unknown;
        }

        // Major brand at offset 8.
        if (header.Length >= 12)
        {
            var brand = header.Slice(8, 4);

            if (EqualsAscii(brand, "avif") ||
                EqualsAscii(brand, "avis"))
            {
                return ImageFormat.Avif;
            }

            if (EqualsAscii(brand, "heic") ||
                EqualsAscii(brand, "heix") ||
                EqualsAscii(brand, "hevc") ||
                EqualsAscii(brand, "hevx"))
            {
                return ImageFormat.Heic;
            }

            if (EqualsAscii(brand, "mif1") ||
                EqualsAscii(brand, "msf1"))
            {
                // mif1/msf1 are HEIF container brands.
                return ImageFormat.Heif;
            }
        }

        // Also inspect compatible brands.
        for (int offset = 16;
             offset + 4 <= header.Length;
             offset += 4)
        {
            var brand = header.Slice(offset, 4);

            if (EqualsAscii(brand, "avif") ||
                EqualsAscii(brand, "avis"))
            {
                return ImageFormat.Avif;
            }

            if (EqualsAscii(brand, "heic") ||
                EqualsAscii(brand, "heix") ||
                EqualsAscii(brand, "hevc") ||
                EqualsAscii(brand, "hevx"))
            {
                return ImageFormat.Heic;
            }

            if (EqualsAscii(brand, "mif1") ||
                EqualsAscii(brand, "msf1"))
            {
                return ImageFormat.Heif;
            }
        }

        return ImageFormat.Unknown;
    }

    private static bool IsIsoBaseMediaFile(
        ReadOnlySpan<byte> header)
    {
        return header.Length >= 12 &&
               EqualsAscii(
                   header.Slice(4, 4),
                   "ftyp");
    }

    private static bool LooksLikeSvg(
        ReadOnlySpan<byte> header)
    {
        if (header.Length == 0)
            return false;

        string text;

        try
        {
            text = System.Text.Encoding.UTF8.GetString(header);
        }
        catch
        {
            return false;
        }

        text = text.TrimStart(
            '\uFEFF',
            ' ',
            '\t',
            '\r',
            '\n');

        if (text.StartsWith("<?xml",
                StringComparison.OrdinalIgnoreCase))
        {
            int svgIndex = text.IndexOf(
                "<svg",
                StringComparison.OrdinalIgnoreCase);

            return svgIndex >= 0;
        }

        return text.StartsWith(
            "<svg",
            StringComparison.OrdinalIgnoreCase);
    }

    private static ImageFormatInfo Info(ImageFormat format, bool multiFrame)
    {
        return new ImageFormatInfo
        {
            Format = format,
            IsMultiFrameCandidate = multiFrame,
        };
    }

    private static bool StartsWith(
        ReadOnlySpan<byte> data,
        params byte[] signature)
    {
        if (data.Length < signature.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (data[i] != signature[i])
                return false;
        }

        return true;
    }

    private static bool StartsWithAscii(
        ReadOnlySpan<byte> data,
        string text)
    {
        if (data.Length < text.Length)
            return false;

        return EqualsAscii(
            data[..text.Length],
            text);
    }

    private static bool EqualsAscii(
        ReadOnlySpan<byte> data,
        string text)
    {
        if (data.Length != text.Length)
            return false;

        for (int i = 0; i < text.Length; i++)
        {
            if (data[i] != (byte)text[i])
                return false;
        }

        return true;
    }

    private static int ReadAtLeast(Stream stream, Span<byte> buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = stream.Read(buffer[total..]);

            if (read == 0)
                break;

            total += read;
        }

        return total;
    }
    private int GetFrameCount(Stream imageStream)
    {
        try
        {
            if (imageStream.CanSeek) imageStream.Position = 0;
            using var image = Image.Load<Rgba32>(imageStream);

            return image.Frames.Count;
        }
        catch
        {
            return 0;
        }
    }
}
