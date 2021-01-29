using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prototype1.Foundation.Unity;
using Microsoft.Practices.Unity;

namespace Prototype1.Foundation.Events
{
    public static class EventManager
    {
        [ThreadStatic]
        private static List<Delegate> _callbacks;

        [ThreadStatic]
        private static Dictionary<Type, Stack<EventInterceptor>> _interceptors;

        [ThreadStatic]
        private static List<EventInterceptor> _interceptorList;

        public static void Register<T>(Action<T> callback)
        {
            if (_callbacks == null)
                _callbacks = new List<Delegate>();
            _callbacks.Add(callback);
        }

        public static void ClearCallbacks()
        {
            _callbacks = null;
        }

        public static void Raise(IDomainEvent args)
        {
            var argsType = args.GetType();
            var method = RaiseMethod.MakeGenericMethod(argsType);
            method.Invoke(null, new object[] { args });
        }

        private static readonly MethodInfo RaiseMethod = typeof(EventManager).GetMethod("RaiseImpl",
                                                                                          BindingFlags.Static |
                                                                                          BindingFlags.NonPublic);
        private static void RaiseImpl<T>(T args)
             where T : IDomainEvent
        {
            if (Intercepted(args)) return;

            if (_callbacks != null)
                _callbacks.OfType<Action<T>>().Apply(a => a(args));
            Container.Instance.ResolveAll<IHandles<T>>().Apply(a => a.Handle(args));
        }

        private static bool Intercepted<T>(T args)
            where T : IDomainEvent
        {
            if (_interceptors != null)
            {
                var firstInterceptor = _interceptorList.LastOrDefault(x => x is EventInterceptor<T> || x is EventInterceptor<IDomainEvent>);
                if (firstInterceptor != null)
                {
                    return (firstInterceptor is EventInterceptor<T>)
                               ? firstInterceptor.As<EventInterceptor<T>>().Intercept(args)
                               : firstInterceptor.As<EventInterceptor<IDomainEvent>>().Intercept(args);
                }

            }
            return false;
        }

        /// <summary>
        /// Intercepts and Aggregates Events of a specific type, 
        /// and raises the event created by the eventFactory when disposed.
        /// Events that are intercepted will not be passed to other handlers.
        /// </summary>
        /// <typeparam name="T">The type of event you wish to aggregate</typeparam>
        /// <param name="action">Action performed on the events when the interceptor is disposed.  
        /// Will only fire if any events are intercepted</param>
        /// <returns></returns>
        public static IEventInterceptor<T> Aggregate<T>(Action<IEnumerable<T>> action)
            where T : class, IDomainEvent
        {
            return AddInterceptor(new EventAggregator<T>(action));
        }

        /// <summary>
        /// Intercepts and Aggregates all IDomainEvents, 
        /// and raises the event created by the eventFactory when disposed.
        /// Events that are intercepted will not be passed to other handlers.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEventInterceptor<IDomainEvent> AggregateAll(Action<IEnumerable<IDomainEvent>> action)
        {
            return AddInterceptor(new EventAggregator<IDomainEvent>(action));
        }


        /// <summary>
        /// Intercepts events of the specified type, and executes the action on them.
        /// Events that are intercepted will not be passed to other handlers.
        /// </summary>
        /// <typeparam name="T">The type of event you wish to aggregate</typeparam>
        /// <param name="action">Action performed on the event each time it is intercepted</param>
        /// <returns></returns>
        public static IEventInterceptor<T> Intercept<T>(Action<T> action)
            where T : class,IDomainEvent
        {
            return AddInterceptor(new EventInterceptor<T>(action));
        }

        public static IEventInterceptor<IDomainEvent> InterceptAll(Action<IDomainEvent> action)
        {
            return AddInterceptor(new EventInterceptor<IDomainEvent>(action));
        }

        private static IEventInterceptor<T> AddInterceptor<T>(EventInterceptor<T> interceptor)
            where T : IDomainEvent
        {
            if (_interceptors == null)
                _interceptors = new Dictionary<Type, Stack<EventInterceptor>>();

            if (_interceptorList == null)
                _interceptorList = new List<EventInterceptor>();

            Stack<EventInterceptor> stack;
            Stack<EventInterceptor> value;
            if (!_interceptors.TryGetValue(interceptor.EventType, out value))
            {
                stack = new Stack<EventInterceptor>();
                _interceptors.Add(interceptor.EventType, stack);
            }
            else
            {
                stack = value;
            }
            stack.Push(interceptor);
            _interceptorList.Add(interceptor);
            return interceptor;
        }

        private static void RemoveInterceptor(EventInterceptor interceptor)
        {
            if (_interceptors == null || _interceptorList == null) return;

            var stack = _interceptors[interceptor.EventType];
            var top = stack.Pop();
            if (top != interceptor)
                throw new ArgumentException("An undisposed interceptor was at the top of the stack");
            if (!stack.Any())
                _interceptors.Remove(interceptor.EventType);
            _interceptorList.Remove(interceptor);
            if (!_interceptors.Any())
                _interceptors = null;
        }

        private abstract class EventInterceptor : IDisposable
        {
            public abstract Type EventType { get; }
            public virtual void Dispose()
            {
                RemoveInterceptor(this);
            }
        }

        private class EventAggregator<T> : EventInterceptor<T>
            where T : IDomainEvent
        {
            private readonly Action<IEnumerable<T>> _action;
            private readonly Type _type = typeof(T);
            private readonly List<T> _events = new List<T>();

            public EventAggregator(Action<IEnumerable<T>> action)
            {
                _action = action;
            }

            public override void Dispose()
            {
                base.Dispose();
                if (_events.Any())
                    _action.Invoke(_events);
            }

            public override Action<T> Action
            {
                get { return _events.Add; }
            }

            public override Type EventType
            {
                get { return _type; }
            }
        }

        private class EventInterceptor<T> : EventInterceptor, IEventInterceptor<T>
            where T : IDomainEvent
        {
            private readonly Action<T> _action;
            private readonly Type _type;
            private Func<T, bool> _condition = x => true;

            protected EventInterceptor()
            {
            }

            public EventInterceptor(Action<T> action)
            {
                _type = typeof(T);
                _action = action;
            }


            public IDisposable When(Func<T, bool> condition)
            {
                _condition = condition;
                return this;
            }

            public virtual bool Intercept(T domainEvent)
            {
                if (_condition.Invoke(domainEvent))
                {
                    Action.Invoke(domainEvent);
                    return true;
                }
                return false;
            }

            public virtual Action<T> Action
            {
                get { return _action; }
            }

            public override Type EventType { get { return _type; } }


        }
    }
}