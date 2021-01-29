using System.Reflection;
using System.Web.Hosting;

namespace Prototype1.Foundation.Web.Mvc
{
    public class AssemblyResourceVirtualFile : VirtualFile
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;

        public AssemblyResourceVirtualFile(Assembly assembly, string resourceName, string virtualPath)
            : base(virtualPath)
        {
            _assembly = assembly;
	        _resourceName = resourceName;
        }

        public override System.IO.Stream Open()
        {
            return _assembly.GetManifestResourceStream(_resourceName);
        }
    }
}
