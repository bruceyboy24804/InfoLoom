using Colossal.UI.Binding;
using InfoLoomTwo.Domain.DataDomain;

namespace InfoLoomTwo.JsonWriterExtensions
{
    public static class JsonWriters
    {
        public static void Write(this IJsonWriter writer, PopulationAtAgeInfo value)
        {
            writer.TypeBegin(typeof(PopulationAtAgeInfo).FullName);
            
            writer.PropertyName("Age");
            writer.Write(value.Age);
            writer.PropertyName("Total");
            writer.Write(value.Total);
            writer.PropertyName("Work");
            writer.Write(value.Work);
            writer.PropertyName("School1");
            writer.Write(value.School1);
            writer.PropertyName("School2");
            writer.Write(value.School2);
            writer.PropertyName("School3");
            writer.Write(value.School3);
            writer.PropertyName("School4");
            writer.Write(value.School4);
            writer.PropertyName("Unemployed");
            writer.Write(value.Unemployed);
            writer.PropertyName("Retired");
            writer.Write(value.Retired);
            writer.PropertyName("ChildCount");
            writer.Write(value.ChildCount);
            writer.PropertyName("TeenCount");
            writer.Write(value.TeenCount);
            writer.PropertyName("AdultCount");
            writer.Write(value.AdultCount);
            writer.PropertyName("ElderlyCount");
            writer.Write(value.ElderlyCount);
            writer.PropertyName("Uneducated");
            writer.Write(value.Uneducated);
            writer.PropertyName("PoorlyEducated");
            writer.Write(value.PoorlyEducated);
            writer.PropertyName("Educated");
            writer.Write(value.Educated);
            writer.PropertyName("WellEducated");
            writer.Write(value.WellEducated);
            writer.PropertyName("HighlyEducated");
            writer.Write(value.HighlyEducated);
            writer.PropertyName("ChildOrTeenWithNoSchool");
            writer.Write(value.ChildOrTeenWithNoSchool);
            writer.TypeEnd();
        }
        public static void Write(this IJsonWriter writer, PopulationAtAgeInfo[] array)
        {
            writer.ArrayBegin(array.Length);
            foreach (var item in array)
            {
                Write(writer, item);
            }
            writer.ArrayEnd();
        }
    }
}