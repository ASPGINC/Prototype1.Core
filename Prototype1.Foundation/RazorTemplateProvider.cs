using System;
using System.IO;
using System.Net;
using System.Web.Hosting;
using Microsoft.Practices.Unity;
using Prototype1.Foundation.Logging;
using Prototype1.Foundation.Unity;
using RazorEngine;
using RazorEngine.Templating;

namespace Prototype1.Foundation
{
    public static class RazorTemplateProvider
    {
        public static string Apply<T>(T model, string templatename, string baseUrl = "~/Views/EmailTemplates/{0}.cshtml")
        {
            var parsed = "";

            try
            {
                var template = GetTemplate(templatename, baseUrl);
                //parsed = WebUtility.HtmlDecode(Engine.Razor.RunCompile(GetTemplate(templatename, baseUrl), null, model));
                parsed = WebUtility.HtmlDecode(Razor.Parse(template, model, string.Concat(templatename, "_", template.MD5Hash()))); 
            }
            catch (Exception ex)
            {
                var exceptionLogger = Container.Instance.Resolve<IExceptionLogger>();
                exceptionLogger.LogException(ex);
            }
            return parsed;
        }

        private static string GetTemplate(string templatename, string baseUrl)
        {
            var contents = "";
            if (HostingEnvironment.VirtualPathProvider == null)
            {
                using (var stream = new StreamReader(Path.Combine(HostingEnvironment.MapPath(baseUrl), string.Format("{0}.cshtml", templatename))))
                    contents = stream.ReadToEnd();
            }
            else if (HostingEnvironment.VirtualPathProvider.FileExists(string.Format(baseUrl, templatename)))
            {
                var vf = HostingEnvironment.VirtualPathProvider.GetFile(string.Format(baseUrl, templatename));
                using (var stream = vf.Open())
                    using (var reader = new StreamReader(stream))
                        contents = reader.ReadToEnd();
            }
            return contents;
        }
    }
}