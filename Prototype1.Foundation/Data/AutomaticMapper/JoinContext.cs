namespace Prototype1.Foundation.Data.AutomaticMapper
{
    public class JoinContext
    {
        public JoinContext()
        {
        }

        public JoinContext(string tableName, string keyColumnName)
        {
            this.TableName = tableName;
            this.KeyColumnName = keyColumnName;
        }

        public string TableName { get; set; }
        public string KeyColumnName { get; set; }
    }
}
