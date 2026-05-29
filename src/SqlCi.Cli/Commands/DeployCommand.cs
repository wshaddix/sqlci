using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.ScriptRunner;
using SqlCi.ScriptRunner.Events;
using System.ComponentModel;

namespace SqlCi.Cli.Commands;

public sealed class DeployCommand : AsyncCommand<DeployCommand.Settings>
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

            executor.StatusUpdate += (_, e) =>
            {
                var color = e.Level switch
                {
                    StatusLevelEnum.Success => "green",
                    StatusLevelEnum.Warning => "yellow",
                    StatusLevelEnum.Error => "red",
                    _ => "grey"
                };
                AnsiConsole.MarkupLineInterpolated($"[{color}]{Markup.Escape(e.Status)}[/]");
            };

            await AnsiConsole.Status()
                .StartAsync($"Deploying to [bold]{settings.Environment}[/]...", async _ =>
                {
                    var result = await executor.ExecuteAsync(config, settings.Environment);

                    if (result.WasSuccessful)
                    {
                        AnsiConsole.MarkupLine("\n[bold green]Deployment Complete.[/]");
                    }
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return -1;
        }
    }
}
