using NHibernate;
using NHibernate.Cfg;

namespace Prototype1.Foundation.Data.NHibernate
{
    public interface ISessionFactoryFactory
    {
        ISessionFactory CreateSessionFactory(string connectionStringKey);

        Configuration GetConfiguration();
    }
}
