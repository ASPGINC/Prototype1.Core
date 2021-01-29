using Prototype1.Foundation.Models;

namespace Prototype1.Foundation.Data
{
    public interface IModelBackedEntityBase
    {
        IEntityBackedModel Model { get; }
    }
}
