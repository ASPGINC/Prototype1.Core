using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate.UserTypes;
using NHibernate.SqlTypes;
using NHibernate;
using System.ComponentModel;

namespace Prototype1.Foundation.Data
{
    public class ObjectAsStringUserType : IUserType
    {
        public object Assemble(object cached, object owner)
        {
            return DeepCopy(cached);
        }

        public object DeepCopy(object value)
        {
            return Clone(value);
        }

        public object Disassemble(object value)
        {
            return DeepCopy(value);
        }

        public bool Equals(object x, object y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public bool IsMutable
        {
            get { return true; }
        }

        public object NullSafeGet(System.Data.IDataReader rs, string[] names, object owner)
        {
            var val = (string) NHibernateUtil.String.NullSafeGet(rs, names[0]);

            var valTypeString = (string) NHibernateUtil.String.NullSafeGet(rs, names[1]);

            if (val == null || valTypeString == null)
                return null;

            return GetConverter(valTypeString).ConvertFromInvariantString(val);
        }

        private static readonly Dictionary<string, TypeConverter> Converters = new Dictionary<string, TypeConverter>();
        private static TypeConverter GetConverter(string valTypeString)
        {
            if (Converters.ContainsKey(valTypeString))
                return Converters[valTypeString];

            var valType = Type.GetType(valTypeString);
            var converter = TypeDescriptor.GetConverter(valType);
            Converters[valTypeString] = converter;
            return converter;
        }

        public void NullSafeSet(System.Data.IDbCommand cmd, object value, int index)
        {
            var val = GetConverter(value.GetType().AssemblyQualifiedName).ConvertToInvariantString(value);
            var valType = value.GetType().AssemblyQualifiedName;

            NHibernateUtil.String.NullSafeSet(cmd, val, index);
            NHibernateUtil.String.NullSafeSet(cmd, valType, index + 1);
        }

        public object Replace(object original, object target, object owner)
        {
            return Clone(original);
        }

        public Type ReturnedType
        {
            get { return typeof(object); }
        }

        public SqlType[] SqlTypes
        {
            get { return new SqlType[] { new SqlType(System.Data.DbType.String), new SqlType(System.Data.DbType.String) }; }
        }

        private T Clone<T>(T obj)
        {
            if (obj == null)
                return default(T);

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Context = new System.Runtime.Serialization.StreamingContext(System.Runtime.Serialization.StreamingContextStates.Clone);
                formatter.Serialize(stream, obj);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }
    }
}
