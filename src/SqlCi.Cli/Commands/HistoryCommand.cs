using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.ScriptRunner;
using System.ComponentModel;

namespace SqlCi.Cli.Commands;

public sealed class HistoryCommand : AsyncCommand<HistoryCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<ENVIRONMENT>")]
        [Description("The target environment from config.json")]
        public string Environment { get; set; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            var config = ConfigLoader.Load();
            var envConfig = config.Verify(settings.Environment);

            var provider = DatabaseProviderResolver.ResolveForEnvironment(envConfig);
            var executor = new Executor(provider);

            var history = (await executor.GetHistoryAsync(config, settings.Environment)).ToList();

            if (history.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No scripts have been run against this environment yet.[/]");
                return 0;
            }

            var table = new Table()
                .AddColumn("Version")
                .AddColumn("Date Ran")
                .AddColumn("Script Name");

            foreach (var script in history.OrderBy(s => s.AppliedOnUtc).ThenBy(s => s.Name))
            {
                table.AddRow(
                    script.Release,
                    script.AppliedOnUtc.ToLocalTime().ToString("g"),
                    script.Name);
            }

            AnsiConsole.Write(table);

            var last = history.OrderByDescending(s => s.AppliedOnUtc).First();
            AnsiConsole.MarkupLineInterpolated(
                $"\nCurrent Database Version: [bold]{last.Release}[/] ({last.AppliedOnUtc.ToLocalTime():g})");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return -1;
        }
    }
}
