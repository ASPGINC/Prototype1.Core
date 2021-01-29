using System;
using Microsoft.Practices.Unity;
using System.Web;

namespace Prototype1.Foundation.Unity
{
    public class CachedLifetimeManager : SynchronizedLifetimeManager, IHierarchicalLifetimeManagerBase
    {
        private readonly ICacheManager _cacheManager;
	    private readonly Guid _instanceId = Guid.NewGuid();

        public CachedLifetimeManager()
        {
            _cacheManager = new DefaultCacheManager(Guid.NewGuid().ToString());
        }

        public CachedLifetimeManager(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

	    public ICacheManager CacheManager
	    {
			get { return _cacheManager; }
	    }

        public override void RemoveValue()
        {
            HttpRuntime.Cache.Remove(_cacheManager.Key);
        }

        protected override object SynchronizedGetValue()
        {
            return HttpRuntime.Cache.Get(_cacheManager.Key);
        }

        protected override void SynchronizedSetValue(object newValue)
        {
            var dependency = _cacheManager.Dependency != null ? _cacheManager.Dependency.Invoke(_cacheManager) : null;
            if (_cacheManager.OnUpdating != null)
            {
                HttpRuntime.Cache.Insert(_cacheManager.Key, newValue, dependency,
                                         _cacheManager.AbsoluteExpiration, _cacheManager.SlidingExpiration,
                                         _cacheManager.OnUpdating);
            }
            else
            {
                HttpRuntime.Cache.Insert(_cacheManager.Key, newValue, dependency,
                                         _cacheManager.AbsoluteExpiration, _cacheManager.SlidingExpiration);
            }
        }

        public IHierarchicalLifetimeManagerBase Duplicate()
        {
            return new CachedLifetimeManager(this.CacheManager);
        }
    }
}
