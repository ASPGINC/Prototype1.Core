using System;
using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Mapping;
using System.Reflection;
using FluentNHibernate;
using System.Linq.Expressions;
using FluentNHibernate.Utils.Reflection;
using System.Runtime.CompilerServices;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public static class MappingExtensions
    {
        //These MethodInfo objects are used to create late bound calls to generic methods.  
        private static readonly MethodInfo _mapManyToManyMethod = typeof(MappingExtensions).GetMethod("MapManyToManyImplementation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _mapOneToManyMethod = typeof(MappingExtensions).GetMethod("MapOneToManyImplementation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo _mapReferenceMethod = typeof(MappingExtensions).GetMethod("MapReferenceImplementation", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly Dictionary<Type, MethodInfo> _interfaceMappings = new Dictionary<Type, MethodInfo>();
        private const string INTERFACE_MAPPING_EXCEPTION_MESSAGE = "InterfaceMappingAttribute only applies to public static Generic Methods with signature MapIInterfaceName<T>(ClasslikeMapBase<T> mapping) where T: IInterface";

        public static void ExtractInterfaceMappingsFromAssemblyOf<T>()
        {
            IEnumerable<Type> types = typeof(T).Assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(InterfaceMappingAttribute), false).Any());
            foreach (Type type in types)
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var mapAttribute = (InterfaceMappingAttribute)method.GetCustomAttributes(typeof(InterfaceMappingAttribute), false).FirstOrDefault();
                    if (mapAttribute != null)
                    {
                        if (!method.IsGenericMethod)
                            throw new ArgumentException(INTERFACE_MAPPING_EXCEPTION_MESSAGE);

                        var genericArgs = method.GetGenericArguments();
                        if (genericArgs.Count() != 1)
                            throw new ArgumentException(INTERFACE_MAPPING_EXCEPTION_MESSAGE);

                        var typeParameter = genericArgs[0];

                        var interfaceConstraint = typeParameter.GetGenericParameterConstraints().FirstOrDefault(t => t.Equals(mapAttribute.Type));
                        if (interfaceConstraint == null)
                            throw new ArgumentException(INTERFACE_MAPPING_EXCEPTION_MESSAGE);

                        if(!_interfaceMappings.ContainsKey(mapAttribute.Type))
                            _interfaceMappings.Add(mapAttribute.Type, method);
                    }
                }
            }
        }

        public static bool AnythingToMap<T>(this ClasslikeMapBase<T> mapping)
        {
            Type t = typeof(T);
            if (GetMappableProperties(t).Any())
                return true;

            if (MappableInterfaces(t).Any())
                return true;

            return false;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AutoMap<T>(this ClasslikeMapBase<T> mapping)
        {
            Type t = typeof(T);
            var mappableProperties = GetMappableProperties(t);
            if (mappableProperties.Any())
            {
                foreach (var property in mappableProperties)
                {
                    if (!property.GetGetMethod().IsAbstract)
                        AutoMap<T>(mapping, property);
                }
                AutoMapInterfaces<T>(mapping);
            }
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties(Type t)
        {
            return t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(x => !x.GetGetMethod().IsAbstract && !x.GetCustomAttributes(typeof(TransientAttribute), false).Any() && !x.GetCustomAttributes(typeof(VersionAttribute), false).Any());
        }

        private static void AutoMapInterfaces<T>(ClasslikeMapBase<T> mapping)
        {
            Type t = typeof(T);
            foreach (var @interface in MappableInterfaces(t))
            {
                MethodInfo mapInterfaceMethod = _interfaceMappings[@interface].MakeGenericMethod(t);
                mapInterfaceMethod.Invoke(null, new object[] { mapping });
            }
        }

        private static IEnumerable<Type> MappableInterfaces(Type type)
        {
            return from @interface in type.GetInterfaces()
                   where _interfaceMappings.ContainsKey(@interface) && type.HasConcreteImplementation(@interface)
                   select @interface;
        }

        private static void AutoMap<T>(ClasslikeMapBase<T> mapping, PropertyInfo property)
        {
            var ownershipAttribute = (OwnershipAttribute)property.GetCustomAttributes(typeof(OwnershipAttribute), true).FirstOrDefault();
            Ownership ownership = ownershipAttribute == null ? Ownership.None : ownershipAttribute.Ownership;
            var expression = ExpressionBuilder.Create<T>(property.ToMember());
            
            if (property.PropertyType.IsGenericType 
                &&  typeof(IEnumerable<EntityBase>).IsAssignableFrom(property.PropertyType))
            {
                var childType = property.PropertyType.GetGenericArguments()[0];
                
                if (ownership == Ownership.Shared)
                {
                    MapManyToMany<T>(mapping, property, childType);
                }
                else
                {
                    MapOneToMany<T>(mapping, property, ownership, childType);
                }                
            }
            else if (property.PropertyType.IsSubclassOf(typeof(EntityBase)))
            {
                MapReference<T>(mapping, property, ownership);
            }
            else if (property.PropertyType.Name.EndsWith("Component") || property.PropertyType.Namespace.Contains("Components"))
            {
                //TODO: AutoMap components
                //MapComponent<T>(mapping, property, ownership) or some such.
            }
            else if (property.PropertyType.IsInterface)
            {
                //TODO: AutoMap anys
            }
            else if (IsEnum(property))
            {
                mapping.Map(CreatePropertyExpression<T>(property)).CustomType(property.PropertyType.AssemblyQualifiedName);
            }
            else
            {
                mapping.Map(CreatePropertyExpression<T>(property)).Column("[" + property.Name + "]");
            }
        }

        private static bool IsEnum(PropertyInfo property)
        {
            return (property.PropertyType.IsEnum 
            || (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && property.PropertyType.GetGenericArguments()[0].IsEnum))
            && !property.PropertyType.GetCustomAttributes(typeof(StoreAsStringAttribute), true).Any();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void MapManyToMany<T>(ClasslikeMapBase<T> mapping, PropertyInfo property, Type childType)
        {
            //Make generic method for MapManyToManyCollection<T,TChild>
            MethodInfo method = _mapManyToManyMethod.MakeGenericMethod(typeof(T), childType);
            method.Invoke(null, new object[] { mapping, property });
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void MapOneToMany<T>(ClasslikeMapBase<T> mapping, PropertyInfo property, Ownership ownership, Type childType)
        {
            MethodInfo method = _mapOneToManyMethod.MakeGenericMethod(typeof(T), childType);
            method.Invoke(null, new object[] { mapping, property, ownership });
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void MapReference<T>(ClasslikeMapBase<T> mapping, PropertyInfo property, Ownership ownership)
        {
            MethodInfo method = _mapReferenceMethod.MakeGenericMethod(typeof(T), property.PropertyType);
            method.Invoke(null, new object[] { mapping, property, ownership });
        }


        #region Late Bound Generic Methods

        
        [Obsolete("This method should never be called directly.", true)]
        /// <summary>
        /// Implementation for late binding generic call. Shouldn't be called directly  Called through reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="property"></param>
        /// <param name="ownership"></param>        
        private static void MapReferenceImplementation<T, TChild>(ClasslikeMapBase<T> mapping, PropertyInfo property, Ownership ownership)
        {
            var part = mapping.References(CreateReferenceExpression<T, TChild>(property));
            if (ownership == Ownership.None)
            {
                part.Cascade.None();
            }
            else if (ownership == Ownership.Exclusive)
            {
                part.Cascade.All();
            }
            else if (ownership == Ownership.Shared)
            {
                part.Cascade.SaveUpdate();
            }
            part.Column(property.Name + "ID");
            //part.ForeignKey("FK_" + typeof(T).Name + "_" + typeof(TChild).Name);
        }

        [Obsolete("This method should never be called directly.", true)]
        /// <summary>
        /// Implementation for late binding generic call. Shouldn't be called directly  Called through reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="property"></param>
        /// <param name="ownership"></param>      
        private static void MapManyToManyImplementation<T, TChild>(ClasslikeMapBase<T> mapping, PropertyInfo property)
        {
            var part = mapping.HasManyToMany<TChild>(CreateCollectionExpression<T,TChild>(property));
            
            string parentName = typeof(T).Name;
            string childName = typeof(TChild).Name;

            part.Table(parentName + childName + "Xref")
                .ParentKeyColumn(parentName + "ID")
                .ChildKeyColumn(childName + "ID")            
                .Cascade.SaveUpdate();
        }

        [Obsolete("This method should never be called directly.", true)]
        /// <summary>
        /// Implementation for late binding generic call. Shouldn't be called directly  Called through reflection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="property"></param>
        /// <param name="ownership"></param>      
        private static void MapOneToManyImplementation<T, TChild>(ClasslikeMapBase<T> mapping, PropertyInfo property, Ownership ownership)
        {
            var part = mapping.HasMany<TChild>(CreateCollectionExpression<T, TChild>(property));
            part.KeyColumn(typeof(T).Name + "ID");
            switch (ownership)
            {
                case Ownership.Exclusive:
                    part.Cascade.All();
                    break;
                case Ownership.None:
                    part.Cascade.None();
                    break;
                case Ownership.Shared:
                default:
                    part.Cascade.AllDeleteOrphan();
                    break;
            }
            //part.ForeignKeyConstraintName("FK_" + typeof(T).Name + "_" + typeof(TChild).Name);
        }

        #endregion

        private static Expression<Func<T, object>> CreatePropertyExpression<T>(PropertyInfo property)
        {
            var type = typeof(T);
            var param = Expression.Parameter(type, "entity");
            var expression = Expression.Property(param, property);
            var castedProperty = Expression.Convert(expression, typeof(object));
            return (Expression<Func<T, object>>)Expression.Lambda(castedProperty, param);
        }

        private static Expression<Func<T, TChild>> CreateReferenceExpression<T, TChild>(PropertyInfo property)
        {
            var type = typeof(T);
            var returnType = typeof(TChild);
            var param = Expression.Parameter(type, "entity");
            var expression = Expression.Property(param, property);
            var castedProperty = Expression.Convert(expression, returnType);
            return (Expression<Func<T, TChild>>)Expression.Lambda(castedProperty, param);
        }

        private static Expression<Func<T,IEnumerable<TChild>>> CreateCollectionExpression<T, TChild>(PropertyInfo property)
        {
            var type = typeof(T);
            var returnType = typeof(IEnumerable<TChild>);
            var param = Expression.Parameter(type, "entity");
            var expression = Expression.Property(param, property);
            var castedProperty = Expression.Convert(expression, returnType);
            return (Expression<Func<T,IEnumerable<TChild>>>)Expression.Lambda(castedProperty, param);
        }

        #region Type

        public static bool HasConcreteImplementation(this Type type, Type @interface)
        {
            if (!@interface.IsInterface)
                throw new ArgumentException(string.Format("{0} is not an interface.", @interface.FullName));

            if (!@interface.IsAssignableFrom(type))
                throw new ArgumentException(string.Format("{0} does not implement {1}", type.FullName, @interface.FullName));

            var interfaceProperties = @interface.GetProperties();
            foreach (var interfaceProperty in interfaceProperties)
            {
                PropertyInfo property = type.GetProperty(interfaceProperty.Name,
                                                         BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetGetMethod().IsAbstract)
                    return false;
            }
            return true;
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        #endregion
    }
}
