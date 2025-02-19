using System;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.UI;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems.DistrictData
{
    

    public partial class DistrictDataSystem : SystemBase
    {
        private EntityQuery m_DistrictQuery;
        private NameSystem m_NameSystem;
        public NativeList<Entity> districts {get; set;}

        protected override void OnCreate()
        {
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            districts = new NativeList<Entity>(Allocator.Persistent);
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>()
            );
            RequireForUpdate<District>();
        }

        protected override void OnDestroy()
        {
            if (districts.IsCreated)
            {
                districts.Dispose();
            }
        }

        protected override void OnUpdate()
        {
            districts.Clear();
            var districtsArray = m_DistrictQuery.ToEntityArray(Allocator.Temp);

            try
            {
                for (int i = 0; i < districtsArray.Length; i++)
                {
                    districts.Add(districtsArray[i]);
                }
            }
            finally
            {
                if (districtsArray.IsCreated)
                {
                    districtsArray.Dispose();
                }
            }
        }
        public void WriteDistricts(IJsonWriter writer)
        {
            writer.ArrayBegin(districts.Length);
            for (int i = 0; i < districts.Length; i++)
            {
                Entity entity = districts[i];
                writer.TypeBegin("District");
                writer.PropertyName("name");
                m_NameSystem.BindName(writer, entity);
                writer.PropertyName("entity");
                writer.Write(entity);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }
        public bool HasDistricts()
        {
            return !m_DistrictQuery.IsEmpty;
        }

        public NativeList<Entity> GetDistricts()
        {
            return districts;
        }
    }
}