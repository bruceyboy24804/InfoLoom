using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using InfoLoomBrucey.Systems;


namespace InfoLoomBrucey
{
    public class Mod : IMod
    {
        public static readonly string Id = "InfoLoom";

        public static Mod Instance
        {
            get;
            private set;
        }
        internal ILog Log { get; private set; }
        public static ILog log = LogManager.GetLogger($"{nameof(InfoLoom)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
       

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

          

            updateSystem.UpdateAt<PopulationStructureUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<WorkforceInfoLoomUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
           
        }
    }
}
