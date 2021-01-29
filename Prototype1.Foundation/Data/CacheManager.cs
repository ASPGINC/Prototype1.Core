using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using NHibernate;
using NHibernate.Linq;
using Microsoft.Practices.Unity;
using Prototype1.Foundation.Logging;
using Prototype1.Foundation.Unity;

namespace Prototype1.Foundation.Data
{
    public class CacheManager
    {
        private const string CacheKey = "CacheManager";
        private const string TableName = "dbo.TrackedEntityUpdate";

        private readonly ISessionFactory _sessionFactory;
        private readonly string _connectionStringKey;

        private static readonly Dictionary<string, Dictionary<string, DateTime>> CachedList = new Dictionary<string, Dictionary<string, DateTime>>();

        private static readonly Dictionary<string, Func<Guid, string>[]> KeyGenerator =
            new Dictionary<string, Func<Guid, string>[]>
                {
                    /*
                    Define keys for row-level dependencies
                    Example:
                     * 
                    {
                        "Account",
                        new Func<Guid, string>[]
                            {
                                guid => "AccountContextData_" + guid.ToString(),
                                guid => "AccountStatusContextData_" + guid.ToString()
                            }
                    },
                    {
                        "Table",
                        new Func<Guid, string>[]
                            {
                                guid => "TableContextData"
                            }
                    }
                    */
                };

        public CacheManager(ISessionFactory sessionFactory, string connectionStringKey)
        {
            _sessionFactory = sessionFactory;
            _connectionStringKey = connectionStringKey;
            System.Web.HttpRuntime.Cache.Insert(CacheKey, new object(),
                new SqlCacheDependency(connectionStringKey, TableName),
                Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                this.CacheItemUpdateCallback);
            InitCacheList();
            IdentifyChanges();
        }

        private void CacheItemUpdateCallback(string key, CacheItemUpdateReason reason, out Object expensiveObject,
                                             out CacheDependency dependency, out DateTime absoluteExpiration,
                                             out TimeSpan slidingExpiration)
        {
            expensiveObject = new object();
            dependency = new SqlCacheDependency(_connectionStringKey, TableName);
            absoluteExpiration = Cache.NoAbsoluteExpiration;
            slidingExpiration = Cache.NoSlidingExpiration;
            try
            {
                InitCacheList();
                IdentifyChanges();
            }
            catch (Exception ex)
            {
                var logger = Container.Instance.Resolve<IExceptionLogger>();
                logger.LogException(ex, "CACHE DEPENDENCY FAILED ON UPDATE CALLBACK");
            }
        }

        private static void InitCacheList()
        {
            if (CachedList.Keys.Any())
                return;

            foreach (var key in KeyGenerator.Keys)
            {
                CachedList.Add(key, new Dictionary<string, DateTime>());
            }
        }

        private void IdentifyChanges()
        {
            using (var session = _sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                var liveTracked = session.Query<TrackedEntityUpdate>().ToList();

                foreach (var item in liveTracked)
                {
                    foreach (var cacheKey in KeyGenerator[item.TableName]
                                                .Select(cacheKeyFunc => cacheKeyFunc.Invoke(item.TrackedKey)))
                    {
                        if (!CachedList[item.TableName].ContainsKey(cacheKey))
                            CachedList[item.TableName].Add(cacheKey, item.LastUpdated);
                        else if (CachedList[item.TableName][cacheKey] != item.LastUpdated)
                            if (!NotifiableCacheDependency.Notify(cacheKey))
                                System.Web.HttpRuntime.Cache.Remove(cacheKey);

                        CachedList[item.TableName][cacheKey] = item.LastUpdated;
                    }
                }
                transaction.Commit();
            }
        }

        public class NotifiableCacheDependency : CacheDependency
        {
            private static readonly ConcurrentDictionary<string, NotifiableCacheDependency> Dependencies = new ConcurrentDictionary<string, NotifiableCacheDependency>();

            public static bool Notify(string key)
            {
                if (Dependencies.ContainsKey(key))
                {
                    Dependencies[key].Notify();
                    return true;
                }

                return false;
            }

            public NotifiableCacheDependency(ICacheManager cacheManager)
            {
                Dependencies[cacheManager.Key] = this;
            }

            public NotifiableCacheDependency(string key)
            {
                Dependencies[key] = this;
            }

            private void Notify()
            {
                NotifyDependencyChanged(null, EventArgs.Empty);
            }
        }
    }
}