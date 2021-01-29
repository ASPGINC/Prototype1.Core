using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Prototype1.Foundation.Web.Mvc
{
    public class ContextualMasterNameViewEngine : IViewEngine
    {
        private readonly IViewEngine _innerViewEngine;
        private readonly Func<ControllerContext, bool> _predicate;
        private readonly Func<string, string> _masterNameProvider;

        public ContextualMasterNameViewEngine(IViewEngine innerViewEngine, Expression<Func<ControllerContext, bool>> predicate, Expression<Func<string,string>> masterNameProvider)
        {
            _innerViewEngine = innerViewEngine;
            _predicate = predicate.Compile();
            _masterNameProvider = masterNameProvider.Compile();
        }
        
        /// <summary>
        /// The first provider that returns a non-empty non-null master name will be used.  If no master name is returned, the master page defined on the view will be used.
        /// </summary>
        public IList<Func<ControllerContext, string, string>> MasterNameProviders { get; protected set; }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return _innerViewEngine.FindPartialView(controllerContext, partialViewName, useCache);
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            if (_predicate(controllerContext))
                masterName = _masterNameProvider(masterName);

            return _innerViewEngine.FindView(controllerContext, viewName, masterName, useCache);
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            _innerViewEngine.ReleaseView(controllerContext, view);
        }
    }
}
