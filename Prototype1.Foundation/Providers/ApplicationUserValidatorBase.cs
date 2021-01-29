using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;

namespace Prototype1.Foundation.Providers
{
    public abstract class ApplicationUserValidatorBase : IIdentityValidator<IApplicationUser>
    {
        public virtual async Task<IdentityResult> ValidateAsync(IApplicationUser item)
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(item.UserName) && item.UserName.Trim().Length > 0)
                errors.Add("Username cannot be empty");

            if (string.IsNullOrEmpty(item.Id))
            {
                if (IsDuplicated(item.UserName))
                    errors.Add("Account with this UserName already exists");
            }
            else
            {
                Guid userId;
                if (Guid.TryParse(item.Id, out userId))
                {
                    if (IsDuplicated(item.UserName, userId))
                        errors.Add("Account with this UserName already exists");
                }
                else
                {
                    errors.Add("Invalid format for Id");
                }
            }

            return errors.Any() ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        protected abstract bool IsDuplicated(string username, Guid? id = null);
    }
}