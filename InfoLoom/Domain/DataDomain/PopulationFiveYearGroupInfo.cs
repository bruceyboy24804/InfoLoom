using Colossal.UI.Binding;

namespace InfoLoomTwo.Domain.DataDomain
{
    public struct PopulationFiveYearGroupInfo : IJsonWritable
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
         public int Uneducated { get; set; }
         public int PoorlyEducated { get; set; }
         public int Educated { get; set; }
         public int WellEducated { get; set; }
         public int HighlyEducated { get; set; }
         public int ChildOrTeenWithNoSchool { get; set; }
         
         public PopulationFiveYearGroupInfo(int age)
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
             Uneducated = 0;
             PoorlyEducated = 0;
             Educated = 0;
             WellEducated = 0;
             HighlyEducated = 0;
             ChildOrTeenWithNoSchool = 0;
         }
         
         public void Write(IJsonWriter writer)
         {
             writer.TypeBegin(typeof(PopulationFiveYearGroupInfo).FullName);
             writer.PropertyName("Age"); writer.Write(Age);
             writer.PropertyName("Total"); writer.Write(Total);
             writer.PropertyName("Work"); writer.Write(Work);
             writer.PropertyName("School1"); writer.Write(School1);
             writer.PropertyName("School2"); writer.Write(School2);
             writer.PropertyName("School3"); writer.Write(School3);
             writer.PropertyName("School4"); writer.Write(School4);
             writer.PropertyName("Unemployed"); writer.Write(Unemployed);
             writer.PropertyName("Retired"); writer.Write(Retired);
             writer.PropertyName("Uneducated"); writer.Write(Uneducated);
             writer.PropertyName("PoorlyEducated"); writer.Write(PoorlyEducated);
             writer.PropertyName("Educated"); writer.Write(Educated);
             writer.PropertyName("WellEducated"); writer.Write(WellEducated);
             writer.PropertyName("HighlyEducated"); writer.Write(HighlyEducated);
             writer.PropertyName("ChildOrTeenWithNoSchool"); writer.Write(ChildOrTeenWithNoSchool);
             writer.TypeEnd();
         }
    }
}