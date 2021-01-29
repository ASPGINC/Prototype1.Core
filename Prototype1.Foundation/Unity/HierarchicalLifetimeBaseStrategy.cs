using System;
using Microsoft.Practices.ObjectBuilder2;

namespace Prototype1.Foundation.Unity
{
    public class HierarchicalLifetimeBaseStrategy : BuilderStrategy
	{
		/// <summary>
		/// Called during the chain of responsibility for a build operation. The
		/// PreBuildUp method is called when the chain is being executed in the
		/// forward direction.
		/// </summary>
		/// <param name="context">Context of the build operation.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public override void PreBuildUp(IBuilderContext context)
		{
			IPolicyList lifetimePolicySource;

			var activeLifetime = context.PersistentPolicies.Get<ILifetimePolicy>(context.BuildKey, out lifetimePolicySource);
			if (activeLifetime is IHierarchicalLifetimeManagerBase && !ReferenceEquals(lifetimePolicySource, context.PersistentPolicies))
			{
			    var lifetime = activeLifetime as IHierarchicalLifetimeManagerBase;
			    var newLifetime = lifetime.Duplicate();
				context.PersistentPolicies.Set<ILifetimePolicy>(newLifetime, context.BuildKey);
				// Add to the lifetime container - we know this one is disposable
			    if (newLifetime is IDisposable)
			    {
			        context.Lifetime.Add(newLifetime); 
			    }
			}
		}
	}
}
