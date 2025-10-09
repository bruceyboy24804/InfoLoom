using Game.Economy;

namespace InfoLoomTwo.Domain.DataDomain
{
    public struct IndustrialProductsData
    {
        public string ResourceName { get; set; }
        public int Demand { get; set; }
        public int Building { get; set; }
        public int Free { get; set; }
        public int Companies { get; set; }
        public int Workers { get; set; }
        public int WrkPercent { get; set; }
        public int TaxFactor { get; set; }
    }
}