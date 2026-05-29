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
        [Description("Database provider to use (SqlServer, PostgreSql, Sqlite). Omit to choose interactively.")]
        public string? Provider { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (File.Exists("config.json"))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] config.json already exists.");
            return -1;
        }

        // Resolve provider: explicit flag > interactive prompt (if possible) > Sqlite default
        var provider = ResolveProvider(settings.Provider);

        AnsiConsole.Status()
            .Start("Initializing new SqlCi project...", ctx =>
            {
                var connectionString = GetDefaultConnectionString(provider);

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
                            ConnectionString = connectionString,
                            ResetDatabase = true,
                            DbProvider = provider
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

    private static string ResolveProvider(string? explicitProvider)
    {
        if (!string.IsNullOrWhiteSpace(explicitProvider))
        {
            return explicitProvider;
        }

        if (AnsiConsole.Profile.Capabilities.Interactive)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]database provider[/]:")
                    .PageSize(6)
                    .AddChoices("Sqlite", "SqlServer", "PostgreSql"));

            AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Using provider: [bold]{choice}[/]");
            return choice;
        }

        // Non-interactive fallback (CI, pipes, etc.)
        AnsiConsole.MarkupLine("[dim]Non-interactive environment detected. Defaulting to Sqlite.[/]");
        return "Sqlite";
    }

    private static string GetDefaultConnectionString(string provider)
    {
        return provider?.ToLowerInvariant() switch
        {
            "sqlite" => "Data Source=local.db;Cache=Shared",
            "sqlserver" or "mssql" or "sql server" => "Server=(localdb)\\\\MSSQLLocalDB;Database=YourDb_Local;Integrated Security=true;",
            "postgresql" or "postgres" or "pgsql" => "Host=localhost;Database=yourdb;Username=postgres;Password=changeme",
            _ => "Data Source=local.db;Cache=Shared"
        };
    }
}
