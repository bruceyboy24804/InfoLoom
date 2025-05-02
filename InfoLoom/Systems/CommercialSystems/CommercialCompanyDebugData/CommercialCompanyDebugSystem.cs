using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Companies;
using Unity.Entities;

namespace InfoLoomTwo.Systems.CommercialSystems.CommercialCompanyDebugData
{
    public struct CommercialStats
    {
        public int totalCompanies;
    }

    public partial class CommercialCompanyDebugSystem : GameSystemBase
    {
        private EntityQuery m_CommercialCompanyQuery;
        private CommercialStats m_Stats;
        private ILog m_Log;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CommercialCompanyQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<CommercialCompany>(),
                    ComponentType.ReadOnly<Building>()
                }
            });
        }

        protected override void OnUpdate()
        {
            m_Stats.totalCompanies = m_CommercialCompanyQuery.CalculateEntityCount();

            if (m_Stats.totalCompanies > 0)
            {
               m_Log.Debug($"{nameof(CommercialCompanyDebugSystem)}: {m_Stats.totalCompanies} commercial companies found.");
            }
        }
    }
}