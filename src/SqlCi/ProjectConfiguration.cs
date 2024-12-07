using System.Text.Json;

namespace SqlCi;

public sealed class ProjectConfiguration(string name)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Name { get; set; } = name;
    public List<DatabaseConfiguration> Databases { get; set; } = [];

    public static bool Exists(string workingDirectory)
    {
        return File.Exists(Path.Combine(workingDirectory, Globals.ProjectFileName));
    }

    public static ProjectConfiguration? EnsureEnvironmentExists(string environment)
    {
        // load the configuration file
        var configFileContents = File.ReadAllText(Globals.ProjectFileName);

        // if the environment does not exist, throw an exception
        var config = JsonSerializer.Deserialize<ProjectConfiguration>(configFileContents, JsonSerializerOptions);

        if (config is null) throw new Exception("The configuration file could not be deserialized.");

        // if the environment was not specified then it will be "all" and we don't need to verify it exists
        if (environment == "all") return null;

        if (config.Databases.All(db => db.Environments.All(e => e.Name != environment)))
            throw new Exception($"The environment '{environment}' does not exist in the configuration file.");

        return config;
    }

    public void AddDatabase(DatabaseConfiguration databaseConfiguration)
    {
        Databases.Add(databaseConfiguration);
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }
}