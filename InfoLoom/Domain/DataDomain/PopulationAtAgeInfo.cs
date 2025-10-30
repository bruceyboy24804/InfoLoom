using Colossal.UI.Binding;
using Unity.Collections;
using Unity.Mathematics;

namespace InfoLoomTwo.Domain.DataDomain
{
    public struct PopulationAtAgeInfo
    {
        public int Age { get; set; }
        public int Total { get; set; }
        public int Work { get; set; }
        public int School1 { get; set; }
        public int School2 { get; set; }
        public int School3 { get; set; }
        public int School4 { get; set; }
        public int Unemployed { get; set; }
        public int Retired { get; set; }
        public int ChildCount { get; set; }
        public int TeenCount { get; set; }
        public int AdultCount { get; set; }
        public int ElderlyCount { get; set; }
        
        public int Uneducated { get; set; }
        public int PoorlyEducated { get; set; }
        public int Educated { get; set; }
        public int WellEducated { get; set; }
        public int HighlyEducated { get; set; }
        
        public int ChildOrTeenWithNoSchool { get; set; }
        
        public PopulationAtAgeInfo(int age)
        {
            Age = age;
            Total = 0;
            Work = 0;
            School1 = 0;
            School2 = 0;
            School3 = 0;
            School4 = 0;
            Unemployed = 0;
            Retired = 0;
            ChildCount = 0;
            TeenCount = 0;
            AdultCount = 0;
            ElderlyCount = 0;
            Uneducated = 0;
            PoorlyEducated = 0;
            Educated = 0;
            WellEducated = 0;
            HighlyEducated = 0;
            ChildOrTeenWithNoSchool = 0;
            
        }
        
    }
}