using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using AreaType = Game.Zones.AreaType;
using CitizenHappiness = Game.UI.InGame.CitizenHappiness;
using ServiceCoverage = Game.Net.ServiceCoverage;

namespace InfoLoomTwo.Systems.ResidentialData.ResidentialInfoSection
{
    [BurstCompile]
    public struct CollectResidentialEntitiesJob : IJobChunk
    {
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] public BufferTypeHandle<Game.Buildings.Renter> RenterBufferHandle;
        [ReadOnly] public NativeHashSet<Entity>.ReadOnly HouseholdSet;

        public NativeList<Entity> ValidEntities;
        public NativeList<int> HouseholdCounts;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityTypeHandle);
            var hasRenters = chunk.Has(ref RenterBufferHandle);

            if (!hasRenters)
                return;

            var renterAccessor = chunk.GetBufferAccessor(ref RenterBufferHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var renterBuffer = renterAccessor[i];
                if (renterBuffer.Length > 0)
                {
                    int validRenters = 0;

                    for (int r = 0; r < renterBuffer.Length; r++)
                    {
                        Entity householdEntity = renterBuffer[r].m_Renter;

                        if (HouseholdSet.Contains(householdEntity))
                        {
                            validRenters++;
                        }
                    }

                    if (validRenters > 0)
                    {
                        ValidEntities.Add(entities[i]);
                        HouseholdCounts.Add(validRenters);
                    }
                }
            }
        }
    }

    [BurstCompile]
    public struct CountHappinessJob : IJob
    {
        [ReadOnly] public Entity m_SelectedEntity;

        [ReadOnly] public ComponentLookup<Building> m_BuildingFromEntity;
        [ReadOnly] public ComponentLookup<ResidentialProperty> m_ResidentialPropertyFromEntity;
        [ReadOnly] public ComponentLookup<Household> m_HouseholdFromEntity;
        [ReadOnly] public ComponentLookup<Citizen> m_CitizenFromEntity;
        [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;
        [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenterFromEntity;
        [ReadOnly] public ComponentLookup<Abandoned> m_AbandonedFromEntity;
        [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;
        [ReadOnly] public BufferLookup<Renter> m_RenterFromEntity;
        [ReadOnly] public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;
        [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;
        [ReadOnly] public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDataFromEntity;
        [ReadOnly] public ComponentLookup<ElectricityConsumer> m_ElectricityConsumerFromEntity;
        [ReadOnly] public ComponentLookup<WaterConsumer> m_WaterConsumerFromEntity;
        [ReadOnly] public ComponentLookup<Locked> m_LockedFromEntity;
        [ReadOnly] public ComponentLookup<Transform> m_TransformFromEntity;
        [ReadOnly] public ComponentLookup<GarbageProducer> m_GarbageProducersFromEntity;
        [ReadOnly] public ComponentLookup<CrimeProducer> m_CrimeProducersFromEntity;
        [ReadOnly] public ComponentLookup<MailProducer> m_MailProducerFromEntity;
        [ReadOnly] public ComponentLookup<BuildingData> m_BuildingDataFromEntity;
        [ReadOnly] public BufferLookup<CityModifier> m_CityModifierFromEntity;
        [ReadOnly] public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverageFromEntity;

        public CitizenHappinessParameterData m_CitizenHappinessParameters;
        public GarbageParameterData m_GarbageParameters;
        public HealthcareParameterData m_HealthcareParameters;
        public ParkParameterData m_ParkParameters;
        public EducationParameterData m_EducationParameters;
        public TelecomParameterData m_TelecomParameters;

        [ReadOnly] public DynamicBuffer<HappinessFactorParameterData> m_HappinessFactorParameters;
        [ReadOnly] public NativeArray<GroundPollution> m_PollutionMap;
        [ReadOnly] public NativeArray<NoisePollution> m_NoisePollutionMap;
        [ReadOnly] public NativeArray<AirPollution> m_AirPollutionMap;
        [ReadOnly] public CellMapData<TelecomCoverage> m_TelecomCoverage;
        [ReadOnly] public NativeArray<int> m_TaxRates;
        
        public NativeArray<int2> m_Factors;
        public NativeArray<int> m_Results;
        public Entity m_City;
        public float m_RelativeElectricityFee;
        public float m_RelativeWaterFee;

        public void Execute()
        {
            int happiness = 0;
            int citizenCount = 0;
            if (m_BuildingFromEntity.HasComponent(m_SelectedEntity) && m_ResidentialPropertyFromEntity.HasComponent(m_SelectedEntity))
            {
                bool isAbandoned = m_AbandonedFromEntity.HasComponent(m_SelectedEntity);
                BuildingHappiness.GetResidentialBuildingHappinessFactors(
                    m_City, 
                    m_TaxRates, 
                    m_SelectedEntity, 
                    m_Factors,
                    ref m_PrefabRefFromEntity, 
                    ref m_SpawnableBuildingDataFromEntity, 
                    ref m_BuildingPropertyDataFromEntity,
                    ref m_CityModifierFromEntity, 
                    ref m_BuildingFromEntity, 
                    ref m_ElectricityConsumerFromEntity, 
                    ref m_WaterConsumerFromEntity,
                    ref m_ServiceCoverageFromEntity, 
                    ref m_LockedFromEntity, 
                    ref m_TransformFromEntity, 
                    ref m_GarbageProducersFromEntity,
                    ref m_CrimeProducersFromEntity, 
                    ref m_MailProducerFromEntity, 
                    ref m_RenterFromEntity, 
                    ref m_CitizenFromEntity,
                    ref m_HouseholdCitizenFromEntity, 
                    ref m_BuildingDataFromEntity,
                    m_CitizenHappinessParameters, 
                    m_GarbageParameters, 
                    m_HealthcareParameters, 
                    m_ParkParameters,
                    m_EducationParameters, 
                    m_TelecomParameters, 
                    m_HappinessFactorParameters, 
                    m_PollutionMap, 
                    m_NoisePollutionMap,
                    m_AirPollutionMap, 
                    m_TelecomCoverage,
                    m_RelativeElectricityFee, 
                    m_RelativeWaterFee);

                if (TryAddPropertyHappiness(ref happiness, ref citizenCount, m_SelectedEntity))
                {
                    m_Results[1] = citizenCount;
                    m_Results[2] = happiness;
                }
                m_Results[0] = ((citizenCount > 0 || isAbandoned) ? 1 : 0);
            }
            else
            {
                if (!m_HouseholdCitizenFromEntity.TryGetBuffer(m_SelectedEntity, out var bufferData))
                {
                    return;
                }

                for (int i = 0; i < bufferData.Length; i++)
                {
                    Entity citizen = bufferData[i].m_Citizen;
                    if (m_CitizenFromEntity.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref m_HealthProblemFromEntity))
                    {
                        happiness += m_CitizenFromEntity[citizen].Happiness;
                        citizenCount++;
                    }
                }
                
                m_Results[0] = 1;
                m_Results[1] = citizenCount;
                m_Results[2] = happiness;
                
                if (m_PropertyRenterFromEntity.TryGetComponent(m_SelectedEntity, out var componentData))
                {
                    BuildingHappiness.GetResidentialBuildingHappinessFactors(
                        m_City, 
                        m_TaxRates, 
                        componentData.m_Property, 
                        m_Factors,
                        ref m_PrefabRefFromEntity, 
                        ref m_SpawnableBuildingDataFromEntity, 
                        ref m_BuildingPropertyDataFromEntity,
                        ref m_CityModifierFromEntity, 
                        ref m_BuildingFromEntity, 
                        ref m_ElectricityConsumerFromEntity, 
                        ref m_WaterConsumerFromEntity,
                        ref m_ServiceCoverageFromEntity, 
                        ref m_LockedFromEntity, 
                        ref m_TransformFromEntity, 
                        ref m_GarbageProducersFromEntity,
                        ref m_CrimeProducersFromEntity, 
                        ref m_MailProducerFromEntity, 
                        ref m_RenterFromEntity, 
                        ref m_CitizenFromEntity,
                        ref m_HouseholdCitizenFromEntity, 
                        ref m_BuildingDataFromEntity,
                        m_CitizenHappinessParameters, 
                        m_GarbageParameters, 
                        m_HealthcareParameters, 
                        m_ParkParameters,
                        m_EducationParameters, 
                        m_TelecomParameters, 
                        m_HappinessFactorParameters, 
                        m_PollutionMap, 
                        m_NoisePollutionMap,
                        m_AirPollutionMap, 
                        m_TelecomCoverage,
                        m_RelativeElectricityFee, 
                        m_RelativeWaterFee);
                }
            }
        }

        private bool TryAddPropertyHappiness(ref int happiness, ref int citizenCount, Entity entity)
        {
            if (!m_RenterFromEntity.TryGetBuffer(entity, out var renters))
                return false;

            for (int i = 0; i < renters.Length; i++)
            {
                Entity household = renters[i].m_Renter;
                if (!m_HouseholdFromEntity.HasComponent(household))
                    continue;

                if (!m_HouseholdCitizenFromEntity.TryGetBuffer(household, out var householdCitizens))
                    continue;

                for (int j = 0; j < householdCitizens.Length; j++)
                {
                    Entity citizen = householdCitizens[j].m_Citizen;
                    if (m_CitizenFromEntity.HasComponent(citizen) && !CitizenUtils.IsDead(citizen, ref m_HealthProblemFromEntity))
                    {
                        happiness += m_CitizenFromEntity[citizen].Happiness;
                        citizenCount++;
                    }
                }
            }
            return citizenCount > 0;
        }
        
    }
}