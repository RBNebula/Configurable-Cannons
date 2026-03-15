CONFIGURABLE CANNONS - 1.0.0

Configurable Cannons is a standalone MineMogul mod for adjusting Conveyor Cannon launch velocity
per cannon with a simple in-game UI.

Each cannon can be configured individually.
Untouched cannons keep their normal stock values.
Edited cannon values are saved with the world save.

:: REQUIREMENTS ::
- MineMogul (up to date)
- BepInEx 5

:: OPTIONAL ::
- Rebind (com.rebind) to change the Open/Close key from the in-game keybind UI
- BepInEx Configuration Manager for easier config editing

:: INSTALLATION ::
1. Install BepInEx 5 for MineMogul.
2. Copy ConfigurableCannons.dll into:
   BepInEx\plugins\Configurable Cannons\
3. Launch the game once to generate config files.

:: USAGE ::
- Default open key: C
- Look at a Conveyor Cannon and press C to open the Configurable Cannons panel
- Adjust Push Velocity X / Y / Z
- Press Set to apply changes to that cannon

Key behavior:
- Each cannon is configured separately
- Fresh cannons use the game’s default values until edited
- Edited values persist through save/load

:: CONFIG ::
Main config file:
- BepInEx\config\com.configurablecannons.cfg

Current config options:
- OpenUiKey

:: NOTES ::
- This mod stores edited cannon values per cannon in the save data.
- Removing the mod stops the UI/editing behavior, but edited cannon custom save data may remain in the save.
- Default cannon behavior is unchanged unless you edit that specific cannon.

:: TROUBLESHOOTING ::
- If the UI does not open, make sure you are looking directly at a Conveyor Cannon
- If the keybind does not work, check for key conflicts
- If using Rebind, configure keybinds under:
  Settings > Keybinds > MODS > ConfigurableCannons
- If the mod does not load, check BepInEx log output for:
  com.configurablecannons

:: KNOWN LIMITATIONS ::
- This mod currently focuses on cannon launch velocity only
- It does not add chat commands
- It does not add Accessibility menu integration

:: UNINSTALL ::
1. Remove ConfigurableCannons.dll from:
   BepInEx\plugins\Configurable Cannons\
2. Optionally remove:
   BepInEx\config\com.configurablecannons.cfg

:: CREDITS ::
- Built for MineMogul modding with BepInEx and Harmony
- Optional keybind integration via Rebind
