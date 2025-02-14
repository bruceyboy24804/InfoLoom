using System;
using UnityEngine;

namespace InfoLoomTwo.Domain
{
    public struct TradeCostResource
        {
            public string Resource;
            public int Amount;
            
            public TradeCostResource(string Resource, int Amount)
            {
                this.Resource = Resource;
                this.Amount = Amount;
                
            }

            
        }
}