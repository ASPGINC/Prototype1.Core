namespace Prototype1.Foundation.Events
{
    public interface IHandles<T> where T : IDomainEvent
    {
        void Handle(T args);
    }
}