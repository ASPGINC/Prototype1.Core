using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Prototype1.Foundation.Web.Mvc
{
    /// <summary>
    /// Adds inheritance support when registering model binders.
    /// Any model binders added here will be invoked if the Type being bound inherits from the type registered.
    /// </summary>
    public class InheritanceAwareModelBinderProvider : Dictionary<Type, IModelBinder>, IModelBinderProvider
    {
        public IModelBinder GetBinder(Type modelType)
        {
            var binders = from binder in this                       
                          where binder.Key.IsAssignableFrom(modelType)                        
                          select binder.Value;         
            return binders.FirstOrDefault();
        }
    } 
}
