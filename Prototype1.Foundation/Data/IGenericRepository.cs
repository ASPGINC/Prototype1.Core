using System.Collections.Generic;

namespace Prototype1.Foundation.Data
{
    public interface IGenericRepository<T, IdType>
     where T : EntityBase
    {
        IList<T> GetAll(int firstResult, int maxResults);
        IList<T> GetAll();
        IList<T> GetByExample(T exampleInstance, int firstResult, int maxResults);
        IList<T> GetByExample(T exampleInstance, bool excludeNulls, bool excludeZeroes, int firstResult, int maxResults);
        IList<T> GetByExample(T exampleInstance, bool excludeNulls, bool excludeZeroes);
        IList<T> GetByExample(T exampleInstance, string[] propertiesToExclude);
        IList<T> GetByExample(T exampleInstance, string[] propertiesToExclude, int firstResult, int maxResults);
        IList<T> GetByExample(T exampleInstance);
        T GetById(IdType id);
        void Save(T item);
        void Delete(T item);
        void Evict(T item);
    }
}
