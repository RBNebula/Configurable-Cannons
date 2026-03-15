using BepInEx.Configuration;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal static class ModInfo
    {
        internal const string PluginGuid = "com.configurablecannons";
        internal const string PluginName = "ConfigurableCannons";
        internal const string PluginVersion = "1.0.0";

        internal const string RebindPluginGuid = "com.rebind";
        internal const string RebindApiTypeName = "Rebind.RebindApi";
        internal const string RebindRegisterMethodName = "RegisterKeybind";
        internal const string RebindSectionTitle = "ConfigurableCannons";
        internal const string RebindOpenUiTitle = "Open Cannon UI";
        internal const string CannonCustomDataPrefix = "CCT5";
        internal static readonly KeyboardShortcut DefaultOpenUiShortcut = new KeyboardShortcut(KeyCode.C);
        internal const string UiBundleFileName = "conveyorcannontweaks_ui";
        internal const string UiBundleDevPath = @"C:\Users\main\Documents\Unity\ConveyorCannonTweaksUI\AssetBundles\conveyorcannontweaks_ui";
        internal const string UiPreviewBundleBuildPath = @"C:\Users\main\Documents\Game modding\MineMogul\Conveyor Cannon Tweaks\UnityUiPreview\Build\AssetBundles\conveyorcannontweaks_ui";

        internal const int ConveyorCannonId = 104;
        internal const float LookAtDistance = 8f;
    }
}
