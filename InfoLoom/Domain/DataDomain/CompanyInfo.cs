using System;
using Colossal.UI.Binding;
using Unity.Entities;

namespace InfoLoomTwo.Domain.DataDomain
{
	public class CompanyInfo : IJsonWritable, IComparable<CompanyInfo>
	{
		// Brand entity and name of the company
		public Entity brandEntity { get; set; }
		public string name { get; set; }

		public CompanyInfo(Entity brandEntity, string name)
		{
			this.brandEntity = brandEntity;
			this.name = name;
		}

		/// <summary>
		/// Write company info to the UI.
		/// </summary>
		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(InfoLoomMod.Instance.ModName + ".CompanyInfo");
			writer.PropertyName("brandEntity");
			writer.Write(brandEntity);
			writer.PropertyName("name");
			writer.Write(name);
			writer.TypeEnd();
		}

		/// <summary>
		/// Compare the names of two companies.
		/// </summary>
		public int CompareTo(CompanyInfo other)
		{
			return String.Compare(this.name, other.name, StringComparison.OrdinalIgnoreCase);
		}
	}
}
