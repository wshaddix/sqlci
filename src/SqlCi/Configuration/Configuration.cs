using System.Text.Json;

namespace SqlCi.Configuration;

public sealed class Configuration
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public required string ScriptTable { get; set; }
    public required string Version { get; set; }
    public required List<DbEnvironment> Environments { get; set; } = [];

    public static string Initialize()
    {
        var config = new Configuration
        {
            Version = "0.0.1",
            ScriptTable = "SqlCi",
            Environments =
            [
                new DbEnvironment { Name = "workstation", ResetDatabase = true },
                new DbEnvironment { Name = "dev", ResetDatabase = false },
                new DbEnvironment { Name = "prod", ResetDatabase = false }
            ]
        };

        return JsonSerializer.Serialize(config, JsonSerializerOptions);
    }

    public static bool Exists()
    {
        return File.Exists(Globals.ConfigFileName);
    }

    public static Configuration? EnsureEnvironmentExists(string environment)
    {
        // load the configuration file
        var configFileContents = File.ReadAllText(Globals.ConfigFileName);

        // if the environment does not exist, throw an exception
        var config = JsonSerializer.Deserialize<Configuration>(configFileContents, JsonSerializerOptions);

        if (config is null) throw new Exception("The configuration file could not be deserialized.");

        // if the environment was not specified then it will be "all" and we don't need to verify it exists
        if (environment == "all") return null;

        if (config.Environments.All(e => e.Name != environment))
            throw new Exception($"The environment '{environment}' does not exist in the configuration file.");

        return config;
    }
}