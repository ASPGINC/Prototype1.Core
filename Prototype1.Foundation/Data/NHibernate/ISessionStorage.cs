using NHibernate;

namespace Prototype1.Foundation.Data.NHibernate
{
    public interface ISessionStorage
    {
        ISession GetSession<T>();
    }
}
