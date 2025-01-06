using Game.Buildings;
using Game.Prefabs;
using Game;
using Unity.Collections;
using Unity.Entities;
using Game.UI;
using Game.City;
using System.Collections.Generic;
using Game.UI.InGame;

namespace InfoLoomTwo.Systems
{
    [UpdateAfter(typeof(PrefabSystem))]
    public partial class ModifierDataUISystem : SystemBase
    {
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_SignatureQuery;
        private Dictionary<CityModifierType, float> m_GlobalEffects = new();

        protected override void OnCreate()
        {
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_SignatureQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<Signature, PrefabRef>()
                .Build(this);
        }

        public float GetModifierValue(CityModifierType type) =>
            m_GlobalEffects.TryGetValue(type, out var value) ? value : 0f;

        protected override void OnUpdate()
        {
            m_GlobalEffects.Clear();
            using var entities = m_SignatureQuery.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var prefabRef = EntityManager.GetComponentData<PrefabRef>(entity);
                if (EntityManager.HasBuffer<CityModifierData>(prefabRef.m_Prefab))
                {
                    foreach (var mod in EntityManager.GetBuffer<CityModifierData>(prefabRef.m_Prefab))
                    {
                        if (!m_GlobalEffects.TryGetValue(mod.m_Type, out float current))
                            current = 0f;
                        m_GlobalEffects[mod.m_Type] = current + mod.m_Range.max;
                    }
                }
            }
        }
    }
}
