using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.City;
using Game.Prefabs;
using Game.UI;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Unity.Collections;
using Unity.Entities;

namespace InfoLoomTwo.Systems
{
	// City-wide "all buildings" effects list, shown in the Effects menu panel.
	// Distinct from ILEffectsSection, which shows effects for the currently selected entity only.
	public partial class AllEffectsSystem : CommonUISystemBase
	{
		protected override string ModId => InfoLoomMod.Instance.Id;

		public struct EntityModifierInfo
		{
			public int EntityIndex { get; set; }
			public string Name { get; set; }
			public List<LocalInfo> Modifiers { get; set; }
			public List<CityInfo> CityModifiers { get; set; }
		}

		public struct LocalInfo
		{
			public string Type { get; set; }
			public string Mode { get; set; }
			public string RadiusCombineMode { get; set; }
			public float DeltaMin { get; set; }
			public float DeltaMax { get; set; }
			public float RadiusMin { get; set; }
			public float RadiusMax { get; set; }
		}

		public struct CityInfo
		{
			public string Type { get; set; }
			public string Mode { get; set; }
			public float DeltaMin { get; set; }
			public float DeltaMax { get; set; }
		}

		// Enum.ToString() is reflection-based and surprisingly expensive; cache it per distinct value.
		private static class EnumCache<T> where T : struct, Enum
		{
			private static readonly Dictionary<T, string> Cache = new();

			public static string ToStringCached(T value)
			{
				if (!Cache.TryGetValue(value, out var s))
				{
					s = value.ToString();
					Cache[value] = s;
				}
				return s;
			}
		}

		public bool IsPanelVisible { get; set; }

		private ValueBindingHelper<List<EntityModifierInfo>> m_EffectsBinding;
		private NameSystem _NameSystem;

		protected override void OnCreate()
		{
			base.OnCreate();
			_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
			m_EffectsBinding = CreateBinding("Effects", new List<EntityModifierInfo>());
		}

		public override int GetUpdateInterval(SystemUpdatePhase phase) => 32;

		protected override void OnUpdate()
		{
			if (!IsPanelVisible)
			{
				base.OnUpdate();
				return;
			}

			RefreshData();
			base.OnUpdate();
		}

		// Public so InfoLoomUISystem can force an immediate refresh the moment the panel is opened,
		// rather than waiting up to GetUpdateInterval frames for the next scheduled OnUpdate.
		public void RefreshData()
		{
			var providerEntities = SystemAPI.QueryBuilder()
				.WithAny<LocalEffectProvider, CityEffectProvider>()
				.Build();
			var array = providerEntities.ToEntityArray(Allocator.Temp);

			var effects = new List<EntityModifierInfo>();

			foreach (var entity in array)
			{
				var info = TryConvertToInfo(entity);
				if (info.HasValue)
					effects.Add(info.Value);
			}

			array.Dispose();

			// The UI derives its Local/City/Both counts from this list, so entities with a
			// provider component but no actual modifiers are intentionally not represented.
			m_EffectsBinding.Value = effects;
		}

		private EntityModifierInfo? TryConvertToInfo(Entity entity)
		{
			if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
				return null;

			var prefab = prefabRef.m_Prefab;
			List<LocalInfo> modifiers = null;
			List<CityInfo> cityModifiers = null;

			if (EntityManager.TryGetBuffer(prefab, true, out DynamicBuffer<LocalModifierData> buffer) && buffer.Length > 0)
			{
				modifiers = new List<LocalInfo>(buffer.Length);
				for (int i = 0; i < buffer.Length; i++)
				{
					var data = buffer[i];
					modifiers.Add(new LocalInfo
					{
						Type = EnumCache<LocalModifierType>.ToStringCached(data.m_Type),
						Mode = EnumCache<ModifierValueMode>.ToStringCached(data.m_Mode),
						RadiusCombineMode = EnumCache<ModifierRadiusCombineMode>.ToStringCached(data.m_RadiusCombineMode),
						DeltaMin = data.m_Delta.min,
						DeltaMax = data.m_Delta.max,
						RadiusMin = data.m_Radius.min,
						RadiusMax = data.m_Radius.max
					});
				}
			}

			if (EntityManager.TryGetBuffer(prefab, true, out DynamicBuffer<CityModifierData> cityBuffer) && cityBuffer.Length > 0)
			{
				cityModifiers = new List<CityInfo>(cityBuffer.Length);
				for (int i = 0; i < cityBuffer.Length; i++)
				{
					var data = cityBuffer[i];
					cityModifiers.Add(new CityInfo
					{
						Type = EnumCache<CityModifierType>.ToStringCached(data.m_Type),
						Mode = EnumCache<ModifierValueMode>.ToStringCached(data.m_Mode),
						DeltaMin = data.m_Range.min,
						DeltaMax = data.m_Range.max
					});
				}
			}

			if (modifiers == null && cityModifiers == null)
				return null;

			return new EntityModifierInfo
			{
				EntityIndex = entity.Index,
				Name = _NameSystem.GetRenderedLabelName(entity),
				Modifiers = modifiers ?? new List<LocalInfo>(),
				CityModifiers = cityModifiers ?? new List<CityInfo>()
			};
		}
	}
}
