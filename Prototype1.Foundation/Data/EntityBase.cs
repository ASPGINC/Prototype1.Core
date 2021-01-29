using System;

namespace Prototype1.Foundation.Data
{
    [Serializable]
    public abstract class EntityBase : IIdentifiable<Guid>
    {
        protected Guid _ID;

        public static Guid NewCombGuid()
        {
            var dateBytes = BitConverter.GetBytes(DateTime.Now.Ticks);
            var guidBytes = Guid.NewGuid().ToByteArray();
            // copy the last six bytes from the date to the last six bytes of the GUID 
            Array.Copy(dateBytes, dateBytes.Length - 7, guidBytes, guidBytes.Length - 7, 6);
            return new Guid(guidBytes);

        }

        [NotExported]
        public virtual Guid ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public override bool Equals(object obj)
        {
            var toCompare = obj as EntityBase;
            if (toCompare == null)
                return false;

            if (this.ID == Guid.Empty)
                return this == toCompare;

            return this.ID == toCompare.ID && obj.GetType() == this.GetType();
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + this.ID.GetHashCode();
        }
    }
}
