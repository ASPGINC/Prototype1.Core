using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Event;
using Microsoft.Practices.Unity;
using Prototype1.Foundation.Logging;
using Prototype1.Foundation.Unity;
using System.Threading.Tasks;
using System.Threading;

namespace Prototype1.Foundation.Data.Listener
{
    public class AuditListener : IAuditListener
    {
        private readonly IEnumerable<IAuditLogEntryFactory> _auditLogEntryFactories;
        private readonly ConcurrentDictionary<Type, IAuditLogEntryFactory> _factoryMap = new ConcurrentDictionary<Type, IAuditLogEntryFactory>();

        public AuditListener(IEnumerable<IAuditLogEntryFactory> auditLogEntryFactories)
        {
            _auditLogEntryFactories = auditLogEntryFactories;
        }

        public virtual void OnPostUpdate(PostUpdateEvent e)
        {
            if (e.Entity is AuditLogEntry)
                return;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
        }

        public virtual Task OnPostUpdateAsync(PostUpdateEvent e, CancellationToken cancellationToken) {
            if (e.Entity is AuditLogEntry)
                return Task.CompletedTask;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
            return Task.CompletedTask;
        }

        public virtual void OnPostInsert(PostInsertEvent e)
        {
            if (e.Entity is AuditLogEntry)
                return;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
        }

        public virtual Task OnPostInsertAsync(PostInsertEvent e, CancellationToken cancellationToken) {
            if (e.Entity is AuditLogEntry)
                return Task.CompletedTask;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
            return Task.CompletedTask;
        }

        public virtual void OnPostDelete(PostDeleteEvent e)
        {
            if (e.Entity is AuditLogEntry)
                return;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
        }

        public virtual Task OnPostDeleteAsync(PostDeleteEvent e, CancellationToken cancellationToken) {
            if (e.Entity is AuditLogEntry)
                return Task.CompletedTask;

            var factory = GetFactory(e.Entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e), e);
            return Task.CompletedTask;
        }

        public void OnPostUpdateCollection(PostCollectionUpdateEvent e)
        {
            var entity = e.AffectedOwnerOrNull;
            if (entity == null || entity is AuditLogEntry)
                return;

            var factory = GetFactory(entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e).ToList(), e);
        }

        public virtual Task OnPostUpdateCollectionAsync(PostCollectionUpdateEvent e, CancellationToken cancellationToken) {
            var entity = e.AffectedOwnerOrNull;
            if (entity == null || entity is AuditLogEntry)
                return Task.CompletedTask;

            var factory = GetFactory(entity.GetType());
            if (factory != null)
                SaveEntries(factory.CreateEntries(e).ToList(), e);
            return Task.CompletedTask;
        }

        private IAuditLogEntryFactory GetFactory(Type entityType)
        {
            return _factoryMap.GetOrAdd(entityType, t => (from f in _auditLogEntryFactories
                                                            where f.Applicable(t)
                                                            let d = f.CalculateInheritanceDepth(t)
                                                            select new { Depth = d, Factory = f }).OrderBy(x => x.Depth)
                                                                                                .Select(x => x.Factory)
                                                                                                .FirstOrDefault());
        }

        private void SaveEntries(IEnumerable<AuditLogEntry> entries, IDatabaseEventArgs e)
        {
            try
            {
                if (entries == null || !entries.Any())
                    return;

                var session = e.Session.GetSession(EntityMode.Poco);
                entries.Apply(x => session.Save(x));

                session.Flush();
            }
            catch (Exception ex)
            {
                var exceptionLogger = Container.Instance.Resolve<IExceptionLogger>();
                exceptionLogger.LogException(ex);
            }
        }
    }
}