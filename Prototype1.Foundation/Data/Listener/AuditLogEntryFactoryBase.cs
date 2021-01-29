using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using NHibernate.Collection;
using NHibernate.Event;

namespace Prototype1.Foundation.Data.Listener
{
    public abstract class AuditLogEntryFactoryBase<T> : IAuditLogEntryFactory
    {
        protected const string NullState = "Unknown";
        private readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> _propertyGetters = new ConcurrentDictionary<Type, Dictionary<string, MethodInfo>>();

        private readonly Type _targetType = typeof(T);

        public virtual bool Applicable(Type t)
        {
            return _targetType.IsAssignableFrom(t);
        }

        public virtual int CalculateInheritanceDepth(Type t)
        {
            if (!Applicable(t))
                return -1;

            var type = t;
            int depth = 0;

            while (type != _targetType)
            {
                type = type.BaseType;
                depth++;
            }
            return depth;
        }

        protected virtual string GetUserName()
        {
            return HttpContext.Current?.User?.Identity?.Name ?? Environment.UserName;
        }

        protected static readonly Dictionary<Type, string[]> ExcludedProperties = new Dictionary<Type, string[]>();

        protected static string[] GetExcludedProperties(Type type)
        {
            if (!ExcludedProperties.ContainsKey(type))
                ExcludedProperties[type] =
                    (from p in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     let ownership = p.GetCustomAttributes(typeof(OwnershipAttribute), true).OfType<OwnershipAttribute>().FirstOrDefault()
                     let audit = p.GetCustomAttributes(typeof(AuditAttribute), true).OfType<AuditAttribute>().FirstOrDefault()
                     where (ownership != null && ownership.Ownership == Ownership.None)
                           || (audit != null && !audit.AuditChanges)
                           || p.GetCustomAttributes(typeof(VersionAttribute), true).Any()
                     select p.Name).ToArray();
            return ExcludedProperties[type];
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostInsertEvent e)
        {
            var typeName = e.Entity.GetType().Name;
            var id = (Guid)e.Id;

            var excludedProperties = GetExcludedProperties(e.Entity.GetType());

            for (var i = 0; i < e.State.Count(); i++)
                if (!excludedProperties.Contains(e.Persister.PropertyNames[i], StringComparer.OrdinalIgnoreCase))
                {
                    if (e.Persister.PropertyTypes[i].IsComponentType)
                    {
                        foreach (
                            var entry in
                                GetComponentAuditLogEntries(id, typeName, e.Persister.PropertyNames[i], null, e.State[i], Operation.Insert))
                            yield return entry;

                        continue;
                    }

                    var collection = e.State[i] as IEnumerable<EntityBase>;
                    if (collection != null)
                    {
                        foreach (
                            var entry in
                                ParseCollectionEntries(new EntityBase[0], collection.ToList(), typeName,
                                                       e.Persister.PropertyNames[i], id))
                            yield return entry;
                        continue;
                    }

                    yield return new AuditLogEntry
                    {
                        EntityType = typeName,
                        PropertyName = e.Persister.PropertyNames[i],
                        OldValue = string.Empty,
                        NewValue = GetStringValue(e.State[i]),
                        Username = GetUserName(),
                        EntityID = id,
                        Event = "Insert"
                    };
                }
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostUpdateEvent e)
        {
            if (e.OldState == null)
                yield break;

            var dirtyFieldIndexes = e.Persister.FindDirty(e.State, e.OldState, e.Entity, e.Session);
            
            var excludedProperties = GetExcludedProperties(e.Entity.GetType());

            foreach (var dirtyFieldIndex in dirtyFieldIndexes)
            {
                var oldValue = e.OldState == null ? null : e.OldState[dirtyFieldIndex];
                var newValue = e.State[dirtyFieldIndex];

                if (Equals(oldValue, newValue))
                    continue;

                var propertyName = e.Persister.PropertyNames[dirtyFieldIndex];

                if (excludedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
                    continue;

                var typeName = e.Entity.GetType().Name;
                var id = (Guid)e.Id;

                if (e.Persister.PropertyTypes[dirtyFieldIndex].IsComponentType)
                {
                    foreach (var entry in GetComponentAuditLogEntries(id, typeName, propertyName, oldValue, newValue, Operation.Update))
                        yield return entry;

                    continue;
                }

                yield return new AuditLogEntry
                {
                    EntityType = typeName,
                    PropertyName = propertyName,
                    OldValue = e.OldState == null ? NullState : GetStringValue(oldValue),
                    NewValue = GetStringValue(newValue),
                    Username = GetUserName(),
                    EntityID = id,
                    Event = "Update"
                };
            }
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostDeleteEvent e)
        {
            var typeName = e.Entity.GetType().Name;
            var id = (Guid)e.Id;

            var excludedProperties = GetExcludedProperties(e.Entity.GetType());

            for (var i = 0; i < e.DeletedState.Count(); i++)
                if (!excludedProperties.Contains(e.Persister.PropertyNames[i], StringComparer.OrdinalIgnoreCase))
                {
                    if (e.Persister.PropertyTypes[i].IsComponentType)
                    {
                        foreach (
                            var entry in
                                GetComponentAuditLogEntries(id, typeName, e.Persister.PropertyNames[i], null,
                                                            e.DeletedState[i], Operation.Delete))
                            yield return entry;

                        continue;
                    }

                    var collection = e.DeletedState[i] as IEnumerable<EntityBase>;
                    if (collection != null)
                    {
                        foreach (
                            var entry in
                                ParseCollectionEntries(collection.ToList(), new EntityBase[0], typeName,
                                                       e.Persister.PropertyNames[i], id))
                            yield return entry;
                        continue;
                    }

                    yield return new AuditLogEntry
                    {
                        EntityType = typeName,
                        PropertyName = e.Persister.PropertyNames[i],
                        OldValue = GetStringValue(e.DeletedState[i]),
                        NewValue = string.Empty,
                        Username = GetUserName(),
                        EntityID = id,
                        Event = "Delete"
                    };
                }
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostCollectionRemoveEvent e)
        {
            return CreateCollectionEntries(e.AffectedOwnerOrNull, (Guid)e.AffectedOwnerIdOrNull, e.Collection);
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostCollectionRecreateEvent e)
        {
            return CreateCollectionEntries(e.AffectedOwnerOrNull, (Guid)e.AffectedOwnerIdOrNull, e.Collection);
        }

        public virtual IEnumerable<AuditLogEntry> CreateEntries(PostCollectionUpdateEvent e)
        {
            return CreateCollectionEntries(e.AffectedOwnerOrNull, (Guid)e.AffectedOwnerIdOrNull, e.Collection);
        }

        protected IEnumerable<AuditLogEntry> CreateCollectionEntries(object affectedOwnerOrNull, Guid id, IPersistentCollection collection)
        {
            var typeName = affectedOwnerOrNull.GetType().Name;

            var excludedProperties = GetExcludedProperties(affectedOwnerOrNull.GetType());

            if (excludedProperties.Any(p => collection.Role.EndsWith(p)) || !(collection.GetValue() is IEnumerable<EntityBase>))
                yield break;

            var newList = ((IEnumerable<EntityBase>)collection.GetValue()).ToList();
            var oldList = ((List<object>)collection.StoredSnapshot).OfType<EntityBase>().ToList();

            foreach (
                var entry in
                    ParseCollectionEntries(oldList, newList, typeName,
                        collection.Role.Replace("Prototype1.Declarations.Entities." + typeName + ".", ""), id))
                yield return entry;
        }

        protected IEnumerable<AuditLogEntry> ParseCollectionEntries(IList<EntityBase> oldList, IList<EntityBase> newList, string typeName, string propertyName, Guid id)
        {
            // Entities removed from list
            foreach (var entity in oldList.Except(newList))
            {
                yield return new AuditLogEntry
                {
                    EntityType = typeName,
                    PropertyName = propertyName,
                    OldValue = GetStringValue(entity.ID),
                    NewValue = string.Empty,
                    Username = GetUserName(),
                    EntityID = id,
                    Event = "Removed"
                };
            }

            // Entities added to list
            foreach (var entity in newList.Except(oldList))
            {
                yield return new AuditLogEntry
                {
                    EntityType = typeName,
                    PropertyName = propertyName,
                    OldValue = string.Empty,
                    NewValue = GetStringValue(entity.ID),
                    Username = GetUserName(),
                    EntityID = id,
                    Event = "Added"
                };
            }
        }

        protected virtual IEnumerable<AuditLogEntry> GetComponentAuditLogEntries(Guid entityId, string entityType, string propertyName, object oldValue, object newValue, Operation operation)
        {
            var componentType = newValue != null
                                        ? newValue.GetType()
                                        : oldValue != null
                                            ? oldValue.GetType()
                                            : null;

            if (componentType == null)
                yield break;

            var username = GetUserName();
            var getters = _propertyGetters.GetOrAdd(componentType, CreateGetters);
            var excludedProperties = GetExcludedProperties(componentType);

            foreach (var propName in getters.Keys.Where(propName => !excludedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase)))
            {
                var getter = getters[propName];
                var o = oldValue == null ? null : getter.Invoke(oldValue, null);
                var n = newValue == null ? null : getter.Invoke(newValue, null);
                if (!Equals(o, n))
                {
                    yield return new AuditLogEntry
                    {
                        EntityID = entityId,
                        EntityType = entityType,
                        PropertyName = string.Format("{0}.{1}", propertyName, propName),
                        OldValue = GetStringValue(o),
                        NewValue = GetStringValue(n),
                        Event = operation.ToString(),
                        Username = username
                    };
                }
            }
        }

        private static Dictionary<string, MethodInfo> CreateGetters(Type type)
        {
            var dictionary = new Dictionary<string, MethodInfo>();
            var props =
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => !x.GetAttributes<TransientAttribute>(false).Any());

            foreach (var propertyInfo in props)
            {
                var get = propertyInfo.GetGetMethod(false);
                if (get == null)
                    continue;

                dictionary[propertyInfo.Name] = get;
            }

            return dictionary;
        }

        protected virtual string GetStringValue(object value)
        {
            EntityBase entity;
            return value == null || value.ToString() == string.Empty
                       ? null
                       : ((entity = (value as EntityBase)) != null ? entity.ID : value).ToString();
        }

        protected enum Operation
        {
            Insert,
            Delete,
            Update
        }
    }
}