using System;

namespace Prototype1.Security
{
    [Serializable]
    public class EncryptedCardNumber
    {
        private string _number;

        public virtual string Number
        {
            get { return _number; }
            set
            {
                if (value == null || !value.Contains("*"))
                    _number = value;
            }
        }

        public virtual int DecryptionKeyID { get; set; }
    }
}
