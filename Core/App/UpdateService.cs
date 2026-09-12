using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ownly.Core.App;

public sealed record UpdateCheckResult(bool Success, bool HasUpdate, string CurrentVersion, string? LatestVersion, string? DownloadUrl, string? Message);

/// <summary>
/// Checks the public GitHub release feed for a newer build than this one. Ownly ships as a single
/// preview .exe with no installer, so an update is a browser download of the latest release asset —
/// Ownly never replaces its own running executable.
/// </summary>
public static class UpdateService
{
    public const string CurrentVersion = "0.0.1";
    private const string ReleasesApi = "https://api.github.com/repos/OwnlyTeam/Ownly/releases/latest";
    public const string DownloadUrl = "https://github.com/OwnlyTeam/Ownly/releases/latest/download/Ownly.exe";

    public static async Task<UpdateCheckResult> CheckAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Ownly-App");
            client.Timeout = TimeSpan.FromSeconds(10);

            using var response = await client.GetAsync(ReleasesApi);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(false, false, CurrentVersion, null, null,
                    "Ownly could not reach GitHub to check for an update right now.");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var tag = doc.RootElement.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag))
            {
                return new UpdateCheckResult(false, false, CurrentVersion, null, null,
                    "Ownly could not read the latest release version.");
            }

            var latest = tag.TrimStart('v', 'V');
            var isNewer = TryParse(latest, out var latestVersion) && TryParse(CurrentVersion, out var current)
                && latestVersion > current;

            return isNewer
                ? new UpdateCheckResult(true, true, CurrentVersion, latest, DownloadUrl,
                    $"Ownly {latest} is available — you have {CurrentVersion}.")
                : new UpdateCheckResult(true, false, CurrentVersion, latest, null,
                    "You're on the latest version of Ownly.");
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, false, CurrentVersion, null, null,
                $"Ownly could not check for an update: {ex.Message}");
        }
    }

    private static bool TryParse(string value, out Version version) =>
        Version.TryParse(value, out version!) || Version.TryParse(value + ".0", out version!);
}
