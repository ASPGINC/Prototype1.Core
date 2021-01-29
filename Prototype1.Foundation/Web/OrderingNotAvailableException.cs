using System;

namespace Prototype1.Foundation.Web
{
    public class OrderingNotAvailableException : Exception
    {
        public OrderingNotAvailableException()
            : base()
        {
        }

        public OrderingNotAvailableException(string message)
            : base(message)
        {
        }

        public OrderingNotAvailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
