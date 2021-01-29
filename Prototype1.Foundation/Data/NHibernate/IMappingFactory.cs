using FluentNHibernate.Automapping;

namespace Prototype1.Foundation.Data.NHibernate
{
    public interface IMappingFactory
    {
        AutoPersistenceModel CreateAutoPersistenceModel();
    }
}
