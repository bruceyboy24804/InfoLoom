using System.Collections.Generic;
using Colossal.UI.Binding;

namespace InfoLoomTwo.Domain.DataDomain
{
	public class CompanyInfos : List<CompanyInfo>, IJsonWritable
	{
		/// <summary>
		/// Write company infos to the UI.
		/// </summary>
		public void Write(IJsonWriter writer)
		{
			writer.ArrayBegin(this.Count);
			foreach (CompanyInfo companyInfo in this)
			{
				companyInfo.Write(writer);
			}
			writer.ArrayEnd();
		}
	}
}
