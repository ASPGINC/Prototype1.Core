using Microsoft.Practices.Unity;

namespace Prototype1.Foundation.Unity
{
    public interface IContainerProvider
    {
        IUnityContainer GetContainer();
    }
}
