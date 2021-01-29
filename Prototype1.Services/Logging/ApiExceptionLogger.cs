using System.Web.Http.Dependencies;
using System.Web.Http.ExceptionHandling;
using IExceptionLogger = Prototype1.Foundation.Logging.IExceptionLogger;

namespace Prototype1.Services.Logging
{
    public class ApiExceptionLogger : ExceptionLogger
    {
        private readonly IExceptionLogger _exceptionLogger;

        public ApiExceptionLogger(IDependencyResolver dependencyResolver)
        {
            _exceptionLogger = dependencyResolver.GetService(typeof(IExceptionLogger)) as IExceptionLogger;
        }

        public override void Log(ExceptionLoggerContext context)
        {
            _exceptionLogger.LogException(context.Exception);
        }
    }
}