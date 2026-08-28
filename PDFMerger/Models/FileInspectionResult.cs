namespace PDFMerger.Models;

public sealed class FileInspectionResult
{
    public bool IsSupported { get; init; }

    public FileType Type { get; init; }

    public int PageCount { get; init; }

    public long FileSize { get; init; }

    public string Author { get; init; } = string.Empty;

    public bool IsEncrypted { get; init; }

    public string? ErrorCode { get; init; }
}
