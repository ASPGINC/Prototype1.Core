using System;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Prototype1.Foundation.Unity;

namespace Prototype1.Foundation.Web.Mvc
{
    /// <summary>
    /// A Controller factory to use with the MVC Framework that resolves the depencencies of the controller
    /// from the IoC container.
    /// To use, you must add the following code to the Application_Start method of the Global class of your
    /// MVC Application:
    ///		IControllerFactory controllerFactory = new DependencyInjectedControllerFactory();            
    ///		ControllerBuilder.Current.SetControllerFactory(controllerFactory);
    /// </summary>
    public class UnityControllerFactory : DefaultControllerFactory
    {
        private readonly Type _errorControllerType;
        private readonly string _defaultErrorAction;
        private readonly string _controllerName;
        
        public UnityControllerFactory(Type errorControllerType, string defaultErrorAction)
        {
            if (errorControllerType == null && !typeof(IController).IsAssignableFrom(errorControllerType))
                throw new ArgumentException("You must set a valid Error Controller.");

            if (string.IsNullOrEmpty(defaultErrorAction))
                throw new ArgumentException("defaultErrorAction is required.");

            _errorControllerType = errorControllerType;
            _defaultErrorAction = defaultErrorAction;
            _controllerName = errorControllerType.Name.Replace("Controller", string.Empty);
        }

        protected override IController  GetControllerInstance(System.Web.Routing.RequestContext requestContext, Type controllerType)
        {
            try
            {
                string path = requestContext.HttpContext.Request.Path;
                if (controllerType == null)
                    throw new ArgumentNullException("controllerType");

                if (!typeof(IController).IsAssignableFrom(controllerType))
                    throw new ArgumentException(string.Format(
                        "Type requested is not a controller: {0}", controllerType.Name),
                        "controllerType");
            }
            catch
            {
                requestContext.RouteData.Values["action"] = _defaultErrorAction;
                requestContext.RouteData.Values["controller"] = _controllerName;
                controllerType = _errorControllerType;
            }
            return Container.Instance.Resolve(controllerType) as IController;
        }
    }
}

