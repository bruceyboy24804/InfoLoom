using System;
using System.Collections.Generic;
using Game;
using Game.Economy;
using InfoLoomTwo;
using InfoLoomTwo.Extensions;

namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData
{
    public partial class IndustrialUISystem :ExtendedUISystemBase
    {
        private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;
        private ValueBindingHelper<int[]> m_IndustrialBinding;
        public override GameMode gameMode => GameMode.Game;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_IndustrialBinding = CreateBinding("ilIndustrial", new int[16]);
            m_ExcludedResourcesBinding = CreateBinding("ilIndustrialExRes", new string[0]);
            Mod.log.Info("IndustrialUISystem created.");
        }
        protected override void OnUpdate()
        {
            var industrialSystem = base.World.GetOrCreateSystemManaged<IndustrialSystem>();
            
            // Convert the excluded resources to a list of strings
            m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
            m_ExcludedResourcesBinding.Value = 
                industrialSystem.m_ExcludedResources.value == Resource.NoResource 
                ? new string[0] 
                : ExtractExcludedResources(industrialSystem.m_ExcludedResources.value);

            base.OnUpdate();
        }
        private string[] ExtractExcludedResources(Resource excludedResources)
        {
            List<string> excludedResourceNames = new List<string>();

            // Check if all resources are excluded
            if (excludedResources == Resource.All)
            {
                return new string[] { Resource.All.ToString() };
            }

            foreach (Resource resource in Enum.GetValues(typeof(Resource)))
            {
                // Skip 'NoResource' and 'All'
                if ((excludedResources & resource) != 0 && 
                    resource != Resource.NoResource && 
                    resource != Resource.All)
                {
                    excludedResourceNames.Add(resource.ToString());
                }
            }

            return excludedResourceNames.ToArray();
        }
    }
}