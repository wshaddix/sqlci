namespace SqlCi;

public sealed class Configuration
{
    public required string ScriptTable { get; set; }
    public required string Version { get; set; }
    public required List<DbEnvironment> Environments { get; set; } = [];
}