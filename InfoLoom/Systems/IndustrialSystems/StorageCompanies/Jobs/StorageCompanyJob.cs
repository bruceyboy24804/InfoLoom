using Game.Citizens;
using Game.Companies;
using Game.Vehicles;
using InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Data;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Jobs
{
    public struct StorageCompanyJob : IJobChunk
    {
        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        [ReadOnly] public ComponentTypeHandle<CompanyData> companyDataType;

        [ReadOnly] public BufferLookup<StorageTransferRequest> transferRequestHandle;
        [ReadOnly] public BufferLookup<TripNeeded> tripNeededHandle;
        [ReadOnly] public BufferLookup<OwnedVehicle> ownedVehicleHandle;
        [ReadOnly] public BufferLookup<GuestVehicle> guestVehicleHandle;
        
        public NativeList<StorageCompanyInfo> storageCompanyInfo;
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(entityTypeHandle);
            var companyDataArray = chunk.GetNativeArray(ref companyDataType);
           
            // Check if chunk has CompanyData component
            if (!chunk.Has(ref companyDataType))
            {
                return;
            }
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entities[i];
                var companyData = companyDataArray[i];
                int transferRequests = 0;
                int trip = 0;
                int ownedVehicles = 0;
                int guestVehicles = 0;
                if (transferRequestHandle.TryGetBuffer(entity, out var requests))
                {
                    transferRequests = requests.Length;
                }
                if (tripNeededHandle.TryGetBuffer(entity, out var trips))
                {
                    trip = trips.Length;
                }
                if (ownedVehicleHandle.TryGetBuffer(entity, out var owned))
                {
                    ownedVehicles = owned.Length;
                }
                if (guestVehicleHandle.TryGetBuffer(entity, out var guest))
                {
                    guestVehicles = guest.Length;
                }
                storageCompanyInfo.Add(new StorageCompanyInfo
                {
                    EntityId = entity,
                    Brand = companyData.m_Brand,
                    TransferRequests = transferRequests,
                    Trips = trip,
                    OwnedVehicles = ownedVehicles,
                    GuestVehicles = guestVehicles
                });

            }
        }
    }
}