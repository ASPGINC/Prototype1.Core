using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;

namespace Prototype1.Foundation.Web.Mvc
{
    /// <summary>
    /// ASP.NET MVC Default Dictionary Model Binder
    /// </summary>
    public class DefaultDictionaryModelBinder : CollectionPreservingModelBinder
    {
        readonly IModelBinder _nextBinder;

        /// <summary>
        /// Create an instance of DefaultDictionaryBinder.
        /// </summary>
        public DefaultDictionaryModelBinder()
            : this(null)
        {
        }

        /// <summary>
        /// Create an instance of DefaultDictionaryBinder.
        /// </summary>
        /// <param name="nextBinder">The next model binder to chain call. If null, by default, the DefaultModelBinder is called.</param>
        public DefaultDictionaryModelBinder(IModelBinder nextBinder)
        {
            this._nextBinder = nextBinder;
        }

        private IEnumerable<string> GetValueProviderKeys(ControllerContext context)
        {
            var keys = new List<string>();
            keys.AddRange(context.HttpContext.Request.Form.Keys.Cast<string>());
            keys.AddRange(((IDictionary<string, object>)context.RouteData.Values).Keys);
            keys.AddRange(context.HttpContext.Request.QueryString.Keys.Cast<string>());
            keys.AddRange(context.HttpContext.Request.Files.Keys.Cast<string>());
            return keys;
        }

        private object ConvertType(string stringValue, Type type)
        {
            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter == null)
                throw new InvalidOperationException();

            return typeConverter.ConvertFrom(stringValue);
        }

        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            Type modelType = bindingContext.ModelType;
            Type idictType = modelType.GetInterface("System.Collections.Generic.IDictionary`2");
            if (idictType != null)
            {
                object result = null;

                Type[] ga = idictType.GetGenericArguments();
                IModelBinder valueBinder = Binders.GetBinder(ga[1]);

                foreach (string key in GetValueProviderKeys(controllerContext))
                {
                    if (key.StartsWith(bindingContext.ModelName + "[", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int endbracket = key.IndexOf("]", bindingContext.ModelName.Length + 1);
                        if (endbracket == -1)
                            continue;

                        object dictKey;
                        try
                        {
                            dictKey = ConvertType(key.Substring(bindingContext.ModelName.Length + 1, endbracket - bindingContext.ModelName.Length - 1), ga[0]);
                        }
                        catch (NotSupportedException)
                        {
                            continue;
                        }

                        var modelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => null, ga[1]);
                        modelMetadata.Model = idictType.GetProperty("Item").GetValue(bindingContext.Model, new[] {dictKey});
                        var innerBindingContext = new ModelBindingContext
                        {
                            ModelMetadata = modelMetadata,
                            ModelName = key.Substring(0, endbracket + 1),
                            ModelState = bindingContext.ModelState,
                            PropertyFilter = bindingContext.PropertyFilter,
                            ValueProvider = bindingContext.ValueProvider
                        };
                        object newPropertyValue = valueBinder.BindModel(controllerContext, innerBindingContext);

                        if (result == null)
                            result = CreateModel(controllerContext, bindingContext, modelType);

                        if (!(bool)idictType.GetMethod("ContainsKey").Invoke(result, new[] { dictKey }))
                            idictType.GetProperty("Item").SetValue(result, newPropertyValue, new[] { dictKey });
                    }
                }

                return result;
            }

            if (_nextBinder != null)
            {
                return _nextBinder.BindModel(controllerContext, bindingContext);
            }

            return base.BindModel(controllerContext, bindingContext);
        }
    }
}