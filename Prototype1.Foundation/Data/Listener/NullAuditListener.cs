using NHibernate.Event;

namespace Prototype1.Foundation.Data.Listener
{
    public class NullAuditListener : IAuditListener
    {
        public void OnPostUpdate(PostUpdateEvent @event)
        {
            //no-op
        }

        public void OnPostInsert(PostInsertEvent @event)
        {
            //no-op
        }

        public void OnPostDelete(PostDeleteEvent @event)
        {
            //no-op
        }

        public void OnPostUpdateCollection(PostCollectionUpdateEvent @event)
        {
            //no-op
        }
    }
}