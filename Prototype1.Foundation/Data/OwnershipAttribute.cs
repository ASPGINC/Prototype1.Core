using System;

namespace Prototype1.Foundation.Data
{
    public enum Ownership
    {
        None,
        Shared,
        Exclusive
    }

    public class OwnershipAttribute : Attribute
    {
        public readonly Ownership Ownership;
        public OwnershipAttribute(Ownership ownership)
        {
            this.Ownership = ownership;
        }
    }
}
