using PDFMerger.Contracts;

namespace PDFMerger.Infrastructure;
public class I18nTranslationProvider : ITranslationProvider
{
    public string GetString(string key, string? defaultValue = null)
    {
        var value = I18n.GetString(key);
        return string.IsNullOrEmpty(value) ? (defaultValue ?? key) : value;
    }
}
