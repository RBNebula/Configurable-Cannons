using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class ConveyorCannonTweaksApplier
    {
        private const string CannonRootName = "ConveyorCannon(Clone)";
        private const string LauncherChildName = "ConveyorLauncherTrigger";

        private readonly ManualLogSource _logger;
        private readonly FieldInfo _pushVelocityField;

        internal ConveyorCannonTweaksApplier(ManualLogSource logger)
        {
            _logger = logger;
            _pushVelocityField = AccessTools.Field(typeof(ConveyorBelt), "_pushVelocity");
            if (_pushVelocityField == null)
            {
                _logger.LogError("Failed to find ConveyorBelt._pushVelocity field. Conveyor cannon push velocity tweaks will not apply.");
            }
        }

        internal void ApplyToAllConveyorCannons()
        {
            BuildingObject[] buildings = UnityEngine.Object.FindObjectsByType<BuildingObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buildings.Length; i++)
            {
                ApplyToBuildingIfConveyorCannon(buildings[i]);
            }
        }

        internal void ApplyToBuildingIfConveyorCannon(BuildingObject building)
        {
            if (!IsConveyorCannon(building))
            {
                return;
            }

            ConveyorCannonTweaksState state = ConveyorCannonTweaksState.Get(building);
            CannonTweaksSettingsSnapshot snapshot;
            if (state == null || !state.TryGetOverride(out snapshot))
            {
                return;
            }

            ApplySnapshotToLegacyTarget(building, snapshot);
        }

        internal bool TryGetSettingsForUi(BuildingObject building, out CannonTweaksSettingsSnapshot snapshot)
        {
            snapshot = default(CannonTweaksSettingsSnapshot);
            if (!IsConveyorCannon(building))
            {
                return false;
            }

            ConveyorCannonTweaksState state = ConveyorCannonTweaksState.Get(building);
            if (state != null && state.TryGetOverride(out snapshot))
            {
                return true;
            }

            ConveyorBelt launcherBelt;
            if (!TryGetLauncherBelt(building, out launcherBelt))
            {
                return false;
            }

            Vector3 pushVelocity;
            if (!TryGetPushVelocity(launcherBelt, out pushVelocity))
            {
                return false;
            }

            snapshot = new CannonTweaksSettingsSnapshot
            {
                PushVelocityX = pushVelocity.x,
                PushVelocityY = pushVelocity.y,
                PushVelocityZ = pushVelocity.z
            };
            return true;
        }

        internal void ApplySnapshotToBuilding(BuildingObject building, CannonTweaksSettingsSnapshot snapshot)
        {
            if (!IsConveyorCannon(building))
            {
                return;
            }

            ConveyorCannonTweaksState state = ConveyorCannonTweaksState.EnsureAttached(building);
            if (state == null)
            {
                return;
            }

            state.SetOverride(snapshot);
            ApplySnapshotToLegacyTarget(building, snapshot);
        }

        private void ApplySnapshotToLegacyTarget(BuildingObject building, CannonTweaksSettingsSnapshot snapshot)
        {
            ConveyorBelt targetBelt = GetLegacyApplyTarget(building);
            if (targetBelt == null)
            {
                return;
            }

            TrySetPushVelocity(
                targetBelt,
                new Vector3(snapshot.PushVelocityX, snapshot.PushVelocityY, snapshot.PushVelocityZ));
        }

        private ConveyorBelt GetLegacyApplyTarget(BuildingObject building)
        {
            if (building == null)
            {
                return null;
            }

            ConveyorBelt[] belts = building.GetComponentsInChildren<ConveyorBelt>(true);
            ConveyorBelt strongestBelt = null;
            float strongestMagnitude = -1f;

            for (int i = 0; i < belts.Length; i++)
            {
                ConveyorBelt belt = belts[i];
                if (belt == null)
                {
                    continue;
                }

                Vector3 pushVelocity;
                if (!TryGetPushVelocity(belt, out pushVelocity))
                {
                    continue;
                }

                float magnitude = pushVelocity.sqrMagnitude;
                if (magnitude <= strongestMagnitude)
                {
                    continue;
                }

                strongestMagnitude = magnitude;
                strongestBelt = belt;
            }

            return strongestBelt;
        }

        private bool TryGetLauncherBelt(BuildingObject building, out ConveyorBelt launcherBelt)
        {
            launcherBelt = null;
            if (building == null)
            {
                return false;
            }

            Transform cannonRoot = FindNearestCannonRoot(building.transform);
            if (cannonRoot != null && TryFindNamedBeltUnderRoot(cannonRoot, LauncherChildName, out launcherBelt))
            {
                return true;
            }

            if (TryFindNamedBeltUnderRoot(building.transform, LauncherChildName, out launcherBelt))
            {
                return true;
            }

            Transform current = building.transform != null ? building.transform.parent : null;
            while (current != null)
            {
                if (TryFindNamedBeltUnderRoot(current, LauncherChildName, out launcherBelt))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static Transform FindNearestCannonRoot(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                if (NameMatches(current.name, CannonRootName))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static bool TryFindNamedBeltUnderRoot(Transform root, string beltObjectName, out ConveyorBelt belt)
        {
            belt = null;
            if (root == null)
            {
                return false;
            }

            ConveyorBelt[] belts = root.GetComponentsInChildren<ConveyorBelt>(true);
            for (int i = 0; i < belts.Length; i++)
            {
                ConveyorBelt candidate = belts[i];
                if (candidate != null && NameMatches(candidate.gameObject.name, beltObjectName))
                {
                    belt = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetPushVelocity(ConveyorBelt belt, out Vector3 pushVelocity)
        {
            pushVelocity = Vector3.zero;
            if (_pushVelocityField == null || belt == null)
            {
                return false;
            }

            object rawValue = _pushVelocityField.GetValue(belt);
            if (!(rawValue is Vector3))
            {
                return false;
            }

            pushVelocity = (Vector3)rawValue;
            return true;
        }

        private bool TrySetPushVelocity(ConveyorBelt belt, Vector3 pushVelocity)
        {
            if (_pushVelocityField == null || belt == null)
            {
                return false;
            }

            _pushVelocityField.SetValue(belt, pushVelocity);
            return true;
        }

        internal bool TryGetLookedAtConveyorCannon(out BuildingObject building)
        {
            building = null;

            PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null || player.PlayerCamera == null)
            {
                return false;
            }

            RaycastHit hit;
            if (!Physics.Raycast(player.PlayerCamera.transform.position, player.PlayerCamera.transform.forward, out hit, ModInfo.LookAtDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            BuildingObject candidate = TryResolveBuildingFromHit(hit);
            if (!IsConveyorCannon(candidate))
            {
                return false;
            }

            building = candidate;
            return true;
        }

        private static BuildingObject TryResolveBuildingFromHit(RaycastHit hit)
        {
            if (hit.rigidbody != null)
            {
                return hit.rigidbody.GetComponentInParent<BuildingObject>();
            }

            if (hit.collider != null)
            {
                return hit.collider.GetComponentInParent<BuildingObject>();
            }

            return null;
        }

        private static bool IsConveyorCannon(BuildingObject building)
        {
            return building != null && (int)building.SavableObjectID == ModInfo.ConveyorCannonId;
        }

        private static bool NameMatches(string actualName, string expectedName)
        {
            if (actualName == null || expectedName == null)
            {
                return false;
            }

            return string.Equals(actualName.Trim(), expectedName, System.StringComparison.Ordinal);
        }
    }
}
