using System;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;

namespace Prototype1.Foundation.Web.Mvc
{
    public class CustomKeyedViewLocationCache : IViewLocationCache
    {
        private readonly string _cacheKeyPrefix;
        private static readonly TimeSpan _defaultTimeSpan = new TimeSpan(0, 15, 0);

        public CustomKeyedViewLocationCache(string cacheKeyPrefix)
            : this(_defaultTimeSpan, cacheKeyPrefix) {
        }

        public CustomKeyedViewLocationCache(TimeSpan timeSpan, string cacheKeyPrefix)
        {
            if(string.IsNullOrEmpty(cacheKeyPrefix))
                throw new ArgumentException("cacheKeyPrefix must be provided");
            
            if (timeSpan.Ticks < 0) {
                throw new InvalidOperationException("Cannot provide a negative timespan.");
            }
            _cacheKeyPrefix = cacheKeyPrefix;
            TimeSpan = timeSpan;
        }


        public TimeSpan TimeSpan {
            get;
            private set;
        }

        #region IViewLocationCache Members
        public string GetViewLocation(HttpContextBase httpContext, string key) {
            if (httpContext == null) {
                throw new ArgumentNullException("httpContext");
            }
            return (string)httpContext.Cache[_cacheKeyPrefix + ":" + key];
        }

        public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath) {
            if (httpContext == null) {
                throw new ArgumentNullException("httpContext");
            }
            httpContext.Cache.Insert(_cacheKeyPrefix + ":" + key, virtualPath, null /* dependencies */, Cache.NoAbsoluteExpiration, TimeSpan);
        }
        #endregion
    }
}
