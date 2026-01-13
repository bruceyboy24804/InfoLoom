using System;
using System.Collections.Generic;
using System.Text;
using Game;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Bridge;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.WorkforceData;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Chirp = Game.Triggers.Chirp;

namespace InfoLoomTwo.Systems.ChirpSystem_s_
{
    public partial class InfoLoomChirpSystem : GameSystemBase
    {
        private WorkforceSystem _workforceSystem;
        private SimulationSystem _sim;
        
        
        private uint _lastUnemploymentChirpFrame;
        private uint _lastUnderemploymentChirpFrame;
        private uint _lastHomelessChirpFrame;
        private uint _lastCommercialDemandChirpFrame;
        public uint _lastInfoviewChirpFrame;
        private uint _lastElectricityChirpFrame;
        private uint _lastWaterAndSewageChirpFrame;


        private uint MinFramesBetweenUnemploymentChirps = 18000;
        private uint MinFramesBetweenUnderemploymentChirps = 18000;
        private uint MinFramesBetweenHomelessChirps = 18000;
        private uint MinFramesBetweenDemandChirps = 18000;
        public uint MinFramesBetweenInfoviewChirps = 18000;
        
        
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 512;

        protected override void OnCreate()
        {
            base.OnCreate();

            _workforceSystem = World.GetExistingSystemManaged<WorkforceSystem>();
            _sim = World.GetOrCreateSystemManaged<SimulationSystem>();
            var _timeDataQ = SystemAPI.QueryBuilder().WithAll<TimeData>().Build();
            var _chirpQuery = SystemAPI.QueryBuilder().WithAll<Chirp, PrefabRef>().Build();
            

            _lastUnemploymentChirpFrame = 0;
            _lastUnderemploymentChirpFrame = 0;
            _lastHomelessChirpFrame = 0;

            RequireForUpdate(_timeDataQ);
        }

        protected override void OnUpdate()
        {
            PostWorkforceWarnings();
            PostDemandChirp();
            PostInfoviewChirps();
        }

        private enum ChirpType
        {
            Unemployment,
            Underemployment,
            Homeless,
            Demand,
            Infoviews
        }
        
        private string FormatElectricityValue(int value)
        {
            if (value >= 1000)
            {
                return $"{value / 1000.0:F1} MW";
            }
            return $"{value} kW";
        }
        
        private bool CanPostChirp(uint currentFrame, uint lastChirpFrame, ChirpType chirpType)
        {
            if (lastChirpFrame == 0)
                return true;

            uint framesSinceLast = currentFrame - lastChirpFrame;
            
            uint minFramesRequired = chirpType switch
            {
                ChirpType.Unemployment => MinFramesBetweenUnemploymentChirps,
                ChirpType.Underemployment => MinFramesBetweenUnderemploymentChirps,
                ChirpType.Homeless => MinFramesBetweenHomelessChirps,
                ChirpType.Demand => MinFramesBetweenDemandChirps,
                ChirpType.Infoviews => MinFramesBetweenInfoviewChirps,
                _ => uint.MaxValue
            };
            
            return framesSinceLast >= minFramesRequired;
        }

        private string GetEducationLevelName(int level)
        {
            return level switch
            {
                0 => "Uneducated",
                1 => "Poorly Educated",
                2 => "Educated",
                3 => "Well Educated",
                4 => "Highly Educated",
                _ => "Unknown"
            };
        }

        private string GetEducationLevelRates(Func<WorkforcesInfo, int> selector, string label)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                var levelData = _workforceSystem.m_Results[i];
                if (levelData.Total > 0)
                {
                    float rate = (float)selector(levelData) / levelData.Total;
                    sb.AppendLine($"{GetEducationLevelName(i)}: {rate:P1}");
                }
            }
            return sb.ToString().TrimEnd();
        }
        private string GetEducationLevelUnemploymentRates() =>
            GetEducationLevelRates(ld => ld.Unemployed, "Unemployment");

        private string GetEducationLevelUnderemploymentRates() =>
            GetEducationLevelRates(ld => ld.Under, "Underemployment");

        private string GetEducationLevelHomelessRates() =>
            GetEducationLevelRates(ld => ld.Homeless, "Homelessness");
        
        public string GetComDemandResourcesList()
        {
            var commercialDemandSystem = World.GetExistingSystemManaged<CommercialDemandSystem>();
            if (commercialDemandSystem == null)
                return "Commercial demand system unavailable";

            JobHandle deps;
            NativeArray<int> resourceDemands = commercialDemandSystem.GetResourceDemands(out deps);
            deps.Complete(); // Wait for any pending jobs
            
            var resources = new List<(Resource resource, int demand)>();
            ResourceIterator iterator = ResourceIterator.GetIterator();

            while (iterator.Next())
            {
                if (EconomyUtils.IsCommercialResource(iterator.resource))
                {
                    int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                    int demandValue = resourceDemands[resourceIndex];

                    if (demandValue >= 100)
                    {
                        resources.Add((iterator.resource, demandValue));
                    }
                }
            }
            
            if (resources.Count == 0)
                return "No resources currently in demand";

            var sb = new StringBuilder();
            int columnsCount = 4; // Number of columns
            int itemsPerColumn = (int)Math.Ceiling(resources.Count / (double)columnsCount);

            for (int row = 0; row < itemsPerColumn; row++)
            {
                for (int col = 0; col < columnsCount; col++)
                {
                    int index = col * itemsPerColumn + row;
                    if (index < resources.Count)
                    {
                        var (resource, demand) = resources[index];
                        sb.Append($"{resource}".PadRight(50));
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        public string GetIndDemandResourcesList()
        {
            IndustrialDemandSystem industrialDemandSystem = World.GetExistingSystemManaged<IndustrialDemandSystem>();
            ResourceSystem _resourceSystem = World.GetExistingSystemManaged<ResourceSystem>();
            if (industrialDemandSystem == null)
                return "Industrial demand system unavailable";

            JobHandle deps;
            NativeArray<int> buildingDemands = industrialDemandSystem.GetBuildingDemands(out deps);
            deps.Complete(); // Wait for any pending jobs
            
            var resources = new List<(Resource resource, int demand)>();
            ResourceIterator iterator = ResourceIterator.GetIterator();
            var resourceDatas = SystemAPI.GetComponentLookup<ResourceData>(true);
            var resourcePrefabs = _resourceSystem.GetPrefabs();

            while (iterator.Next())
            {
                if (!resourceDatas.HasComponent(resourcePrefabs[iterator.resource]))
                    continue;

                var resourceData = resourceDatas[resourcePrefabs[iterator.resource]];
                
                // Only include produceable resources (same as game's industrial demand system)
                if (!resourceData.m_IsProduceable)
                    continue;
                
                int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
                int buildingDemand = buildingDemands[resourceIndex];

                if (buildingDemand > 0)
                {
                    resources.Add((iterator.resource, buildingDemand));
                }
            }
            
            if (resources.Count == 0)
                return "No resources currently in demand";

            var sb = new StringBuilder();
            int columnsCount = 4; // Number of columns
            int itemsPerColumn = (int)Math.Ceiling(resources.Count / (double)columnsCount);

            for (int row = 0; row < itemsPerColumn; row++)
            {
                for (int col = 0; col < columnsCount; col++)
                {
                    int index = col * itemsPerColumn + row;
                    if (index < resources.Count)
                    {
                        var (resource, demand) = resources[index];
                        sb.Append($"{resource}".PadRight(50));
                    }
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
        



        
        private void PostWorkforceWarnings()
        {
            if (!CustomChirpsBridge.IsAvailable)
                return;
                
            MinFramesBetweenUnemploymentChirps = 18000;
            MinFramesBetweenUnderemploymentChirps = 18000;
            MinFramesBetweenHomelessChirps = 18000;

            var highUnemploymentThreshold = Mod.setting.unemploymentThreshold / 100f;
            var highUnderemploymentThreshold = Mod.setting.underemploymentThreshold / 100f;
            var highHomelessThreshold = Mod.setting.homelessThreshold / 100f;

            uint currentFrame = _sim.frameIndex;
            var totals = _workforceSystem.m_Results[5];

            float unemploymentRate = 0f;
            float underemploymentRate = 0f;
            float homelessRate = 0f;

            if (totals.Total > 0)
            {
                unemploymentRate = (float)totals.Unemployed / totals.Total;
                underemploymentRate = (float)totals.Under / totals.Total;
                homelessRate = (float)totals.Homeless / totals.Total;
            }

            string educationRates = GetEducationLevelUnemploymentRates();
            string underemploymentRates = GetEducationLevelUnderemploymentRates();
            string homelessRates = GetEducationLevelHomelessRates();

            if (Mod.setting.enableUnemploymentChirps)
            {
                if (unemploymentRate >= highUnemploymentThreshold)
                {
                    if (CanPostChirp(currentFrame, _lastUnemploymentChirpFrame, ChirpType.Unemployment))
                    {
                        bool posted = CustomChirpsBridge.PostChirp(
                            text: $"Warning: High unemployment ({unemploymentRate:P1})\n\nUnemployment by Education Level:\n{educationRates}",
                            department: DepartmentAccountBridge.CensusBureau,
                            entity: Entity.Null,
                            customSenderName: "Info Loom - Unemployment"
                        );
                        
                        if (posted)
                        {
                            uint framesSinceLastChirp = _lastUnemploymentChirpFrame > 0 
                                ? currentFrame - _lastUnemploymentChirpFrame 
                                : 0;
                            
                            //Mod.log.Debug($"Posted unemployment chirp: {unemploymentRate:P1} | Frames since last: {framesSinceLastChirp} | Current frame: {currentFrame}");
                            _lastUnemploymentChirpFrame = currentFrame;
                        }
                    }
                    else
                    {
                        uint framesSinceLast = currentFrame - _lastUnemploymentChirpFrame;
                        uint framesRemaining = MinFramesBetweenUnemploymentChirps - framesSinceLast;
                       // Mod.log.Debug($"Suppressed unemployment chirp: {unemploymentRate:P1} | Frames since last: {framesSinceLast} | Frames until next: {framesRemaining}");
                    }
                } 
            }

            if (Mod.setting.enableUnderemploymentChirps)
            {
                if (underemploymentRate >= highUnderemploymentThreshold)
                {
                    if (CanPostChirp(currentFrame, _lastUnderemploymentChirpFrame, ChirpType.Underemployment))
                    {
                        bool posted = CustomChirpsBridge.PostChirp(
                            text: $"Warning: High underemployment  {underemploymentRate:P1}.\n\nUnderemployment by Education Level:\n{underemploymentRates}",
                            department: DepartmentAccountBridge.CensusBureau,
                            entity: Entity.Null,
                            customSenderName: "Info Loom - Underemployment"
                        );
                        
                        if (posted)
                        {
                            uint framesSinceLastChirp = _lastUnderemploymentChirpFrame > 0 
                                ? currentFrame - _lastUnderemploymentChirpFrame 
                                : 0;
                            
                           // Mod.log.Debug($"Posted underemployment chirp: {underemploymentRate:P1} | Frames since last: {framesSinceLastChirp} | Current frame: {currentFrame}");
                            _lastUnderemploymentChirpFrame = currentFrame;
                        }
                    }
                    else
                    {
                        uint framesSinceLast = currentFrame - _lastUnderemploymentChirpFrame;
                        uint framesRemaining = MinFramesBetweenUnderemploymentChirps - framesSinceLast;
                        //Mod.log.Debug($"Suppressed underemployment chirp: {underemploymentRate:P1} | Frames since last: {framesSinceLast} | Frames until next: {framesRemaining}");
                    }
                } 
            }

            if (Mod.setting.enableHomelessChirps)
            {
                if (homelessRate >= highHomelessThreshold)
                {
                    if (CanPostChirp(currentFrame, _lastHomelessChirpFrame, ChirpType.Homeless))
                    {
                        bool posted = CustomChirpsBridge.PostChirp(
                            text: $"Warning: High homelessness {homelessRate:P1}.\n\nHomelessness by Education Level:\n{homelessRates}",
                            department: DepartmentAccountBridge.CensusBureau,
                            entity: Entity.Null,
                            customSenderName: "Info Loom - Homelessness"
                        );
                        
                        if (posted)
                        {
                            uint framesSinceLastChirp = _lastHomelessChirpFrame > 0 
                                ? currentFrame - _lastHomelessChirpFrame 
                                : 0;
                            
                            //Mod.log.Debug($"Posted homelessness chirp: {homelessRate:P1} | Frames since last: {framesSinceLastChirp} | Current frame: {currentFrame}");
                            _lastHomelessChirpFrame = currentFrame;
                        }
                    }
                    else
                    {
                        uint framesSinceLast = currentFrame - _lastHomelessChirpFrame;
                        uint framesRemaining = MinFramesBetweenHomelessChirps - framesSinceLast;
                        //Mod.log.Debug($"Suppressed homelessness chirp: {homelessRate:P1} | Frames since last: {framesSinceLast} | Frames until next: {framesRemaining}");
                    }
                } 
            }
        }
        private void PostDemandChirp()
        {
            if (!CustomChirpsBridge.IsAvailable || !Mod.setting.enableDemandChirps)
                return;
            
            uint currentFrame = _sim.frameIndex;
            
            if (!CanPostChirp(currentFrame, _lastCommercialDemandChirpFrame, ChirpType.Demand))
                return;
            
            string commercialResources = GetComDemandResourcesList();
            string industrialResources = GetIndDemandResourcesList();
            
            if (string.IsNullOrEmpty(commercialResources) && string.IsNullOrEmpty(industrialResources))
                return;
            
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(commercialResources))
            {
                sb.AppendLine("Commercial Demand:");
                sb.AppendLine(commercialResources);
            }
            
            if (!string.IsNullOrEmpty(industrialResources))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.AppendLine("Industrial Demand:");
                sb.AppendLine(industrialResources);
            }
            
            bool posted = CustomChirpsBridge.PostChirp(
                text: sb.ToString().TrimEnd(),
                department: DepartmentAccountBridge.CensusBureau,
                entity: Entity.Null,
                customSenderName: "Info Loom - Demand Report"
            );
            
            if (posted)
            {
                _lastCommercialDemandChirpFrame = currentFrame;
            }
        }
        private void PostInfoviewChirps()
        {
            if (!CustomChirpsBridge.IsAvailable)
                return;
            HealthcareInfoviewUISystem healthcareInfoviewUISystem = World.GetExistingSystemManaged<HealthcareInfoviewUISystem>();
            uint currentFrame = _sim.frameIndex;
            
            // Electricity chirps
            if (Mod.setting.enableElectrictyChirps && 
                CanPostChirp(currentFrame, _lastElectricityChirpFrame, ChirpType.Infoviews))
            {
                var electricityStatsSystem = World.GetExistingSystemManaged<ElectricityStatisticsSystem>();
                if (electricityStatsSystem != null && 
                    electricityStatsSystem.consumption > electricityStatsSystem.production)
                {
                    bool posted = CustomChirpsBridge.PostChirp(
                        text: $"Warning: High electricity consumption: {FormatElectricityValue(electricityStatsSystem.consumption)}. Not enough electricity: {FormatElectricityValue(electricityStatsSystem.production)}.",
                        department: DepartmentAccountBridge.Electricity,
                        entity: Entity.Null,
                        customSenderName: "Info Loom - Electricity"
                    );

                    if (posted)
                    {
                        _lastElectricityChirpFrame = currentFrame;
                    }
                }
            }

            // Water and sewage chirps (share same cooldown)
            if (Mod.setting.enableWaterAndSweageChirps && 
                CanPostChirp(currentFrame, _lastWaterAndSewageChirpFrame, ChirpType.Infoviews))
            {
                var waterStatsSystem = World.GetExistingSystemManaged<WaterStatisticsSystem>();
                if (waterStatsSystem != null)
                {
                    if (waterStatsSystem.freshConsumption > waterStatsSystem.freshCapacity)
                    {
                        bool posted = CustomChirpsBridge.PostChirp(
                            text: $"Warning: High fresh water consumption: {waterStatsSystem.freshConsumption}. Not enough fresh water: {waterStatsSystem.freshCapacity}.",
                            department: DepartmentAccountBridge.Water,
                            entity: Entity.Null,
                            customSenderName: "Info Loom - Water"
                        );
                        
                        if (posted)
                        {
                            _lastWaterAndSewageChirpFrame = currentFrame;
                        }
                    }

                    if (waterStatsSystem.sewageConsumption > waterStatsSystem.sewageCapacity)
                    {
                        bool posted = CustomChirpsBridge.PostChirp(
                            text: $"Warning: High sewage consumption: {waterStatsSystem.sewageConsumption}. Not enough sewage: {waterStatsSystem.sewageCapacity}.",
                            department: DepartmentAccountBridge.Water,
                            entity: Entity.Null,
                            customSenderName: "Info Loom - Sewage"
                        );
                        
                        if (posted)
                        {
                            _lastWaterAndSewageChirpFrame = currentFrame;
                        }
                    }
                }
            }
        }
        
    }
}