using Microsoft.Practices.Unity;
using System.Web;

namespace Prototype1.Foundation.Unity
{
    public class SessionLifetimeManager : LifetimeManager
    {
        private readonly string _key;
        public SessionLifetimeManager(string key)
        {
            _key = key;
        }
        public override object GetValue()
        {
            return InSession() ? HttpContext.Current.Session[_key] : null;
        }

        public override void RemoveValue()
        {
            HttpContext.Current.Session.Remove(_key);
        }

        public override void SetValue(object newValue)
        {
            if (InSession())
            {
                HttpContext.Current.Session[_key] = newValue;
            }
        }

        private static bool InSession()
        {
            return HttpContext.Current != null && HttpContext.Current.Session != null;
        }
    }
}
