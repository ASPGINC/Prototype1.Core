using System;
using System.Web;
using Prototype1.Foundation.Data.NHibernate;

namespace Prototype1.Foundation.Web
{
    public class NHibernateSessionModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.EndRequest += new EventHandler(context_EndRequest);
        }

        void context_EndRequest(object sender, EventArgs e)
        {
            NHibernateSessionManager.Instance.CloseSession();
        }

        public void Dispose()
        {
            
        }
    }
}
