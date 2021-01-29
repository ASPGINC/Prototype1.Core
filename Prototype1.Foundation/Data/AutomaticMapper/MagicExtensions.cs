using System;
using FluentNHibernate.Mapping;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public static class MagicExtensions
    {
        public static TableBuilder Add(this AutomaticMapper mapper)
        {
            return new TableBuilder(mapper);
        }

        public static JoinBuilder<T> JoinTo<T>(this ClasslikeMapBase<T> map)
            where T : EntityBase
        {
            return new JoinBuilder<T>(map, AutomaticMapper.Current);
        }

        public static SubclassBuilder<T> AddSubclass<T>(this ClasslikeMapBase<T> mapping)
            where T : EntityBase
        {
            return new SubclassBuilder<T>(mapping, AutomaticMapper.Current);
        }

        public static void JoinContext<T>(this ClasslikeMapBase<T> mapping, Type joinContext, Action<ClasslikeMapBase<T>> action)
        {
            using (AutomaticMapper.Current.OverrideJoinContext("[" + joinContext.Name + "]", joinContext.Name + "ID"))
            {
                if (action != null)
                    action(mapping);
            }
        }
    }
}
