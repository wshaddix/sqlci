using Spectre.Console.Cli;
using SqlCi.Cli;
using SqlCi.Cli.Commands;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("sqlci");
    config.SetApplicationVersion(AppVersion.Current);

    config.AddCommand<InitCommand>("init")
        .WithDescription("Initializes a new default config.json file and folders.");

    config.AddCommand<DeployCommand>("deploy")
        .WithDescription("Deploy the database to the specified environment.");

    config.AddCommand<HistoryCommand>("history")
        .WithDescription("Show the history of scripts ran against an environment.");

    config.AddCommand<GenerateCommand>("generate")
        .WithDescription("Generates a new script file.");

    config.AddCommand<UpdateCheckCommand>("update-check")
        .WithDescription("Check if a newer version of sqlci is available.")
        .WithExample("update-check");
});

return await app.RunAsync(args);
