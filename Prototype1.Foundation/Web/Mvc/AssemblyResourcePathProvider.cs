using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;

namespace Prototype1.Foundation.Web.Mvc
{
    public class AssemblyResourceProvider : VirtualPathProvider
    {
        private readonly SortedDictionary<string, AssemblyResourcePathDefinition> _files;
        public SortedDictionary<string, AssemblyResourcePathDefinition> Files { get { return _files; } }

        private readonly DateTime _assemblyLastModified;
        public DateTime AssemblyLastModified { get { return _assemblyLastModified; } }

		public AssemblyResourceProvider(Assembly assembly)
		{
			var removeLength = assembly.GetName().Name.Length + 1;
			_files = new SortedDictionary<string, AssemblyResourcePathDefinition>(assembly.GetManifestResourceNames()
						.Select(x => new AssemblyResourcePathDefinition
							{
								Assembly = assembly,
								ResourceName = x,
								VirtualPath = ResourceNameToVirtualPath(x, removeLength)
							})
				        .ToDictionary(x => x.VirtualPath), StringComparer.InvariantCultureIgnoreCase);
		    
            _assemblyLastModified = new FileInfo(assembly.Location).LastWriteTimeUtc;
		}

		private string ResourceNameToVirtualPath(string resourceName, int removeLength)
		{
			var lastDot = resourceName.LastIndexOf('.');
			var extension = resourceName.Substring(lastDot);
			var withouExtension = resourceName.Substring(0, lastDot);
			return "~/" + withouExtension.Remove(0, removeLength).Replace('.', '/') + extension;
		}

        public override bool FileExists(string virtualPath)
        {
	        return Previous.FileExists(virtualPath) || _files.ContainsKey(VirtualPathUtility.ToAppRelative(virtualPath));
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (Previous.FileExists(virtualPath))
                return Previous.GetFile(virtualPath);

	        var relative = VirtualPathUtility.ToAppRelative(virtualPath);
	        if (!_files.ContainsKey(relative))
		        return null;

	        var def = _files[relative];
            return new AssemblyResourceVirtualFile(def.Assembly, def.ResourceName, virtualPath);
        }

		public override VirtualDirectory GetDirectory(string virtualDir)
		{
			return Previous.GetDirectory(virtualDir);
		}

		public override bool DirectoryExists(string virtualDir)
		{
			return Previous.DirectoryExists(virtualDir);
		}

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies,  DateTime utcStart)
        {
	        return !Previous.FileExists(virtualPath)
		               ? null
		               : base.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }
    }

}
