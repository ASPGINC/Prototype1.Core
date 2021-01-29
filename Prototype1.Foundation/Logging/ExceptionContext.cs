using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using FluentNHibernate.Utils;

namespace Prototype1.Foundation.Logging
{
    public class ExceptionContext
    {
        public ExceptionContext(HttpContext currentHttpContext)
        {
            if(currentHttpContext == null)
                return;

            HttpMethod = currentHttpContext.Request.HttpMethod;
            Url = currentHttpContext.Request.Url.ToString();
            QueryString = currentHttpContext.Request.QueryString.Cast<string>()
                .ToDictionary(p => p, p => currentHttpContext.Request.QueryString[p].ToString());
            UserHostAddress = currentHttpContext.Request.UserHostAddress;
            if (HttpMethod.In("PUT", "POST"))
                RequestBody = GetRequestBody(currentHttpContext);

            if (currentHttpContext.User != null)
                UserID = GetUserId(currentHttpContext.User.Identity);
        }

        public static string GetRequestBody(HttpContext context)
        {
            using (var bodyStream = new StreamReader(context.Request.InputStream))
            {
                bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
                var bodyText = bodyStream.ReadToEnd();
                return bodyText;
            }
        }

        public string HttpMethod { get; set; }

        public string Url { get; set; }

        public string RequestBody { get; set; }

        public IDictionary<string, string> QueryString { get; set; }

        public string UserHostAddress { get; set; }

        public string UserID { get; set; }

        private static string GetUserId(IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var ci = identity as ClaimsIdentity;
            if (ci != null)
            {
                return FindFirstValue(ci, ClaimTypes.NameIdentifier);
            }
            return null;
        }

        private static string FindFirstValue(ClaimsIdentity identity, string claimType)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var claim = identity.FindFirst(claimType);
            return claim != null ? claim.Value : null;
        }
    }
}