namespace PDFMerger.Models;

public sealed class FileInspectionResult
{
    public string FileName { get; init; } = string.Empty;
    public bool IsSupported { get; set; }

    public FileType Type { get; init; }

    public int PageCount { get; set; }

    public long FileSize { get; init; }

    public string Author { get; set; } = string.Empty;

    public bool IsEncrypted { get; set; }
    public string? Password { get; set; } = null;

    public string? ErrorCode { get; set; }
}
