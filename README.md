# Configurable Cannons

Configurable Cannons is a small MineMogul mod that lets you adjust Conveyor Cannon launch velocity per cannon with a simple in-game UI.

Fresh cannons keep their normal stock values until you change them. Edited cannon values are saved with the world.

## Features

- Per-cannon velocity tuning
- Simple in-game `X / Y / Z` velocity UI
- Default open key: `C`
- Optional Rebind support for changing the keybind in-game
- Save/load support for edited cannons

## Requirements

- MineMogul
- BepInEx 5

Optional:

- Rebind (`com.rebind`)

## Installation

1. Install BepInEx 5 for MineMogul.
2. Copy `ConfigurableCannons.dll` to:

   `BepInEx\plugins\Configurable Cannons\`

3. Launch the game once to generate the config file.

## Usage

1. Look at a Conveyor Cannon.
2. Press `C` to open the UI.
3. Adjust the cannon push velocity values.
4. Press `Set` to apply the change to that cannon.

## Config

Main config file:

`BepInEx\config\com.configurablecannons.cfg`

Current config options:

- `Open Cannon Ui Key`

## Notes

- Plugin GUID: `com.configurablecannons`
- Current version: `1.0.0`
- Edited cannon values are stored in the save data for those cannons
