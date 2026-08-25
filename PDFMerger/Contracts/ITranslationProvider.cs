namespace PDFMerger.Contracts;
public interface ITranslationProvider
{
    string GetString(string key, string? defaultValue = null);
}
