using System;
using System.Web;
using System.Web.Security;
using Prototype1.Foundation.Data.NHibernate;
using Prototype1.Foundation.Events;
using Prototype1.Foundation.Interfaces;
using Prototype1.Foundation.Providers;

namespace Prototype1.Services
{
    public class FormsAuthenticationService : IFormsAuthenticationService
    {
        private readonly IEntityRepository _dataContext;

        public FormsAuthenticationService(IEntityRepository dataContext)
        {
            _dataContext = dataContext;
        }

        public void SignIn(AccountBase account, bool createPersistentCookie)
        {
            if (createPersistentCookie)
            {
                HttpContext.Current.Response.Cookies["FirstName"].Value = account.FirstName;
                HttpContext.Current.Response.Cookies["FirstName"].Expires = DateTime.Now.AddDays(7);
                HttpContext.Current.Response.Cookies["Username"].Value = account.Username;
                HttpContext.Current.Response.Cookies["Username"].Expires = DateTime.Now.AddDays(7);
            }
            else
            {
                HttpContext.Current.Response.Cookies["FirstName"].Expires = DateTime.Now.AddYears(-30);
                HttpContext.Current.Response.Cookies["Username"].Expires = DateTime.Now.AddYears(-30);
            }

            account.DateLastLoggedIn = DateTime.Now;
            _dataContext.Save(account);

            HttpContext.Current.Session["Authenticated"] = true;

            System.Web.HttpCookie authcookie = FormsAuthentication.GetAuthCookie(account.Username, false);
            //authcookie.Domain = ConfigSettings.Current.Domain;
            HttpContext.Current.Response.AppendCookie(authcookie);
            FormsAuthentication.SetAuthCookie(account.Username, false);
            EventManager.Raise(new UserLoggedInEvent(new UserLoggedInEvent.EventArgs
            {
                UserID = account.ID,
                UserName = account.Username
            }));
        }

        public void SignOut()
        {
            if (HttpContext.Current.Session != null)
            {
                HttpContext.Current.Session["Authenticated"] = false;
            }

            FormsAuthentication.SignOut();
            EventManager.Raise(new UserLoggedOutEvent());
        }
    }
}