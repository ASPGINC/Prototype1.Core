using System;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;
using System.Runtime.Remoting.Messaging;
using System.Web;

namespace Prototype1.Foundation.Unity
{
    public static class Container
    {
        private const string Key = "User_Container";
        private static readonly IContainerProvider _containerProvider = InitializeContainer();

        private static IContainerProvider InitializeContainer()
        {
            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            if (section == null)
            {
                throw new ConfigurationErrorsException("Could not find section named 'unity' configured for this application.");
            }
            var container = new UnityContainer();
            section.Configure(container);
            var containerProvider = container.Resolve<IContainerProvider>();
            return containerProvider;
        } 

    	public static IContainerProvider ContainerProvider
    	{
    		get { return _containerProvider; }
    	}

        public static IUnityContainer Instance
        {
            get
            {
                
                var container = HttpContext.Current != null
                                ? HttpContext.Current.Items[Key] as Lazy<IUnityContainer>
                                : CallContext.GetData(Key) as Lazy<IUnityContainer>;
                
				if (container == null)
                {
                    container = new Lazy<IUnityContainer>(() => _containerProvider.GetContainer().CreateChildContainer());
                    SetContainer(container);
                }
                return container.Value;
            }
        }

        [Obsolete("Only to be used to add SessionFactoryFactory", true)]
        public static IUnityContainer Root
        {
            get { return _containerProvider.GetContainer(); }
        }

        public static void SetContainer(Lazy<IUnityContainer> container)
        {
            if(HttpContext.Current != null)
                HttpContext.Current.Items[Key] = container;
            else
            {
                CallContext.SetData(Key, container);
            }
        }
    }
}
