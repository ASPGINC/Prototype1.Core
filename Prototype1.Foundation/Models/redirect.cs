using System;
using System.Dynamic;

namespace Prototype1.Foundation.Models
{
    [Serializable]
    public class redirect
    {
        public string state { get; set; }
        public ExpandoObject @params { get; set; }

        public redirect(string state, ExpandoObject @params = null)
        {
            this.state = state;
            this.@params = @params;
        }
    }
}