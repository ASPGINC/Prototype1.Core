using System;

namespace Prototype1.Foundation.Data
{
    public class AuditAttribute : Attribute
    {
        public readonly bool AuditChanges;
        public AuditAttribute(bool auditChanges = true)
        {
            this.AuditChanges = auditChanges;
        }
    }
}
