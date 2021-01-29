using NHibernate.Event;

namespace Prototype1.Foundation.Data.Listener
{
    public interface IAuditListener : IPostUpdateEventListener, IPostInsertEventListener, IPostDeleteEventListener, IPostCollectionUpdateEventListener
    {
    }

    public enum AuditEvent
    {
        Insert,
        Update,
        Delete
    }
}
