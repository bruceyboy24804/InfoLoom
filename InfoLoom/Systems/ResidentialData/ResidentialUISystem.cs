using Game;
using Game.City;
using Game.Prefabs;
using Game.Simulation;
using InfoLoomTwo.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace InfoLoomTwo.Systems.ResidentialData
{
    public partial class ResidentialUISystem : ExtendedUISystemBase
    {
        

         public ValueBindingHelper<int[]> m_ResidentialBinding;
        
         private SimulationSystem m_SimulationSystem;  // Declare it here
         
         public override GameMode gameMode => GameMode.Game;
         
         protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();  // Initialize it here
            
            m_ResidentialBinding = CreateBinding("ilResidential", new int[18]);
            
            Mod.log.Info("ResidentialUISystem created.");
        }

        protected override void OnUpdate()
        {
            ResidentialSystem residentialSystem = base.World.GetOrCreateSystemManaged<ResidentialSystem>();

            

            // Populate the UI binding with the correct values
           m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();

            base.OnUpdate();
        }
    }
}
