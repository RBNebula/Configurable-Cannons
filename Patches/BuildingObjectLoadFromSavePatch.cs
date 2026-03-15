using HarmonyLib;

namespace MineMogul.ConveyorCannonTweaks
{
    [HarmonyPatch(typeof(BuildingObject), "LoadFromSave")]
    internal static class BuildingObjectLoadFromSavePatch
    {
        private static void Prefix(BuildingObject __instance, ref string __0)
        {
            ConveyorCannonTweaksState state = ConveyorCannonTweaksState.EnsureAttached(__instance);
            if (state == null)
            {
                return;
            }

            CannonTweaksSettingsSnapshot snapshot;
            if (ConveyorCannonTweaksCustomData.TryParse(__0, out snapshot))
            {
                state.SetOverride(snapshot);
                __0 = string.Empty;
                return;
            }

            state.ClearOverride();
        }

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
