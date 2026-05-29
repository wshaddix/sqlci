namespace SqlCi.ScriptRunner;

public class ExecutionResults(bool wasSuccessful)
{
    public bool WasSuccessful { get; } = wasSuccessful;
}