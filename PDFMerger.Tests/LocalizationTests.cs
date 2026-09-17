using System.Text.Json;

namespace PDFMerger.Tests;

public class LocalizationTests
{
    private const string LocaleDir = "I18n";

    private const string BaseLanguageFile = "zh-CN.json";

    public static IEnumerable<object[]> GetAllLocaleFiles()
    {
        foreach (var file in Directory.GetFiles(LocaleDir, "*.json"))
            yield return new object[] { file };
    }

    [Fact]
    public void AllLanguages_ShouldHaveSameKeys_AsBaseLanguage()
    {
        var baseKeys = LoadKeys(Path.Combine(LocaleDir, BaseLanguageFile));

        var allFiles = Directory.GetFiles(LocaleDir, "*.json");
        var missingReport = new List<string>();

        foreach (var file in allFiles)
        {
            var keys = LoadKeys(file);

            var missing = baseKeys.Except(keys).ToList();
            var extra = keys.Except(baseKeys).ToList();

            if (missing.Any())
                missingReport.Add($"[{Path.GetFileName(file)}] Missing: {string.Join(", ", missing)}");

            if (extra.Any())
                missingReport.Add($"[{Path.GetFileName(file)}] Extra: {string.Join(", ", extra)}");
        }

        Assert.True(missingReport.Count == 0,
            "Find different key:\n" + string.Join("\n", missingReport));
    }

    [Fact]
    public void PrintAllLanguagesKeyCoverage()
    {
        var baseKeys = LoadKeys(Path.Combine(LocaleDir, BaseLanguageFile));
        var report = new System.Text.StringBuilder();

        report.AppendLine($"Base Language Count: {baseKeys.Count}");
        report.AppendLine();

        foreach (var file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            var keys = LoadKeys(file);
            int missing = baseKeys.Except(keys).Count();
            int extra = keys.Except(baseKeys).Count();
            double coverage = keys.Count == 0 ? 0 : (double)(keys.Count - extra) / baseKeys.Count * 100;

            report.AppendLine($"{Path.GetFileName(file),-20} " +
                $"Coverage: {coverage:F1}%  " +
                $"Missing: {missing}  " +
                $"Extra: {extra}");
        }

        System.Diagnostics.Debug.WriteLine(report.ToString());
        Assert.True(true);
    }


    private static HashSet<string> LoadKeys(string path)
    {
        var json = File.ReadAllText(path);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .Where(k => k != "language" && k != "description" && k != "cultureName")
            .ToHashSet();
    }
}
