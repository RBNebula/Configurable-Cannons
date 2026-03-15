using HarmonyLib;

namespace MineMogul.ConveyorCannonTweaks
{
    [HarmonyPatch(typeof(BuildingObject), "GetCustomSaveData")]
    internal static class BuildingObjectGetCustomSaveDataPatch
    {
        private static void Postfix(BuildingObject __instance, ref string __result)
        {
            ConveyorCannonTweaksState state = ConveyorCannonTweaksState.Get(__instance);
            if (state == null)
            {
                return;
            }

            CannonTweaksSettingsSnapshot snapshot;
            if (!state.TryGetOverride(out snapshot))
            {
                return;
            }

            __result = ConveyorCannonTweaksCustomData.Serialize(snapshot);
        }
    }
}
