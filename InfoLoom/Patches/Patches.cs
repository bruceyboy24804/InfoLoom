using Colossal.UI.Binding;
using Game.Economy;
using Game.Prefabs;
using Game.UI.InGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace InfoLoomBrucey.Patches
{
    [HarmonyPatch]
    static class GamePatches
    {
        /* not used
        [HarmonyPatch(typeof(Game.Common.SystemOrder), "Initialize")]
        [HarmonyPostfix]
        public static void Initialize_Postfix(UpdateSystem updateSystem)
        {
            updateSystem.UpdateAt<InfoLoom.BuildingDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.WorkplacesInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.WorkforceInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.CommercialDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.ResidentialDemandUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<InfoLoom.IndustrialDemandUISystem>(SystemUpdatePhase.UIUpdate);
        }
        */

        [HarmonyPatch(typeof(CityInfoUISystem), "WriteDemandFactors")]
        [HarmonyPrefix]
        public static bool WriteDemandFactors_Prefix(CityInfoUISystem __instance, IJsonWriter writer, NativeArray<int> factors, JobHandle deps)
        {
            deps.Complete();
            NativeList<FactorInfo> list = FactorInfo.FromFactorArray(factors, Allocator.Temp);
            list.Sort();
            try
            {
                //int num = math.min(5, list.Length);
                int num = list.Length;
                writer.ArrayBegin(num);
                for (int i = 0; i < num; i++)
                {
                    list[i].WriteDemandFactor(writer);
                }
                writer.ArrayEnd();
            }
            finally
            {
                list.Dispose();
            }
            return false; // don't execute the original
        }

       

    }
}
