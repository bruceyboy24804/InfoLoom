using System;
using System.Collections.Generic;
using Colossal.Logging;
using Colossal.UI.Binding;
using Game;
using Game.Areas;
using Game.Economy;
using Game.Simulation;
using Game.UI;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Domain.DataDomain;
using InfoLoomTwo.Domain.DataDomain.Enums;
using InfoLoomTwo.Systems.CommercialSystems.CommercialDemandData;
using InfoLoomTwo.Systems.DemographicsData;
using InfoLoomTwo.Systems.IndustrialSystems.IndustrialDemandData;
using InfoLoomTwo.Systems.ResidentialData;
using InfoLoomTwo.Systems.WorkforceData;
using InfoLoomTwo.Systems.WorkplacesData;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.UI
{
    public partial class InfoLoomUISystem : CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        private const string ModID = "InfoLoomTwo";

        private const string InfoLoomMenuOpen = "InfoLoomMenuOpen";
        private const string CommercialMenuOpen = "CommercialMenuOpen";
        private const string IndustrialMenuOpen = "IndustrialMenuOpen";
        private const string DistrictMenuOpen = "DistrictMenuOpen";
        private const string ResidentialMenuOpen = "ResidentialMenuOpen";
        private const string SankeyMenuOpen = "SankeyMenuOpen";
        private const string BuildingDemandOpen = "BuildingDemandOpen";
        private const string CommercialDemandOpen = "CommercialDemandOpen";
        private const string CommercialProductsOpen = "CommercialProductsOpen";
        private const string DemographicsOpen = "DemographicsOpen";
        private const string DistrictDataOpen = "DistrictDataOpen";
        private const string IndustrialDemandOpen = "IndustrialDemandOpen";
        private const string IndustrialProductsOpen = "IndustrialProductsOpen";
        private const string ResidentialDemandOpen = "ResidentialDemandOpen";
        private const string WorkforceOpen = "WorkforceOpen";
        private const string WorkplacesOpen = "WorkplacesOpen";
        private const string CommercialCompanyDebugOpen = "CommercialCompanyDebugOpen";
        private const string IndustrialCompanyDebugOpen = "IndustrialCompanyDebugOpen";
        private const string EffectsOpen = "EffectsOpen";
        public static Entity CityWide { get; } = Entity.Null;
        public Entity selectedDistrict { get; set; } = CityWide;
        private EntityQuery m_DistrictQuery;
        private DistrictInfos _DistrictInfos = new();
        private RawValueBinding m_DistrictInfos;

        public static Resource ShowAllResource { get; } = Resource.All;
        public Resource selectedResource { get; set; } = ShowAllResource;
        private EntityQuery m_ResourceQuery;

        private RawValueBinding m_ResourceInfos;
        private ValueBindingHelper<float> positionXBinding;
        private ValueBindingHelper<float> positionYBinding;

        //Systems
        private NameSystem m_NameSystem;
        private ResidentialDemandSystem m_ResidentialDemandSystem;
        private CommercialDemandSystem m_CommercialDemandSystem;
        private IndustrialDemandSystem m_IndustrialDemandSystem;

        private CommercialSystem m_CommercialSystem;
        private Demographics m_Demographics;
        private IndustrialSystem m_IndustrialSystem;
        private ResidentialSystem m_ResidentialSystem;
        private WorkforceSystem m_WorkforceSystem;
        private WorkplacesSystem m_WorkplacesSystem;

        //Bindings
        //BuildingDemandUI
        private ValueBindingHelper<int[]> m_uiBuildingDemand;

        //CommercialDemandDataUI
        private ValueBindingHelper<string[]> m_ExcludedResourcesBinding;

        private ValueBindingHelper<int[]> m_CommercialBinding;

        //CommercialProductsDataUI
        private ValueBindingHelper<CommercialProductsData[]> m_CommercialProductBinding;


        //HouseholdData
        //DemographicsUI
        public GetterValueBinding<int> m_OldCitizenBinding;
        private ValueBindingHelper<int[]> m_TotalsBinding;
        public ValueBinding<bool> m_DemoStatsToggledOnBinding;
        private ValueBinding<bool> m_DemoAgeGroupingToggledOnBinding;
        private ValueBindingHelper<GroupingStrategy> m_DemoGroupingStrategyBinding;
        private ValueBinding<Entity> m_SelectedDistrict;
        private ValueBindingHelper<Demographics1> m_Demographics1Binding;
        private ValueBindingHelper<Demographics2> m_Demographics2Binding;
        private ValueBindingHelper<bool> _RefreshDataBinding;
        private ValueBindingHelper<int[]> m_DemographicsLifecycleTotalsBinding;

        private ValueBindingHelper<PopulationDetailedGroupInfo[]> m_DemographicsDetailedGroupDetailsBinding;
        private ValueBindingHelper<PopulationFiveYearGroupInfo[]> m_DemographicsFiveYearDetailsBinding;
        private ValueBindingHelper<PopulationTenYearGroupInfo[]> m_DemographicsTenYearDetailsBinding;
        private ValueBindingHelper<PopulationLifecycleInfo[]> m_DemographicsLifecycleDetailsBinding;


        private ValueBinding<int> m_SelectedResource;

        private RawValueBinding m_DistrictInfosBinding;
//DistrictDataUI

        //IndustrialDemandDataUI
        private ValueBindingHelper<string[]> m_IndustrialExcludedResourcesBinding;

        private ValueBindingHelper<int[]> m_IndustrialBinding;

        //IndustrialProductsDataUI
        //ResidentialDemandDataUI
        public ValueBindingHelper<float[]> m_ResidentialBinding;


        //TrafficDataUI
        private RawValueBinding m_uiTrafficData;


        //WorkforceUI
        private ValueBindingHelper<WorkforcesInfo[]> m_WorkforcesBinder;
        //private ValueBindingHelper<int> hideColumnsBindingWF;


        //WorkplacesUI
        private ValueBindingHelper<WorkplacesInfo[]> m_WorkplacesBinder;
        //private ValueBindingHelper<int> hideColumnsBindingWP;


        //Historical data
        private ValueBindingHelper<List<float>> m_ResourceHistoricalDataBinding;

        // Panel 
        private ValueBinding<bool> _panelVisibleBinding;
        private ValueBinding<bool> _commercialPanelVisibleBinding;
        private ValueBinding<bool> _industrialPanelVisibleBinding;
        private ValueBinding<bool> _districtPanelVisibleBinding;
        private ValueBinding<bool> _residentialPanelVisibleBinding;
        private ValueBinding<bool> _sankeyPanelVisibleBinding;
        private ValueBinding<bool> _bDPVBinding;
        private ValueBinding<bool> _cDPVBinding;
        private ValueBinding<bool> _cPPVBinding;
        private ValueBinding<bool> _dPVBinding;

        private ValueBinding<bool> _iDPVBinding;
        private ValueBinding<bool> _iPPVBinding;
        private ValueBinding<bool> _rDPVBinding;
        private ValueBinding<bool> _wFPVBinding;
        private ValueBinding<bool> _wPPVBinding;
        private ValueBinding<bool> _householdsDataVisibleBinding;
        private ValueBinding<bool> _TrafficDataVisibleBinding;
        private ValueBinding<bool> _effectsVisibleBinding;


        private ILog m_Log;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
            m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<Resources>());
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_ResidentialDemandSystem = World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
            m_CommercialDemandSystem = World.GetOrCreateSystemManaged<CommercialDemandSystem>();
            m_IndustrialDemandSystem = World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
            m_CommercialSystem = World.GetOrCreateSystemManaged<CommercialSystem>();
            m_Demographics = World.GetOrCreateSystemManaged<Demographics>();
            m_IndustrialSystem = World.GetOrCreateSystemManaged<IndustrialSystem>();
            m_ResidentialSystem = World.GetOrCreateSystemManaged<ResidentialSystem>();
            m_WorkforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
            m_WorkplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();

            _DistrictInfos = new DistrictInfos();


            //InfoLoomMenu
            _panelVisibleBinding = new ValueBinding<bool>(ModID, InfoLoomMenuOpen, false);
            AddBinding(_panelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, InfoLoomMenuOpen, SetInfoLoomMenuVisibility));

            //CommercialMenu
            _commercialPanelVisibleBinding = new ValueBinding<bool>(ModID, CommercialMenuOpen, false);
            AddBinding(_commercialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, CommercialMenuOpen, SetCommercialMenuVisibility));

            //IndustrialMenu
            _industrialPanelVisibleBinding = new ValueBinding<bool>(ModID, IndustrialMenuOpen, false);
            AddBinding(_industrialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, IndustrialMenuOpen, SetIndustrialMenuVisibility));

            //DistrictMenu
            _districtPanelVisibleBinding = new ValueBinding<bool>(ModID, DistrictMenuOpen, false);
            AddBinding(_districtPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, DistrictMenuOpen, SetDistrictMenuVisibility));

            //ResidentialMenu
            _residentialPanelVisibleBinding = new ValueBinding<bool>(ModID, ResidentialMenuOpen, false);
            AddBinding(_residentialPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, ResidentialMenuOpen, SetResidentialMenuVisibility));

            //SankeyMenu
            _sankeyPanelVisibleBinding = new ValueBinding<bool>(ModID, SankeyMenuOpen, false);
            AddBinding(_sankeyPanelVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, SankeyMenuOpen, SetSankeyMenuVisibility));


            _bDPVBinding = new ValueBinding<bool>(ModID, BuildingDemandOpen, false);
            AddBinding(_bDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, BuildingDemandOpen, SetBuildingDemandVisibility));

            _cDPVBinding = new ValueBinding<bool>(ModID, CommercialDemandOpen, false);
            AddBinding(_cDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, CommercialDemandOpen, SetCommercialDemandVisibility));

            _dPVBinding = new ValueBinding<bool>(ModID, DemographicsOpen, false);
            AddBinding(_dPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, DemographicsOpen, SetDemographicsVisibility));

            _iDPVBinding = new ValueBinding<bool>(ModID, IndustrialDemandOpen, false);
            AddBinding(_iDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, IndustrialDemandOpen, SetIndustrialDemandVisibility));


            _rDPVBinding = new ValueBinding<bool>(ModID, ResidentialDemandOpen, false);
            AddBinding(_rDPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, ResidentialDemandOpen, SetResidentialDemandVisibility));

            _wFPVBinding = new ValueBinding<bool>(ModID, WorkforceOpen, false);
            AddBinding(_wFPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, WorkforceOpen, SetWorkforceVisibility));

            _wPPVBinding = new ValueBinding<bool>(ModID, WorkplacesOpen, false);
            AddBinding(_wPPVBinding);
            AddBinding(new TriggerBinding<bool>(ModID, WorkplacesOpen, SetWorkplacesVisibility));

            _TrafficDataVisibleBinding = new ValueBinding<bool>(ModID, "TrafficDataVisible", false);
            AddBinding(_TrafficDataVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, "TrafficDataVisible", SetTrafficDataVisibility));

            _effectsVisibleBinding = new ValueBinding<bool>(ModID, EffectsOpen, false);
            AddBinding(_effectsVisibleBinding);
            AddBinding(new TriggerBinding<bool>(ModID, EffectsOpen, SetEffectsVisibility));

            positionXBinding = CreateBinding("LoadPositionX", 0f);
            positionYBinding = CreateBinding("LoadPositionY", 0f);

            m_uiBuildingDemand = CreateBinding("BuildingDemandData", new int[0]);
            //CommercialDemandDataUI
            m_CommercialBinding = CreateBinding("CommercialData", new int[10]);
            m_ExcludedResourcesBinding = CreateBinding("CommercialDataExRes", new string[0]);

            //CommercialProductsDataUI
            m_CommercialProductBinding = CreateBinding("CommercialProductsData", Array.Empty<CommercialProductsData>());


            //DemographicsUI
            m_TotalsBinding = CreateBinding("DemographicsDataTotals", new int[10]);
            m_OldCitizenBinding = CreateBinding("DemographicsDataOldestCitizen", () =>
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                return demographics.m_Totals[6];
            });
            m_DemoStatsToggledOnBinding = new ValueBinding<bool>(ModID, "DemoStatsToggledOn", false);
            AddBinding(m_DemoStatsToggledOnBinding);
            AddBinding(new TriggerBinding<bool>(ModID, "DemoStatsToggledOn", SetDemoStatsVisibility));
            m_DemoAgeGroupingToggledOnBinding = new ValueBinding<bool>(ModID, "DemoAgeGroupingToggledOn", false);
            AddBinding(m_DemoAgeGroupingToggledOnBinding);
            AddBinding(new TriggerBinding<bool>(ModID, "DemoAgeGroupingToggledOn", SetDemoAgeGroupingVisibility));
            m_DemoGroupingStrategyBinding = CreateGenericBinding("DemoGroupingStrategy", "SetDemoGroupingStrategy",
                GroupingStrategy.None);
            AddBinding(m_SelectedDistrict = new ValueBinding<Entity>(ModID, "selectedDistrict", CityWide));
            AddBinding(new TriggerBinding<Entity>(ModID, "selectedDistrictChanged", SelectedDistrictChanged));
            AddBinding(m_DistrictInfos = new RawValueBinding(ModID, "districtInfos", UpdateDistrictInfos));
            m_Demographics1Binding = CreateGenericBinding("Demographics1", "SetDemographics1", Demographics1.All);
            m_Demographics2Binding = CreateGenericBinding("Demographics2", "SetDemographics2", Demographics2.All);
            _RefreshDataBinding =
                CreateGenericBinding("demographics", "updateDemographics", false, UpdateDemographicsData);
            m_DemographicsLifecycleTotalsBinding = CreateBinding("DemographicsLifecycleTotals", new int[4]);
            m_DemographicsLifecycleDetailsBinding =
                CreateBinding("DemographicsLifecycleDetails", new PopulationLifecycleInfo[0]);
            m_DemographicsDetailedGroupDetailsBinding =
                CreateBinding("DemographicsDetailedData", new PopulationDetailedGroupInfo[0]);
            m_DemographicsFiveYearDetailsBinding =
                CreateBinding("DemographicsFiveYearDetails", new PopulationFiveYearGroupInfo[0]);
            m_DemographicsTenYearDetailsBinding =
                CreateBinding("DemographicsTenYearDetails", new PopulationTenYearGroupInfo[0]);


            //IndustrialDemandDataUI
            m_IndustrialBinding = CreateBinding("IndustrialData", new int[16]);
            m_IndustrialExcludedResourcesBinding = CreateBinding("IndustrialDataExRes", new string[0]);

            //IndustrialProductsDataUI

            //ResidentialDemandDataUI
            m_ResidentialBinding = CreateBinding("ResidentialData", new float[21]);

            //WorkforceUI
            m_WorkforcesBinder = CreateBinding("WorkforceData", new WorkforcesInfo[0]);

            //WorkplacesUI
            m_WorkplacesBinder = CreateBinding("WorkplacesData", new WorkplacesInfo[0]);
            //hideColumnsBindingWP = CreateBinding("ShowExtraWorkplaces", 0);
        }


        protected override void OnUpdate()
        {
            CheckForDistrictChange();
            //CheckForResource();
            if (_bDPVBinding.value)
                m_uiBuildingDemand.Value = new[]
                {
                    m_ResidentialDemandSystem.buildingDemand.x,
                    m_ResidentialDemandSystem.buildingDemand.y,
                    m_ResidentialDemandSystem.buildingDemand.z,
                    m_CommercialDemandSystem.buildingDemand,
                    m_IndustrialDemandSystem.industrialBuildingDemand,
                    m_IndustrialDemandSystem.storageBuildingDemand,
                    m_IndustrialDemandSystem.officeBuildingDemand
                };

            if (_cDPVBinding.value)
            {
                var commercialSystem = World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    commercialSystem.m_IncludedResources.value == Resource.NoResource
                        ? new string[0]
                        : UIUtil.ExtractExcludedResources(commercialSystem.m_IncludedResources.value);
                m_CommercialSystem.IsPanelVisible = true;
            }


            //m_Log.Debug($"{nameof(InfoLoomUISystem)}.{nameof(OnUpdate)} 2.");
            if (_dPVBinding.value)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();

                var currentStrategy = m_DemoGroupingStrategyBinding.Value;
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                m_DemoGroupingStrategyBinding.UpdateCallback(currentStrategy);
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }

                m_Demographics.IsPanelVisible = true;
            }

            if (_iDPVBinding.value)
            {
                var industrialSystem = World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    industrialSystem.m_IncludedResources.value == Resource.NoResource
                        ? new string[0]
                        : UIUtil.IndustrialExtractExcludedResources(industrialSystem.m_IncludedResources.value);

                m_IndustrialSystem.IsPanelVisible = true;
            }


            if (_rDPVBinding.value)
            {
                var residentialSystem = World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();


                m_ResidentialSystem.IsPanelVisible = true;
            }

            if (_wFPVBinding.value)
            {
                var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();

                m_WorkforceSystem.IsPanelVisible = true;
            }

            if (_wPPVBinding.value)
            {
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();


                m_WorkplacesSystem.IsPanelVisible = true;
                m_WorkplacesSystem.ForceUpdateOnce();
                //hideColumnsBindingWP.Value = Mod.setting.hideNoColumnsWP;
            }


            base.OnUpdate();
        }

        private void SetInfoLoomMenuVisibility(bool open)
        {
            _panelVisibleBinding.Update(open);
        }

        private void SetCommercialMenuVisibility(bool open)
        {
            _commercialPanelVisibleBinding.Update(open);
        }

        private void SetIndustrialMenuVisibility(bool open)
        {
            _industrialPanelVisibleBinding.Update(open);
        }

        private void SetDistrictMenuVisibility(bool open)
        {
            _districtPanelVisibleBinding.Update(open);
        }

        private void SetResidentialMenuVisibility(bool open)
        {
            _residentialPanelVisibleBinding.Update(open);
        }

        private void SetSankeyMenuVisibility(bool open)
        {
            _sankeyPanelVisibleBinding.Update(open);
        }

        private void SetBuildingDemandVisibility(bool open)
        {
            _bDPVBinding.Update(open);

            if (open)
                m_uiBuildingDemand.Value = new[]
                {
                    m_ResidentialDemandSystem.buildingDemand.x,
                    m_ResidentialDemandSystem.buildingDemand.y,
                    m_ResidentialDemandSystem.buildingDemand.z,
                    m_CommercialDemandSystem.buildingDemand,
                    m_IndustrialDemandSystem.industrialBuildingDemand,
                    m_IndustrialDemandSystem.storageBuildingDemand,
                    m_IndustrialDemandSystem.officeBuildingDemand
                };
        }


        private void SetCommercialDemandVisibility(bool open)
        {
            _cDPVBinding.Update(open);
            m_CommercialSystem.IsPanelVisible = open;
            if (open)
            {
                var commercialSystem = World.GetOrCreateSystemManaged<CommercialSystem>();
                m_CommercialBinding.Value = commercialSystem.m_Results.ToArray();
                m_ExcludedResourcesBinding.Value =
                    commercialSystem.m_IncludedResources.value == Resource.NoResource
                        ? new string[0]
                        : UIUtil.ExtractExcludedResources(commercialSystem.m_IncludedResources.value);
            }
        }


        private void SetDemographicsVisibility(bool open)
        {
            _dPVBinding.Update(open);
            m_Demographics.IsPanelVisible = open;

            if (open)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                m_DemoAgeGroupingToggledOnBinding.TriggerUpdate();
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
            }
        }

        private void SetIndustrialDemandVisibility(bool open)
        {
            _iDPVBinding.Update(open);
            m_IndustrialSystem.IsPanelVisible = open;

            if (open)
            {
                var industrialSystem = World.GetOrCreateSystemManaged<IndustrialSystem>();
                m_IndustrialBinding.Value = industrialSystem.m_Results.ToArray();
                m_IndustrialExcludedResourcesBinding.Value =
                    industrialSystem.m_IncludedResources.value == Resource.NoResource
                        ? new string[0]
                        : UIUtil.IndustrialExtractExcludedResources(industrialSystem.m_IncludedResources.value);
            }
        }


        private void SetResidentialDemandVisibility(bool open)
        {
            _rDPVBinding.Update(open);
            m_ResidentialSystem.IsPanelVisible = open;

            if (open)
            {
                var residentialSystem = World.GetOrCreateSystemManaged<ResidentialSystem>();
                m_ResidentialBinding.Value = residentialSystem.m_Results.ToArray();
            }
        }


        private void SetWorkforceVisibility(bool open)
        {
            _wFPVBinding.Update(open);
            m_WorkforceSystem.IsPanelVisible = open;

            if (open)
            {
                m_WorkforceSystem.ForceUpdateOnce();
                var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }
        }

        private void SetWorkplacesVisibility(bool open)
        {
            _wPPVBinding.Update(open);
            m_WorkplacesSystem.IsPanelVisible = open;

            if (open)
            {
                m_WorkplacesSystem.ForceUpdateOnce();
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
            }
        }

        private void SetDemoStatsVisibility(bool on)
        {
            m_DemoStatsToggledOnBinding.Update(on);
            if (on)
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = m_Demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
        }

        private void SetDemoAgeGroupingVisibility(bool on)
        {
            m_DemoAgeGroupingToggledOnBinding.Update(on);
        }

        private void SetTrafficDataVisibility(bool open)
        {
            _TrafficDataVisibleBinding.Update(open);
            if (open) m_uiTrafficData.Update();
        }

        private void SetEffectsVisibility(bool open)
        {
            _effectsVisibleBinding.Update(open);
        }

        private void CheckForDistrictChange()
        {
            // Get district infos and check for changes
            var foundSelectedDistrict = selectedDistrict == CityWide;
            var districtInfos = new DistrictInfos();

            var districtEntities = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            foreach (var districtEntity in districtEntities)
            {
                var districtName = m_NameSystem.GetRenderedLabelName(districtEntity);
                if (districtName != "Assets.DISTRICT_NAME")
                {
                    districtInfos.Add(new DistrictInfo(districtEntity, districtName));
                    if (districtEntity == selectedDistrict) foundSelectedDistrict = true;
                }
            }

            if (!foundSelectedDistrict)
            {
                selectedDistrict = CityWide;
                m_SelectedDistrict.Update(selectedDistrict);
            }

            districtInfos.Sort();
            districtInfos.Insert(0, new DistrictInfo(CityWide, "City Wide"));

            // Check if district infos have changed
            var districtsChanged = false;
            if (districtInfos.Count != _DistrictInfos.Count)
                districtsChanged = true;
            else
                for (var i = 0; i < districtInfos.Count; i++)
                    if (districtInfos[i].entity != _DistrictInfos[i].entity ||
                        districtInfos[i].name != _DistrictInfos[i].name)
                    {
                        districtsChanged = true;
                        break;
                    }

            if (districtsChanged)
            {
                _DistrictInfos = districtInfos;
                m_DistrictInfos.Update();
            }

            districtEntities.Dispose();
        }

        private void UpdateDistrictInfos(IJsonWriter writer)
        {
            _DistrictInfos.Write(writer);
        }

        private void SelectedDistrictChanged(Entity newDistrict)
        {
            selectedDistrict = newDistrict;
            m_SelectedDistrict.Update(selectedDistrict);
            if (_dPVBinding.value)
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                demographics.SetSelectedDistrict(newDistrict);
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                if (m_DemoStatsToggledOnBinding.value)
                {
                    m_TotalsBinding.Value = demographics.m_Totals.ToArray();
                    m_OldCitizenBinding.Update();
                }
            }

            if (_wFPVBinding.value)
            {
                var workforceSystem = World.GetOrCreateSystemManaged<WorkforceSystem>();
                workforceSystem.SetSelectedDistrict(newDistrict);
                m_WorkforcesBinder.Value = workforceSystem.m_Results.ToArray();
            }

            if (_wPPVBinding.value)
            {
                var workplacesSystem = World.GetOrCreateSystemManaged<WorkplacesSystem>();
                workplacesSystem.SetSelectedDistrict(newDistrict);
                m_WorkplacesBinder.Value = workplacesSystem.m_Results.ToArray();
            }
        }


        private void UpdateDemographicsData(bool buttonPressed)
        {
            _RefreshDataBinding.Value = buttonPressed;
            if (buttonPressed)
            {
                // Force immediate demographics update
                m_Demographics.UpdateDemographics();
                var demographics = World.GetExistingSystemManaged<Demographics>();
                m_DemographicsLifecycleTotalsBinding.Value = demographics.m_LifecycleTotals.ToArray();
                m_DemographicsLifecycleDetailsBinding.Value = demographics.m_LifecycleDetails.ToArray();
                m_DemographicsDetailedGroupDetailsBinding.Value = demographics.m_Results.ToArray();
                m_DemographicsFiveYearDetailsBinding.Value = demographics.m_FiveYearDetails.ToArray();
                m_DemographicsTenYearDetailsBinding.Value = demographics.m_TenYearDetails.ToArray();
                if (m_DemoStatsToggledOnBinding.value) m_TotalsBinding.Value = demographics.m_Totals.ToArray();

                // Immediately push the updated data to UI bindings
                m_DemographicsLifecycleTotalsBinding.Binding.TriggerUpdate();
                m_DemographicsLifecycleDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsDetailedGroupDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsFiveYearDetailsBinding.Binding.TriggerUpdate();
                m_DemographicsTenYearDetailsBinding.Binding.TriggerUpdate();
                m_TotalsBinding.Binding.TriggerUpdate();
                m_OldCitizenBinding.Update();

                //m_Log.Info("Demographics data manually refreshed and bindings updated");
            }
        }
    }
}