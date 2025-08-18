namespace InfoLoomTwo.Systems.IndustrialSystems.IndustrialCompanyData.IndustrialCompanyDomain
{
    public struct ResourceInfo
    {
        public string ResourceName;
        public int Amount;
        public string Icon;

        public ResourceInfo(string resourceName, int amount, string icon)
        {
            ResourceName = resourceName;
            Amount = amount;
            Icon = icon;
        }
    }
}