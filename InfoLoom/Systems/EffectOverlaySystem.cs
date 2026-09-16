using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Game.Buildings;
using Game.Prefabs;
using Game.Rendering;
using ModsCommon.Extensions;
using ModsCommon.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Mod = InfoLoomTwo.InfoLoomMod;

namespace InfoLoomTwo.Systems
{
	// Owns effect-overlay state (which entity/effect pairs are shown, and their colors) for the whole
	// mod. Lives outside ILEffectsSection deliberately: that section is scoped to the selected entity,
	// so overlays toggled from the city-wide Effects panel would never render once selection changed.
	public partial class EffectOverlaySystem : CommonUISystemBase
	{
		protected override string ModId => InfoLoomMod.Instance.Id;

		public struct EffectColorInfo
		{
			public string Type { get; set; }
			public int R { get; set; }
			public int G { get; set; }
			public int B { get; set; }
			public int A { get; set; }
		}

		private OverlayRenderSystem m_OverlayRenderSystem;
		private readonly HashSet<string> m_OverlayEffects = new();

		protected override void OnCreate()
		{
			base.OnCreate();
			m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();

			CreateBinding("EffectColors", GetEffectColorList);
			CreateBinding("OverlayEffects", () => m_OverlayEffects.ToArray());
			CreateTrigger<string>("ToggleOverlay", ToggleEffectOverlay);
			CreateTrigger<string>("ChangeEffectColor", ChangeEffectColor);
		}

		/// <summary>Key format is "{entity.Index}:{effectType}", matching what both UI panels send.</summary>
		private void ToggleEffectOverlay(string key)
		{
			if (!m_OverlayEffects.Remove(key))
				m_OverlayEffects.Add(key);
		}

		protected override void OnUpdate()
		{
			if (m_OverlayEffects.Count > 0)
				DrawEffectOverlays();

			base.OnUpdate();
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
				if (!EntityManager.TryGetBuffer(prefabRef.m_Prefab, true, out DynamicBuffer<LocalModifierData> buffer))
					continue;

				for (int i = 0; i < buffer.Length; i++)
				{
					var data = buffer[i];
					var effectType = EnumNameCache<LocalModifierType>.ToStringCached(data.m_Type);
					if (!m_OverlayEffects.Contains($"{entity.Index}:{effectType}"))
						continue;

					float radius = data.m_Radius.max;
					if (radius < 1f)
						continue;

					overlayBuffer.DrawCircle(
						GetEffectTypeColor(effectType),
						UnityEngine.Color.clear,
						3f,
						OverlayRenderSystem.StyleFlags.Projected,
						new float2(0f, 1f),
						transform.m_Position,
						radius * 2f
					);
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
	}

	/// <summary>Enum.ToString() is reflection-based; cache it per distinct value.</summary>
	internal static class EnumNameCache<T> where T : struct, Enum
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
}
