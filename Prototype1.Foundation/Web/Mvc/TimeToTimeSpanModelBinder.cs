using System;
using System.Web.Mvc;
using System.Globalization;

namespace Prototype1.Foundation.Web.Mvc
{
    public class TimeToTimeSpanModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            string time = valueResult.AttemptedValue;

            if(string.IsNullOrEmpty(time) && bindingContext.ModelType.IsNullable())
                return null;

            DateTime parsed;
            if (DateTime.TryParseExact(time, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.TimeOfDay;
            }
            else if (DateTime.TryParseExact(time, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.TimeOfDay;
            }
            else if (DateTime.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.TimeOfDay;
            }
            else if (DateTime.TryParseExact(time, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.TimeOfDay;
            }
            else
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Time was not in the expected format.  Ex. 11:00 AM, 1:00 PM");
                return null;
            }
        }
    }
}
