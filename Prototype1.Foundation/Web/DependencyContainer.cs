using System.Web.Http.Dependencies;
using Prototype1.Foundation.Web.Mvc;

namespace Prototype1.Foundation.Web
{
    public class DependencyContainer : DependencyScope, IDependencyResolver
    {
        public IDependencyScope BeginScope()
        {
            return this;
        }
    } 
}