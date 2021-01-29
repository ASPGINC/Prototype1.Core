using System;
using System.Collections.Generic;
using NHibernate.Event;

namespace Prototype1.Foundation.Data.Listener
{
    public interface IAuditLogEntryFactory
    {
        int CalculateInheritanceDepth(Type t);
        bool Applicable(Type t);
        IEnumerable<AuditLogEntry> CreateEntries(PostInsertEvent e);
        IEnumerable<AuditLogEntry> CreateEntries(PostUpdateEvent e);
        IEnumerable<AuditLogEntry> CreateEntries(PostDeleteEvent e);
        IEnumerable<AuditLogEntry> CreateEntries(PostCollectionUpdateEvent e);
    }
}