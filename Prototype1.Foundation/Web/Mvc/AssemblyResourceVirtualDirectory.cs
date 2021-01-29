using System;
using System.Web.Hosting;

namespace Prototype1.Foundation.Web.Mvc
{
	public class AssemblyResourceVirtualDirectory : VirtualDirectory
	{
		public AssemblyResourceVirtualDirectory(string virtualPath) 
			: base(virtualPath)
		{
		}


		public override System.Collections.IEnumerable Children
		{
			get { throw new NotImplementedException(); }
		}

		public override System.Collections.IEnumerable Directories
		{
			get { throw new NotImplementedException(); }
		}

		public override System.Collections.IEnumerable Files
		{
			get { throw new NotImplementedException(); }
		}
	}
}
