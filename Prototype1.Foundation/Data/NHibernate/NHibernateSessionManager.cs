using System;
using System.Web;
using System.Runtime.Remoting.Messaging;
using NHibernate;

namespace Prototype1.Foundation.Data.NHibernate
{
    /// <summary>
    /// Handles creation and management of sessions and transactions.  It is a singleton because
    /// building the initial session factory is very expensive. Inspiration for this class came
    /// from Chapter 8 of Hibernate in Action by Bauer and King.  Although it is a sealed singleton
    /// you can use TypeMock (http://www.typemock.com) for more flexible testing.
    /// </summary>
    public sealed class NHibernateSessionManager
    {
        private ISessionFactory _sessionFactory;

        #region Thread-safe, lazy Singleton

        /// <summary>
        /// This is a thread-safe, lazy singleton.  See http://www.yoda.arachsys.com/csharp/singleton.html
        /// for more details about its implementation.
        /// </summary>
        public static NHibernateSessionManager Instance
        {
            get { return Nested.nHibernateNHibernateSessionManager; }
        }

        /// <summary>
        /// Initializes the NHibernate session factory upon instantiation.
        /// </summary>
        private NHibernateSessionManager()
        {
        }

        /// <summary>
        /// Assists with ensuring thread-safe, lazy singleton
        /// </summary>
        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly NHibernateSessionManager nHibernateNHibernateSessionManager = new NHibernateSessionManager();
        }

        #endregion

        public void InitSessionFactory(ISessionFactory sessionFactory)
        {
            if(_sessionFactory != null)
                throw new ArgumentException("Session manager has already been initialized.  You cannot initialize the session manager twice.");
            _sessionFactory = sessionFactory;
        }

        public void TrackChanges(object obj)
        {
            GetSession().Lock(obj, LockMode.None);
        }

        public ISession GetSession()
        {
            ISession session = ThreadSession;

            if (session == null || !session.IsOpen)
            {
                session = _sessionFactory.OpenSession();
                session.EnableFilter("PermanentRecordFilter").SetParameter("deleted", false);
                session.FlushMode = IsInWebContext()
                                ? FlushMode.Manual
                                : FlushMode.Commit;
                ThreadSession = session;
            }

            return session;
        }

        public void CloseSession()
        {
            ISession session = ThreadSession;
            ThreadSession = null;

            if (session != null && session.IsOpen)
            {
                session.Close();
                session.Dispose();
            }
        }

        public bool IsInitialized()
        {
            return _sessionFactory != null;
        }

        private ITransaction ThreadTransaction
        {
            get
            {
                if (IsInWebContext())
                {
                    return (ITransaction)HttpContext.Current.Items[TRANSACTION_KEY];
                }
                else
                {
                    return (ITransaction)CallContext.GetData(TRANSACTION_KEY);
                }
            }
            set
            {
                if (IsInWebContext())
                {
                    HttpContext.Current.Items[TRANSACTION_KEY] = value;
                }
                else
                {
                    CallContext.SetData(TRANSACTION_KEY, value);
                }
            }
        }

        private ISession ThreadSession
        {
            get
            {
                if (IsInWebContext())
                {
                    return (ISession)HttpContext.Current.Items[SESSION_KEY];
                }
                else
                {
                    return (ISession)CallContext.GetData(SESSION_KEY);
                }
            }
            set
            {
                if (IsInWebContext())
                {
                    HttpContext.Current.Items[SESSION_KEY] = value;
                }
                else
                {
                    CallContext.SetData(SESSION_KEY, value);
                }
            }
        }

        private static bool IsInWebContext()
        {
            return HttpContext.Current != null;
        }

        private const string TRANSACTION_KEY = "CONTEXT_TRANSACTION";
        private const string SESSION_KEY = "CONTEXT_SESSION";
    }
}
