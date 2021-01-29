using System.Web;
using NHibernate.Event;
using Prototype1.Foundation.Providers;

namespace Prototype1.Foundation.Data.Listener
{
    public class ApplicationUserCacheListener : IPostUpdateEventListener, IPostInsertEventListener, IPostDeleteEventListener
    {
        protected const string CACHE_KEY = "ApplicationUser_{0}";

        public virtual void OnPostUpdate(PostUpdateEvent e)
        {
            if (!(e.Entity is AccountBase)) return;

            RemoveFromCache((AccountBase) e.Entity);
        }

        public virtual void OnPostInsert(PostInsertEvent e)
        {
            if (!(e.Entity is AccountBase)) return;

            RemoveFromCache((AccountBase) e.Entity);
        }

        public virtual void OnPostDelete(PostDeleteEvent e)
        {
            if (!(e.Entity is AccountBase)) return;

            RemoveFromCache((AccountBase) e.Entity);
        }

        public static void RemoveFromCache<TUser>(TUser user)
            where TUser : AccountBase
        {
            if (user == null) return;

            if (!user.Username.IsNullOrEmpty())
                HttpRuntime.Cache.Remove(string.Format(CACHE_KEY, user.Username.ToLower()));

            HttpRuntime.Cache.Remove(string.Format(CACHE_KEY, user.ID.ToString().ToLower()));
        }
    }
}