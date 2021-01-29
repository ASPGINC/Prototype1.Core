using Prototype1.Foundation.Providers;

namespace Prototype1.Foundation.Interfaces
{
    public interface IFormsAuthenticationService
    {
        void SignIn(AccountBase account, bool createPersistentCookie);
        void SignOut();
    }
}