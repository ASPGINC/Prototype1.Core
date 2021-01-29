using System.Web;

namespace Prototype1.Foundation.Unity
{
    public class SessionStorage : IStorage
    {
        public object GetValue(string key)
        {
            return HttpContext.Current.Session == null
                ? null 
                : HttpContext.Current.Session[key];
        }

        public void SetValue(string key, object value)
        {
            if(HttpContext.Current.Session != null)
                HttpContext.Current.Session[key] = value;
        }
    }
}
