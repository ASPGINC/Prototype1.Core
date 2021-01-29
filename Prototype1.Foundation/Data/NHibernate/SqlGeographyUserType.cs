using System;
using NHibernate;
using NHibernate.SqlTypes;
using Microsoft.SqlServer.Types;
using System.Data;
using System.Data.SqlTypes;
using NHibernate.UserTypes;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Prototype1.Foundation.Data.NHibernate
{
    public class SqlGeographyUserType : IUserType
    {
        public new bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
                return true;

            var xGeo = x as SqlGeography;
            var yGeo = y as SqlGeography;

            if (xGeo == null || yGeo == null)
                return false;

            return xGeo.STEquals(yGeo).Equals(SqlBoolean.True);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            object prop1 = NHibernateUtil.String.NullSafeGet(rs, names[0]);
            if (prop1 == null)
                return null;

            SqlGeography geo = SqlGeography.Parse(new SqlString(prop1.ToString()));
            return geo;
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
	        object val = DBNull.Value;
            var geography = value as SqlGeography;
            if (geography != null && !geography.IsNull)
			{
				var chars = ((SqlGeography) value).STAsText();
				if (chars != null)
					val = chars.Value;
			}

	        ((IDataParameter) cmd.Parameters[index]).Value = val;

        }

        public object DeepCopy(object value)
        {
            if (value == null)
                return null;

            var sourceTarget = (SqlGeography)value;
            SqlGeography targetGeography = null;

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, sourceTarget);
                stream.Position = 0;
                targetGeography = (SqlGeography)formatter.Deserialize(stream);
            }

            return targetGeography;
        }

        public object Replace(object original, object target, object owner)
        {
            return DeepCopy(original);
        }

        public object Assemble(object cached, object owner)
        {
            return DeepCopy(cached);
        }

        public object Disassemble(object value)
        {
            return DeepCopy(value);
        }

        public SqlType[] SqlTypes
        {
            get { return new[] { NHibernateUtil.StringClob.SqlType }; }
        }

        public Type ReturnedType
        {
            get { return typeof(SqlGeography); }
        }

        public bool IsMutable
        {
            get { return true; }
        }
    }
}
