using System;
using Prototype1.Foundation.Data;

namespace Prototype1.Foundation.Providers
{
    [Serializable]
    public class AccountLoginInfoBase : EntityBase
    {
        public virtual string LoginProvider { get; set; }
        public virtual string ProviderKey { get; set; }
        public virtual string AccountId { get; set; }
    }
}
