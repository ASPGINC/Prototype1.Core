using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNet.Identity;
using Prototype1.Security;

namespace Prototype1.Foundation.Providers
{
    public abstract class ResetPasswordProviderBase<TUser>
        where TUser : class, IApplicationUser
    {
        protected readonly Func<UserManager<TUser>> UserManagerFactory;

        public ResetPasswordProviderBase(Func<UserManager<TUser>> userManagerFactory)
        {
            UserManagerFactory = userManagerFactory;
        }

        public virtual IdentityResult ForgotPassword(string userName, bool management)
        {
            var result = IdentityResult.Failed("Invalid username.");
            var user = UserManagerFactory().FindByName(userName);
            if (user != null)
            {
                result = SendForgotPasswordEmail(user, management);
            }
            return result;
        }

        protected abstract IdentityResult SendForgotPasswordEmail(TUser user, bool management);

        public virtual IdentityResult ResetPassword(string userName, string securityKey, string newPassword)
        {
            var result = IdentityResult.Failed("Invalid Username or Password");

            if (string.IsNullOrEmpty(securityKey) || string.IsNullOrEmpty(securityKey)
                || string.IsNullOrEmpty(newPassword))
                return result;

            var decodedSecurityKey = HttpUtility.UrlDecode(securityKey);
            var decodedBytes = Convert.FromBase64String(decodedSecurityKey);

            var securityKeyValues = Encoding.UTF8.GetString(decodedBytes).Split('|');
            // Should contain encoded user_name and reset_token
            if (securityKeyValues.Count() != 2)
                return result;

            var username = securityKeyValues[0];
            var resetToken = securityKeyValues[1];

            if (!username.Equals(userName, StringComparison.CurrentCultureIgnoreCase))
                return result;

            var user = UserManagerFactory().FindByName(username);
            if (user != null && user.Status.HasFlag(AccountStatus.PasswordReset)
                && user.PasswordResetTokenExpirationDate.HasValue
                && user.PasswordResetTokenExpirationDate.Value > DateTime.UtcNow
                && user.PasswordResetToken == resetToken)
            {
                var hashedPassword = Crypto.HashPassword(newPassword);
                if (hashedPassword.Length > 128)
                    result = IdentityResult.Failed("Invalid Username or Password");
                else
                {
                    user.HashedPassword = hashedPassword;
                    user.PasswordResetToken = null;
                    user.PasswordResetTokenExpirationDate = null;
                    user.Status &= ~AccountStatus.PasswordReset;
                    UserManagerFactory().Update(user);
                    result = IdentityResult.Success;
                }
            }
            return result;
        }

        protected virtual string GeneratePasswordResetToken(TUser account, int expirationMinutes = 30)
        {
            if (!account.PasswordResetTokenExpirationDate.HasValue
                || account.PasswordResetTokenExpirationDate.Value <= DateTime.UtcNow)
            {
                account.PasswordResetToken = GetRandomToken();
                account.PasswordResetTokenExpirationDate = DateTime.UtcNow.AddMinutes(expirationMinutes);
            }
            account.Status |= AccountStatus.PasswordReset;
            UserManagerFactory().Update(account);

            var resetToken = Encoding.UTF8.GetBytes(account.UserName + "|" + account.PasswordResetToken);
            return Convert.ToBase64String(resetToken);
        }

        protected static string GetRandomToken()
        {
            string token;
            using (var generator = new RNGCryptoServiceProvider())
            {
                var data = new byte[16];
                generator.GetBytes(data);
                token = HttpServerUtility.UrlTokenEncode(data);
            }
            return token;
        }
    }
}