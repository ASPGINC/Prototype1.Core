using Prototype1.Foundation.Data;

namespace Prototype1.Foundation.Models
{
    public interface IEntityBackedModel : IIdentifiable<string>
    {
        EntityBase Entity { get; }
    }
}