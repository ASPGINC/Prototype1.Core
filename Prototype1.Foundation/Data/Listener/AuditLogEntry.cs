using System;

namespace Prototype1.Foundation.Data.Listener
{
    public class AuditLogEntry : EntityBase
    {
        public AuditLogEntry()
        {
            TimeStamp = DateTimeOffset.Now;
        }

        public virtual Guid EntityID { get; set; }
        public virtual string EntityType { get; set; }
        public virtual string Event { get; set; }
        public virtual string PropertyName { get; set; }
        public virtual string OldValue { get; set; }
        public virtual string NewValue { get; set; }
        public virtual DateTimeOffset TimeStamp { get; set; }
        public virtual string Username { get; set; }
    }
}