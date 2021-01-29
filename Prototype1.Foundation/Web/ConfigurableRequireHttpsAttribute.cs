using System;
using System.Linq;
using System.Web.Mvc;
using System.Configuration;

namespace Prototype1.Foundation.Web
{
    public class ConfigurableRequireHttpsAttribute : RequireHttpsAttribute
    {
        public static readonly bool RequiresSsl = ConfigurationManager.AppSettings["EnableSSL"].ToBool(false);

        public static readonly string[] SecurePorts =
            ConfigurationManager.AppSettings["SecurePorts"].IfNullOrEmpty("443")
                .Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if(RequiresSsl && !IsSecureRequest(filterContext))
            {
                HandleNonHttpsRequest(filterContext);
            }
        }

        private static bool IsSecureRequest(AuthorizationContext filterContext)
        {
            return IsSecureRequest(filterContext.HttpContext.Request.Url.Port,
                filterContext.HttpContext.Request.ServerVariables["SERVER_PORT_SECURE"],
                filterContext.HttpContext.Request.Headers["CLUSTER_HTTPS"]);
        }

        public static bool IsSecureRequest(int port, string serverPortSecure, string clusterHttps)
        {
            return SecurePorts.Contains(port.ToString()) ||
                   serverPortSecure != "0" ||
                   clusterHttps == "1";
        }

        protected override void HandleNonHttpsRequest(AuthorizationContext filterContext)
        {
            base.HandleNonHttpsRequest(filterContext);

            var port = filterContext.HttpContext.Request.Url.Port;

            if (port == 80) return;

            // If the port isn't 80, add custom handling so the port number is maintained during redirect.
            var url = "https://" + filterContext.HttpContext.Request.Url.Host + ":" + port +
                      filterContext.HttpContext.Request.RawUrl;
            filterContext.Result = new RedirectResult(url,true);
        }
    }    
}
