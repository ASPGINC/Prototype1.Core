using System;
using FluentNHibernate.Cfg;

namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class AutomaticMapper
    {
        internal static AutomaticMapper Current 
        { 
            get; set; 
        }

        public MappingConfiguration Config { get; protected set; }
        public JoinContext JoinContext { get; set; }

        public IDisposable OverrideJoinContext<T>()
        {
            string tableName = typeof(T).Name;
            string keyColumn = tableName + "ID";
            return this.OverrideJoinContext(tableName, keyColumn);
        }

        public IDisposable OverrideJoinContext(string tableName, string keyColumn)
        {
            return this.Override(x => x.JoinContext, new JoinContext(tableName, keyColumn));
        }

        public AutomaticMapper(MappingConfiguration config)
        {
            AutomaticMapper.Current = this;
            this.Config = config;            
        }

        public void Map(Action<AutomaticMapper> action)
        {
            action(this);
        }

        public void LoadInterfaceMappingsFromAssemblyOf<T>()
        {
            MappingExtensions.ExtractInterfaceMappingsFromAssemblyOf<T>();
        }
    }
}
