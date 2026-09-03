using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace PDFMerger.Services
{
    public static class UpdateService
    {
        private const string RepoOwner = "359193585";
        private const string RepoName = "PDFMerger";

        public static async Task<string?> GetLatestVersionAsync()
        {
            try
            {
                var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(RepoName);
                return await GetLatestVersionAsync(client);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string?> GetLatestVersionAsync(HttpClient client)
        {
            try
            {
                var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("tag_name", out var tagElement)) return null;
                var tag = tagElement.GetString()?.TrimStart('v');
                if (!IsValidVersion(tag)) return null;

                return tag;
            }
            catch
            {
                return null;
            }
        }
        private static bool IsValidVersion(string? version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            var parts = version.Split('.');

            return parts.Length == 3 &&
                   int.TryParse(parts[0], out var major) &&
                   int.TryParse(parts[1], out var minor) &&
                   int.TryParse(parts[2], out var patch) &&
                   major >= 0 &&
                   minor >= 0 &&
                   patch >= 0;
        }
        public static string GetCurrentVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null) return "1.0.0";
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        public static int CompareVersions(string latest, string current)
        {
            var v1 = new Version(latest);
            var v2 = new Version(current);
            return v1.CompareTo(v2);
        }
       
        public static void OpenDownloadPage()
        {
            var url = $"https://github.com/{RepoOwner}/{RepoName}/releases/latest";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
