using System.Collections.Generic;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.UI;
using Game.Vehicles;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Data;
using InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Mod = InfoLoomTwo.InfoLoomMod;

namespace InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Systems
{
    public partial class StoragePropertyCompanies : CommonUISystemBase
    {
        protected override string ModId => InfoLoomMod.Instance.Id;

        private ValueBindingHelper<List<StorageCompanyUI>> m_StorageCompanyInfoBinding;
        private ValueBindingHelper<bool> _storageCompaniesVisibleBinding;
        private RawValueBinding _bindingStorageCompanyUISettings;

        private NativeList<StorageCompanyInfo> m_JobResults;
        private EntityQuery storageCompanyQuery;
        private NameSystem nameSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            
            AddBinding(new TriggerBinding<float2>(Mod.Instance.ModName, "StoragePanelMoved",   StorageCompanyPanelMoved   ));
            
            
            storageCompanyQuery = SystemAPI.QueryBuilder().WithAll<StorageCompany, CompanyData>().Build();
            RequireForUpdate(storageCompanyQuery);
            nameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_JobResults = new NativeList<StorageCompanyInfo>(Allocator.Persistent);
            m_StorageCompanyInfoBinding = CreateBinding("StorageCompanies", new List<StorageCompanyUI>());
            _storageCompaniesVisibleBinding = CreateGenericBinding("StoragePanelVisible", "SetStoragePanelVisible", false, SetStorageVisibility);
            AddBinding(_bindingStorageCompanyUISettings     = new RawValueBinding(Mod.Instance.ModName, "StorageCompanyUISettings",  WriteStorageCompanyUISettings));
        }

        protected override void OnDestroy()
        { 
            m_JobResults.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            m_JobResults.Clear();
            if(_storageCompaniesVisibleBinding)
            {
                SetStorageVisibility(_storageCompaniesVisibleBinding.Value);
            }
            
            
            base.OnUpdate();
        }
        
        private void WriteStorageCompanyUISettings(IJsonWriter writer)
        {
			writer.TypeBegin(Mod.Instance.ModName + ".StorageCompanyUISettings");
			writer.PropertyName("panelPositionX");
			writer.Write(Mod.setting.panelPosition.x);
			writer.PropertyName("panelPositionY");
			writer.Write(Mod.setting.panelPosition.y);
			writer.TypeEnd();
        }

        
        private void StorageCompanyPanelMoved(float2 value)
        {
            float2 currentPosition = Mod.setting.panelPosition;
            currentPosition.x = value.x;
            currentPosition.y = value.y;
            Mod.setting.panelPosition = currentPosition;
            UpdateStoragePanelPosition();
        }
        public void UpdateStoragePanelPosition()
        {
            _bindingStorageCompanyUISettings.Update();
        }
        private StorageCompanyUI ConvertForUI(StorageCompanyInfo storageCompanyInfo)
        {
            var forUI = new StorageCompanyUI()
            {
                EntityId = storageCompanyInfo.EntityId,
                Brand = nameSystem.GetRenderedLabelName(storageCompanyInfo.Brand),
                TransferRequests = storageCompanyInfo.TransferRequests,
                Trips = storageCompanyInfo.Trips,
                OwnedVehicles = storageCompanyInfo.OwnedVehicles,
                GuestVehicles = storageCompanyInfo.GuestVehicles
            };
            return forUI;
        }
        private void SetStorageVisibility(bool value)
        {
            
            var job = new StorageCompanyJob
                {
                    entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                    companyDataType = SystemAPI.GetComponentTypeHandle<CompanyData>(true),
                    transferRequestHandle = SystemAPI.GetBufferLookup<StorageTransferRequest>(true),
                    tripNeededHandle = SystemAPI.GetBufferLookup<TripNeeded>(true),
                    ownedVehicleHandle = SystemAPI.GetBufferLookup<OwnedVehicle>(true),
                    guestVehicleHandle = SystemAPI.GetBufferLookup<GuestVehicle>(true),
                    storageCompanyInfo = m_JobResults
                };
                var jobHandle = job.Schedule(storageCompanyQuery, Dependency);
                Dependency = jobHandle;
                jobHandle.Complete();
                var uiList = new List<StorageCompanyUI>(m_JobResults.Length);
                foreach (var info in m_JobResults)
                {
                    uiList.Add(ConvertForUI(info));
                }
                m_StorageCompanyInfoBinding.Value = uiList;
        }
    }
}

