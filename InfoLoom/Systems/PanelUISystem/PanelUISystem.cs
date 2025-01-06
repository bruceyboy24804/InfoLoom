using Game;
using Game.Routes;
using InfoLoomTwo.Extensions;
using InfoLoomTwo.Domain;
using Unity.Entities.UniversalDelegates;

namespace InfoLoomTwo.Systems
{
    public partial class PanelUISystem : ExtendedUISystemBase
    {
        private ValueBindingHelper<Domain.PanelState[]> m_PanelStates { get; set; }

        // 240209 Set gameMode to avoid errors in the Editor
        public override GameMode gameMode => GameMode.Game;

        //[Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();

            // Define bindings between UI and C#
            m_PanelStates = CreateBinding("PanelStates", Mod.setting.PanelStates);


            // Define Trigger Bindings
            CreateTrigger<string, Domain.Position, Domain.Size>("SavePanelState", TrySavePanelState);


            // allocate storage

            Mod.log.Info("BuildingDemandUISystem created.");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        private void TrySavePanelState(string id, Domain.Position pos, Domain.Size size)
        {
            Mod.log.Debug($"{nameof(TrySavePanelState)}.Start");
            Mod.log.Debug($"{nameof(TrySavePanelState)} {id} {pos.left} {pos.top} {size.width} {size.height}");
            PanelState[] panelStates = Mod.setting.PanelStates;
            PanelState[] newPanelStates = new PanelState[panelStates.Length + 1];

            // Check if panel id is already saved, if so override values.
            for (int i = 0; i < panelStates.Length; i++)
            {
                if (panelStates[i].Id == id)
                {
                    panelStates[i].Position = pos;
                    panelStates[i].Size = size;
                    Mod.setting.PanelStates[i] = panelStates[i];
                    Mod.setting.ApplyAndSave();
                    m_PanelStates.Value = panelStates;
                    m_PanelStates.Binding.TriggerUpdate();
                    return;
                }
                else
                {
                    newPanelStates[i] = panelStates[i];
                }
            }

            newPanelStates[panelStates.Length] = new PanelState(id, pos, size);
            Mod.setting.PanelStates = newPanelStates;
            Mod.setting.ApplyAndSave();
            m_PanelStates.Value = newPanelStates;
            m_PanelStates.Binding.TriggerUpdate();
            Mod.log.Debug($"{nameof(TrySavePanelState)}.Complete");
        }
    }
}


