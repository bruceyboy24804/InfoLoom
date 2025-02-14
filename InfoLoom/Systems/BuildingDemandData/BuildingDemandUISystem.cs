using Colossal.UI.Binding;
using Game;
using Game.Simulation;
using Game.UI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using InfoLoomTwo.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace InfoLoomTwo.Systems
{
    public partial class BuildingDemandUISystem : ExtendedUISystemBase
{
    // systems to get the data from
    private SimulationSystem m_SimulationSystem;
    private ResidentialDemandSystem m_ResidentialDemandSystem;
    private CommercialDemandSystem m_CommercialDemandSystem;
    private IndustrialDemandSystem m_IndustrialDemandSystem;

    // ui bindings
    private ValueBindingHelper<int[]> m_uiBuildingDemand;
    

    // building demands
        
    // 0 - low res (ResidentialDemandSystem.BuildingDemand.x)
    // 1 - med res (ResidentialDemandSystem.BuildingDemand.y)
    // 2 - high res (ResidentialDemandSystem.BuildingDemand.z)
    // 3 - commercial (CommercialDemandSystem.m_BuildingDemand)
    // 4 - industry (IndustrialDemandSystem.m_IndustrialBuildingDemand)
    // 5 - storage (IndustrialDemandSystem.m_StorageBuildingDemand)
    // 6 - office (IndustrialDemandSystem.m_OfficeBuildingDemand)

    // company demands
    /*
    private NativeArray<int> m_CompanyDemand;
    // 0 - low res
    // 1 - med res
    // 2 - high res
    // 3 - commercial (CommercialDemandSystem.m_BuildingDemand)
    // 4 - industry (IndustrialDemandSystem.m_IndustrialCompanyDemand)
    // 5 - storage (IndustrialDemandSystem.m_StorageCompanyDemand)
    // 6 - office (IndustrialDemandSystem.m_OfficeCompanyDemand)
    */

    // 240209 Set gameMode to avoid errors in the Editor
    public override GameMode gameMode => GameMode.Game;

    //[Preserve]
    protected override void OnCreate()
    {
        base.OnCreate();
        m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
        m_ResidentialDemandSystem = base.World.GetOrCreateSystemManaged<ResidentialDemandSystem>();
        m_CommercialDemandSystem = base.World.GetOrCreateSystemManaged<CommercialDemandSystem>();
        m_IndustrialDemandSystem = base.World.GetOrCreateSystemManaged<IndustrialDemandSystem>();
        m_uiBuildingDemand = CreateBinding("ilBuildingDemand", new int[0]);
        

        // allocate storage
        
        Mod.log.Info("InfoLoomUISystem created.");
    }

    protected override void OnUpdate()
    {
        
			

        
        base.OnUpdate();
        
        m_uiBuildingDemand.Value = new int[]
        {
          m_ResidentialDemandSystem.buildingDemand.x,
          m_ResidentialDemandSystem.buildingDemand.y,
          m_ResidentialDemandSystem.buildingDemand.z,
          m_CommercialDemandSystem.buildingDemand,
          m_IndustrialDemandSystem.industrialBuildingDemand,
          m_IndustrialDemandSystem.storageBuildingDemand,
          m_IndustrialDemandSystem.officeBuildingDemand
          
        };
        
        
        

        

        
    }

    


}
}


