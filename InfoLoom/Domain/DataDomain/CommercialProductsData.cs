using System;
using Game.Economy;
using Unity.Collections;

namespace InfoLoomTwo.Domain.DataDomain
{
    public struct CommercialProductsData
    {
            public string ResourceName;
            public int Demand;
            public int Building;
            public int Free;
            public int Companies;
            public int Workers;
            public int SvcFactor;
            public int SvcPercent;
            public int ResourceNeedPercent;
            public int ResourceNeedPerCompany;
            public int WrkPercent;
            public int TaxFactor;
            
            public int CurrentTourists;
            public int AvailableLodging;
            public int RequiredRooms;
        
    }
}