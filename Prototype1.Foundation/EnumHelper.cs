using System;
using System.Collections.Generic;
using System.Linq;

namespace Prototype1.Foundation
{
    public static class EnumHelper
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return typeof(T).FilterEnum().OfType<T>();
        }

        public static int GetAllFlagsValue(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("Type must be an enum.");

            return type.FilterEnum()
                .Cast<int>()
                .Where(x => x != 0)
                .Aggregate(0, (current, flag) => current | flag);
        }

        public static T GetAllFlagsValue<T>() where T : struct
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException("Type must be an enum.");

            var value = type.FilterEnum()
                .Cast<int>()
                .Where(x => x != 0)
                .Aggregate(0, (current, flag) => current | flag);

            return (T)Enum.Parse(type, value.ToString(), true);
        }
    }
}
