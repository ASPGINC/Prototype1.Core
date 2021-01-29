using System;
using System.Web.Mvc;

namespace Prototype1.Foundation.Web.Mvc
{
    public class DateTimeOffsetModelBinder : IModelBinder
    {
        public DateTimeOffsetModelBinder() { }

        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException("bindingContext");
            }

            //Maybe we're lucky and they just want a DateTimeOffset the regular way.
            DateTimeOffset? dateTimeOffsetAttempt = GetA<DateTimeOffset>(bindingContext, string.Empty);
            if (dateTimeOffsetAttempt != null)
            {
                return dateTimeOffsetAttempt.Value;
            }

            //If they haven't set Date, set "date" and get ready for an attempt
            if (!this.DateTimeSet)
            {
                this.DateTimePart = "DateTimePart";
            }

            //If they haven't set time, set "Hour", "Minute", "Period" and get ready for an attempt
            if (!this.OffsetSet)
            {
                this.Offset = "OffsetPart";
            }

            var dateTime = GetA<DateTime>(bindingContext, this.DateTimePart).GetValueOrDefault(DateTime.MinValue);
            var dateTimeOffset = new DateTimeOffset(dateTime, GetA<TimeSpan>(bindingContext, this.Offset).GetValueOrDefault(TimeSpan.Zero));

            return dateTimeOffset;
        }

        private Nullable<T> GetA<T>(ModelBindingContext bindingContext, string key) where T : struct
        {
            return (Nullable<T>)GetX<T>(bindingContext, key);
        }
        
        private object GetX<T>(ModelBindingContext bindingContext, string key)
        {
            ValueProviderResult valueResult;
            if (String.IsNullOrEmpty(key))
            {
                //if we have no key, we're just gonna try to use the prefix for regular dates.
                valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            }
            else
            {
                //Try it with the prefix...
                valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName + "." + key);
                //Didn't work? Try without the prefix if needed...
                if (valueResult == null && bindingContext.FallbackToEmptyPrefix == true)
                {
                    valueResult = bindingContext.ValueProvider.GetValue(key);
                }
            }
            if (valueResult == null)
            {
                return null;
            }

            object result;
            try
            {
                result = valueResult.ConvertTo(typeof(T));
            }
            catch
            {
                result = null;
            }
            return result;
        }

        public string DateTimePart { get; set; }
        public string Offset { get; set; }

        public bool DateTimeSet { get { return !String.IsNullOrEmpty(DateTimePart); } }

        public bool OffsetSet { get { return !String.IsNullOrEmpty(Offset); } }
    }

}
