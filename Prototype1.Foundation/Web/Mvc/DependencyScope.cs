using System;
using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Dependencies;
using Microsoft.Practices.Unity;
using Prototype1.Foundation.Unity;

namespace Prototype1.Foundation.Web.Mvc
{
    public class DependencyScope: IDependencyScope 
    {
        public object GetService(Type serviceType)
        {
            if (Container.Instance.IsRegistered(serviceType) || typeof(ApiController).IsAssignableFrom(serviceType))
            {
                return Container.Instance.Resolve(serviceType);
            }
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Container.Instance.IsRegistered(serviceType) ? Container.Instance.ResolveAll(serviceType) : new List<object>();
        }

        public void Dispose()
        {
            // Handled by the ContainerManager
            //Container.Instance.Dispose(); 
        } 
    } 

}
