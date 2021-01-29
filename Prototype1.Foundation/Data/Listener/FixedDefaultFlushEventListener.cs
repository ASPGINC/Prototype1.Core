using System;
using NHibernate.Event;
using NHibernate.Event.Default;

namespace Prototype1.Foundation.Data.Listener
{
    [Serializable]
    public class FixedDefaultFlushEventListener : DefaultFlushEventListener
    {
        protected override void PerformExecutions(IEventSource session)
        {
            session.ConnectionManager.FlushBeginning();
            session.PersistenceContext.Flushing = true;
            session.ActionQueue.PrepareActions();
            session.ActionQueue.ExecuteActions();
            session.PersistenceContext.Flushing = false;
            session.ConnectionManager.FlushEnding();
        }
    } 
}
