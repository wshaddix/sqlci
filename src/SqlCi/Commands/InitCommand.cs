using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Helpers;

namespace SqlCi.Commands;

internal sealed class InitCommand : Command<InitCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Directory where initial folder structure will be created")]
        [CommandOption("-d|--directory")]
        public string? Directory { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var workingDirectory = settings.Directory ?? Environment.CurrentDirectory;
        AnsiConsole.MarkupLine($"Working Directory :open_file_folder:: {workingDirectory}");
        
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .Start("Initializing...", ctx => {
                
                var resetDir = Path.Combine(workingDirectory, Globals.ResetDirectoryName);
                var beforeDeploymentDir = Path.Combine(workingDirectory, Globals.BeforeDeploymentDirectoryName);
                var deploymentDir = Path.Combine(workingDirectory, Globals.DeploymentDirectoryName);
                var afterDeploymentDir = Path.Combine(workingDirectory, Globals.AfterDeploymentDirectoryName);
                var configFilePath = Path.Combine(workingDirectory, Globals.ConfigFileName);
                
                // create the reset directory
                Thread.Sleep(1000);
                DirectoryHelper.EnsureDirectoryExists(Globals.ResetDirectoryName, resetDir);
                
                // create the before deployment directory
                Thread.Sleep(1000);
                DirectoryHelper.EnsureDirectoryExists(Globals.BeforeDeploymentDirectoryName, beforeDeploymentDir);
                
                // create the deployment directory
                Thread.Sleep(1000);
                DirectoryHelper.EnsureDirectoryExists(Globals.DeploymentDirectoryName, deploymentDir);
                
                // create the after deployment directory
                Thread.Sleep(1000);
                DirectoryHelper.EnsureDirectoryExists(Globals.AfterDeploymentDirectoryName, afterDeploymentDir);
                
                // create the config file
                Thread.Sleep(1000);
                FileHelper.EnsureFileExists(Globals.ConfigFileName, configFilePath, () =>
                {
                    var config = new Configuration
                    {
                        Version = "0.0.1",
                        ScriptTable = "SqlCi",
                        Environments =
                        [
                            new DbEnvironment { Name = "Workstation", ResetDatabase = true },
                            new DbEnvironment { Name = "Development", ResetDatabase = false }
                        ]
                    };
        
                    var jsonSerializerOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    return JsonSerializer.Serialize(config, jsonSerializerOptions);
                });
            });

        return 0;
    }
}
