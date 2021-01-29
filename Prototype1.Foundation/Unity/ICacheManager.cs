using System;
using System.Web.Caching;

namespace Prototype1.Foundation.Unity
{
    public interface ICacheManager
    {
        string Key { get; }
        Func<ICacheManager, CacheDependency> Dependency { get; }
        TimeSpan SlidingExpiration { get; }
        DateTime AbsoluteExpiration { get; }
        CacheItemUpdateCallback OnUpdating { get; }
    }
}
