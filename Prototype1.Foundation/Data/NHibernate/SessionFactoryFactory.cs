using System.Linq;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Event;
using NHibernate.Caches.SysCache2;
using Prototype1.Foundation.Data.Filters;
using Prototype1.Foundation.Data.Listener;
using Prototype1.Foundation.Data.AutomaticMapper;
using Prototype1.Foundation.Interfaces;

namespace Prototype1.Foundation.Data.NHibernate
{
    public class SessionFactoryFactory : ISessionFactoryFactory
    {
        private readonly IAutoMaps _autoMaps;
        private readonly IAuditListener[] _auditListener;
        private readonly ApplicationUserCacheListener[] _applicationUserCacheListener = {new ApplicationUserCacheListener()};

        public SessionFactoryFactory(IAuditListener auditListener, IAutoMaps autoMaps)
        {
            _autoMaps = autoMaps;
            _auditListener = auditListener != null ? new[] {auditListener} : new IAuditListener[0];
        }

        #region ISessionFactoryFactory Members

        public ISessionFactory CreateSessionFactory(string connectionStringKey)
        {
            return Configuration(connectionStringKey).BuildSessionFactory();
        }

        public Configuration GetConfiguration()
        {
            return _configuration;
        }

        private Configuration _configuration = null;
        public FluentConfiguration Configuration(string connectionStringKey)
        {
            return Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2008
                    .ConnectionString(c => c.FromConnectionStringWithKey(connectionStringKey))
                    .AdoNetBatchSize(1000)
                    .DefaultSchema("dbo")
                    .UseReflectionOptimizer()
                    .DoNot.ShowSql())
                .Mappings(m =>
                {
                    m.FluentMappings.Add(typeof (PermanentRecordFilter))
                        .Conventions.Add<HasManyForeignKeyConstraintNamingConvention>()
                        .Conventions.Add<ReferenceForeignKeyConstraintNamingConvention>()
                        .Conventions.Add<HasOneForeignKeyConstraintNamingConvention>()
                        .Conventions.Add<HasManyToManyForeignKeyConstraintNamingConvention>();
                    m.HbmMappings.AddFromAssemblyOf<SessionFactoryFactory>();
                    _autoMaps.Map(m);
                })
                .ExposeConfiguration(c =>
                {
                    c.SessionFactory().Caching.Through<SysCacheProvider>().WithDefaultExpiration(255);
                    foreach (var mapping in c.ClassMappings.Where(mapping => typeof(IPermanentRecord).IsAssignableFrom(mapping.MappedClass)))
                    {
                        mapping.AddFilter("PermanentRecordFilter", "Deleted = :deleted");
                    }
                    c.SetProperty("command_timeout", "0");
                    c.SetProperty("transaction.factory_class", typeof(global::NHibernate.Transaction.AdoNetTransactionFactory).AssemblyQualifiedName);

                    c.EventListeners.DeleteEventListeners = new IDeleteEventListener[] { new DeleteEventListener() };
                    c.EventListeners.PostUpdateEventListeners =
                        c.EventListeners.PostUpdateEventListeners.Concat(_auditListener).Concat(_applicationUserCacheListener).ToArray();
                    c.EventListeners.PostInsertEventListeners =
                        c.EventListeners.PostInsertEventListeners.Concat(_auditListener).Concat(_applicationUserCacheListener).ToArray();
                    c.EventListeners.PostDeleteEventListeners =
                        c.EventListeners.PostDeleteEventListeners.Concat(_auditListener).Concat(_applicationUserCacheListener).ToArray();
                    c.EventListeners.PostCollectionUpdateEventListeners =
                        c.EventListeners.PostCollectionUpdateEventListeners.Concat(_auditListener).ToArray();
                    c.EventListeners.FlushEventListeners = new IFlushEventListener[] { new FixedDefaultFlushEventListener() };
                    
                    _configuration = c;
                });
        }

        #endregion
    }
}
