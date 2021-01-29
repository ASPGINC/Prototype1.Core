using System;
using System.Collections.Generic;
using System.Linq;

namespace Prototype1.Foundation.Data.NHibernate
{
    public interface IEntityRepository
    {
        void Save<T>(T obj) where T : IIdentifiable<Guid>;
        void Delete<T>(T obj) where T : IIdentifiable<Guid>;
        T GetByID<T>(Guid id) where T : IIdentifiable<Guid>;
        IIdentifiable<Guid> GetByID(Guid id, Type type);
        IEnumerable<T> GetAll<T>() where T : IIdentifiable<Guid>;
        IQueryable<T> Queryable<T>() where T : IIdentifiable<Guid>;
    }
}
