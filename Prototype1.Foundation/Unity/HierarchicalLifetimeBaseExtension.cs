using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;

namespace Prototype1.Foundation.Unity
{
    public class HierarchicalLifetimeBaseExtension : UnityContainerExtension
	{
		protected override void Initialize()
		{
			Context.Strategies.Clear();
			Context.Strategies.AddNew<BuildKeyMappingStrategy>(UnityBuildStage.TypeMapping);
			Context.Strategies.AddNew<HierarchicalLifetimeBaseStrategy>(UnityBuildStage.Lifetime);
			Context.Strategies.AddNew<HierarchicalLifetimeStrategy>(UnityBuildStage.Lifetime);
			Context.Strategies.AddNew<LifetimeStrategy>(UnityBuildStage.Lifetime);

			Context.Strategies.AddNew<ArrayResolutionStrategy>(UnityBuildStage.Creation);
			Context.Strategies.AddNew<BuildPlanStrategy>(UnityBuildStage.Creation);
		}
	}
}
