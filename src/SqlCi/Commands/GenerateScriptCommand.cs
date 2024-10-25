using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Helpers;

namespace SqlCi.Commands;

public sealed class GenerateScriptCommand : Command<GenerateScriptCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Name of the script to generate")]
        [CommandOption("-n|--name")]
        public required string ScriptName { get; init; }
        
        [Description("Environment to run the script in. Leave blank to run in all environments")]
        [CommandOption("-e|--env")]
        public string? Environment { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Generating Script...", ctx =>
            {
                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssffffff}_{settings.Environment ?? "all"}_{settings.ScriptName}.sql";
                var filePath = $"{Path.Combine(Environment.CurrentDirectory, Globals.DeploymentDirectoryName, fileName)}";
                
                FileHelper.EnsureFileExists(fileName, filePath, () => string.Empty);
            });
           
        return 0;
    }
}