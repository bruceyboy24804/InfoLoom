using System.Collections.Generic;
using ModsCommon.Extensions;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;

namespace InfoLoomTwo.Systems.Sections
{
	public partial class ILEffectsSection : ExtendedInfoSectionBase
	{
		protected override string ModId => InfoLoomMod.Instance.Id;
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

		protected override string group => nameof(ILEffectsSection);

		private List<LocalInfo> _LocalModifiers = new();
		private List<CityInfo> _CityModifiers = new();

		protected override void OnCreate()
		{
			base.OnCreate();
			m_InfoUISystem.AddMiddleSection(this);
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

	}
}
