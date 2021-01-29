using System;

namespace Prototype1.Foundation.Logging
{
    public interface IExceptionLogger
    {
        void LogException(Exception ex, string info = "", ExceptionContext currentContext = null);
    }
}
