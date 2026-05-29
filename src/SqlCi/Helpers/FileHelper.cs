using Spectre.Console;

namespace SqlCi.Helpers;

internal static class FileHelper
{
    internal static void EnsureFileExists(string path, Func<string> writeContent)
    {
        AnsiConsole.MarkupLine($"Checking to see if {path} exists...");
        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[green3]Creating {path} ...[/]");
            File.WriteAllText(path, writeContent());
        }
        else
        {
            AnsiConsole.MarkupLine($"[red3]{path} already exists...[/]");
        }
    }
}