using System.Collections.Generic;
using System.Linq;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.UI;
using InfoLoomTwo.Extensions;
using Unity.Entities;
using Unity.Mathematics;

namespace InfoLoomTwo.Systems.Sections
{
	public partial class ILEffectsSection : ExtendedInfoSectionBase
	{
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

		protected override string group => nameof(ILEffectsSection);

		private OverlayRenderSystem m_OverlayRenderSystem;
		private readonly HashSet<string> m_OverlayEffects = new();
		private List<LocalInfo> _LocalModifiers = new();
		private List<CityInfo> _CityModifiers = new();

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InfoUISystem.AddMiddleSection(this);
			m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();

			CreateBinding("EffectColors", () => GetEffectColorList());
			CreateBinding("OverlayEffects", () => m_OverlayEffects.ToArray());
			CreateTrigger<string>("ToggleOverlay", ToggleEffectOverlay);
			CreateTrigger<string>("ChangeEffectColor", ChangeEffectColor);
		}

		protected override void Reset()
		{
			_LocalModifiers.Clear();
			_CityModifiers.Clear();
		}

		private bool Visible()
		{
			return EntityManager.HasComponent<LocalEffectProvider>(selectedEntity)
				|| EntityManager.HasComponent<CityEffectProvider>(selectedEntity);
		}

		protected override void OnUpdate()
		{
			visible = Visible();

			if (visible && m_OverlayEffects.Count > 0)
			{
				DrawEffectOverlays();
			}

			base.OnUpdate();
		}

		protected override void OnProcess()
		{
			_LocalModifiers.Clear();
			_CityModifiers.Clear();

			if (selectedPrefab == Entity.Null)
				return;

			if (EntityManager.TryGetBuffer(selectedPrefab, true, out DynamicBuffer<LocalModifierData> localBuf))
			{
				for (int i = 0; i < localBuf.Length; i++)
				{
					var data = localBuf[i];
					_LocalModifiers.Add(new LocalInfo
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

			if (EntityManager.TryGetBuffer(selectedPrefab, true, out DynamicBuffer<CityModifierData> cityBuf))
			{
				for (int i = 0; i < cityBuf.Length; i++)
				{
					var data = cityBuf[i];
					_CityModifiers.Add(new CityInfo
					{
						Type = data.m_Type.ToString(),
						Mode = data.m_Mode.ToString(),
						DeltaMin = data.m_Range.min,
						DeltaMax = data.m_Range.max
					});
				}
			}
		}

		public override void OnWriteProperties(IJsonWriter writer)
		{
			writer.PropertyName("entityIndex");
			writer.Write(selectedEntity.Index);

			writer.PropertyName("localModifiers");
			writer.ArrayBegin(_LocalModifiers.Count);
			foreach (var m in _LocalModifiers)
			{
				writer.TypeBegin("LocalInfo");
				writer.PropertyName("type");
				writer.Write(m.Type);
				writer.PropertyName("mode");
				writer.Write(m.Mode);
				writer.PropertyName("radiusCombineMode");
				writer.Write(m.RadiusCombineMode);
				writer.PropertyName("deltaMin");
				writer.Write(m.DeltaMin);
				writer.PropertyName("deltaMax");
				writer.Write(m.DeltaMax);
				writer.PropertyName("radiusMin");
				writer.Write(m.RadiusMin);
				writer.PropertyName("radiusMax");
				writer.Write(m.RadiusMax);
				writer.TypeEnd();
			}
			writer.ArrayEnd();

			writer.PropertyName("cityModifiers");
			writer.ArrayBegin(_CityModifiers.Count);
			foreach (var m in _CityModifiers)
			{
				writer.TypeBegin("CityInfo");
				writer.PropertyName("type");
				writer.Write(m.Type);
				writer.PropertyName("mode");
				writer.Write(m.Mode);
				writer.PropertyName("deltaMin");
				writer.Write(m.DeltaMin);
				writer.PropertyName("deltaMax");
				writer.Write(m.DeltaMax);
				writer.TypeEnd();
			}
			writer.ArrayEnd();
		}

		private void ToggleEffectOverlay(string key)
		{
			if (!m_OverlayEffects.Remove(key))
				m_OverlayEffects.Add(key);
		}

		private void DrawEffectOverlays()
		{
			if (!EntityManager.TryGetComponent(selectedEntity, out Game.Objects.Transform transform))
				return;

			var overlayBuffer = m_OverlayRenderSystem.GetBuffer(out var dependencies);
			dependencies.Complete();

			if (EntityManager.TryGetBuffer(selectedPrefab, true, out DynamicBuffer<LocalModifierData> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					var data = buffer[i];
					var effectType = data.m_Type.ToString();
					var key = $"{selectedEntity.Index}:{effectType}";
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
}