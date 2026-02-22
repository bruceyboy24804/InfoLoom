using System;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.IndustrialSystems.StorageCompanies.Data
{
    public struct StorageCompanyInfo 
    {
        public Entity EntityId;
        public Entity Brand;
        public int TransferRequests;
        public int Trips;
        public int OwnedVehicles;
        public int GuestVehicles;
    }
    public struct StorageCompanyUI
    {
        public Entity EntityId;
        public string Brand;
        public int TransferRequests;
        public int Trips;
        public int OwnedVehicles;
        public int GuestVehicles;
    }
}
