using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace SqlCi.Cli.Commands;

public sealed class GenerateCommand : Command<GenerateCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<ENVIRONMENT>")]
        [Description("Environment name (used in filename and for _env_ filtering)")]
        public string Environment { get; set; } = string.Empty;

        [CommandArgument(1, "<SCRIPT_NAME>")]
        [Description("Descriptive name for the script (e.g. add_users_table)")]
        public string ScriptName { get; set; } = string.Empty;
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var config = ConfigLoader.Load();

            var fullPath = ScriptGenerator.GenerateScriptFile(
                config.ScriptsFolder,
                settings.Environment,
                settings.ScriptName);

            AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Created script: [bold]{Path.GetFileName(fullPath)}[/]");

            ScriptGenerator.OpenInEditor(fullPath);

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return -1;
        }
    }
}
