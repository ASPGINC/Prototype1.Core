using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Prototype1.Foundation.Web;

namespace Prototype1.Foundation.ActionFilters
{
    public class HttpsRequiredHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ConfigurableRequireHttpsAttribute.RequiresSsl && !IsSecureRequest(request))
                return
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
                    {
                        Content = new StringContent("Request must be made over HTTPS")
                    });

            return base.SendAsync(request, cancellationToken);
        }

        private static bool IsSecureRequest(HttpRequestMessage request)
        {
            return ConfigurableRequireHttpsAttribute.IsSecureRequest(request.RequestUri.Port,
                GetSecurePortServerVariable(request), request.Headers.GetHeaderValue("CLUSTER_HTTPS"));
        }

        private static string GetSecurePortServerVariable(HttpRequestMessage request)
        {
            var wrapper = request.Properties["MS_HttpContext"] as HttpContextWrapper;
            return wrapper == null ? null : wrapper.Request.ServerVariables["SERVER_PORT_SECURE"];
        }
    }
}