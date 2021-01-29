using System.Collections.Generic;
using System.Web;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.Remoting.Messaging;

namespace Prototype1.Foundation.Web
{
    public class SessionContext
    {
        public static readonly string _storageKey = "Prototype1.Declarations.SessionContext";
        private readonly StringCollection _initializedKeys = new StringCollection();
        
        public SessionContext()
        {
        }

        public static SessionContext Current
        {
            get
            {
                SessionContext context;
                if (HttpContext.Current == null || HttpContext.Current.Session == null)
                {
                    context = CallContext.GetData(SessionContext._storageKey) as SessionContext;
                    if (context == null)
                    {
                        context = new SessionContext();
                        CallContext.SetData(SessionContext._storageKey, context);
                    }
                }
                else
                {
                    context = HttpContext.Current.Items[SessionContext._storageKey] as SessionContext;
                    if (context == null)
                    {
                        context = new SessionContext();
                        HttpContext.Current.Items[SessionContext._storageKey] = context;
                    }
                }
                return context;
            }
        }

        public void Flush()
        {
            if (HttpContext.Current.Session != null)
            {
                foreach (string key in _initializedKeys)
                {
                    HttpContext.Current.Session[key] = this.Items[key];
                }
            }
        }

        private void LoadFromSession(string key)
        {
            _initializedKeys.Add(key);
            if (HttpContext.Current == null || HttpContext.Current.Session == null)
                return;

            object obj = HttpContext.Current.Session[key];
            //if(obj is EntityBase)
            //    NHibernateSessionManager.Instance.TrackChanges(obj);
            this.Items[key] = obj;
        }

        public T GetValueFor<T>(string propertyName)
        {
            string key = GetKeyFor(propertyName);
            if (!_initializedKeys.Contains(key))
                LoadFromSession(key);
            if (!this.Items.Contains(key) || this.Items[key] == null)
                return default(T);
            return (T)this.Items[key];
        }

        public void SetValueFor(string propertyName, object value)
        {
            string key = GetKeyFor(propertyName);
            if (!_initializedKeys.Contains(key))
                _initializedKeys.Add(key);
            this.Items[key] = value;
            if (HttpContext.Current != null && HttpContext.Current.Session != null)
                HttpContext.Current.Session[key] = value;
        }

        private string GetKeyFor(string propertyName)
        {
            return SessionContext._storageKey + "." + propertyName;
        }

        private IDictionary _items;
        private IDictionary Items
        {
            get
            {
                if (_items == null)
                {
                    _items = HttpContext.Current == null ? new Dictionary<object, object>() : HttpContext.Current.Items;
                }
                return _items;
            }
        }
    }
}
