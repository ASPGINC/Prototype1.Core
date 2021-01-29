namespace Prototype1.Foundation.Unity
{
    public interface IStorage
    {
        object GetValue(string key);
        void SetValue(string key, object value);
    }
}
