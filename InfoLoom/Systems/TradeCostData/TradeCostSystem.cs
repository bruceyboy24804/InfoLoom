using System.Collections.Generic;
using System.Linq;
using Game.Companies;
using Game.Economy;
using InfoLoomTwo.Systems.TradeCostData;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class TradeCostSystem : SystemBase
{
    private EntityQuery m_Query;
    private ResourceTradeCost[] m_ResourceTradeCost;

    protected override void OnCreate()
    {
        m_Query = GetEntityQuery(ComponentType.ReadOnly<TradeCost>());
    }

    protected override void OnUpdate()
    {
        // First, count the total number of TradeCost entries

        var dictionary = new Dictionary<Resource, ResourceTradeCost>();
        Entities
            .WithAll<TradeCost>()
            .ForEach((in DynamicBuffer<TradeCost> tradeCosts) =>
            {
                foreach (var tradeCost in tradeCosts)
                {
                    if (!dictionary.TryGetValue(tradeCost.m_Resource, out var val))
                        dictionary[tradeCost.m_Resource] = val = new ResourceTradeCost { Resource = tradeCost.m_Resource.ToString() };

                    val.Count++;
                    val.BuyCost += tradeCost.m_BuyCost;
                    val.SellCost += tradeCost.m_SellCost;
                }
            }).WithoutBurst().Run();

        m_ResourceTradeCost = dictionary.Values.ToArray();
    }

    public ResourceTradeCost[] GetResourceTradeCostData()
    {
        return m_ResourceTradeCost;
    }
}