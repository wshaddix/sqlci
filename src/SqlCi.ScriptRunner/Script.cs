namespace SqlCi.ScriptRunner;

public class Script(string id, string name, string release, DateTime appliedOnUtc)
{
    public DateTime AppliedOnUtc { get; set; } = appliedOnUtc;
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public string Release { get; set; } = release;
}