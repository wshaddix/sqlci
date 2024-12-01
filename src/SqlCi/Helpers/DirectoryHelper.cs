using Spectre.Console;

namespace SqlCi.Helpers;

internal static class DirectoryHelper
{
    internal static void EnsureDirectoryExists(string name, string path)
    {
        AnsiConsole.MarkupLine($"Checking to see if {name} directory exists...");
        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[green3]Creating {name} directory...[/]");
            Directory.CreateDirectory(path);
        }
        else
        {
            AnsiConsole.MarkupLine($"[red3]{name} directory already exists...[/]");
        }
    }
}