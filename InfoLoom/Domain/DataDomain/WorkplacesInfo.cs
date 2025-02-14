namespace InfoLoomTwo.Domain
{
    public struct WorkplacesInfo
    {
            public int Level { get; set; }
            public int Total { get; set; }
            public int Service { get; set; }
            public int Commercial { get; set; }
            public int Leisure { get; set; }
            public int Extractor { get; set; }
            public int Industrial { get; set; }
            public int Office { get; set; }
            public int Employee { get; set; }
            public int Open { get; set; }
            public int Commuter { get; set; }

            public string Name { get; set; }


        public WorkplacesInfo(int _level) {
            Level = _level;
            Total = 0;
            Service = 0;
            Commercial = 0;
            Leisure = 0;
            Extractor = 0;
            Industrial = 0;
            Office = 0;
            Employee = 0;
            Open = 0;
            Commuter = 0; 
            Name = "";
        }
    }
}