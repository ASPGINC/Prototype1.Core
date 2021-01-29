using System;
using System.Linq;
using FluentNHibernate.Mapping;
using System.Reflection;
using System.Linq.Expressions;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class TableBuilder
    {
        private readonly AutomaticMapper _mapper;

        public TableBuilder(AutomaticMapper mapper)
        {
            _mapper = mapper;
        }

        public void TableForHierarchy<T>()
            where T: EntityBase
        {
            this.TableForHierarchy<T>(null);
        }

        public void TableForHierarchy<T>(Action<ClassMap<T>> action)
            where T : EntityBase
        {
            this.TableFor(action, null, true);
        }

        public void TableForHierarchy<T>(string tableName, Action<ClassMap<T>> action)
            where T : EntityBase
        {
            this.TableFor(action, tableName, true);
        }

        public void TableFor<T>()
            where T : EntityBase
        {
            this.TableFor<T>(null, null, false);
        }

        public void TableFor<T>(Action<ClassMap<T>> action)
            where T : EntityBase
        {
            this.TableFor(action, null, false);
        }

        private void TableFor<T>(Action<ClassMap<T>> action, string tableName, bool discriminated)
            where T : EntityBase
        {
            Type t = typeof(T);
            var classMap = new ClassMap<T>();
            tableName = string.IsNullOrEmpty(tableName) ? t.Name : tableName;

            PropertyInfo versionProperty = t.GetProperties().Where(p => p.GetCustomAttributes(typeof(VersionAttribute), true).Any()).FirstOrDefault();
            if (versionProperty != null)
            {
                classMap.Id(m => m.ID).Column(tableName + "ID").Access.Property().GeneratedBy.Assigned();
                classMap.Version(CreatePropertyExpression<T>(versionProperty)).Access.CamelCaseField(Prefix.Underscore).UnsavedValue("-1");
            }
            else
            {
                classMap.Id(m => m.ID).Column(tableName + "ID").Access.Property().GeneratedBy.GuidComb();
            }

            classMap.Table("[" + tableName + "]");
            if (discriminated)
                classMap.DiscriminateSubClassesOnColumn("Discriminator");

            classMap.AutoMap();
            if (action != null)
                action(classMap);


            _mapper.Config.FluentMappings.Add(classMap);
        }

        public void MapTable<T>(string tableName = null)
        {
            MapTable<T>(null, tableName);
        }

        public void MapTable<T>(Action<ClassMap<T>> action, string tableName = null)
        {
            Type t = typeof(T);
            var classMap = new ClassMap<T>();
            tableName = string.IsNullOrEmpty(tableName) ? t.Name : tableName;


            classMap.Table("[" + tableName + "]");

            classMap.AutoMap();
            if (action != null)
                action(classMap);


            _mapper.Config.FluentMappings.Add(classMap);
        }

        private static Expression<Func<T, object>> CreatePropertyExpression<T>(PropertyInfo property)
        {
            var type = typeof(T);
            var param = Expression.Parameter(type, "entity");
            var expression = Expression.Property(param, property);
            var castedProperty = Expression.Convert(expression, typeof(object));
            return (Expression<Func<T, object>>)Expression.Lambda(castedProperty, param);
        }
    }
}
