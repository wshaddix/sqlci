using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.ScriptRunner;
using System.ComponentModel;

namespace SqlCi.Cli.Commands;

public sealed class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--provider")]
        [Description("Database provider to use (SqlServer, PostgreSql, Sqlite). Defaults to SqlServer.")]
        public string Provider { get; set; } = "SqlServer";
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (File.Exists("config.json"))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] config.json already exists.");
            return -1;
        }

        AnsiConsole.Status()
            .Start("Initializing new SqlCi project...", ctx =>
            {
                // Create default configuration (matches old behavior)
                var config = new Configuration
                {
                    ScriptsFolder = ".\\Scripts",
                    ResetScriptsFolder = ".\\ResetScripts",
                    ScriptTable = "ScriptTable",
                    Version = "1.0.0",
                    Environments =
                    {
                        new EnvironmentConfiguration
                        {
                            Name = "local",
                            ConnectionString = "Server=(localdb)\\\\MSSQLLocalDB;Database=YourDb_Local;Integrated Security=true;",
                            ResetDatabase = true,
                            DbProvider = settings.Provider
                        }
                    }
                };

                ConfigLoader.Save(config);

                AnsiConsole.MarkupLine("[green]✓[/] Created [bold]config.json[/]");

                // Create Scripts folder
                if (!Directory.Exists(config.ScriptsFolder))
                {
                    Directory.CreateDirectory(config.ScriptsFolder);
                    AnsiConsole.MarkupLine("[green]✓[/] Created [bold]Scripts[/] directory");
                }

                // Create ResetScripts folder
                if (!Directory.Exists(config.ResetScriptsFolder))
                {
                    Directory.CreateDirectory(config.ResetScriptsFolder);
                    AnsiConsole.MarkupLine("[green]✓[/] Created [bold]ResetScripts[/] directory");
                }

                // Create baseline script
                var baselinePath = ScriptGenerator.GenerateScriptFile(config.ScriptsFolder, "all", "baseline");
                AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Created baseline script: [bold]{Path.GetFileName(baselinePath)}[/]");

                ScriptGenerator.OpenInEditor(baselinePath);
            });

        AnsiConsole.MarkupLine("\n[green]Initialization complete.[/] Edit the baseline script and config.json to get started.");
        return 0;
    }
}
