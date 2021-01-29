using System;

namespace Prototype1.Foundation.Events
{
    public interface IEventInterceptor<T> : IDisposable
        where T : IDomainEvent
    {
        /// <summary>
        /// Events will only be intercepted if the condition is met
        /// </summary>
        /// <param name="condition">Predicate to determine which events to intercept</param>
        /// <returns></returns>
        IDisposable When(Func<T, bool> condition);
    }
}