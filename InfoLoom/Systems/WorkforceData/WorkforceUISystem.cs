using Game;
using Game.Simulation;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using InfoLoomTwo.Domain;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceUISystem : ExtendedUISystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforcesBinder;
        public override GameMode gameMode => GameMode.Game;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_WorkforcesBinder = CreateBinding("ilWorkforce", new WorkforcesInfo[0]);
        }

        protected override void OnUpdate()
        {

            var workforcsSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
            m_WorkforcesBinder.Value = workforcsSystem.m_Results.ToArray();
            base.OnUpdate();
        }
    }
}