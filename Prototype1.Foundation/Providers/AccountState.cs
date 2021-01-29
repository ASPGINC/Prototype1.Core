using System;

namespace Prototype1.Foundation.Providers
{
    [Flags]
    public enum AccountStatus
    {
        None = 0,
        Closed = 1,
        PasswordReset = 2
    }
}
