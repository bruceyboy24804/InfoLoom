namespace InfoLoomTwo.Domain
{
    public struct WorkforcesInfo
        {
            public int Level { get; set; }
            public int Total { get; set; } 
            public int Worker { get; set; } 
            public int Unemployed { get; set; } 
            public int Homeless { get; set; } 
            public int Employable { get; set; } 
            public int Outside { get; set; } 
            public int Under; 
            public WorkforcesInfo(int _level)
            {
                Level = _level;
                Total = 0;
                Worker = 0;
                Unemployed = 0;
                Homeless = 0;
                Employable = 0;
                Outside = 0;
                Under = 0;
            }
        }
}