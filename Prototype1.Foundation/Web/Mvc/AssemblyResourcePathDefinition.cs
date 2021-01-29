using System.Reflection;

namespace Prototype1.Foundation.Web.Mvc
{
	public class AssemblyResourcePathDefinition
	{
		public Assembly Assembly { get; set; }
		public string VirtualPath { get; set; }
		public string ResourceName { get; set; }
	}
}
