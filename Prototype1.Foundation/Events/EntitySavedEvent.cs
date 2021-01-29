using System;
using Prototype1.Foundation.Data;

namespace Prototype1.Foundation.Events
{
    public class EntitySavedEvent<T> : IDomainEvent
        where T : IIdentifiable<Guid>
    {
        public T Entity { get; protected set; }

        public EntitySavedEvent(T entity)
        {
            this.Entity = entity;
        }
    }
}