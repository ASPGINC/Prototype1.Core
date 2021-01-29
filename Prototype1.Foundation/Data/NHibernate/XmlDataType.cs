using System;
using System.Data;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace Prototype1.Foundation.Data.NHibernate
{
    public class XmlDataType : IUserType
    {
        //private static readonly ILog log = LogManager.GetLogger( typeof( XmlDataType ) );
 
        private static SqlType[] _sqlTypes = new SqlType[] { new StringClobSqlType() };
        
        public object NullSafeGet( IDataReader rs, string[] names, object owner )
        {
            string text;
            object value = rs.GetValue( rs.GetOrdinal( names[0] ) );
            if( value == DBNull.Value )
                return null;
 
            text = (string)value;
            XmlData data = new XmlData();
            data.String = text;
            return data;
        }
 
        public void NullSafeSet( IDbCommand cmd, object value, int index )
        {
            if( value != null )
            {
                string str = ((XmlData)value).String;
                if( str.Length > 0) 
                {
                    ((IDataParameter)cmd.Parameters[index]).Value = str;
                    return;
                }
            }
 
            ((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
        }
 
        public object DeepCopy( object value )
        {
            if( value == null )
                return null;
 
            XmlData data = (XmlData)value;
            XmlData copy = new XmlData( data.NamespaceManager );
            copy.String = data.String;
            return copy;
        }
 
        public SqlType[] SqlTypes
        {
            get { return _sqlTypes; }
        }
 
        public Type ReturnedType
        {
            get { return typeof( XmlData ); }
        }
 
        public bool IsMutable
        {
            get { return true; }
        }
 
        bool IUserType.Equals( object x, object y )
        {
            if( x == null && y == null )
                // Both of them are null.
                return true;
            
            XmlData d1 = x as XmlData;
            XmlData d2 = y as XmlData;
 
            if( d1 == null || d2 == null )
                // 1. Both are XmlData, but not both null.
                // 2. One of given files is not of the right type.
                return false;
 
            return d1.String.Equals( d2.String );
        }

        public object Assemble(object cached, object owner)
        {
            return DeepCopy(cached);
        }

        public object Disassemble(object value)
        {
            return DeepCopy(value);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public object Replace(object original, object target, object owner)
        {
            return DeepCopy(original);
        }
    }
}
 
