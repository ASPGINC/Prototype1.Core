using System;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class InterfaceMappingAttribute : Attribute
    {
        public Type Type;
        public InterfaceMappingAttribute()
        {
        }

        public InterfaceMappingAttribute(Type type)
        {
            if (!type.IsInterface)
                throw new ArgumentException("InterfaceMappingAttribute expects an interface as it's type parameter");
            Type = type;
        }
    }
}
