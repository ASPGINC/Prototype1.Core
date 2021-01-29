using System.Collections.Generic;
using System.Web;
using System.Collections;

namespace Prototype1.Foundation.Web
{
    public class RequestContext
    {
        public static readonly string STORAGE_KEY = "Prototype1.Declarations.RequestContext";

        public T GetValueFor<T>(string propertyName)
        {
            string key = GetKeyFor(propertyName);
            if (!this.Items.Contains(key))
                return default(T);
            return (T)this.Items[key];
        }

        public void SetValueFor(string propertyName, object value)
        {
            string key = GetKeyFor(propertyName);
            this.Items[key] = value;
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
