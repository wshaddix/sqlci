using System;

namespace SqlCi.ScriptRunner.Events
{
    public class StatusUpdateEvent : EventArgs
    {
        public string Status { get; private set; }  

        public StatusUpdateEvent(string status)
        {
            Status = status;
        }
    }
}