using System.ComponentModel;
using Spectre.Console.Cli;

namespace SqlCi.Commands;

public class DeployCommand : Command<DeployCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Database to deploy to.")]
        [CommandOption("-d|--db")]
        public required string Database { get; init; }

        [Description("Environment to deploy to.")]
        [CommandOption("-e|--env")]
        public required string Environment { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // if the environment is not specified we can't proceed
        if (string.IsNullOrWhiteSpace(settings.Environment))
        {
            throw new ArgumentException("Environment is required");
        }

        // if the database is not specified we can't proceed
        if (string.IsNullOrWhiteSpace(settings.Database))
        {
            throw new ArgumentException("Database is required");
        }

        // verify the environment exists that we are going to deploy to
        var environment = settings.Environment.ToLowerInvariant();
        var config = ProjectConfiguration.EnsureEnvironmentExists(environment);

        // verify the 

        return 0;
    }
}