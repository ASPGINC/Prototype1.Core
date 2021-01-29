using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Caching;
using Microsoft.Practices.Unity;
using Prototype1.Foundation;
using Prototype1.Foundation.ActionFilters;
using Prototype1.Foundation.Data;
using Prototype1.Foundation.Data.Listener;
using Prototype1.Foundation.Data.NHibernate;
using Prototype1.Foundation.Interfaces;
using Prototype1.Foundation.Logging;
using Prototype1.Foundation.Unity;
using Prototype1.Services.Logging;

namespace Prototype1.Services.ContainerProviders
{
    public abstract class ApiContainerProviderBase : IContainerProvider
    {
        private IUnityContainer _container;

        public virtual IUnityContainer GetContainer()
        {
            return _container ?? InitializeUserContainer();
        }

        protected virtual IUnityContainer InitializeUserContainer()
        {
            _container = new UnityContainer();
            _container.AddNewExtension<HierarchicalLifetimeBaseExtension>();
            RegisterCommonImplementations(_container);
            RegisterEnvironmentSpecificImplementations(_container);
            RegisterHandlers(_container);
            RegisterEventHandlers(_container);

            return _container;
        }

        protected virtual void RegisterCommonImplementations(IUnityContainer container)
        {
            container
                // Common types
                .RegisterType(typeof (Lazy<>), new InjectionConstructor(new ResolvedParameter(typeof (Func<>))))
                .RegisterType(typeof (Factory<>), new InjectionConstructor(new ResolvedParameter(typeof (Func<>))))

                // Common Web
                .RegisterType<IStorage, SessionStorage>()
                .RegisterType<IFormsAuthenticationService, FormsAuthenticationService>(new HierarchicalLifetimeManager())

                // Database Session
                .RegisterType<ISessionFactoryFactory, SessionFactoryFactory>(new ContainerControlledLifetimeManager())
                .RegisterType<IEntityRepository, EntityRepository>(new HierarchicalLifetimeManager())

                // Auditing
                .RegisterType<IEnumerable<IAuditLogEntryFactory>, IAuditLogEntryFactory[]>()
                .RegisterType<IAuditLogEntryFactory, NullAuditLogEntryFactory>("EntityBaseAuditLogEntryFactory")
                .RegisterType<IAuditListener, NullAuditListener>()

                // Common Services
                .RegisterType<IMailService, MailService>(new ContainerControlledLifetimeManager())

                // Logging
                .RegisterType<IExceptionLogger, EmailExceptionLogger>(new ContainerControlledLifetimeManager())
                ;
        }

        protected static AggregateCacheDependency CreateAggregateSqlCacheDependency(string connectionStringKey, params string[] tableNames)
        {
            var aggregate = new AggregateCacheDependency();
            tableNames.Apply(x => aggregate.Add(new SqlCacheDependency(connectionStringKey, x)));
            return aggregate;
        }

        public virtual void RegisterHandlers(IUnityContainer container)
        {
            container
                .RegisterType<DelegatingHandler, SetFlushModeOnNonGets>("SetFlushModeOnNonGets",
                    new ContainerControlledLifetimeManager());
        }

        protected virtual void RegisterEventHandlers(IUnityContainer container)
        {
        }

        protected abstract void RegisterEnvironmentSpecificImplementations(IUnityContainer container);
    }
}