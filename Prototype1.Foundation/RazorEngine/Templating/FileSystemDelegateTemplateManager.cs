using System;
using System.IO;
using System.Web.Hosting;
using RazorEngine.Templating;

namespace Prototype1.Foundation.RazorEngine.Templating
{
    public class FileSystemDelegateTemplateManager : DelegateTemplateManager
    {
        protected static Func<string, string> GetTemplateDelegate = s => GetTemplate(s);

        public FileSystemDelegateTemplateManager()
            : base(GetTemplateDelegate)
        {
        }

        public FileSystemDelegateTemplateManager(Func<string, string> resolver)
            : base(resolver)
        {
        }

        protected override string Resolve(string key)
        {
            var result = "";

            if (GetTemplateDelegate != Resolver)
                result = GetTemplateDelegate(key);

            return result.IsNullOrEmpty() ? base.Resolve(key) : result;
        }

        private static string GetTemplate(string templatename)
        {
            if (string.IsNullOrEmpty(templatename))
                return "";

            var contents = "";
            if (HostingEnvironment.VirtualPathProvider == null)
            {
                using (var stream = new StreamReader(HostingEnvironment.MapPath(templatename)))
                    contents = stream.ReadToEnd();
            }
            else if (HostingEnvironment.VirtualPathProvider.FileExists(templatename))
            {
                var vf = HostingEnvironment.VirtualPathProvider.GetFile(templatename);
                using (var stream = vf.Open())
                using (var reader = new StreamReader(stream))
                    contents = reader.ReadToEnd();
            }
            return contents;
        }
    }
}