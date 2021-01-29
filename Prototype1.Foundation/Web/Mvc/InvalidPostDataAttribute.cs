using System;
using System.Web.Mvc;

namespace Prototype1.Foundation.Web.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited=true)]
    public class InvalidPostDataAttribute : ActionFilterAttribute
    {
        public InvalidPostDataAttribute()
            : base()
        {
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if(!filterContext.Controller.ViewData.ModelState.IsValid)
                filterContext.HttpContext.Response.AddHeader("InvalidPostData", "true");

            base.OnActionExecuted(filterContext);
        }
    }
}
