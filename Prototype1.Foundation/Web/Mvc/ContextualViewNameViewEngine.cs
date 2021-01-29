using System;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Prototype1.Foundation.Web.Mvc
{
    public class ContextualViewNameViewEngine : IViewEngine
    {
        private readonly IViewEngine _innerViewEngine;
        private readonly Func<ControllerContext, bool> _predicate;
        private readonly Func<string, string> _viewNameProvider;

        public ContextualViewNameViewEngine(IViewEngine innerViewEngine, Expression<Func<ControllerContext, bool>> predicate, Expression<Func<string, string>> viewNameProvider)
        {
            _innerViewEngine = innerViewEngine;
            _predicate = predicate.Compile();
            _viewNameProvider = viewNameProvider.Compile();
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            if (_predicate(controllerContext))
            {
                var result = _innerViewEngine.FindPartialView(controllerContext, _viewNameProvider(partialViewName), useCache);
                if (result != null && result.View != null)
                    return result;
            }
            return new ViewEngineResult(new string[] { });
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            if (_predicate(controllerContext))
            {
                var result = _innerViewEngine.FindView(controllerContext, _viewNameProvider(viewName), masterName, useCache);
                if (result != null && result.View != null)
                     return result;
            }
            return new ViewEngineResult(new string[] { });
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            _innerViewEngine.ReleaseView(controllerContext, view);
        }
    }
}
