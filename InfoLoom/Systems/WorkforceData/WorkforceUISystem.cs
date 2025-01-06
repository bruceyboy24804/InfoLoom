using Game;
using Game.Simulation;
using InfoLoomTwo.Extensions;
using Unity.Entities;

namespace InfoLoomTwo.Systems.WorkforceData
{
    public partial class WorkforceUISystem : ExtendedUISystemBase
    {
        public struct workforcesInfo
        {
            public int Level { get; set; }
            public int Total { get; set; } // all Adults and Teens, not Dead, not Students - potential workers; Total = Worker+Unemployed
            public int Worker { get; set; } // working citizens
            public int Unemployed { get; set; } // not-working citizens
            public int Homeless { get; set; } // not-working and homeless; Homeless is part of Unemployed
            public int Employable { get; set; } // working but in weird places Employable = Outside + Under, Employable is part of Worker
            public int Outside { get; set; } // working out of the city
            public int Under; // underemployed, working at jobs with lower level
            public workforcesInfo(int _level)
            {
                Level = _level;
                Total = 0;
                Worker = 0;
                Unemployed = 0;
                Homeless = 0;
                Employable = 0;
                Outside = 0;
                Under = 0;
            }
        }
        private SimulationSystem m_SimulationSystem;
        private ValueBindingHelper<workforcesInfo[]> m_WorkforcesBinder;
        public override GameMode gameMode => GameMode.Game;
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_WorkforcesBinder = CreateBinding("ilWorkforce", new workforcesInfo[0]);
        }

        protected override void OnUpdate()
        {
            
            var workforcsSystem = base.World.GetOrCreateSystemManaged<WorkforceSystem>();
		    m_WorkforcesBinder.Value = workforcsSystem.m_Results.ToArray();
            base.OnUpdate();
        }
    }
}