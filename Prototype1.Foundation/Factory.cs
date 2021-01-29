using System;

namespace Prototype1.Foundation
{
    [Serializable]
    public class Factory<T>
    {
        private readonly Func<T> _creator;

        public Factory(Func<T> creator)
        {
            _creator = creator;
        }

        public T Create()
        {
            return _creator.Invoke();
        }
    }
}
