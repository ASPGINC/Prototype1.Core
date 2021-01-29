using System.Web.Mvc;
using Prototype1.Foundation.Data.NHibernate;

namespace Prototype1.Foundation.Web
{
    public class NHSessionAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            NHibernateSessionManager.Instance.CloseSession();
        }
    }
}
