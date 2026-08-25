//MergeResult.cs

using System.Collections.Generic;

namespace PDFMerger.Contracts;
/// <summary>
/// merge result information, used to report the result of merging multiple PDF files.
/// </summary>
public class MergeResult
{
    public bool Success { get; set; }
    public int TotalPages { get; set; }
    public string? OutputPath { get; set; }
    public MergeError? Error { get; set; }
    /// <summary>if ignoreDuplicates=true, list of duplicated files</summary>
    public List<string> DuplicatedFiles { get; set; } = new List<string>();
    /// <summary>list of actually merged files</summary>
    public List<string> MergedFiles { get; set; } = new List<string>();
    public IList<BookmarkEntry>? Bookmarks { get; set; }
}

public sealed class MergeError
{
    public MergeErrorCode Code { get; init; }
    public string? FilePath { get; init; }
    public long? FileSize { get; init; }
    public long? Limit { get; init; }
    public string TechnicalDetail { get; init; } = string.Empty;
}

public enum MergeErrorCode
{
    Unknown,
    FileNotFound,
    FileAccessDenied,
    InvalidPdf,
    EncryptedPdf,
    InputFileTooLarge,
    InvalidImage,
    UnsupportedFileType,
    OutputFileError,
    Cancelled
}
