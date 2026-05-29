using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Helpers;

namespace SqlCi.Commands;

public class InitCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var workingDirectory = Environment.CurrentDirectory;

        // If the config file exists let the user know and quit
        if (ProjectConfiguration.Exists(workingDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Config file already exists. Quitting...[/]");
            return 1;
        }

        // get the name of the overall project
        var projectName = AnsiConsole
            .Ask<string>("What's the name of your project?")
            .Trim();

        // If the config file exists let the user know and quit
        if (ProjectConfiguration.Exists(Path.Combine(workingDirectory, projectName)))
        {
            AnsiConsole.MarkupLine($"[red]Config file already exists. Quitting...[/]");
            return 1;
        }

        // create a new project configuration
        var project = new ProjectConfiguration(projectName);

        // as the user in a loop to add database information (a project can have multiple databases)
        while (true)
        {
            // get the name of the database. when there are multiple databases we want to be able to
            // differentiate them looking at the file system (<db_name>.<db_type>)
            var dbName = AnsiConsole
                .Ask<string>("What's the name of the database?")
                .Trim()
                .ToLowerInvariant();

            // get the type of database
            var dbType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What type of database are you using?")
                    .AddChoices("AmazonAurora", "MSSqlServer", "MariaDB", "MySQL", "Oracle", "PostgreSQL", "SQLite"))
                .Trim()
                .ToLowerInvariant();

            // get the name of the table that will store the executed scripts
            var dbScriptTable = AnsiConsole.Prompt(
                    new TextPrompt<string>("What's the name of the table that you want to store the executed scripts in?")
                .DefaultValue("ScriptHistory"))
                .Trim();

            // create a database configuration
            var databaseConfiguration = new DatabaseConfiguration
            {
                Name = dbName,
                DbType = dbType,
                ScriptTable = dbScriptTable
            };

            // get the environments for this database
            var acceptEnvironmentDefaults = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Would you like to create the default environments for this project (workstation, dev, qa, prod)?")
                    .AddChoices("yes", "no"));

            var specifiedEnvironments = new List<string>();

            // if the user doesn't want to accept the defaults then we need to ask for the environments
            if (acceptEnvironmentDefaults.ToLowerInvariant() == "no")
            {
                specifiedEnvironments = AnsiConsole
                    .Ask<string>("Enter a comma separated list of environments for your project:")
                    .Trim()
                    .ToLowerInvariant()
                    .Split(",")
                    .ToList();
            }
            else
            {
                // if the user wants to accept the defaults then we need to add the defaults to the list of environments
                specifiedEnvironments.Add("workstation");
                specifiedEnvironments.Add("dev");
                specifiedEnvironments.Add("qa");
                specifiedEnvironments.Add("prod");
            }

            // get the ADO.Net connection string for each environment
            foreach (var environment in specifiedEnvironments)
            {
                var connectionString = AnsiConsole.Ask<string>($"Enter the ADO.Net connection string for the {environment} environment: ");

                // add the environment to the database configuration
                databaseConfiguration.AddEnvironment(environment, connectionString);
            }

            // add the database to the project configuration
            project.AddDatabase(databaseConfiguration);

            // ask if the user wants to add another database to the project
            var addAnotherDatabase = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Would you like to add another database to this project?")
                    .AddChoices("yes", "no"));

            if (addAnotherDatabase.ToLowerInvariant() == "no")
            {
                break;
            }
        }

        AnsiConsole.WriteLine($"Creating the {projectName} project in {workingDirectory}");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Initializing...", _ =>
            {
                // generate the project's directory
                var projectDir = Path.Combine(workingDirectory, projectName);

                // create the project directory
                DirectoryHelper.EnsureDirectoryExists(projectDir);

                // create the project's config file
                FileHelper.EnsureFileExists(Path.Combine(projectDir, Globals.ProjectFileName), project.Serialize);

                foreach (var databaseConfig in project.Databases)
                {
                    var databaseDir = Path.Combine(workingDirectory, project.Name, databaseConfig.DeploymentKey );

                    var resetDir = Path.Combine(databaseDir, Globals.ResetDirectoryName);
                    var beforeDeploymentDir = Path.Combine(databaseDir, Globals.BeforeDeploymentDirectoryName);
                    var deploymentDir = Path.Combine(databaseDir, Globals.DeploymentDirectoryName);
                    var afterDeploymentDir = Path.Combine(databaseDir, Globals.AfterDeploymentDirectoryName);

                    // create the database directory
                    DirectoryHelper.EnsureDirectoryExists(databaseDir);

                    // create the reset directory
                    DirectoryHelper.EnsureDirectoryExists(resetDir);

                    // create the before deployment directory
                    DirectoryHelper.EnsureDirectoryExists(beforeDeploymentDir);

                    // create the deployment directory
                    DirectoryHelper.EnsureDirectoryExists(deploymentDir);

                    // create the after deployment directory
                    DirectoryHelper.EnsureDirectoryExists(afterDeploymentDir);
                }
            });

        return 0;
    }
}