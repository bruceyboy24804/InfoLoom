using Game;
using Game.Buildings;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using Unity.Collections;

namespace InfoLoomTwo.Systems.WorkplacesData
{
    public partial class WorkplacesUISystem : ExtendedUISystemBase
    {
        
        private SimulationSystem m_SimulationSystem;
        private ValueBindingHelper<WorkplacesInfo[]> m_WorkplacesBinder;
        private EntityQuery m_WorkplaceQuery;
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_WorkplacesBinder = CreateBinding("ilWorkplaces", new WorkplacesInfo[0]);

            Mod.log.Info("WorkplacesUISystem created.");
        }
        protected override void OnUpdate()
        {
            var workplacesSystem = base.World.GetOrCreateSystemManaged<WorkplacesSystem>();
            m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();

            base.OnUpdate();
        }
    }
}