namespace SqlCi.Configuration;

public sealed class DbEnvironment
{
    public string ConnectionString { get; set; }
    public string Name { get; set; }
    public bool ResetDatabase { get; set; }
}