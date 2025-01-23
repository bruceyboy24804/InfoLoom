using Game;
using Game.Buildings;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using Unity.Collections;

namespace InfoLoomTwo.Systems.WorkplacesData
{
    public partial class WorkplacesUISystem : ExtendedUISystemBase
    {
        public struct workplacesInfo
        {
            public int Level { get; set; }
            public int Total { get; set; }
            public int Service { get; set; }
            public int Commercial { get; set; }
            public int Leisure { get; set; }
            public int Extractor { get; set; }
            public int Industrial { get; set; }
            public int Office { get; set; }
            public int Employee { get; set; }
            public int Open { get; set; }
            public int Commuter { get; set; }

            public string Name { get; set; }


            public workplacesInfo(int _level)
            {
                Level = _level;
                Total = 0;
                Service = 0;
                Commercial = 0;
                Leisure = 0;
                Extractor = 0;
                Industrial = 0;
                Office = 0;
                Employee = 0;
                Open = 0;
                Commuter = 0;
                Name = "";
            }
        }
        private SimulationSystem m_SimulationSystem;
        private ValueBindingHelper<workplacesInfo[]> m_WorkplacesBinder;
        private EntityQuery m_WorkplaceQuery;
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_WorkplacesBinder = CreateBinding("ilWorkplaces", new workplacesInfo[0]);

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