using System;
using System.Web;

namespace Prototype1.Foundation.Web
{
    public class IgnoreAsset404sModule : IHttpModule
    {
        private static readonly string[] _ignoreExtensions = new string[] { ".jpg", ".gif", ".png", ".css", ".js" };
        public void Dispose()
        {

        }

        public void Init(HttpApplication context)
        {
            context.Error += new EventHandler(context_Error);
        }

        void context_Error(object sender, EventArgs e)
        {
            string url = HttpContext.Current.Request.Url.ToString();
            foreach (string ignoreExtenstion in _ignoreExtensions)
            {
                if (url.Contains(ignoreExtenstion))
                {
                    //TODO: We could generate a blank image here and store it in the filesystem so no more 404's happen for it?
                    HttpContext.Current.Server.ClearError();
                }
                    
            }
        }
    }
}
