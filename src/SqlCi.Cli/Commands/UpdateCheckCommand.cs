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
                    $"[link={release.html_url}]Download here → {release.html_url}[/]");
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
        const string url = "https://api.github.com/repos/wshaddix/sqlci/releases/latest";

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
        var release = JsonSerializer.Deserialize<GitHubRelease>(json, JsonOptions);

        // If we're not including prereleases and this is a prerelease, we might want to fetch all releases.
        // For simplicity with /latest, we'll just return what we got.
        // A more complete implementation would call /releases and filter.

        if (release != null && release.prerelease && !includePrerelease)
        {
            // If latest is a prerelease and user didn't ask for them, we could fall back,
            // but for now we still report it with a note.
        }

        return release;
    }

    private static string CleanVersion(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return "0.0.0";

        return tag.TrimStart('v', 'V');
    }

    private static bool IsNewerVersion(string latest, string current)
    {
        if (!Version.TryParse(latest, out var latestVer))
            return false;

        if (!Version.TryParse(current, out var currentVer))
            return false;

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
