using System;
using System.Reflection;

namespace Prototype1.Foundation.Data.Listener
{
    public class EntityBaseAuditLogEntryFactory : AuditLogEntryFactoryBase<EntityBase>
    {
        public override bool Applicable(Type t)
        {
            var auditAttribute = t.GetCustomAttribute(typeof(AuditAttribute), true).As<AuditAttribute>();
            return (auditAttribute == null || auditAttribute.AuditChanges) && base.Applicable(t);
        }
    }
}