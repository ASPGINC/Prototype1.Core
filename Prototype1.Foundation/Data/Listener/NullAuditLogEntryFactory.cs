using System;
using System.Collections.Generic;
using NHibernate.Event;

namespace Prototype1.Foundation.Data.Listener
{
    public class NullAuditLogEntryFactory : IAuditLogEntryFactory
    {
        public int CalculateInheritanceDepth(Type t)
        {
            return 0;
        }

        public bool Applicable(Type t)
        {
            return false;
        }

        public IEnumerable<AuditLogEntry> CreateEntries(PostInsertEvent e)
        {
            yield break;
        }

        public IEnumerable<AuditLogEntry> CreateEntries(PostUpdateEvent e)
        {
            yield break;
        }

        public IEnumerable<AuditLogEntry> CreateEntries(PostCollectionUpdateEvent e)
        {
            yield break;
        }

        public IEnumerable<AuditLogEntry> CreateEntries(PostDeleteEvent e)
        {
            yield break;
        }
    }
}