namespace FluentNHibernate.Conventions.Instances
{
    public interface IGeneratedInstance
    {
        void Never();
        void Insert();
        void Always();
    }
}