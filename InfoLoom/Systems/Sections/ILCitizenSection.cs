using ModsCommon.Extensions;
using Colossal.Entities;
using Colossal.PSI.Common;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Economy;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Entities;
using Mod = InfoLoomTwo.InfoLoomMod;

namespace InfoLoomTwo.Systems.Sections
{
    public partial class ILCitizenSection : ExtendedInfoSectionBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;
        // Private fields following naming conventions from other sections
        private Entity citizenEntity;
        private Entity householdEntity;
        private Entity companyEntity;

        private string _Household;
        private int _HouseholdMoney;
        private int _HouseholdSpendableMoney;
        private string _HouseholdNeedResources;
        private int _HouseholdNeedResourcesAmount;
        private string _Workplace;
        private string _Shift;
        private string _WellBeing;
        private int _Health;
        private int _BirthDay;
        private string _Purpose;
        private int _ShoppingAmount;
        private string _Resource;
        private int _Rent;
        private int _NumberOfCitizensInHousehold;

        protected override string group => nameof(ILCitizenSection);

        protected override void Reset()
        {
            citizenEntity = Entity.Null;
            householdEntity = Entity.Null;
            companyEntity = Entity.Null;

            _Household = "";
            _HouseholdMoney = 0;
            _HouseholdSpendableMoney = 0;
            _HouseholdNeedResources = "";
            _HouseholdNeedResourcesAmount = 0;
            _Workplace = "";
            _Shift = "";
            _WellBeing = "";
            _Health = 0;
            _BirthDay = 0;
            _Purpose = "";
            _ShoppingAmount = 0;
            _Resource = "";
            _Rent = 0;
            _NumberOfCitizensInHousehold = 0;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_InfoUISystem.AddMiddleSection(this);
        }

        private bool Visible()
        {
            if (EntityManager.HasComponent<Citizen>(selectedEntity) && !Mod.setting.hideCitizenSection)
            {
                citizenEntity = selectedEntity;
                return true;
            }

            return false;
        }

        protected override void OnUpdate()
        {
            visible = Visible();
        }

        protected override void OnProcess()
        {
            // Get household entity
            if (EntityManager.TryGetComponent<HouseholdMember>(citizenEntity, out var householdMember))
            {
                householdEntity = householdMember.m_Household;
                _Household = m_NameSystem.GetRenderedLabelName(householdEntity);
            }

            // Get company entity
            companyEntity = CitizenUIUtils.GetCompanyEntity(EntityManager, citizenEntity);
            _Workplace = companyEntity != Entity.Null ? m_NameSystem.GetRenderedLabelName(companyEntity) : "";

            // Process household data
            ProcessHouseholdData();

            // Process citizen data
            ProcessCitizenData();

            // Process travel purpose data
            ProcessTravelPurposeData();
        }

        private void ProcessHouseholdData()
        {
            if (householdEntity == Entity.Null) return;

            // Household needs
            if (EntityManager.TryGetComponent<HouseholdNeed>(householdEntity, out var need))
            {
                _HouseholdNeedResources = need.m_Resource.ToString();
                _HouseholdNeedResourcesAmount = need.m_Amount;
            }

            // Household money
            if (EntityManager.HasBuffer<Resources>(householdEntity))
            {
                EntityManager.TryGetBuffer<Resources>(householdEntity, true, out var resourceBuffer);
                _HouseholdMoney = EconomyUtils.GetResources(Resource.Money, resourceBuffer);

                // Spendable money calculation
                if (EntityManager.TryGetComponent<PropertyRenter>(householdEntity, out var propertyRenter))
                {
                    var renterBufs = SystemAPI.GetBufferLookup<Renter>(true);
                    var consumptionDatas = SystemAPI.GetComponentLookup<ConsumptionData>(true);
                    var prefabRefs = SystemAPI.GetComponentLookup<PrefabRef>(true);

                    _HouseholdSpendableMoney = EconomyUtils.GetHouseholdSpendableMoney(
                        default,
                        resourceBuffer,
                        ref renterBufs,
                        ref consumptionDatas,
                        ref prefabRefs,
                        propertyRenter
                    );

                    _Rent = propertyRenter.m_Rent;
                }
            }

            // Number of citizens in household
            if (EntityManager.HasBuffer<HouseholdCitizen>(householdEntity))
            {
                EntityManager.TryGetBuffer<HouseholdCitizen>(householdEntity, true, out var householdCitizens);
                _NumberOfCitizensInHousehold = householdCitizens.Length;
            }
        }

        private void ProcessCitizenData()
        {
            // Worker shift
            if (EntityManager.TryGetComponent<Worker>(citizenEntity, out var worker))
                _Shift = worker.m_Shift.ToString();

            // Citizen wellbeing and health
            if (EntityManager.TryGetComponent<Citizen>(citizenEntity, out var citizen))
            {
                _WellBeing = $"{WellbeingToString(citizen.m_WellBeing)} ({citizen.m_WellBeing})";
                _Health = citizen.m_Health;
                _BirthDay = citizen.m_BirthDay;
            }
        }

        private void ProcessTravelPurposeData()
        {
            if (EntityManager.TryGetComponent<TravelPurpose>(citizenEntity, out var travelPurpose))
            {
                _Purpose = travelPurpose.m_Purpose.ToString();
                _ShoppingAmount = travelPurpose.m_Data;
                _Resource = travelPurpose.m_Resource.ToString();
            }
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("HideCitizenSection");
            writer.Write(Mod.setting.hideCitizenSection);
            writer.PropertyName("Household");
            writer.Write(_Household);
            writer.PropertyName("HouseholdMoney");
            writer.Write(_HouseholdMoney);
            writer.PropertyName("HouseholdSpendableMoney");
            writer.Write(_HouseholdSpendableMoney);
            writer.PropertyName("HouseholdNeedResources");
            writer.Write(_HouseholdNeedResources);
            writer.PropertyName("HouseholdNeedResourcesAmount");
            writer.Write(_HouseholdNeedResourcesAmount);
            writer.PropertyName("Workplace");
            writer.Write(_Workplace);
            writer.PropertyName("Shift");
            writer.Write(_Shift);
            writer.PropertyName("WellBeing");
            writer.Write(_WellBeing);
            writer.PropertyName("Health");
            writer.Write(_Health);
            writer.PropertyName("BirthDay");
            writer.Write(_BirthDay);
            writer.PropertyName("Purpose");
            writer.Write(_Purpose);
            writer.PropertyName("ShoppingAmount");
            writer.Write(_ShoppingAmount);
            writer.PropertyName("Resource");
            writer.Write(_Resource);
            writer.PropertyName("Rent");
            writer.Write(_Rent);
            writer.PropertyName("NumberOfCitizensInHousehold");
            writer.Write(_NumberOfCitizensInHousehold);
        }

        private static string WellbeingToString(int wellbeing)
        {
            if (wellbeing < 25) return "Depressed";
            if (wellbeing < 40) return "Sad";
            if (wellbeing < 60) return "Neutral";
            if (wellbeing < 80) return "Content";
            return "Happy";
        }
    }
}
