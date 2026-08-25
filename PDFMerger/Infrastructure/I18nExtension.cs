//I18nExtension.cs
using System;
using Avalonia.Markup.Xaml;

namespace PDFMerger.Infrastructure;

public class I18nExtension : MarkupExtension
{
    public string Key { get; set; }

    public I18nExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return I18n.GetString(Key);
    }
}
