using Colossal.Collections;

namespace InfoLoomTwo.Domain
{
    public struct WorkforcesInfo : IAccumulable<WorkforcesInfo>
        {
            public int Level { get; set; }
            public int Total { get; set; }
            public int Worker { get; set; }
            public int Unemployed { get; set; }

            public float UnemploymentRate { get; set; }
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
                UnemploymentRate = 0f;
                Homeless = 0;
                Employable = 0;
                Outside = 0;
                Under = 0;
            }

            // Used by NativeAccumulator<WorkforcesInfo> to safely combine per-thread partial
            // sums from a parallel job. Only additive counters are combined here — Level is
            // fixed per bucket and UnemploymentRate is a derived value recomputed afterwards.
            public void Accumulate(WorkforcesInfo other)
            {
                Total += other.Total;
                Worker += other.Worker;
                Unemployed += other.Unemployed;
                Homeless += other.Homeless;
                Employable += other.Employable;
                Outside += other.Outside;
                Under += other.Under;
            }
        }
}