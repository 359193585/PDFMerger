namespace PDFMerger.Models;

public sealed class FileInspectionResult
{
    public string FileName { get; init; } = string.Empty;
    public bool IsSupported { get; set; }

    public FileType Type { get; init; }

    public int PageCount { get; init; }

    public long FileSize { get; init; }

    public string Author { get; init; } = string.Empty;

    public bool IsEncrypted { get; init; }
    public string? Password { get; set; } = null;

    public string? ErrorCode { get; init; }
}
