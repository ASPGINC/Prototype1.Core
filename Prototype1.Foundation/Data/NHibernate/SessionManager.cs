using System;
using System.Collections.Generic;
using NHibernate;
using System.Web;
using System.Runtime.Remoting.Messaging;

namespace Prototype1.Foundation.Data.NHibernate
{
    public static class SessionManager
    {
        private static readonly Dictionary<string, ISessionFactory> _factories = new Dictionary<string,ISessionFactory>();
        private static object _lockObject = new object();

        public static ISession GetSession<T>()
        {
            ISession session;
            string key = GetSessionStorageKey(typeof(T));
            if (IsInWebContext())
                session = HttpContext.Current.Items[key] as ISession;
            else
                session = CallContext.GetData(key) as ISession;

            //if(session == null)
            //    session = 

            return session;
        }

        public static ISessionFactory GetSessionFactory<T>()
        {
            string key = typeof(T).Namespace;
            if (_factories.ContainsKey(key))
                return _factories[key];
            else
                throw new ArgumentException(string.Format("No SessionFactory registered for: {0}", key));
        }

        private static string GetSessionStorageKey(Type type)
        {
            return "session_" + type.Namespace;
        }

        private static bool IsInWebContext()
        {
            return HttpContext.Current != null;
        }
    }
}
