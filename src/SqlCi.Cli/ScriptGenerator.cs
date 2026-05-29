using Spectre.Console;
using System.Diagnostics;

namespace SqlCi.Cli;

public static class ScriptGenerator
{
    public static string GenerateScriptFile(string scriptsFolder, string environment, string scriptName)
    {
        Directory.CreateDirectory(scriptsFolder);

        // Use UTC for a monotonic, machine- and timezone-independent sequence prefix.
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var fileName = $"{timestamp}_{environment}_{scriptName}.sql";
        var fullPath = Path.Combine(scriptsFolder, fileName);

        // Create empty file
        File.WriteAllText(fullPath, "-- Baseline / new change script\n");

        return fullPath;
    }

    public static void OpenInEditor(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // Best effort - don't fail the command if editor can't be opened
            AnsiConsole.MarkupLineInterpolated($"[grey]Could not automatically open editor for {filePath}[/]");
        }
    }
}
