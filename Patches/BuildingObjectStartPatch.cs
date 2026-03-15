using HarmonyLib;

namespace MineMogul.ConveyorCannonTweaks
{
    [HarmonyPatch(typeof(BuildingObject), "Start")]
    internal static class BuildingObjectStartPatch
    {
        private static void Postfix(BuildingObject __instance)
        {
            ConveyorCannonTweaksRuntimeController runtime = ConveyorCannonTweaksRuntime.Current;
            if (runtime != null)
            {
                runtime.HandleBuildingStarted(__instance);
            }
        }
    }
}
