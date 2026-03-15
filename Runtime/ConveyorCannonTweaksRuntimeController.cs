using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class ConveyorCannonTweaksRuntimeController : IDisposable
    {
        private readonly ConveyorCannonTweaksConfig _config;
        private readonly ConveyorCannonTweaksApplier _applier;
        private readonly RebindKeybindRegistrar _rebindRegistrar;
        private readonly CannonTweaksAssetBundleUi _panelUi;

        private bool _isInitialized;
        private BuildingObject _selectedBuilding;

        internal ConveyorCannonTweaksRuntimeController(ConfigFile configFile, ManualLogSource logger)
        {
            _config = new ConveyorCannonTweaksConfig(configFile);
            _applier = new ConveyorCannonTweaksApplier(logger);
            _rebindRegistrar = new RebindKeybindRegistrar(_config, logger);
            _panelUi = new CannonTweaksAssetBundleUi(ApplySettingsFromUi, logger);
        }

        internal void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _applier.ApplyToAllConveyorCannons();
            _rebindRegistrar.Initialize();
            _isInitialized = true;
        }

        internal void Tick()
        {
            _rebindRegistrar.Tick();

            if (_panelUi.IsOpen)
            {
                _panelUi.TickWhileOpen();
            }

            if (_config.OpenUiKey == null)
            {
                return;
            }

            KeyboardShortcut shortcut = _config.OpenUiKey.Value;
            if (!shortcut.IsDown())
            {
                return;
            }

            if (_panelUi.IsOpen)
            {
                _panelUi.Close();
                _selectedBuilding = null;
                return;
            }

            BuildingObject building;
            if (_applier.TryGetLookedAtConveyorCannon(out building))
            {
                CannonTweaksSettingsSnapshot snapshot;
                if (!_applier.TryGetSettingsForUi(building, out snapshot))
                {
                    snapshot = default(CannonTweaksSettingsSnapshot);
                }

                _selectedBuilding = building;
                _panelUi.Open(snapshot);
            }
        }

        internal void HandleBuildingStarted(BuildingObject building)
        {
            _applier.ApplyToBuildingIfConveyorCannon(building);
        }

        internal void HandleConveyorBeltEnabled(ConveyorBelt belt)
        {
            if (belt == null)
            {
                return;
            }

            BuildingObject building = belt.GetComponentInParent<BuildingObject>();
            _applier.ApplyToBuildingIfConveyorCannon(building);
        }

        public void Dispose()
        {
            _isInitialized = false;

            _rebindRegistrar.Dispose();
            _panelUi.Dispose();
        }

        private void ApplySettingsFromUi(CannonTweaksSettingsSnapshot snapshot)
        {
            if (_selectedBuilding == null)
            {
                return;
            }

            _applier.ApplySnapshotToBuilding(_selectedBuilding, snapshot);
        }
    }
}
