using System.Collections.Generic;
using Colossal.UI.Binding;

namespace InfoLoomTwo.Domain.DataDomain
{
    public class DistrictInfos : List<DistrictInfo>, IJsonWritable
    {
        
        /// <summary>
        /// Write district infos to the UI.
        /// </summary>
        public void Write(IJsonWriter writer)
        {
			writer.ArrayBegin(this.Count);
			foreach (DistrictInfo districtInfo in this)
			{
				districtInfo.Write(writer);
			}
			writer.ArrayEnd();
        }
    }
    
}