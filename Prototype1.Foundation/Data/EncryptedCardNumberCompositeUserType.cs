using System;
using System.Data;
using System.Data.Common;
using NHibernate;
using NHibernate.Engine;
using NHibernate.Type;
using NHibernate.UserTypes;
using Prototype1.Security;

namespace Prototype1.Foundation.Data
{
    public class EncryptedCardNumberCompositeUserType : ICompositeUserType
    {
        public object GetPropertyValue(object component, int property)
        {
            var encryptedCreditCard = (EncryptedCardNumber)component;
            if (property == 0)
                return encryptedCreditCard.Number;
            else
                return encryptedCreditCard.DecryptionKeyID;
        }

        public void SetPropertyValue(object component, int property, object value)
        {
            var encryptedCreditCard = (EncryptedCardNumber)component;
            if (property == 0)
                encryptedCreditCard.Number = (string)value;
            else
                encryptedCreditCard.DecryptionKeyID = (int)value;
        }

        public bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(object x)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }
            return x.GetHashCode();
        }

        public object NullSafeGet(DbDataReader dr, string[] names, ISessionImplementor session, object owner)
        {
            if (dr == null) return null;
            var numberColumn = names[0];
            var decryptionKeyIDColumn = names[1];
            var number = (string)NHibernateUtil.String.NullSafeGet(dr, numberColumn, session, owner);

            if (number.IsNullOrEmpty())
                return new EncryptedCardNumber();

            var decryptionKeyID = (int)NHibernateUtil.Int32.NullSafeGet(dr, decryptionKeyIDColumn, session, owner);

            return new EncryptedCardNumber
            {
                Number = Aes256Encryption.DecryptString(number, decryptionKeyID),
                DecryptionKeyID = decryptionKeyID
            };
        }

        public void NullSafeSet(DbCommand cmd, object value, int index, bool[] settable, ISessionImplementor session)
        {
            if (value == null)
                return;

            var encryptedCreditCard = (EncryptedCardNumber)value;

            if (encryptedCreditCard.Number.IsNullOrEmpty())
            {
                NHibernateUtil.String.NullSafeSet(cmd, null, index, session);
                NHibernateUtil.Int32.NullSafeSet(cmd, null, index + 1, session);
            }
            else
            {
                NHibernateUtil.String.NullSafeSet(cmd, Aes256Encryption.Encrypt(encryptedCreditCard.Number), index, session);
                NHibernateUtil.Int32.NullSafeSet(cmd, Aes256Encryption.NewestDecryptionKeyID, index + 1, session);
            }
        }

        public object DeepCopy(object value)
        {
            var encryptedCreditCard = (EncryptedCardNumber)value;
            return new EncryptedCardNumber
            {
                Number = encryptedCreditCard.Number,
                DecryptionKeyID = encryptedCreditCard.DecryptionKeyID
            };
        }

        public object Disassemble(object value, ISessionImplementor session)
        {
            return DeepCopy(value);
        }

        public object Assemble(object cached, ISessionImplementor session, object owner)
        {
            return DeepCopy(cached);
        }

        public object Replace(object original, object target, ISessionImplementor session, object owner)
        {
            return DeepCopy(original);
        }

        public string[] PropertyNames { get { return new string[0]; } }
        public IType[] PropertyTypes { get { return new IType[] { NHibernateUtil.String, NHibernateUtil.Int32 }; } }
        public Type ReturnedClass { get { return typeof(EncryptedCardNumber); } }
        public bool IsMutable { get { return true; } }
    }
}
