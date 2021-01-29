using System;

namespace Prototype1.Foundation.Data
{
    [Serializable]
    [Audit(false)]
    public abstract class RequestLogBase : EntityBase
    {
        protected RequestLogBase()
        {
            Submitted = DateTime.UtcNow;
        }

        public abstract string ServiceTypeName { get; set; }
        public abstract string Method { get; set; }
        public abstract string Url { get; set; }
        public abstract string AdditionalInfo { get; set; }
        public abstract string Request { get; set; }
        public abstract string RequestHeaders { get; set; }
        public abstract DateTime Submitted { get; set; }
        public abstract string Username { get; set; }
    }

    [Serializable]
    [Audit(false)]
    public abstract class RequestResponseLogBase : RequestLogBase
    {
        public abstract string Response { get; set; }
        public abstract string ResponseHeaders { get; set; }
    }
}