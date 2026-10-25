using BepInEx;
using HarmonyLib;

namespace MineMogul.ConveyorCannonTweaks
{
    [BepInPlugin(ModInfo.PluginGuid, ModInfo.PluginName, ModInfo.PluginVersion)]
    [BepInDependency(ModInfo.RebindPluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class ConveyorCannonTweaksPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private ConveyorCannonTweaksRuntimeController _runtime;

        private void Awake()
        {
            _runtime = new ConveyorCannonTweaksRuntimeController(Config, Logger);
            ConveyorCannonTweaksRuntime.Set(_runtime);

            _harmony = new Harmony(ModInfo.HARMONY_ID);
            _harmony.PatchAll(typeof(ConveyorCannonTweaksPlugin).Assembly);

            _runtime.Initialize();
            Logger.LogInfo("ConfigurableCannons loaded.");
        }

        private void Update()
        {
            if (_runtime != null)
            {
                _runtime.Tick();
            }
        }

        private void OnDestroy()
        {
            if (_runtime != null)
            {
                _runtime.Dispose();
                ConveyorCannonTweaksRuntime.Clear(_runtime);
                _runtime = null;
            }

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
