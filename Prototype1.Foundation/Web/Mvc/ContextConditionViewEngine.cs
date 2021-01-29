using System;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Prototype1.Foundation.Web.Mvc
{
    public class ContextConditionViewEngine : ContextualViewNameViewEngine
    {
        private Func<ControllerContext, bool> _predicate;
        private IViewEngine _innerViewEngine;

        public ContextConditionViewEngine(IViewEngine innerViewEngine, Expression<Func<ControllerContext, bool>> predicate)
            : base(innerViewEngine, predicate, x => x)
        {
        }
    }
}
