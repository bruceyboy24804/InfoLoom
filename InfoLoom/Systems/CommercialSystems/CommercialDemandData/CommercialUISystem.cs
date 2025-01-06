using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Json;
using Colossal.UI.Binding;
using Game;
using Game.Economy;
using InfoLoomTwo.Extensions;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData
{
    public partial class CommercialUISystem : ExtendedUISystemBase
    {
         private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;
         private ValueBindingHelper<int[]> m_CommercialBinding;
         //private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;
         public override GameMode gameMode => GameMode.Game;
         
         protected override void OnCreate()
        {
            base.OnCreate();
            
            m_CommercialBinding = CreateBinding("ilCommercial", new int[10]);
            m_ExcludedResourcesBinding = CreateBinding("ilCommercialExRes", new string[0]);
           
            
            
            Mod.log.Info("CommercialUISystem created.");
        }
        protected override void OnUpdate()
        {
            var commercialSystem = base.World.GetOrCreateSystemManaged<CommercialSystem>();
            
            // Convert the excluded resources to a list of strings
            m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
            m_ExcludedResourcesBinding.Value = 
                commercialSystem.m_ExcludedResources.value == Resource.NoResource 
                ? new string[0] 
                : ExtractExcludedResources(commercialSystem.m_ExcludedResources.value);

            base.OnUpdate();
        }

// Helper method to extract the resource names
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
   