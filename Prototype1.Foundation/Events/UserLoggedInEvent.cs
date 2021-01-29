using System;

namespace Prototype1.Foundation.Events
{
    public class UserLoggedInEvent : IDomainEvent
    {
        public UserLoggedInEvent(EventArgs args)
        {
            this.Args = args;
        }

        public EventArgs Args { get; private set; }

        public struct EventArgs
        {
            public Guid UserID;
            public string UserName;
        }
    }
}