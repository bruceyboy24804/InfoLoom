using System.Linq;
using Game;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using System.Collections.Generic;

namespace InfoLoomTwo.Systems.TradeCostData
{
    public partial class TradeCostUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TradeCostsBinding = CreateBinding("tradeCosts", new List<ResourceTradeCost>());
        }

        protected override void OnUpdate()
        {
            var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
            
            // Get the trade costs from the TradeCostSystem
            var tradeCosts = tradeCostSystem.GetResourceTradeCosts().ToList();

            // Update the binding with the new trade costs
            m_TradeCostsBinding.Value = tradeCosts;

            base.OnUpdate();
        }
    }
}