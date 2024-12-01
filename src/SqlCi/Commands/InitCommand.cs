using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Helpers;

namespace SqlCi.Commands;

public class InitCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var workingDirectory = Environment.CurrentDirectory;

        // If the config file exists prompt the user and quit
        if(Configuration.Configuration.Exists())
        {
            AnsiConsole.MarkupLine($"[red]Config file already exists. Quitting...[/]");
            return 1 ;
        };

        var environments = new List<string>();

        var projectName = AnsiConsole.Ask<string>("What's the name of your project?");

        var dbType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What type of database are you using?")
                .AddChoices("AmazonAurora", "MSSqlServer", "MariaDB", "MySQL ", "Oracle", "PostgreSQL", "SQLite"));

        var projectDir = Path.Combine(workingDirectory, $"{StringHelper.ToSafeFileName(projectName).Replace(" ", "_")}.{dbType.ToLowerInvariant()}");

        var acceptEnvironmentDefaults = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Would you like to create the default environments for this project (workstation, dev, qa, prod)?")
                .AddChoices("yes", "no"));

        if (acceptEnvironmentDefaults.ToLowerInvariant() == "no")
        {
            var specifiedEnvironments = AnsiConsole.Ask<string>("Enter a comma separated list of environments for your project");
            environments = specifiedEnvironments.Split(",").ToList();
        }
        else
        {
            environments.Add("workstation");
            environments.Add("dev");
            environments.Add("qa");
            environments.Add("prod");
        }

        AnsiConsole.WriteLine($"Creating the {projectName} project in {projectDir} using {dbType} database with the environments of {string.Join(", ", environments)}");

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Initializing...", _ =>
            {
                var resetDir = Path.Combine(projectDir, Globals.ResetDirectoryName);
                var beforeDeploymentDir = Path.Combine(projectDir, Globals.BeforeDeploymentDirectoryName);
                var deploymentDir = Path.Combine(projectDir, Globals.DeploymentDirectoryName);
                var afterDeploymentDir = Path.Combine(projectDir, Globals.AfterDeploymentDirectoryName);
                var configFileName = $"{StringHelper.ToSafeFileName(projectName).Replace(" ", "_")}.json";
                var configFilePath = Path.Combine(projectDir, configFileName);

                // create the reset directory
                DirectoryHelper.EnsureDirectoryExists(Globals.ResetDirectoryName, resetDir);

                // create the before deployment directory
                DirectoryHelper.EnsureDirectoryExists(Globals.BeforeDeploymentDirectoryName, beforeDeploymentDir);

                // create the deployment directory
                DirectoryHelper.EnsureDirectoryExists(Globals.DeploymentDirectoryName, deploymentDir);

                // create the after deployment directory
                DirectoryHelper.EnsureDirectoryExists(Globals.AfterDeploymentDirectoryName, afterDeploymentDir);

                // create the config file
                FileHelper.EnsureFileExists(configFilePath, Configuration.Configuration.Initialize);

            });

        // generate the first script file
        var cmd = new GenerateScriptCommand();
        cmd.Execute(context, new GenerateScriptCommand.Settings
        {
            ScriptName = "init"
        });

        return 0;
    }
}