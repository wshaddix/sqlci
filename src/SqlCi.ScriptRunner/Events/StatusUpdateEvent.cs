namespace SqlCi.ScriptRunner.Events;

public class StatusUpdateEvent(string status, StatusLevelEnum level) : EventArgs
{
    public StatusLevelEnum Level { get; set; } = level;
    public string Status { get; } = status;
}