using Spectre.Console;
using Spectre.Console.Cli;
using SqlCi.Commands;

public static class Program
{
    public static void Main(string[] args)
    {
        var app = new CommandApp();
        
        app.Configure(config =>
        {
            config.AddCommand<InitCommand>("init");
            config.AddCommand<GenerateScriptCommand>("generate");
        });
        
        app.Run(args);
    }
}