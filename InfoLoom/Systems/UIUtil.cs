using System;
using System.Collections.Generic;
using Game.Economy;

namespace InfoLoomTwo.Systems
{
    public static class UIUtil
    {
        /// <summary>
        /// Extracts excluded resource names from a Resource flags enum.
        /// </summary>
        /// <param name="excludedResources">The resource flags to extract</param>
        /// <returns>Array of resource names that are included in the flags</returns>
        public static string[] ExtractExcludedResources(Resource excludedResources)
        {
            List<string> excludedResourceNames = new List<string>();
            if (excludedResources == Resource.All)
            {
                return new string[] { Resource.All.ToString() };
            }
            Resource[] resources = (Resource[])Enum.GetValues(typeof(Resource));
            for (int i = 0; i < resources.Length; i++)
            {
                Resource resource = resources[i];
                if ((excludedResources & resource) != 0 &&
                    resource != Resource.NoResource &&
                    resource != Resource.All)
                {
                    excludedResourceNames.Add(resource.ToString());
                }
            }

            return excludedResourceNames.ToArray();
        }

        /// <summary>
        /// Extracts excluded industrial resource names from a Resource flags enum.
        /// </summary>
        /// <param name="excludedResources">The resource flags to extract</param>
        /// <returns>Array of resource names that are included in the flags</returns>
        public static string[] IndustrialExtractExcludedResources(Resource excludedResources)
        {
            List<string> excludedResourceNames = new List<string>();
            if (excludedResources == Resource.All)
            {
                return new string[] { Resource.All.ToString() };
            }
            Resource[] resources = (Resource[])Enum.GetValues(typeof(Resource));
            for (int i = 0; resources.Length > i; i++)
            {
                Resource resource = resources[i];
                if ((excludedResources & resource) != 0 &&
                    resource != Resource.NoResource &&
                    resource != Resource.All)
                {
                    excludedResourceNames.Add(resource.ToString());
                }
            }

            return excludedResourceNames.ToArray();
        }
    }
}