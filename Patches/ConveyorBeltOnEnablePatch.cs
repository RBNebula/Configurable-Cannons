using HarmonyLib;

namespace MineMogul.ConveyorCannonTweaks
{
    [HarmonyPatch(typeof(ConveyorBelt), "OnEnable")]
    internal static class ConveyorBeltOnEnablePatch
    {
        private static void Postfix(ConveyorBelt __instance)
        {
            ConveyorCannonTweaksRuntimeController runtime = ConveyorCannonTweaksRuntime.Current;
            if (runtime != null)
            {
                runtime.HandleConveyorBeltEnabled(__instance);
            }
        }
    }
}
