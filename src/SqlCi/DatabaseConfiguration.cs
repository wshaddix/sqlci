namespace SqlCi;

public sealed class DatabaseConfiguration
{
    public required string Name { get; init; }
    public string DeploymentKey => $"{Name.ToLowerInvariant()}.{DbType.ToLowerInvariant()}";
    public required string ScriptTable { get; init; }
    public required string DbType { get; init; }
    public List<EnvironmentConfiguration> Environments { get; set; } = [];

    public void AddEnvironment(string environment, string connectionString)
    {
        Environments.Add(new EnvironmentConfiguration
        {
            Name = environment,
            ConnectionString = connectionString
        });
    }
}