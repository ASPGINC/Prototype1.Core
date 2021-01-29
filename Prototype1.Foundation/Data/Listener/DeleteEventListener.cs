using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Event.Default;
using NHibernate.Persister.Entity;
using Prototype1.Foundation.Interfaces;

namespace Prototype1.Foundation.Data.Listener
{
    public class DeleteEventListener : DefaultDeleteEventListener
    {
        protected override void DeleteEntity(IEventSource session, object entity, EntityEntry entityEntry, bool isCascadeDeleteEnabled, IEntityPersister persister, System.Collections.Generic.ISet<object> transientEntities)
        {
            var record = entity as IPermanentRecord;
            if (record != null)
            {
                record.Deleted = true;
            }
            else
            {
                base.DeleteEntity(session, entity, entityEntry, isCascadeDeleteEnabled, persister, transientEntities);
            }
        }
    }
}