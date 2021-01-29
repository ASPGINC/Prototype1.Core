using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using Microsoft.AspNet.Identity;

namespace Prototype1.Foundation.Providers
{
    public abstract class ApplicationUserStoreBase<TUser, TAccount> : IUserPasswordStore<TUser>, IUserLoginStore<TUser>, IUserStore<TUser>
        where TUser : class, IApplicationUser
        where TAccount : AccountBase
    {
        protected const string CACHE_KEY = "ApplicationUser_{0}";

        public void Dispose()
        {
        }

        public virtual Task CreateAsync(TUser user)
        {
            return Task.Factory.StartNew(() => this.SaveUser(user));
        }

        public virtual Task UpdateAsync(TUser user)
        {
            return Task.Factory.StartNew(() => this.SaveUser(user));
        }

        public virtual Task DeleteAsync(TUser user)
        {
            return Task.Factory.StartNew(() =>
            {
                var acct = this.GetAccountForUser(user);
                if (acct != null)
                {
                    DeleteUser(acct);
                }
            });
        }

        public Task<TUser> FindByIdAsync(string userId)
        {
            Guid id;
            if (!Guid.TryParse(userId, out id))
                throw new ArgumentException("Invalid format for userId", nameof(userId));

            TUser user = null;
            try
            {
                user = HttpRuntime.Cache[string.Format(CACHE_KEY, userId.ToLower())] as TUser
                           ?? GetAndCacheUser(a => a.ID == id);
            }
            catch
            {
            }
            return Task.FromResult(user);
        }

        public Task<TUser> FindByNameAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentNullException(nameof(username));

            TUser user = null;
            try
            {
                user = HttpRuntime.Cache[string.Format(CACHE_KEY, username.ToLower())] as TUser
                       ?? GetAndCacheUser(a => a.Username == username);
            }
            catch
            {
            }
            return Task.FromResult(user);
        }

        public virtual Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            user.HashedPassword = passwordHash;
            return Task.Factory.StartNew(() => this.SetUserPasswordHash(user));
        }

        public virtual Task<string> GetPasswordHashAsync(TUser user)
        {
            return Task.FromResult(user.HashedPassword);
        }

        public virtual Task<bool> HasPasswordAsync(TUser user)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.HashedPassword));
        }

        public abstract Task AddLoginAsync(TUser user, UserLoginInfo login);

        public abstract Task RemoveLoginAsync(TUser user, UserLoginInfo login);

        public abstract Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user);

        public abstract Task<TUser> FindAsync(UserLoginInfo login);

        protected abstract TUser GetUser(Expression<Func<TAccount, bool>> filter);

        protected TUser GetAndCacheUser(Expression<Func<TAccount, bool>> filter)
        {
            var user = GetUser(filter);
            if (user != null) CacheApplicationUser(user);
            return user;
        }

        protected abstract TAccount GetAccountForUser(TUser user);

        protected abstract void SaveUser(TUser user);

        protected abstract void DeleteUser(TAccount account);

        protected abstract void SetUserPasswordHash(TUser user);

        protected void CacheApplicationUser(TUser user)
        {
            if (user == null) return;
            
            try
            {
                if (!user.UserName.IsNullOrEmpty())
                    HttpRuntime.Cache.Insert(string.Format(CACHE_KEY, user.UserName.ToLower()), user, null,
                        Cache.NoAbsoluteExpiration,
                        TimeSpan.FromMinutes(10));
            }
            catch
            {
            }

            try
            {
                if (!user.Id.IsNullOrEmpty())
                    HttpRuntime.Cache.Insert(string.Format(CACHE_KEY, user.Id.ToLower()), user, null,
                        Cache.NoAbsoluteExpiration,
                        TimeSpan.FromMinutes(10));
            }
            catch
            {
            }
        }
    }
}