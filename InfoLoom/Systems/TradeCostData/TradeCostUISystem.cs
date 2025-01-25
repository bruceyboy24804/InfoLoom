using System;
using System.Linq;
using Game;
using Game.Economy;
using Game.Simulation;
using InfoLoomTwo.Extensions;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostUISystem : ExtendedUISystemBase
    {
        private SimulationSystem m_SimulationSystem;
        private ValueBindingHelper<ResourceTradeCost[]> m_TradeCostsBinder;
        public override GameMode gameMode => GameMode.Game;
        
        private TradeCostSystem m_TradeCostSystem;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
            m_TradeCostSystem = base.World.GetOrCreateSystemManaged<TradeCostSystem>();
            m_TradeCostsBinder = CreateBinding("tradeCosts", Array.Empty<ResourceTradeCost>());
        }

        protected override void OnUpdate()
        {
            // Get the ResourceTradeCost array from TradeCostSystem
            ResourceTradeCost[] tradeCosts = m_TradeCostSystem.GetResourceTradeCostData();
            
            // Update the binding with the new data
            m_TradeCostsBinder.Value = tradeCosts;
            
            base.OnUpdate();
        }
    }
}