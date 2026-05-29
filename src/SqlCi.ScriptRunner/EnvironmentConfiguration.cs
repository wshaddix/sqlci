namespace SqlCi.ScriptRunner;

public class EnvironmentConfiguration
{
    public required string ConnectionString { get; set; }
    public required string Name { get; set; }
    public string? ResetConnectionString { get; set; }
    public bool ResetDatabase { get; set; }

    /// <summary>
    /// The database provider to use for this environment.
    /// Supported values: "SqlServer", "PostgreSql", "Sqlite"
    /// </summary>
    public string DbProvider { get; set; } = "SqlServer";
}