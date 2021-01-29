using System;

namespace Prototype1.Foundation.Data
{
    [Serializable]
    public class TrackedEntityUpdate : EntityBase
    {
        public virtual string TableName { get; set; }
        public virtual Guid TrackedKey { get; set; }
        public virtual DateTime LastUpdated { get; set; }
    }
}