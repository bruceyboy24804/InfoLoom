using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using InfoLoomTwo.Domain;
using InfoLoomTwo.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems.DistrictData
{
    public partial class DistrictDataUISystem : ExtendedUISystemBase
    {
        protected const string group = "ilDistrictData";
        private RawValueBinding m_uiDistricts;
        private NameSystem m_NameSystem;
        private EntityQuery m_DistrictQuery;
        private SimulationSystem m_SimulationSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.Exclude<Temp>()
            );
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            AddBinding(m_uiDistricts = new RawValueBinding(group, "ilDistricts", delegate (IJsonWriter writer)
            {
                var outputData = DistrictDataSystem.SharedDistrictOutputData;
                if (outputData == null)
                {
                    writer.ArrayBegin(0);
                    writer.ArrayEnd();
                    return;
                }

                // Filter out any entries with an invalid district entity.
                List<DistrictOutputData> validEntries = new List<DistrictOutputData>();
                foreach (var data in outputData)
                {
                    if (data.districtEntity != Entity.Null)
                        validEntries.Add(data);
                }

                writer.ArrayBegin(validEntries.Count);
                for (int i = 0; i < validEntries.Count; i++)
                {
                    DistrictOutputData data = validEntries[i];
                    Entity entity = data.districtEntity;
                    writer.TypeBegin("InfoLoomTwo.DistrictData");
                    writer.PropertyName("name");
                    m_NameSystem.BindName(writer, entity);
                    writer.PropertyName("residentCount");
                    writer.Write(data.residentCount);
                    writer.PropertyName("petCount");
                    writer.Write(data.petCount);
                    writer.PropertyName("householdCount");
                    writer.Write(data.householdCount);
                    writer.PropertyName("maxHouseholds");
                    writer.Write(data.maxHouseholds);
                    writer.PropertyName("entity");
                    writer.Write(entity);
                    writer.TypeEnd();
                }
                writer.ArrayEnd();
            }));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // No longer updating a separate _Districts list.
            // Just update the binding so that the UI receives the latest JSON data.
            m_uiDistricts.Update();
            base.OnUpdate();
        }
    }
}
