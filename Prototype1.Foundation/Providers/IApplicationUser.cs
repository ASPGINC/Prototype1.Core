using System;
using Microsoft.AspNet.Identity;

namespace Prototype1.Foundation.Providers
{
    public interface IApplicationUser : IUser
    {
        string FirstName { get; set; }

        string LastName { get; set; }

        string Email { get; set; }

        string HashedPassword { get; set; }

        string PasswordResetToken { get; set; }
        
        DateTime? PasswordResetTokenExpirationDate { get; set; }

        AccountStatus Status { get; set; }
    }
}