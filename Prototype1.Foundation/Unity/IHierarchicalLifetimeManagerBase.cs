using Microsoft.Practices.ObjectBuilder2;

namespace Prototype1.Foundation.Unity
{
    public interface IHierarchicalLifetimeManagerBase : ILifetimePolicy
    {
        IHierarchicalLifetimeManagerBase Duplicate();
    }
}
