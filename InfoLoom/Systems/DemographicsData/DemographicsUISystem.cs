
using System;
using Colossal.UI.Binding;
using InfoLoomTwo.Extensions;

namespace InfoLoomTwo.Systems.DemographicsData
{
    public partial class DemographicsUISystem : ExtendedUISystemBase
    {
        public struct PopulationAtAgeInfo
        {
            public int Age;
            public int Total; // asserion: Total is a sum of the below parts
            public int School1; // elementary school
            public int School2; // high school
            public int School3; // college
            public int School4; // university
            public int Work; // working
            public int Other; // not working, not student
            public int ChildCount;
            public int AdultCount;
            public int TeenCount;
            public int ElderlyCount;


            public PopulationAtAgeInfo(int _age)
            {
                Age = _age;
                Total = 0;
                Work = 0;
                School1 = 0;
                School2 = 0;
                School3 = 0;
                School4 = 0;
                Other = 0;
                ChildCount = 0;
                TeenCount = 0;
                AdultCount = 0;
                ElderlyCount = 0;
            }
        }
        private GetterValueBinding<int> m_OldCitizenBinding;
        private ValueBindingHelper<PopulationAtAgeInfo[]>m_PopulationAtAgeInfoBinding;
        private ValueBindingHelper<int[]> m_TotalsBinding;
        

        protected override void OnCreate()
        {
            base.OnCreate();
            Setting setting = Mod.setting;    
            var demographics = World.GetExistingSystemManaged<Demographics>();
            m_PopulationAtAgeInfoBinding = CreateBinding("StructureDetails", new PopulationAtAgeInfo[0]);
            m_TotalsBinding = CreateBinding("StructureTotals", new int[10]);
            
            m_OldCitizenBinding = CreateBinding("OldestCitizen", () => 
            {
                var demographics = World.GetExistingSystemManaged<Demographics>();
                return demographics.m_Totals[6];
            });
            Mod.log.Info("DemographicsUISystem created.");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            m_PopulationAtAgeInfoBinding.Value = new PopulationAtAgeInfo[120];
            m_TotalsBinding.Value = new int[10];
            m_OldCitizenBinding.Update();
            
            
            
        }
        
    }
}