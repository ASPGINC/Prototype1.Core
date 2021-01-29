using System;
using Prototype1.Foundation.Data;

namespace Prototype1.Foundation.Providers
{
    [Serializable]
    public abstract class AccountBase : EntityBase
    {
        public abstract string FirstName { get; set; }

        public abstract string LastName { get; set; }

        public abstract string Username { get; set; }

        public abstract string Email { get; set; }

        public abstract string HashedPassword { get; set; }

        public abstract string PasswordResetToken { get; set; }

        public abstract DateTime? PasswordResetTokenExpirationDate { get; set; }

        public abstract AccountStatus Status { get; set; }

        public abstract DateTime DateCreated { get; set; }

        public abstract DateTime? DateLastLoggedIn { get; set; }
    }
}
