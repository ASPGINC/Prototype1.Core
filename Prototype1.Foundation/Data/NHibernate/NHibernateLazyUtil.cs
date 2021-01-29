using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NHibernate;
using NHibernate.Collection;
using NHibernate.Proxy;
using Prototype1.Foundation.Unity;
using Microsoft.Practices.Unity;

namespace Prototype1.Foundation.Data.NHibernate
{
    public static class NHibernateLazyUtil
    {
        private static readonly IDictionary<Type, List<PropertyInfo>> PropertiesToInitialise =
            new Dictionary<Type, List<PropertyInfo>>();

        public static void StaticInitialiser()
        {
            //Call static ctor.
        }

        [Obsolete]
        static NHibernateLazyUtil()
        {
            var cfg = Container.Root.Resolve<ISessionFactoryFactory>().GetConfiguration();
            // get all types (with their lazy props) having lazy 
            // many/one-to-one properties
            var toOneQuery = from persistentClass in cfg.ClassMappings
                let props = persistentClass.PropertyClosureIterator
                select new {persistentClass.MappedClass, props}
                into selection
                from prop in selection.props
                where prop.Value is global::NHibernate.Mapping.IFetchable
                where ((global::NHibernate.Mapping.IFetchable) prop.Value).IsLazy
                group selection.MappedClass.GetProperty(prop.Name)
                    by selection.MappedClass;

            // get all types (with their lazy props) having lazy nh bag properties
            var bagQuery = from persistentClass in cfg.ClassMappings
                let props = persistentClass.PropertyClosureIterator
                select new {persistentClass.MappedClass, props}
                into selection
                from prop in selection.props
                where prop.Value is global::NHibernate.Mapping.Collection
                where ((global::NHibernate.Mapping.Collection) prop.Value).IsLazy
                group selection.MappedClass.GetProperty(prop.Name)
                    by selection.MappedClass;

            foreach (var value in toOneQuery)
                PropertiesToInitialise.Add(value.Key, value.ToList());
            foreach (var value in bagQuery)
            {
                if (PropertiesToInitialise.ContainsKey(value.Key))
                    PropertiesToInitialise[value.Key].AddRange(value.ToList());
                else
                    PropertiesToInitialise.Add(value.Key, value.ToList());
            }
        }

        public static T InitializeEntity<T>(T entity, int maxFetchDepth = 20)
            where T : EntityBase
        {
            // Let's reduce the max-fetch depth to something tolerable...
            if (maxFetchDepth < 0 || maxFetchDepth > 20) maxFetchDepth = 20;
            // Okay, first we must identify all the proxies we want to initialize:
            ExtractNHMappedProperties(entity, 0, maxFetchDepth, false, NHibernateSessionManager.Instance.GetSession());
            return entity;
        }

        private static void ExtractNHMappedProperties(object entity, int depth,
            int maxDepth, bool loadGraphCompletely, ISession session)
        {
            try
            {
                bool search;
                if (loadGraphCompletely) search = true;
                else search = (depth <= maxDepth);

                if (null != entity)
                {
                    // Should we stay or should we go now?
                    if (search)
                    {
                        // Check if the entity is a collection.
                        // If so, we must iterate the collection and
                        // check the items in the collection. 
                        // This will increase the depth level.
                        var interfaces = entity.GetType().GetInterfaces();
                        if (interfaces.Any(iface => iface == typeof (ICollection)))
                        {
                            try
                            {
                                var collection = (ICollection) entity;
                                foreach (var item in collection)
                                    try
                                    {
                                        ExtractNHMappedProperties(item, depth + 1, maxDepth, loadGraphCompletely,
                                            session);
                                    }
                                    catch
                                    {
                                    }
                            }
                            catch
                            {
                            }
                            return;
                        }

                        // If we get here, then we know that we are
                        // not working with a collection, and that the entity
                        // holds properties we must search recursively.
                        // We are only interested in properties with NHAttributes.
                        // Maybe there is a better way to specify this
                        // in the GetProperties call (so that we only get an array
                        // of PropertyInfo's that have NH mappings).

                        List<PropertyInfo> props;
                        if (!PropertiesToInitialise.TryGetValue(entity.GetType(), out props)) return;

                        foreach (var proxy in from prop in props
                            select prop.GetGetMethod()
                            into method
                            where null != method
                            select method.Invoke(entity, new object[0]))
                        {
                            if (!NHibernateUtil.IsInitialized(proxy))
                            {
                                LazyInitialise(proxy, entity, session);
                            }

                            if (null != proxy)
                                ExtractNHMappedProperties(proxy, depth + 1, maxDepth, loadGraphCompletely, session);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void LazyInitialise(object proxy, object owner, ISession session)
        {
            if (null == proxy) return;

            var interfaces = proxy.GetType().GetInterfaces();
            foreach (var iface in interfaces.Where(iface => iface == typeof(INHibernateProxy) ||
                                                            iface == typeof(IPersistentCollection)))
            {
                if (!NHibernateUtil.IsInitialized(proxy))
                {
                    session.Lock(iface == typeof(INHibernateProxy) ? proxy : owner, LockMode.None);
                    NHibernateUtil.Initialize(proxy);
                }

                break;
            }
        }
    }
}