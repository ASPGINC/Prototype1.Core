using Microsoft.AspNet.Identity;
using Prototype1.Security;

namespace Prototype1.Foundation.Providers
{
    public abstract class PasswordHasherBase : IPasswordHasher
    {
        public virtual string HashPassword(string password)
        {
            return Crypto.HashPassword(password);
        }

        public virtual PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            return Crypto.VerifyHashedPassword(hashedPassword, providedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}