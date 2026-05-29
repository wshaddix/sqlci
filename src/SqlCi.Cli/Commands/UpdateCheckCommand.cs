using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SqlCi.Cli.Commands;

public sealed class UpdateCheckCommand : AsyncCommand<UpdateCheckCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("--prerelease")]
        [Description("Include prerelease versions when checking for updates.")]
        [DefaultValue(false)]
        public bool IncludePrerelease { get; set; }
    }

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[dim]Checking for updates...[/]");

        try
        {
            var release = await GetLatestReleaseAsync(settings.IncludePrerelease);

            if (release == null)
            {
                AnsiConsole.MarkupLine("[yellow]Could not retrieve the latest release information from GitHub.[/]");
                return 0;
            }

            var latestVersion = CleanVersion(release.tag_name);
            var currentVersion = AppVersion.Current;

            if (IsNewerVersion(latestVersion, currentVersion))
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[green]A new version is available![/] Current: [bold]{currentVersion}[/], Latest: [bold]{latestVersion}[/]");
                AnsiConsole.MarkupLineInterpolated(
                    $"[link={release.html_url}]Download here:    {release.html_url}[/]");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[green]You're running the latest version.[/] (v{currentVersion})");
            }

            return 0;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Unable to check for updates (network error or timeout).[/]");
            AnsiConsole.MarkupLine("[dim]You can manually check at: https://github.com/wshaddix/sqlci/releases[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Unexpected error while checking for updates:[/] {ex.Message}");
            return 0;
        }
    }

    private static async Task<GitHubRelease?> GetLatestReleaseAsync(bool includePrerelease)
    {
        // We use the full releases list instead of /releases/latest because the latter
        // never returns prereleases, even when the user explicitly asks for them.
        string url = "https://api.github.com/repos/wshaddix/sqlci/releases?per_page=100";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("sqlci", AppVersion.Current));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await HttpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // No releases yet
            return null;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, JsonOptions) ?? new List<GitHubRelease>();

        var candidates = releases
            .Where(r => includePrerelease || !r.prerelease)
            .OrderByDescending(r => r.tag_name, Comparer<string>.Create(CompareReleaseTags))
            .ToList();

        return candidates.FirstOrDefault();
    }

    private static int CompareReleaseTags(string tagA, string tagB)
    {
        string keyA = GetSortableVersionKey(tagA);
        string keyB = GetSortableVersionKey(tagB);

        if (Version.TryParse(keyA, out var va) && Version.TryParse(keyB, out var vb))
        {
            return va.CompareTo(vb);
        }

        // Fallback to string comparison if parsing fails
        return string.Compare(keyA, keyB, StringComparison.Ordinal);
    }

    private static string GetSortableVersionKey(string tag)
    {
        string v = CleanVersion(tag);

        // Strip pre-release (-beta.4, -rc.1, etc.) and build metadata (+sha)
        int dash = v.IndexOf('-');
        if (dash >= 0) v = v[..dash];

        int plus = v.IndexOf('+');
        if (plus >= 0) v = v[..plus];

        return v;
    }

    private static string CleanVersion(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return "0.0.0";

        return tag.TrimStart('v', 'V');
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        string latestKey = GetSortableVersionKey(latest);
        string currentKey = GetSortableVersionKey(current);

        if (!Version.TryParse(latestKey, out var latestVer) ||
            !Version.TryParse(currentKey, out var currentVer))
        {
            return false;
        }

        return latestVer > currentVer;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record GitHubRelease(
        string tag_name,
        string html_url,
        string name,
        bool prerelease,
        string published_at
    );
}
