namespace Prototype1.Foundation.Models
{
    public abstract class reset_password_base
    {
        public abstract string username { get; set; }

        public abstract string security_key { get; set; }

        public abstract string new_password { get; set; }
    }
}
