using System.Web.Http.Dependencies;

namespace Prototype1.Foundation.Web.Mvc
{
    public class DependencyContainer : DependencyScope, IDependencyResolver
    {
        public IDependencyScope BeginScope()
        {
            return this;
        }
    } 
}
