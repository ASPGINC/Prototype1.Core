using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web;

namespace Prototype1.Foundation.Web.Mvc
{
    public class CustomizableJsonResult : JsonResult
    {
        private readonly List<JavaScriptConverter> _converters = new List<JavaScriptConverter>();
        private readonly bool _bypassSerialization;

        public CustomizableJsonResult()
        {            
        }

        public CustomizableJsonResult(JsonRequestBehavior behavior, bool bypassSerialization)
            : this()
        {
            this.JsonRequestBehavior = behavior;
            _bypassSerialization = bypassSerialization;
        }

        public CustomizableJsonResult(JsonRequestBehavior behavior, params JavaScriptConverter[] converters) : this()
        {
            this.JsonRequestBehavior = behavior;
            if (converters != null)
                _converters.AddRange(converters);
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (JsonRequestBehavior == JsonRequestBehavior.DenyGet &&
                String.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Json Get Request Denied.  Change the JsonRequestBehavior if you wish to allow get requests.");
            }

            HttpResponseBase response = context.HttpContext.Response;

            if (!String.IsNullOrEmpty(ContentType))
            {
                response.ContentType = ContentType;
            }
            else
            {
                response.ContentType = "application/json";
            }
            if (ContentEncoding != null)
            {
                response.ContentEncoding = ContentEncoding;
            }
            if (Data != null && !_bypassSerialization)
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                if (_converters.Any())
                    serializer.RegisterConverters(_converters);

                response.Write(serializer.Serialize(Data));
            }
            else
            {
                response.Write(Data);
            }
        }
    }
}
