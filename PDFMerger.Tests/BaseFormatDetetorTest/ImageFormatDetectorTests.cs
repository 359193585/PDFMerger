using System.Text;
using PDFMerger.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PDFMerger.Tests.BaseFormatDetetorTest;


public class ImageFormatDetectorTests
{
    private readonly ImageFormatDetector _detector = new();

    // --------------------------------------------------------------------
    // JPEG
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_JpegHeader_ReturnsJpeg()
    {
        // Arrange
        byte[] data =
        {
            0xFF, 0xD8, 0xFF,
            0x00, 0x00, 0x00
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Jpeg, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
        Assert.False(result.IsVector);
    }

    // --------------------------------------------------------------------
    // PNG
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_PngHeader_ReturnsPng()
    {
        // Arrange
        byte[] data =
        {
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Png, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
        Assert.False(result.IsVector);
    }

    [Fact]
    public void Detect_ValidSingleFramePng_ReturnsFrameCountOne()
    {
        using var image = new Image<Rgba32>(10, 10);
        using var stream = new MemoryStream();

        image.SaveAsPng(stream);
        stream.Position = 0;

        var result = _detector.Detect(stream);

        Assert.Equal(ImageFormat.Png, result.Format);
        Assert.Equal(1, result.FrameCount);
    }
    // --------------------------------------------------------------------
    // GIF
    // --------------------------------------------------------------------

    [Theory]
    [InlineData("GIF87a")]
    [InlineData("GIF89a")]
    public void Detect_GifHeader_ReturnsGif(string signature)
    {
        // Arrange
        var data = CreateUtf8Data(signature);

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Gif, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
        Assert.False(result.IsVector);
    }
    [Fact]
    public void Detect_ValidMultiFrameGif_ReturnsCorrectFrameCount()
    {
        // Arrange
        using var image = new Image<Rgba32>(10, 10);

        image.Frames.AddFrame(image.Frames.RootFrame);
        image.Frames.AddFrame(image.Frames.RootFrame);

        using var stream = new MemoryStream();
        image.SaveAsGif(stream);
        stream.Position = 0;

        // Act
        var result = _detector.Detect(stream);

        // Assert
        Assert.Equal(ImageFormat.Gif, result.Format);
        Assert.Equal(3, result.FrameCount);
    }
    // --------------------------------------------------------------------
    // BMP
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_BmpHeader_ReturnsBmp()
    {
        // Arrange
        byte[] data =
        {
            0x42, 0x4D,
            0x00, 0x00,
            0x00, 0x00
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Bmp, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
        Assert.False(result.IsVector);
    }

    // --------------------------------------------------------------------
    // TIFF
    // --------------------------------------------------------------------

    [Theory]
    [InlineData(0x49, 0x49, 0x2A, 0x00)] // Little endian
    [InlineData(0x4D, 0x4D, 0x00, 0x2A)] // Big endian
    public void Detect_TiffHeader_ReturnsTiff(
        byte b0,
        byte b1,
        byte b2,
        byte b3)
    {
        // Arrange
        byte[] data =
        {
            b0, b1, b2, b3,
            0x00, 0x00
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Tiff, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
        Assert.False(result.IsVector);
    }

    // --------------------------------------------------------------------
    // BigTIFF
    // --------------------------------------------------------------------

    [Theory]
    [InlineData(0x49, 0x49, 0x2B, 0x00)] // Little endian
    [InlineData(0x4D, 0x4D, 0x00, 0x2B)] // Big endian
    public void Detect_BigTiffHeader_ReturnsTiff(
        byte b0,
        byte b1,
        byte b2,
        byte b3)
    {
        // Arrange
        byte[] data =
        {
            b0, b1, b2, b3,
            0x00, 0x00
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Tiff, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
    }

    // --------------------------------------------------------------------
    // WebP
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_WebPHeader_ReturnsWebP()
    {
        // Arrange
        byte[] data =
        {
            0x52, 0x49, 0x46, 0x46,
            0x00, 0x00, 0x00, 0x00,
            0x57, 0x45, 0x42, 0x50
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.WebP, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // JPEG XL
    // --------------------------------------------------------------------
    public static IEnumerable<object[]> JpegXlHeaders()
    {
        yield return new object[]
        {
        new byte[]
        {
            0x00, 0x00, 0x00, 0x0C,
            0x4A, 0x58, 0x4C, 0x20,
            0x0D, 0x0A, 0x87, 0x0A
        }
        };

        yield return new object[]
        {
        new byte[]
        {
            0xFF, 0x0A
        }
        };
    }
    [Theory]
    [MemberData(nameof(JpegXlHeaders))]
    public void Detect_JpegXlHeader_ReturnsJpegXl(byte[] signature)
    {
        // Act
        var result = _detector.Detect(new MemoryStream(signature));

        // Assert
        Assert.Equal(ImageFormat.JpegXl, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // QOI
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_QoiHeader_ReturnsQoi()
    {
        // Arrange
        var data = CreateUtf8Data("qoif");

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Qoi, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // PSD
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_PsdHeader_ReturnsPsd()
    {
        // Arrange
        var data = CreateUtf8Data("8BPS");

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Psd, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // ICO
    // --------------------------------------------------------------------

    [Theory]
    [InlineData(0x01)] // ICO
    [InlineData(0x02)] // CUR
    public void Detect_IcoOrCurHeader_ReturnsIco(byte type)
    {
        // Arrange
        byte[] data =
        {
            0x00, 0x00,
            type, 0x00
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Ico, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // AVIF
    // --------------------------------------------------------------------

    [Theory]
    [InlineData("avif")]
    [InlineData("avis")]
    public void Detect_AvifBrand_ReturnsAvif(string brand)
    {
        // Arrange
        var data = CreateIsoBaseMediaHeader(brand);

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Avif, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // HEIC
    // --------------------------------------------------------------------

    [Theory]
    [InlineData("heic")]
    [InlineData("heix")]
    [InlineData("hevc")]
    [InlineData("hevx")]
    public void Detect_HeicBrand_ReturnsHeic(string brand)
    {
        // Arrange
        var data = CreateIsoBaseMediaHeader(brand);

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Heic, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // HEIF
    // --------------------------------------------------------------------

    [Theory]
    [InlineData("mif1")]
    [InlineData("msf1")]
    public void Detect_HeifBrand_ReturnsHeif(string brand)
    {
        // Arrange
        var data = CreateIsoBaseMediaHeader(brand);

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Heif, result.Format);
        Assert.True(result.IsMultiFrameCandidate);
        Assert.True(result.IsRaster);
    }

    // --------------------------------------------------------------------
    // SVG
    // --------------------------------------------------------------------

    [Theory]
    [InlineData("<svg")]
    [InlineData("<?xml version=\"1.0\"?><svg")]
    [InlineData("\uFEFF<?xml version=\"1.0\"?><svg")]
    public void Detect_SvgHeader_ReturnsSvg(string content)
    {
        // Arrange
        var data = CreateUtf8Data(content);

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Svg, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.False(result.IsRaster);
        Assert.True(result.IsVector);
    }

    // --------------------------------------------------------------------
    // Unknown
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_UnknownHeader_ReturnsUnknown()
    {
        // Arrange
        byte[] data =
        {
            0x01, 0x02, 0x03, 0x04,
            0x05, 0x06, 0x07, 0x08
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Unknown, result.Format);
        Assert.False(result.IsMultiFrameCandidate);
        Assert.False(result.IsRaster);
        Assert.False(result.IsVector);
    }

    // --------------------------------------------------------------------
    // Stream behavior
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_SeekableStream_RestoresOriginalPosition()
    {
        // Arrange
        byte[] data =
        {
            0x00, 0x00,
            0xFF, 0xD8, 0xFF,
            0x00, 0x00
        };

        using var stream = new MemoryStream(data);

        stream.Position = 2;

        // Act
        var result = _detector.Detect(stream);

        // Assert
        Assert.Equal(ImageFormat.Jpeg, result.Format);
        Assert.Equal(2, stream.Position);
    }

    // --------------------------------------------------------------------
    // Invalid arguments
    // --------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_Path_InvalidPath_ThrowsArgumentException(
        string? imagePath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _detector.Detect(imagePath!));
    }

    [Fact]
    public void Detect_NullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => _detector.Detect((Stream)null!));
    }

    [Fact]
    public void Detect_NonReadableStream_ThrowsArgumentException()
    {
        // Arrange
        using var stream = new WriteOnlyStream();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => _detector.Detect(stream));
    }

    // --------------------------------------------------------------------
    // Short / empty streams
    // --------------------------------------------------------------------

    [Fact]
    public void Detect_EmptyStream_ReturnsUnknown()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var result = _detector.Detect(stream);

        // Assert
        Assert.Equal(ImageFormat.Unknown, result.Format);
    }

    [Fact]
    public void Detect_TooShortHeader_ReturnsUnknown()
    {
        // Arrange
        byte[] data =
        {
            0xFF,
            0xD8
        };

        // Act
        var result = _detector.Detect(new MemoryStream(data));

        // Assert
        Assert.Equal(ImageFormat.Unknown, result.Format);
    }

    // --------------------------------------------------------------------
    // Helpers
    // --------------------------------------------------------------------

    private static byte[] CreateUtf8Data(string content)
    {
        return Encoding.UTF8.GetBytes(content);
    }

    private static byte[] CreateIsoBaseMediaHeader(string brand)
    {
        var data = new byte[16];

        // Box size
        data[0] = 0x00;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x10;

        // "ftyp"
        data[4] = (byte)'f';
        data[5] = (byte)'t';
        data[6] = (byte)'y';
        data[7] = (byte)'p';

        // Major brand
        var brandBytes = Encoding.ASCII.GetBytes(brand);

        Array.Copy(
            brandBytes,
            0,
            data,
            8,
            Math.Min(4, brandBytes.Length));

        return data;
    }

    //短 header 不应该误判
    [Fact]
    public void Detect_TruncatedJpegXlHeader_ReturnsUnknown()
    {
        byte[] signature =
        {
        0xFF
    };

        using var stream = new MemoryStream(signature);

        var result = _detector.Detect(stream);

        Assert.Equal(ImageFormat.Unknown, result.Format);
    }

    /// <summary>
    /// Minimal non-readable stream used to verify argument validation.
    /// </summary>
    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
