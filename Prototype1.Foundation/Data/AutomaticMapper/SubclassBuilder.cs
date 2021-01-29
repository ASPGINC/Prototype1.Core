using System;
using FluentNHibernate.Mapping;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class SubclassBuilder<T>
        where T : EntityBase
    {
        private readonly ClasslikeMapBase<T> _parentMapping;
        private readonly AutomaticMapper _mapper;

        public SubclassBuilder(ClasslikeMapBase<T> mapping, AutomaticMapper mapper)
        {
            _parentMapping = mapping;
            _mapper = mapper;
        }

        public void OfType<TSubType>()
            where TSubType : EntityBase
        {
            this.Subclass<TSubType>(_parentMapping, null);
        }

        public void OfType<TSubType>(Action<ClasslikeMapBase<TSubType>> action)
            where TSubType : EntityBase
        {
            this.Subclass<TSubType>(_parentMapping, action);
        }


        private void Subclass<TSubType>(ClasslikeMapBase<T> mapping, Action<ClasslikeMapBase<TSubType>> action)
            where TSubType : EntityBase
        {
            var t = typeof(T);
            var st = typeof(TSubType);

            var subclassMap = new SubclassMap<TSubType>();
            subclassMap.Extends<T>();
            if (_mapper.JoinContext != null)
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
            else
            {
                if (subclassMap.AnythingToMap())
                    subclassMap.AutoMap();
                if (action != null)
                    action(subclassMap);
            }

            _mapper.Config.FluentMappings.Add(subclassMap);
        }
    }
}
