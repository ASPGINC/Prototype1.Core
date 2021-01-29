using System;
using FluentNHibernate.Mapping;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class JoinBuilder<T>
         where T : EntityBase
    {
        private readonly ClasslikeMapBase<T> _parentMapping;
        private readonly AutomaticMapper _mapper;

        public JoinBuilder(ClasslikeMapBase<T> mapping, AutomaticMapper mapper)
        {
            _mapper = mapper;
            _parentMapping = mapping;
        }

        public void TableFor<SubType>()
            where SubType : EntityBase
        {
            this.Join<SubType>(_parentMapping, null,null);
        }

        public void TableFor<SubType>(Action<ClasslikeMapBase<SubType>> action)
            where SubType : EntityBase
        {
            this.Join<SubType>(_parentMapping, action, null);
        }

        public void TableFor<SubType>(string tableName, Action<ClasslikeMapBase<SubType>> action)
           where SubType : EntityBase
        {
            this.Join<SubType>(_parentMapping, action, tableName);
        }

        private void Join<SubType>(ClasslikeMapBase<T> map, Action<ClasslikeMapBase<SubType>> action, string tableName)
            where SubType : EntityBase
        {
            var t = typeof(T);
            var st = typeof(SubType);
            tableName = string.IsNullOrEmpty(tableName) ? st.Name : tableName;

            var subclassMap = new SubclassMap<SubType>();
            subclassMap.Extends<T>();

            using (_mapper.OverrideJoinContext("[" + tableName + "]", tableName + "ID"))
            {
                if (subclassMap.AnythingToMap())
                {
                    subclassMap.Join(_mapper.JoinContext.TableName, x =>
                    {
                        x.KeyColumn(_mapper.JoinContext.KeyColumnName);
                        x.AutoMap();
                        if (action != null)
                            action(x);
                    });
                }
                else if (action != null)
                {
                    action(subclassMap);
                }
            }

            _mapper.Config.FluentMappings.Add(subclassMap);
        }
    }
}
