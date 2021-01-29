using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Linq.Expressions;

namespace Prototype1.Foundation.Web.Mvc
{
    public class LocationFormatViewEngine<T> : IViewEngine
        where T : VirtualPathProviderViewEngine, new()
    {
        private readonly T _innerViewEngine;

        public LocationFormatViewEngine(IEnumerable<Expression<Func<string,string>>> formatModifiers, string cacheKeyPrefix)
        {
            _innerViewEngine = new T();
            var modifiers = formatModifiers.Select(modifier => modifier.Compile()).ToList();
            _innerViewEngine.ViewLocationFormats = BuildFormats(_innerViewEngine.ViewLocationFormats, modifiers);
            _innerViewEngine.MasterLocationFormats = BuildFormats(_innerViewEngine.MasterLocationFormats, modifiers);
            _innerViewEngine.PartialViewLocationFormats = BuildFormats(_innerViewEngine.PartialViewLocationFormats, modifiers);
            _innerViewEngine.ViewLocationCache = new CustomKeyedViewLocationCache(cacheKeyPrefix);
        }

        private string[] BuildFormats(string[] originalFormats, IEnumerable<Func<string, string>> formatModifiers)
        {
            var newLocationFormats = new List<string>();
            foreach (var formatModifier in formatModifiers)
            {
                newLocationFormats.AddRange(originalFormats.Select(format => formatModifier(format)));
            }
            return newLocationFormats.Concat(originalFormats).ToArray();
        }

        public ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            var result = _innerViewEngine.FindPartialView(controllerContext, partialViewName, useCache);
            if (result.View == null && useCache)
                result = _innerViewEngine.FindPartialView(controllerContext, partialViewName, false);

            return result;
        }

        public ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            var result = _innerViewEngine.FindView(controllerContext, viewName, masterName, useCache);
            if (result.View == null && useCache)
                result = _innerViewEngine.FindView(controllerContext, viewName, masterName, false);

            return result;
        }

        public void ReleaseView(ControllerContext controllerContext, IView view)
        {
            _innerViewEngine.ReleaseView(controllerContext, view);
        }
    }
}
