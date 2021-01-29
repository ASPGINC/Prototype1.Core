namespace Prototype1.Foundation.Models
{
    public abstract class change_password_base
    {
        public abstract string old_password { get; set; }
        public abstract string new_password { get; set; }  
    }
}