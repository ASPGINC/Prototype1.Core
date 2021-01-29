using System;
using System.Web;
using Microsoft.Practices.Unity;

namespace Prototype1.Foundation.Unity
{
	public class ContainerManager : IHttpModule
	{
	    private const string UserContainerKey = "User_Container";

	    public void Dispose()
		{
		}

		public void Init(HttpApplication context)
		{
			context.BeginRequest += Context_BeginRequest;
			context.EndRequest += Context_EndRequest;
		}

	    static void Context_BeginRequest(object sender, EventArgs e)
		{
			var application = (HttpApplication) sender;
			application.Context.Items[UserContainerKey] =
				new Lazy<IUnityContainer>(() => Container.ContainerProvider.GetContainer().CreateChildContainer());
		}

	    static void Context_EndRequest(object sender, EventArgs e)
		{
			var application = (HttpApplication)sender;

			var userContainer = application.Context.Items[UserContainerKey] as Lazy<IUnityContainer>;
			if(userContainer != null && userContainer.IsValueCreated)
				userContainer.Value.Dispose();
		}

	}
}
