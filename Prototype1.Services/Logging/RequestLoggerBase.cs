using System;
using System.Threading.Tasks;
using NHibernate;
using Prototype1.Foundation.Data;
using Prototype1.Foundation.Logging;

namespace Prototype1.Services.Logging
{
    public abstract class RequestLoggerBase
    {
        private readonly ISessionFactory _sessionFactory;
        private readonly IExceptionLogger _exceptionLogger;

        protected RequestLoggerBase(ISessionFactory sessionFactory, IExceptionLogger exceptionLogger)
        {
            _sessionFactory = sessionFactory;
            _exceptionLogger = exceptionLogger;
        }

        public void LogRequest(RequestLogBase log)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    using (var session = _sessionFactory.OpenSession())
                    using (var tx = session.BeginTransaction())
                    {
                        session.Save(log);
                        tx.Commit();
                    }
                }
                catch (Exception ex)
                {
                    _exceptionLogger.LogException(ex);
                }
            });
        }
    }
}