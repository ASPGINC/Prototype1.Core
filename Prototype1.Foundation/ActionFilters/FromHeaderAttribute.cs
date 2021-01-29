using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Prototype1.Foundation.ActionFilters
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class FromHeaderAttribute : ParameterBindingAttribute
    {
        public override HttpParameterBinding GetBinding(HttpParameterDescriptor parameter)
        {
            return new HeaderParameterBinding(parameter);
        }
    }

    public class HeaderParameterBinding : HttpParameterBinding
    {
        private const string HEADER_MISSING = "Required header {0} was not present on request";
        private const string HEADER_INCORRECT_FORMAT = "Required header {0} was not in the correct format";
        public HeaderParameterBinding(HttpParameterDescriptor descriptor) : base(descriptor)
        {
        }

        public override Task ExecuteBindingAsync(System.Web.Http.Metadata.ModelMetadataProvider metadataProvider, 
            HttpActionContext actionContext, System.Threading.CancellationToken cancellationToken)
        {
            var headerValue = actionContext.Request.Headers.GetHeaderValue(Descriptor.ParameterName);
            var type = GetNonNullableType(Descriptor.ParameterType);

            var isNullOrEmpty = string.IsNullOrEmpty(headerValue);
            if (type == typeof(string) && isNullOrEmpty)
                headerValue = "";

            if (type != typeof(string) && type == Descriptor.ParameterType && string.IsNullOrEmpty(headerValue))
            {
                actionContext.ModelState.AddModelError(Descriptor.ParameterName, string.Format(HEADER_MISSING, Descriptor.ParameterName));
            }
            else
            {
                var converted = ConvertTo(Descriptor.ParameterType, headerValue);
                if(converted == null)
                    actionContext.ModelState.AddModelError(Descriptor.ParameterName, string.Format(HEADER_INCORRECT_FORMAT, Descriptor.ParameterName));
                actionContext.ActionArguments[Descriptor.ParameterName] = converted;
            }

            return Task.FromResult(default(AsyncVoid));
        }

        private struct AsyncVoid { }

        private Type GetNonNullableType(Type parameterType)
        {
            if (!parameterType.IsGenericType)
                return parameterType;

            var genericTypeDef = parameterType.GetGenericTypeDefinition();
            return genericTypeDef == typeof (Nullable<>) 
                                        ? parameterType.GetGenericArguments().First() 
                                        : parameterType;
        }

        private object ConvertTo(Type destinationType, string value)
        {
            TypeConverter converter = TypeDescriptor.GetConverter(destinationType);
            bool canConvertFrom = converter.CanConvertFrom(value.GetType());
            if (!canConvertFrom)
            {
                converter = TypeDescriptor.GetConverter(value.GetType());
            }

            try
            {
                object convertedValue = (canConvertFrom)
                                            ? converter.ConvertFrom(null /* context */, CultureInfo.CurrentCulture, value)
                                            : converter.ConvertTo(null /* context */, CultureInfo.CurrentCulture, value, destinationType);
                return convertedValue;
            }
            catch (Exception ex)
            {
                string message = String.Format("Could not convert header value from string to {0}", destinationType.FullName);
                throw new InvalidOperationException(message, ex);
            }
        }
    }
}