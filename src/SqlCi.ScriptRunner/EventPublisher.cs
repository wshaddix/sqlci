using System;
using SqlCi.ScriptRunner.Events;

namespace SqlCi.ScriptRunner
{
    public class EventPublisher
    {
        public event EventHandler<StatusUpdateEvent> StatusUpdate;

        public void PublishEvent()
        {
            OnPublishEvent(new StatusUpdateEvent("this is my event"));
        }

        protected virtual void OnPublishEvent(StatusUpdateEvent statusUpdateEvent)
        {
            EventHandler<StatusUpdateEvent> handler = StatusUpdate;

            if (handler != null)
            {
                handler(this, statusUpdateEvent);
            }
        }
    }
}