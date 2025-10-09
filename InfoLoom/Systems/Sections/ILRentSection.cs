using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Net;
using Game.Prefabs;
using Game.Zones;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using UnityEngine;

namespace InfoLoomTwo.Systems.Sections
{
    public partial class ILRentSection : ExtendedInfoSectionBase
    {
	    private string _AreaType;
	    private int _Level;
	    private int _LotSize;
	    private float _SpaceMultiplier;
	    private float _BaseRent;
	    private float _LandValueModifier;
	    private float _LandValueBase;
	    private bool _IgnoreLandValue;
	    private float _LandValueRate;
	    private float _TotalRent;
	    private bool _IsMixedUse;
	    private float _BusinessRentPercent;
	    private float _PropertiesCount;
	    private int _RentPerHousehold;
	    private float _ZoneType;
	    private bool _IsRenting;
        protected override string group => nameof(ILRentSection);
        protected override void Reset()
        {
	        _AreaType = "";
	        _Level = 0;
	        _LotSize = 0;
	        _SpaceMultiplier = 0;
	        _BaseRent = 0;
	        _LandValueModifier = 0;
	        _LandValueBase = 0;
	        _IgnoreLandValue = false;
	        _LandValueRate = 0;
	        _TotalRent = 0;
	        _IsMixedUse = false;
	        _BusinessRentPercent = 0;
	        _PropertiesCount = 0;
	        _RentPerHousehold = 0;
        }

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InfoUISystem.AddMiddleSection(this);
		}

		private bool Visible()
		{
			_IsRenting = false;
			if(EntityManager.HasComponent<SpawnableBuildingData>(selectedPrefab) && !Mod.setting.hideRentSection )
			{
				return true;
			}
			return _IsRenting;
		}

		protected override void OnUpdate()
		{
			visible = Visible();
		}
		protected override void OnProcess()
		{
			if (
				EntityManager.TryGetComponent(selectedEntity, out PrefabRef prefabRef)
				&& EntityManager.TryGetComponent(prefabRef.m_Prefab, out BuildingPropertyData propertyData)
				&& EntityManager.TryGetComponent(prefabRef.m_Prefab, out BuildingData buildingData)
				&& EntityManager.TryGetComponent(selectedEntity, out Building building)
				&& EntityManager.HasComponent<LandValue>(building.m_RoadEdge)
				&& EntityManager.TryGetComponent(
					prefabRef.m_Prefab,
					out SpawnableBuildingData spawnableBuildingData
				)
				&& EntityManager.TryGetComponent(
					spawnableBuildingData.m_ZonePrefab,
					out ZoneData zoneData
				)
				&& EntityManager.TryGetComponent(
					spawnableBuildingData.m_ZonePrefab,
					out ZonePropertiesData zonePropData
				)
			)
			{
				var landValue = EntityManager
                        .GetComponentData<LandValue>(building.m_RoadEdge)
                        .m_LandValue;
				 EconomyParameterData economyParameterData =
                        SystemAPI.GetSingleton<EconomyParameterData>();
				_AreaType = zoneData.m_AreaType.ToString();
				_Level = spawnableBuildingData.m_Level;
				_LotSize = buildingData.m_LotSize.x * buildingData.m_LotSize.y;
				_SpaceMultiplier = propertyData.m_SpaceMultiplier;
				_BaseRent = PropertyUtils.GetRentPricePerRenter(
                        propertyData,
                        _Level,
                        buildingData.m_LotSize.x * buildingData.m_LotSize.y,
                        landValue,
                        zoneData.m_AreaType,
                        ref economyParameterData,
                        zonePropData.m_IgnoreLandValue
				);
				 switch (zoneData.m_AreaType)
				 {
					 case Game.Zones.AreaType.Residential:
						 _ZoneType = economyParameterData
							 .m_RentPriceBuildingZoneTypeBase
							 .x;
						 _LandValueModifier = economyParameterData
							 .m_LandValueModifier
							 .x;
						 break;
					 case Game.Zones.AreaType.Commercial:
						 _ZoneType = economyParameterData
							 .m_RentPriceBuildingZoneTypeBase
							 .y;
						 _LandValueModifier = economyParameterData
							 .m_LandValueModifier
							 .y;
						 break;
					 case Game.Zones.AreaType.Industrial:
						 _ZoneType = economyParameterData
							 .m_RentPriceBuildingZoneTypeBase
							 .z;
						 _LandValueModifier = economyParameterData
							 .m_LandValueModifier
							 .z;
						 break;
				 }
				 _LandValueBase = landValue;
				 _IgnoreLandValue = zonePropData.m_IgnoreLandValue;
				 _LandValueRate = _LandValueModifier * _LandValueBase;
				 (float maxRent, float propertyCount) = CheckMaxRent(
						propertyData,
						_Level,
						_LotSize,
						landValue,
						zoneData.m_AreaType,
						ref economyParameterData,
						zonePropData.m_IgnoreLandValue
				 );
				 _TotalRent = maxRent; 
				 _IsMixedUse = PropertyUtils.IsMixedBuilding(propertyData);
				 _BusinessRentPercent = economyParameterData.m_MixedBuildingCompanyRentPercentage;
				 _PropertiesCount = propertyCount;
				 _RentPerHousehold = PropertyUtils.GetRentPricePerRenter(
					 propertyData,
                        _Level,
                        buildingData.m_LotSize.x * buildingData.m_LotSize.y,
                        landValue,
                        zoneData.m_AreaType,
                        ref economyParameterData,
                        zonePropData.m_IgnoreLandValue);
			}
		}

		public override void OnWriteProperties(IJsonWriter writer)
		{
			writer.PropertyName("HideRentSection");
			writer.Write(Mod.setting.hideRentSection);
			writer.PropertyName("AreaType");
			writer.Write(_AreaType);
			writer.PropertyName("Level");
			writer.Write(_Level);
			writer.PropertyName("LotSize");
			writer.Write(_LotSize);
			writer.PropertyName("SpaceMultiplier");
			writer.Write(_SpaceMultiplier);
			writer.PropertyName("BaseRent");
			writer.Write(_BaseRent);
			writer.PropertyName("LandValueModifier");
			writer.Write(_LandValueModifier);
			writer.PropertyName("LandValueBase");
			writer.Write(_LandValueBase);
			writer.PropertyName("LandValueRate");
			writer.Write(_LandValueRate);
			writer.PropertyName("TotalRent");
			writer.Write(_TotalRent);
			writer.PropertyName("IsMixedUse");
			writer.Write(_IsMixedUse);
			writer.PropertyName("BusinessRentPercent");
			writer.Write(_BusinessRentPercent);
			writer.PropertyName("PropertiesCount");
			writer.Write(_PropertiesCount);
			writer.PropertyName("RentPerHousehold");
			writer.Write(_RentPerHousehold);
			writer.PropertyName("ZoneType");
			writer.Write(_ZoneType);
			
		}
		public (float maxRent, float propertyCount) CheckMaxRent(
		    BuildingPropertyData propertyData,
		    int level,
		    int lotSize,
		    float landValue,
		    AreaType areaType,
		    ref EconomyParameterData economyParams,
		    bool ignoreLandValue = false
		)
		{
		    float zoneBaseRent = economyParams.m_RentPriceBuildingZoneTypeBase.x;
		    float landValueModifier = economyParams.m_LandValueModifier.x;

		    switch (areaType)
		    {
		        case AreaType.Commercial:
		            zoneBaseRent = economyParams.m_RentPriceBuildingZoneTypeBase.y;
		            landValueModifier = economyParams.m_LandValueModifier.y;
		            break;
		        case AreaType.Industrial:
		            zoneBaseRent = economyParams.m_RentPriceBuildingZoneTypeBase.z;
		            landValueModifier = economyParams.m_LandValueModifier.z;
		            break;
		    }

		    float rent;
		    if (ignoreLandValue)
		    {
		        rent = zoneBaseRent * level * lotSize * propertyData.m_SpaceMultiplier;
		    }
		    else
		    {
		        rent = (landValue * landValueModifier + zoneBaseRent * level) * lotSize * propertyData.m_SpaceMultiplier;
		    }

		    float properties;
		    if (PropertyUtils.IsMixedBuilding(propertyData))
		    {
		        properties = Mathf.RoundToInt(
		            propertyData.m_ResidentialProperties / (1f - economyParams.m_MixedBuildingCompanyRentPercentage)
		        );
		    }
		    else
		    {
		        properties = propertyData.CountProperties();
		    }

		    return (rent, properties);
		}
    }
}