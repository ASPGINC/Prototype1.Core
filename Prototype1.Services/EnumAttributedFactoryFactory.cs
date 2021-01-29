using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Prototype1.Services
{
    public class EnumAttributedFactoryFactory<TType, TAttribute, TEnum>
        where TAttribute : class 
    {
        private static ConcurrentDictionary<TEnum, List<Type>> _map;

        private static readonly object LockObject = new object();

        private static ConcurrentDictionary<TEnum, List<Type>> GetMap(Assembly callingAssembly)
        {
            lock (LockObject)
            {
                if (_map != null)
                    return _map;

                _map = new ConcurrentDictionary<TEnum, List<Type>>();

                var entryTypes = new List<Type>();
                foreach (
                    var assemblyName in
                        callingAssembly.GetReferencedAssemblies()
                            .Where(
                                a =>
                                    a.FullName.StartsWith(
                                        callingAssembly.FullName.Split('.').FirstOrDefault() ?? "Prototype1"))
                            .Select(a => a.FullName).Union(new[] {callingAssembly.FullName}))
                    entryTypes.AddRange(Assembly.Load(assemblyName)
                        .GetTypes()
                        .Where(t => typeof (TType).IsAssignableFrom(t)));

                foreach (var entryType in entryTypes)
                {
                    var attributes =
                        entryType.GetCustomAttributes(typeof (TAttribute), false)
                            .OfType<TAttribute>()
                            .ToList();

                    if (!attributes.Any())
                        continue;

                    var property = typeof (TAttribute).GetProperty(typeof (TEnum).Name);
                    if (property == null)
                        continue;

                    foreach (var enumValue in attributes.Select(attribute => (TEnum) property.GetValue(attribute)))
                    {
                        if (_map.ContainsKey(enumValue) && _map[enumValue].Contains(entryType))
                            throw new Exception(
                                $"Duplicate entry in factory map - TAttribute: {typeof (TAttribute).Name}; TEnum: {typeof (TEnum).Name}: Enum Val: {enumValue}; Type: {entryType.GetType().Name}");

                        _map.AddOrUpdate(enumValue, new List<Type> {entryType},
                            (key, existingValue) =>
                            {
                                existingValue.Add(entryType);
                                return existingValue;
                            });
                    }
                }
            }
            return _map;
        }

        public TType Create(TEnum enumValue)
        {
            var map = GetMap(Assembly.GetCallingAssembly());

            List<Type> entryType;
            if (!map.TryGetValue(enumValue, out entryType) || !entryType.Any(t=> typeof(TType).IsAssignableFrom(t)))
                throw new Exception(
                    $"Attempting to create an unregistered instance - TAttribute: {typeof(TAttribute).Name}; TEnum: {typeof(TEnum).Name}: Enum Val: {enumValue}; Type: {typeof(TType).Name}");

            return (TType) Activator.CreateInstance(entryType.First(t => typeof (TType).IsAssignableFrom(t)));
        }

        public Type GetType(TEnum enumValue)
        {
            var map = GetMap(Assembly.GetCallingAssembly());

            List<Type> type;
            if (!map.TryGetValue(enumValue, out type) || !type.Any(t => typeof(TType).IsAssignableFrom(t)))
                throw new Exception(
                    $"Attempting to get type of an unregistered instance - TAttribute: {typeof(TAttribute).Name}; TEnum: {typeof(TEnum).Name}: Enum Val: {enumValue};");

            return type.First(t => typeof (TType).IsAssignableFrom(t));
        }
    }
}