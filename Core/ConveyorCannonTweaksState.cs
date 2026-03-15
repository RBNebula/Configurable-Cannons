using System;
using System.Globalization;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class ConveyorCannonTweaksState : MonoBehaviour
    {
        private bool _hasOverride;
        private CannonTweaksSettingsSnapshot _overrideSettings;

        internal static ConveyorCannonTweaksState Get(BuildingObject building)
        {
            if (building == null || (int)building.SavableObjectID != ModInfo.ConveyorCannonId)
            {
                return null;
            }

            return building.GetComponent<ConveyorCannonTweaksState>();
        }

        internal static ConveyorCannonTweaksState EnsureAttached(BuildingObject building)
        {
            ConveyorCannonTweaksState state = Get(building);
            if (state != null)
            {
                return state;
            }

            if (building == null || (int)building.SavableObjectID != ModInfo.ConveyorCannonId)
            {
                return null;
            }

            return building.gameObject.AddComponent<ConveyorCannonTweaksState>();
        }

        internal bool TryGetOverride(out CannonTweaksSettingsSnapshot snapshot)
        {
            snapshot = _overrideSettings;
            return _hasOverride;
        }

        internal void SetOverride(CannonTweaksSettingsSnapshot snapshot)
        {
            _overrideSettings = snapshot;
            _hasOverride = true;
        }

        internal void ClearOverride()
        {
            _hasOverride = false;
            _overrideSettings = default(CannonTweaksSettingsSnapshot);
        }
    }

    internal static class ConveyorCannonTweaksCustomData
    {
        internal static string Serialize(CannonTweaksSettingsSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}",
                ModInfo.CannonCustomDataPrefix,
                snapshot.PushVelocityX,
                snapshot.PushVelocityY,
                snapshot.PushVelocityZ);
        }

        internal static bool TryParse(string customData, out CannonTweaksSettingsSnapshot snapshot)
        {
            snapshot = default(CannonTweaksSettingsSnapshot);

            if (string.IsNullOrEmpty(customData))
            {
                return false;
            }

            string[] parts = customData.Split(new[] { '|' }, 4);
            if (parts.Length != 4 || !string.Equals(parts[0], ModInfo.CannonCustomDataPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            float pushVelocityX;
            float pushVelocityY;
            float pushVelocityZ;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out pushVelocityX)
                || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out pushVelocityY)
                || !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out pushVelocityZ))
            {
                return false;
            }

            snapshot = new CannonTweaksSettingsSnapshot
            {
                PushVelocityX = pushVelocityX,
                PushVelocityY = pushVelocityY,
                PushVelocityZ = pushVelocityZ
            };

            return true;
        }
    }
}
