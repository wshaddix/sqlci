using SqlCi.ScriptRunner;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SqlCi.Cli;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static Configuration Load(string path = "config.json")
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find config file: {path}");

        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Configuration>(json, Options);

        if (config is null)
            throw new InvalidOperationException("Failed to deserialize config.json");

        ExpandEnvironmentVariables(config);

        return config;
    }

    private static void ExpandEnvironmentVariables(Configuration config)
    {
        foreach (var env in config.Environments)
        {
            env.ConnectionString = ExpandVariables(env.ConnectionString, env.Name);

            if (!string.IsNullOrWhiteSpace(env.ResetConnectionString))
            {
                env.ResetConnectionString = ExpandVariables(env.ResetConnectionString!, env.Name);
            }

            // Full connection string override support via environment variables
            // Example: SQLCI_PRODUCTION_CONNECTION or SQLCI_LOCAL_RESET_CONNECTION
            var connectionOverride = Environment.GetEnvironmentVariable($"SQLCI_{env.Name.ToUpperInvariant()}_CONNECTION");
            if (!string.IsNullOrWhiteSpace(connectionOverride))
            {
                env.ConnectionString = connectionOverride;
            }

            var resetConnectionOverride = Environment.GetEnvironmentVariable($"SQLCI_{env.Name.ToUpperInvariant()}_RESET_CONNECTION");
            if (!string.IsNullOrWhiteSpace(resetConnectionOverride))
            {
                env.ResetConnectionString = resetConnectionOverride;
            }
        }
    }

    private static string ExpandVariables(string input, string environmentName)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return Regex.Replace(input, @"\$\{(?:env:)?([A-Za-z0-9_]+)\}", match =>
        {
            var varName = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(varName);

            if (value == null)
            {
                throw new InvalidOperationException(
                    $"Environment variable '{varName}' is referenced in the connection string for environment '{environmentName}', " +
                    "but it is not defined.");
            }

            return value;
        });
    }

    public static void Save(Configuration config, string path = "config.json")
    {
        var json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(path, json);
    }
}
