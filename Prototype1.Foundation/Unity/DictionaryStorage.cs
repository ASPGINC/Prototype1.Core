using System.Collections.Generic;

namespace Prototype1.Foundation.Unity
{
    public class DictionaryStorage : IStorage
    {
        private readonly Dictionary<string, object> _dictionary = new Dictionary<string, object>();
        public object GetValue(string key)
        {
            if (!_dictionary.ContainsKey(key))
                return null;

            return _dictionary[key];
        }

        public void SetValue(string key, object value)
        {
            _dictionary[key] = value;
        }
    }
}
