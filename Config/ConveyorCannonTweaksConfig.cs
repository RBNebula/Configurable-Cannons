using BepInEx.Configuration;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class ConveyorCannonTweaksConfig
    {
        private const string InputSection = "Input";
        private const string OpenCannonUiKey = "Open Cannon Ui Key";

        private readonly ConfigFile _config;

        internal ConfigEntry<KeyboardShortcut> OpenUiKey { get; }

        internal ConveyorCannonTweaksConfig(ConfigFile config)
        {
            _config = config;
            OpenUiKey = config.Bind(InputSection, OpenCannonUiKey, ModInfo.DefaultOpenUiShortcut, "Open/close the ConfigurableCannons UI while looking at a cannon.");
        }

        internal void SaveSafe()
        {
            try
            {
                _config.Save();
            }
            catch
            {
                // Ignore save failures; values still apply in-memory.
            }
        }
    }
}
