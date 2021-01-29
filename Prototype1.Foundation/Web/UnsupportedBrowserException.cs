using System;

namespace Prototype1.Foundation.Web
{
    public class UnsupportedBrowserException : Exception
    {
        public UnsupportedBrowserException()
            : base()
        {
        }

        public UnsupportedBrowserException(string message)
            : base(message)
        {
        }

        public UnsupportedBrowserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
