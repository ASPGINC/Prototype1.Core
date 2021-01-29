using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using NHibernate.Linq;
using Prototype1.Foundation.Data.NHibernate;
using Prototype1.Foundation.Events;

namespace Prototype1.Foundation.Data
{
    public class EntityRepository : IEntityRepository, IDisposable
    {
        public ISession Session
        {
            get
            {
                return NHibernateSessionManager.Instance.GetSession();
            }
        }

        public T GetByID<T>(Guid id)
            where T : IIdentifiable<Guid>
        {
            return this.Session.Get<T>(id);
        }

        public IEnumerable<T> GetAll<T>()
            where T : IIdentifiable<Guid>
        {
            return this.Session.CreateCriteria(typeof(T)).List<T>();
        }

        public virtual void Save<T>(T entity)
            where T : IIdentifiable<Guid>
        {
            using (var tx = this.Session.BeginTransaction())
            {
                this.Session.SaveOrUpdate(entity);
                tx.Commit();
            }
            EventManager.Raise(new EntitySavedEvent<T>(entity));
        }

        public void Delete<T>(T entity)
            where T : IIdentifiable<Guid>
        {
            using (var tx = this.Session.BeginTransaction())
            {
                this.Session.Delete(entity);
                tx.Commit();
            }
            EventManager.Raise(new EntityDeletedEvent<T>(entity));
        }


        public IQueryable<T> Queryable<T>() where T : IIdentifiable<Guid>
        {
            var query = this.Session.Query<T>();
            return query;
        }

        public IIdentifiable<Guid> GetByID(Guid id, Type type)
        {
            return this.Session.Get(type, id).As<IIdentifiable<Guid>>();
        }

        public void Dispose()
        {
            NHibernateSessionManager.Instance.CloseSession();
        }
    }
}