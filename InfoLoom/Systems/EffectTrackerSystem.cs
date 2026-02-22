using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.UI;
using InfoLoomTwo.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems
{
	public partial class EffectTrackerSystem : ExtendedUISystemBase
	{
		public class EntityModifierInfo
		{
			public int EntityIndex { get; set; }
			public string Name {get; set;}
			public List<LocalInfo> Modifiers { get; set; }
			public List<CityInfo> CityModifiers { get; set; }
		}

		public class LocalInfo
		{
			public string Type { get; set; }
			public string Mode { get; set; }
			public string RadiusCombineMode { get; set; }
			public float DeltaMin { get; set; }
			public float DeltaMax { get; set; }
			public float RadiusMin { get; set; }
			public float RadiusMax { get; set; }
		}
		public class CityInfo
		{
			public string Type { get; set; }
			public string Mode { get; set; }
			public float DeltaMin { get; set; }
			public float DeltaMax { get; set; }
		}

		public class EffectColorInfo
		{
			public string Type { get; set; }
			public int R { get; set; }
			public int G { get; set; }
			public int B { get; set; }
			public int A { get; set; }
		}

		private ValueBindingHelper<int[]> m_EffectCountBinding;
		private ValueBindingHelper<List<EntityModifierInfo>> m_EffectsBinding;
		private ValueBindingHelper<List<EffectColorInfo>> m_EffectColorsBinding;
		private NameSystem _NameSystem;
		private OverlayRenderSystem m_OverlayRenderSystem;
		private readonly HashSet<string> m_OverlayEffects = new();

		private static readonly string[] KnownEffectTypes = new[]
		{
			"CrimeAccumulation", "Wellbeing", "Health",
			"ForestFireHazard", "ForestFireResponseTime"
		};

		protected override void OnCreate()
		{
			base.OnCreate();
			_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
			m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
			m_EffectCountBinding = CreateBinding("EffectCount", new int[0]);
			m_EffectsBinding = CreateBinding("Effects", new List<EntityModifierInfo>());
			m_EffectColorsBinding = CreateBinding("EffectColors", new List<EffectColorInfo>());
			CreateBinding("showButton", () => Mod.setting.showEffectsButton);
			CreateBinding("OverlayEffects", () => m_OverlayEffects.ToArray());
			CreateTrigger<string>("ToggleOverlay", ToggleEffectOverlay);
			CreateTrigger<string>("ChangeEffectColor", ChangeEffectColor);
		}

		protected override void OnUpdate()
		{
			var providerEntities = SystemAPI.QueryBuilder()
				.WithAny<LocalEffectProvider, CityEffectProvider>()
				.Build();
			var array = providerEntities.ToEntityArray(Allocator.Temp);

			int localOnlyCount = 0;
			int cityOnlyCount = 0;
			int bothCount = 0;
			var effects = new List<EntityModifierInfo>();

			foreach (var entity in array)
			{
				bool hasLocal = EntityManager.HasComponent<LocalEffectProvider>(entity);
				bool hasCity = EntityManager.HasComponent<CityEffectProvider>(entity);

				switch (hasLocal, hasCity)
				{
					case (true, false):
						localOnlyCount++;
						break;
					case (false, true):
						cityOnlyCount++;
						break;
					case (true, true):
						bothCount++;
						break;
				}

				var info = ConvertToInfo(entity);
				if (info.Modifiers.Count > 0 || info.CityModifiers.Count > 0)
				{
					effects.Add(info);
				}
			}

			array.Dispose();

			m_EffectCountBinding.Value = new int[]
			{
				localOnlyCount,
				cityOnlyCount,
				bothCount
			};
			m_EffectsBinding.Value = effects;
			m_EffectColorsBinding.Value = GetEffectColorList();

			if (m_OverlayEffects.Count > 0)
			{
				DrawEffectOverlays();
			}

			base.OnUpdate();
		}

		private void ToggleEffectOverlay(string key)
		{
			if (!m_OverlayEffects.Remove(key))
				m_OverlayEffects.Add(key);
		}

		private void DrawEffectOverlays()
		{
			var overlayBuffer = m_OverlayRenderSystem.GetBuffer(out var dependencies);
			dependencies.Complete();

			var query = SystemAPI.QueryBuilder()
				.WithAll<LocalEffectProvider>()
				.Build();
			var entities = query.ToEntityArray(Allocator.Temp);

			foreach (var entity in entities)
			{
				if (!EntityManager.TryGetComponent(entity, out Game.Objects.Transform transform))
					continue;
				if (!EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
					continue;

				if (EntityManager.TryGetBuffer(prefabRef.m_Prefab, true, out DynamicBuffer<LocalModifierData> buffer))
				{
					for (int i = 0; i < buffer.Length; i++)
					{
						var data = buffer[i];
						var effectType = data.m_Type.ToString();
						var key = $"{entity.Index}:{effectType}";
						if (!m_OverlayEffects.Contains(key))
							continue;

						float radius = data.m_Radius.max;
						if (radius < 1f)
							continue;

						var outlineColor = GetEffectTypeColor(effectType);

						overlayBuffer.DrawCircle(
							outlineColor,
							UnityEngine.Color.clear,
							3f,
							OverlayRenderSystem.StyleFlags.Projected,
							new float2(0f, 1f),
							transform.m_Position,
							radius * 2f
						);
					}
				}
			}

			entities.Dispose();
		}

		private UnityEngine.Color GetEffectTypeColor(string effectType)
		{
			var s = Mod.setting;
			return effectType switch
			{
				"CrimeAccumulation" => FromRgba(s.crimeColorR, s.crimeColorG, s.crimeColorB, s.crimeColorA),
				"Wellbeing" => FromRgba(s.wellbeingColorR, s.wellbeingColorG, s.wellbeingColorB, s.wellbeingColorA),
				"Health" => FromRgba(s.healthColorR, s.healthColorG, s.healthColorB, s.healthColorA),
				"ForestFireHazard" => FromRgba(s.fireHazardColorR, s.fireHazardColorG, s.fireHazardColorB, s.fireHazardColorA),
				"ForestFireResponseTime" => FromRgba(s.fireResponseColorR, s.fireResponseColorG, s.fireResponseColorB, s.fireResponseColorA),
				_ => new UnityEngine.Color(1f, 1f, 1f, 0.9f),
			};
		}

		private static UnityEngine.Color FromRgba(int r, int g, int b, int a)
		{
			return new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a / 255f);
		}

		private List<EffectColorInfo> GetEffectColorList()
		{
			var s = Mod.setting;
			return new List<EffectColorInfo>
			{
				new() { Type = "CrimeAccumulation", R = s.crimeColorR, G = s.crimeColorG, B = s.crimeColorB, A = s.crimeColorA },
				new() { Type = "Wellbeing", R = s.wellbeingColorR, G = s.wellbeingColorG, B = s.wellbeingColorB, A = s.wellbeingColorA },
				new() { Type = "Health", R = s.healthColorR, G = s.healthColorG, B = s.healthColorB, A = s.healthColorA },
				new() { Type = "ForestFireHazard", R = s.fireHazardColorR, G = s.fireHazardColorG, B = s.fireHazardColorB, A = s.fireHazardColorA },
				new() { Type = "ForestFireResponseTime", R = s.fireResponseColorR, G = s.fireResponseColorG, B = s.fireResponseColorB, A = s.fireResponseColorA },
			};
		}

		private void ChangeEffectColor(string packed)
		{
			var parts = packed.Split(':');
			if (parts.Length != 5) return;
			var effectType = parts[0];
			if (!int.TryParse(parts[1], out int r)) return;
			if (!int.TryParse(parts[2], out int g)) return;
			if (!int.TryParse(parts[3], out int b)) return;
			if (!int.TryParse(parts[4], out int a)) return;

			var s = Mod.setting;
			switch (effectType)
			{
				case "CrimeAccumulation":
					s.crimeColorR = r; s.crimeColorG = g; s.crimeColorB = b; s.crimeColorA = a;
					break;
				case "Wellbeing":
					s.wellbeingColorR = r; s.wellbeingColorG = g; s.wellbeingColorB = b; s.wellbeingColorA = a;
					break;
				case "Health":
					s.healthColorR = r; s.healthColorG = g; s.healthColorB = b; s.healthColorA = a;
					break;
				case "ForestFireHazard":
					s.fireHazardColorR = r; s.fireHazardColorG = g; s.fireHazardColorB = b; s.fireHazardColorA = a;
					break;
				case "ForestFireResponseTime":
					s.fireResponseColorR = r; s.fireResponseColorG = g; s.fireResponseColorB = b; s.fireResponseColorA = a;
					break;
			}
		}

		private EntityModifierInfo ConvertToInfo(Entity entity)
		{
			var modifiers = new List<LocalInfo>();
			var cityModifiers = new List<CityInfo>();

			if (EntityManager.TryGetComponent(entity, out PrefabRef prefabRef))
			{
				var prefab = prefabRef.m_Prefab;

				if (EntityManager.TryGetBuffer(prefab, true, out DynamicBuffer<LocalModifierData> buffer))
				{
					for (int i = 0; i < buffer.Length; i++)
					{
						var data = buffer[i];
						modifiers.Add(new LocalInfo
						{
							Type = data.m_Type.ToString(),
							Mode = data.m_Mode.ToString(),
							RadiusCombineMode = data.m_RadiusCombineMode.ToString(),
							DeltaMin = data.m_Delta.min,
							DeltaMax = data.m_Delta.max,
							RadiusMin = data.m_Radius.min,
							RadiusMax = data.m_Radius.max
						});
					}
				}

				if (EntityManager.TryGetBuffer(prefab, true, out DynamicBuffer<CityModifierData> cityBuffer))
				{
					for (int i = 0; i < cityBuffer.Length; i++)
					{
						var data = cityBuffer[i];
						cityModifiers.Add(new CityInfo
						{
							Type = data.m_Type.ToString(),
							Mode = data.m_Mode.ToString(),
							DeltaMin = data.m_Range.min,
							DeltaMax = data.m_Range.max
						});
					}
				}
			}

			return new EntityModifierInfo
			{
				EntityIndex = entity.Index,
				Name = _NameSystem.GetRenderedLabelName(entity),
				Modifiers = modifiers,
				CityModifiers = cityModifiers
			};
		}
	}
}