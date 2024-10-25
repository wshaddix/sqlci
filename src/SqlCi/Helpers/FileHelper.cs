using Spectre.Console;

namespace SqlCi.Helpers;

internal static class FileHelper
{
    internal static void EnsureFileExists(string name, string path, Func<string> writeContent)
    {
        AnsiConsole.MarkupLine($"Checking to see if {name} file exists...");
        if(!File.Exists(path))
        {
            AnsiConsole.MarkupLine($"[green3]Creating {name} file...[/]");
            File.WriteAllText(path, writeContent());
        }
        else
        {
            AnsiConsole.MarkupLine($"[red3]{name} file already exists...[/]");
        }
    }
}