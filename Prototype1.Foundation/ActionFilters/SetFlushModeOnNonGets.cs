using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using Prototype1.Foundation.Data.NHibernate;

namespace Prototype1.Foundation.ActionFilters
{
    public class SetFlushModeOnNonGets : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Will set to flush on POST,PUT,DELETE
            if (request.Method.Method.ToLower() != "get")
            {
                var session = NHibernateSessionManager.Instance.GetSession();
                session.FlushMode = FlushMode.Commit;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}