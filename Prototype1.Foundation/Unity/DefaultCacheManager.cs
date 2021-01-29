using System;
using System.Web.Caching;

namespace Prototype1.Foundation.Unity
{
    public class DefaultCacheManager : ICacheManager
    {
        private readonly Func<string> _keyGen;
        private readonly IKeyProvider _keyProvider;

        /// <summary>
        /// Creates a cache manager with the supplied key
        /// </summary>
        /// <param name="key"></param>
        public DefaultCacheManager(string key)
        {
            _key = key;
            SlidingExpiration = Cache.NoSlidingExpiration;
            AbsoluteExpiration = Cache.NoAbsoluteExpiration;
        }

        public DefaultCacheManager(IKeyProvider keyProvider)
        {
            _keyProvider = keyProvider;
            SlidingExpiration = Cache.NoSlidingExpiration;
            AbsoluteExpiration = Cache.NoAbsoluteExpiration;
        }

        private string _key;
        public virtual string Key
        {
            get { return _key ?? _keyProvider.GetKey(); }
        }

        public virtual Func<ICacheManager, CacheDependency> Dependency { get; set; }
        public virtual TimeSpan SlidingExpiration { get; set; }
        public virtual DateTime AbsoluteExpiration { get; set; }
        public virtual CacheItemUpdateCallback OnUpdating { get; set; }
    }
}
