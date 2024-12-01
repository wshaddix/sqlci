using System.ComponentModel;
using Spectre.Console.Cli;

namespace SqlCi.Commands;

public class DeployCommand : Command<DeployCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
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

        // verify the environment exists that we are going to deploy to
        var environment = settings.Environment.ToLowerInvariant();
        var config = Configuration.Configuration.EnsureEnvironmentExists(environment);

        return 0;
    }
}