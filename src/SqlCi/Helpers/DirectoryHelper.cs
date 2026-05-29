using Spectre.Console;

namespace SqlCi.Helpers;

internal static class DirectoryHelper
{
    internal static void EnsureDirectoryExists(string path)
    {
        AnsiConsole.MarkupLine($"Checking to see if {path} directory exists...");
        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[green3]Creating {path} directory...[/]");
            Directory.CreateDirectory(path);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red3]{path} directory already exists...[/]");
        }
    }
}