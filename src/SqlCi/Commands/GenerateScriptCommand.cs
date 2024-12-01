using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Helpers;

namespace SqlCi.Commands;

public sealed class GenerateScriptCommand : Command<GenerateScriptCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Generating Script...", ctx =>
            {
                // figure out which environment we should generate the script for
                var environment = settings.Environment?.ToLowerInvariant() ?? "all";

                // extract the script name if provided
                var scriptName = string.IsNullOrWhiteSpace(settings.ScriptName) ? string.Empty : $"_{settings.ScriptName}";

                // ensure that we have a configuration file so that we can verify the environment
                Configuration.Configuration.EnsureEnvironmentExists(environment);

                // generate the filename of the script
                var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssffffff}_{environment}{scriptName}.sql";

                // generate the file path of the script
                var filePath = $"{Path.Combine(Environment.CurrentDirectory, Globals.DeploymentDirectoryName, fileName)}";

                // write the script file to disk
                FileHelper.EnsureFileExists(filePath, () => string.Empty);
            });

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [Description("Name of the script to generate")]
        [CommandOption("-n|--name")]
        public required string ScriptName { get; init; }

        [Description("Environment to run the script in. Leave blank to run in all environments")]
        [CommandOption("-e|--env")]
        public string? Environment { get; init; }
    }
}