using System.Linq;
using Game;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using System.Collections.Generic;
using Game.UI.InGame;
using Unity.Entities;
using UnityEngine;

namespace InfoLoomTwo.Systems.TradeCostData
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TradeCostUISystem : ExtendedUISystemBase
    {
	    private List<TradeCostResource> m_Imports = new List<TradeCostResource>();
	    private List<TradeCostResource> m_Exports = new List<TradeCostResource>();
        private ValueBindingHelper<List<ResourceTradeCost>> m_TradeCostsBinding;
        private ValueBindingHelper<List<TradeCostResource>> m_ImportsBinding;
        private ValueBindingHelper<List<TradeCostResource>> m_ExportsBinding;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TradeCostsBinding = CreateBinding("tradeCosts", new List<ResourceTradeCost>());
            m_ImportsBinding = CreateBinding("imports", new List<TradeCostResource>());
            m_ExportsBinding = CreateBinding("exports", new List<TradeCostResource>());

        }

        protected override void OnUpdate()
        {
            
            var tradeCostSystem = World.GetOrCreateSystemManaged<TradeCostSystem>();
            var tradeCosts = tradeCostSystem.GetResourceTradeCosts().ToList();
            var topImports = tradeCostSystem.GetImports().ToList();
            var topExports = tradeCostSystem.GetExports().ToList();
            // Update the binding with the new trade costs
            m_TradeCostsBinding.Value = tradeCosts;
            m_ImportsBinding.Value = topImports;
            m_ExportsBinding.Value = topExports;
            UpdateImportData();
	        UpdateExportData();

            

            base.OnUpdate();
        }
        private void UpdateImportData()
        { 
            int num = 0;
            int num2 = m_Imports.Count;
            if (m_Imports.Count < num2)
            {
                    num2 = m_Imports.Count;
            }
            for (int i = 0; i < num2; i++)
            {
                m_ImportsBinding.Value = m_Imports;
                num += m_Imports[i].Amount;
            }
        }
        private void UpdateExportData()
		{
			int num = 0;
			int num2 = m_Exports.Count;
			if (m_Exports.Count < num2)
			{
				num2 = m_Exports.Count;
			}
			for (int i = 0; i < num2; i++)
			{
				m_ExportsBinding.Value = m_Exports;
				num += m_Exports[i].Amount;
			}
			
		}
        
    }
}
