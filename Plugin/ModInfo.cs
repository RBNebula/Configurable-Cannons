using BepInEx.Configuration;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal static class ModInfo
    {
        public const string PLUGIN_GUID = "com.configurablecannons";
        public const string PLUGIN_NAME = "ConfigurableCannons";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string ASSEMBLY_VERSION = "1.0.0";
        public const string FILE_VERSION = "1.0.0";
        public const string INFORMATIONAL_VERSION = "1.0.0";
        public const string HARMONY_ID = PLUGIN_GUID;
        public const string LOG_PREFIX = "[ConfigurableCannons]";
        public const string DISPLAY_NAME_SHORT = "ConfigurableCannons";
        public const string DISPLAY_NAME_LONG = "Configurable Cannons";

        internal const string PluginGuid = PLUGIN_GUID;
        internal const string PluginName = PLUGIN_NAME;
        internal const string PluginVersion = PLUGIN_VERSION;

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
