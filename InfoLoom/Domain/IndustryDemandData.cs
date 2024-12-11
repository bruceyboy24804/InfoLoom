using Game.Economy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoLoomTwo.Domain
{
    public class IndustryDemandData
    {
        public Resource Resource;
        public string Name; // resource name
        public int Demand; // company demand
        public int Building; // building demand
        public int Free; // free properties
        public int Companies; // num of companies
        public int Workers; // num of workers
        public int SvcFactor; // service availability
        public int SvcPercent;
        public int CapFactor; // sales capacity
        public int CapPercent;
        public int CapPerCompany;
        public int WrkFactor; // employee ratio
        public int WrkPercent;
        public int EduFactor; // education factor
        public int TaxFactor; // tax factor
        public string Details;

        public IndustryDemandData(Resource resource) { Resource = resource; } // Name = EconomyUtils.GetNameFixed(EconomyUtils.GetResource(resource)); }
    }
}
