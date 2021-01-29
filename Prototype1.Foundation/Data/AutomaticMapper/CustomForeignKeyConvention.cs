using System;
using System.Linq;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class HasManyForeignKeyConstraintNamingConvention : IHasManyConvention
    {
        public void Apply(IOneToManyCollectionInstance instance)
        {
            try
            {
                instance.Key.ForeignKey(
                    string.Format("FK_{0}_{1}_{2}",
                        instance.Relationship.Class.Name.CleanBaseClassName(),
                        instance.EntityType.Name.CleanBaseClassName(),
                        instance.Key.Columns.First().Name.Trim('[',']')).Trim('_'));
            }
            catch { }
        }
    }

    public class ReferenceForeignKeyConstraintNamingConvention : IReferenceConvention
    {
        public void Apply(IManyToOneInstance instance)
        {
            var entityType = (typeof(EntityBase).IsAssignableFrom(instance.EntityType)
                ? instance.EntityType
                : instance.EntityType.GetFirstAbstractBaseType()).Name;
            instance.ForeignKey(string.Format("FK_{0}_{1}_{2}",
                instance.Class.Name.CleanBaseClassName(),
                entityType.CleanBaseClassName(),
                instance.Columns.First().Name.Trim('[', ']')).Trim('_'));
        }
    }

    public class HasOneForeignKeyConstraintNamingConvention : IHasOneConvention
    {
        public void Apply(IOneToOneInstance instance)
        {
            instance.ForeignKey(string.Format("FK_{0}_{1}_{2}ID",
                instance.Name.CleanBaseClassName(),
                instance.EntityType.Name.CleanBaseClassName(),
                instance.Name.CleanBaseClassName()));
        }
    }

    public class HasManyToManyForeignKeyConstraintNamingConvention : IHasManyToManyConvention
    {
        public void Apply(IManyToManyCollectionInstance instance)
        {
            var entityType = (typeof(EntityBase).IsAssignableFrom(instance.EntityType)
                ? instance.EntityType
                : instance.EntityType.GetFirstAbstractBaseType()).Name;
            instance.Key.ForeignKey(string.Format("FK_{0}_{1}_{2}",
                entityType.CleanBaseClassName(),
                instance.ChildType.Name.CleanBaseClassName(),
                ((ICollectionInspector)instance).Name.CleanBaseClassName()));
        }
    }

    public static class TypeExtensions
    {
        public static Type GetFirstAbstractBaseType(this Type type)
        {
            while (true)
            {
                if (type == null)
                {
                    throw new ArgumentNullException("type");
                }
                var baseType = type.BaseType;
                if (baseType == null || baseType.IsAbstract)
                {
                    return baseType;
                }
                type = baseType;
            }
        }

        public static string CleanBaseClassName(this string name)
        {
            return name.EndsWith("Base")
                ? name.Substring(0, name.Length - 4)
                : name;
        }
    }
}
