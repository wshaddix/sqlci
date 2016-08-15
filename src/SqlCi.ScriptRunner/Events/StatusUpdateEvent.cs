using System;

namespace SqlCi.ScriptRunner.Events
{
    public class StatusUpdateEvent : EventArgs
    {
        public StatusLevelEnum Level { get; set; }
        public string Status { get; private set; }

        public StatusUpdateEvent(string status, StatusLevelEnum level)
        {
            Status = status;
            Level = level;
        }
    }
}