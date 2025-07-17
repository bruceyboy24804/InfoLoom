using System;
using Colossal;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.SceneFlow;
using Game.UI;
using Game.UI.InGame;
using InfoLoomTwo.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.Sections
{
	public partial class ILCitizenSection : ExtendedInfoSectionBase
	{
		protected override string group => nameof(ILCitizenSection);
		private bool isCitizen;
		private string Household;
		private int HouseholdMoney;
		private int HouseholdSpendableMoney;
		private string HouseholdNeedResources;
		private int HouseholdNeedResourcesAmount;
		private Entity companyEntity;
		private string Shift;
		private string WellBeing;
		private int Health;
		private int BirthDay;
		private string Purpose;
		private int ShoppingAmount;
		private string Resource;
		private int Rent;
		private int NumberOfCitizensInHousehold;

		protected override void Reset() { }

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InfoUISystem.AddMiddleSection(this);
		}

		private bool Visible()
		{
			isCitizen = false;
			if (EntityManager.HasComponent<Citizen>(selectedEntity))
			{
				isCitizen = true;
			}
			return isCitizen;
		}

		protected override void OnUpdate()
		{
			base.visible = Visible();
		}

		protected override void OnProcess()
		{
			Entity companyEntity = Entity.Null;
			companyEntity = CitizenUIUtils.GetCompanyEntity(base.EntityManager, selectedEntity);
			// Household
			Entity household = Entity.Null;
			if (EntityManager.HasComponent<HouseholdMember>(selectedEntity))
			{
				household = EntityManager.GetComponentData<HouseholdMember>(selectedEntity).m_Household;
				Household = m_NameSystem.GetRenderedLabelName(household);
			}
			HouseholdNeedResources = "";
			HouseholdNeedResourcesAmount = 0;
			if (household != Entity.Null && EntityManager.HasComponent<HouseholdNeed>(household))
			{
				var need = EntityManager.GetComponentData<HouseholdNeed>(household);
				HouseholdNeedResources = need.m_Resource.ToString();
				HouseholdNeedResourcesAmount = need.m_Amount;
			}
			Household householdData = default(Household);
			if (EntityManager.HasComponent<Game.Economy.Resources>(household))
			{
				int resources = EconomyUtils.GetResources(Game.Economy.Resource.Money, base.EntityManager.GetBuffer<Game.Economy.Resources>(household, isReadOnly: true));
				HouseholdMoney = resources;
				if (base.EntityManager.TryGetComponent<PropertyRenter>(household, out var component))
				{
					BufferLookup<Renter> m_RenterBufs = SystemAPI.GetBufferLookup<Renter>(isReadOnly: true);
					ComponentLookup<Game.Prefabs.ConsumptionData> consumptionDatas = SystemAPI.GetComponentLookup<Game.Prefabs.ConsumptionData>(isReadOnly: true);
					ComponentLookup<PrefabRef> prefabRefs = SystemAPI.GetComponentLookup<PrefabRef>(isReadOnly: true);
					HouseholdSpendableMoney = EconomyUtils.GetHouseholdSpendableMoney
						(householdData,
							EntityManager.GetBuffer<Game.Economy.Resources>(household, isReadOnly: true),
							ref m_RenterBufs,
							ref consumptionDatas,
							ref prefabRefs,
							component
						);
				}
			}
			//Shift
			Shift = "";
			if (EntityManager.TryGetComponent<Worker>(selectedEntity, out var worker))
			{
				Shift = worker.m_Shift.ToString();
			}
			WellBeing = "";
			if (EntityManager.TryGetComponent<Citizen>(selectedEntity, out var citizenData))
			{
				Citizen componentData19 = base.EntityManager.GetComponentData<Citizen>(selectedEntity);
				WellBeing = $"{WellbeingToString(componentData19.m_WellBeing)} ({componentData19.m_WellBeing})";
			}
			// Get rent and money values from the selected entity
			Rent = 0;
			Entity householder = Entity.Null;
			if (EntityManager.HasComponent<HouseholdMember>(selectedEntity))
			{
				householder = EntityManager.GetComponentData<HouseholdMember>(selectedEntity).m_Household;
				if (household != Entity.Null && EntityManager.HasComponent<PropertyRenter>(householder))
				{
					PropertyRenter componentData = EntityManager.GetComponentData<PropertyRenter>(householder);
					Rent = componentData.m_Rent;
				}
			}
			// Number of Citizens in Household
			NumberOfCitizensInHousehold = 0;
			if (household != Entity.Null && EntityManager.HasBuffer<HouseholdCitizen>(household))
			{
				DynamicBuffer<HouseholdCitizen> householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(household);
				NumberOfCitizensInHousehold = householdCitizens.Length;
			}

			// Purpose
			Purpose = "";
			if (EntityManager.TryGetComponent<TravelPurpose>(selectedEntity, out var component2))
			{
				Purpose purpose = component2.m_Purpose;
				Purpose = purpose.ToString();

			}


			// Health
			Health = 0;
			if (EntityManager.TryGetComponent<Citizen>(selectedEntity, out var health))
			{
				Health = health.m_Health;
			}

			// BirthDay
			BirthDay = 0;
			if (EntityManager.TryGetComponent<Citizen>(selectedEntity, out var age))
			{
				BirthDay = age.m_BirthDay;
			}

			// ShoppingAmount
			ShoppingAmount = 0;
			if (EntityManager.TryGetComponent<TravelPurpose>(selectedEntity, out var shopper))
			{
				ShoppingAmount = shopper.m_Data;
			}

			// Resource
			Resource = "";
			if (EntityManager.TryGetComponent<TravelPurpose>(selectedEntity, out var shopper2))
			{
				Resource = shopper2.m_Resource.ToString();
			}
		}

		public override void OnWriteProperties(IJsonWriter writer)
		{
			writer.PropertyName("Household");
			writer.Write(Household);

			writer.PropertyName("HouseholdMoney");
			writer.Write(HouseholdMoney);
			writer.PropertyName("HouseholdSpendableMoney");
			writer.Write(HouseholdSpendableMoney);
			writer.PropertyName("HouseholdNeedResources");
			writer.Write(HouseholdNeedResources);
			writer.PropertyName("HouseholdNeedResourcesAmount");
			writer.Write(HouseholdNeedResourcesAmount);
			writer.PropertyName("Workplace");
			m_NameSystem.BindName(writer, companyEntity);

			writer.PropertyName("Shift");
			writer.Write(Shift);

			writer.PropertyName("WellBeing");
			writer.Write(WellBeing);

			writer.PropertyName("Health");
			writer.Write(Health);

			writer.PropertyName("BirthDay");
			writer.Write(BirthDay);

			writer.PropertyName("Purpose");
			writer.Write(Purpose);

			writer.PropertyName("ShoppingAmount");
			writer.Write(ShoppingAmount);

			writer.PropertyName("Resource");
			writer.Write(Resource);

			writer.PropertyName("Rent");
			writer.Write(Rent);

			writer.PropertyName("NumberOfCitizensInHousehold");
			writer.Write(NumberOfCitizensInHousehold);
		}


		private static string WellbeingToString(int wellbeing)
		{
			if (wellbeing < 25)
				return "Depressed";
			if (wellbeing < 40)
				return "Sad";
			if (wellbeing < 60)
				return "Neutral";
			if (wellbeing < 80)
				return "Content";
			return "Happy";
		}
    }
}