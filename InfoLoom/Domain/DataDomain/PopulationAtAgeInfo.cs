namespace InfoLoomTwo.Domain
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
}