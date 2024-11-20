using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;
using Game.UI;

using Game.Areas;
using System.Text;
using Game;
using System;

namespace InfoLoomTwo.Systems
{
    [CompilerGenerated]
    public partial class DistrictInfoLoomUISystem : ExtendedUISystemBase
    {
        

        public override GameMode gameMode => GameMode.Game;

        private EntityQuery disquery;

        private NativeArray<Entity> disArray;

        private Array IndexArray = new int[2] { 1, 1 };

        private string Indexes = "";

        

        private ValueBindingHelper<string> DistrictListBinding;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            disquery = GetEntityQuery(ComponentType.ReadOnly<District>(), ComponentType.Exclude<Temp>());

            DistrictListBinding = CreateBinding("Districts", Indexes);

           
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            disArray = disquery.ToEntityArray(Allocator.Temp);

            if (IndexArray.Length != disArray.Length)
            {
                GetIndexes();
                DistrictListBinding.Value = Indexes;
            }
        }
        protected override void OnDestroy()
        {
            disquery.Dispose();
            disArray.Dispose();
        }
        private void GetIndexes()
        {
            Indexes = "";
            IndexArray = new int[disArray.Length];
            int i = 0;
            foreach (var entity in disArray)
            {
                Indexes += entity.Index.ToString() + ",";
                IndexArray.SetValue(entity.Index, i);
                //DataSecretary.Mod.log.Info($"DistrictItem {i} = {IndexArray.GetValue(i)}.");
                i++;
            }
        }
        
    }
}
