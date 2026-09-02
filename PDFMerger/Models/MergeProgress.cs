//MergeProgress.cs
namespace PDFMerger.Models;
/// <summary>
/// merge progress information, used to report the progress of merging multiple PDF files.
/// </summary>
public class MergeProgress
{
    public int FileIndex { get; init; }
    public int TotalFiles { get; init; }
    public string? FileName { get; init; }
    public int PageCount { get; init; }
    public int TotalPagesProcessed { get; init; }
    public bool IsComplete { get; init; }
    public double PercentComplete => TotalFiles > 0
        ? (double)FileIndex / TotalFiles * 100 : 0;
}


