using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace MineMogul.ConveyorCannonTweaks
{
    internal sealed class RebindKeybindRegistrar : IDisposable
    {
        private readonly ConveyorCannonTweaksConfig _config;
        private readonly ManualLogSource _logger;
        private readonly List<IDisposable> _rebindHandles;

        private bool _rebindRegistered;
        private float _nextRebindAttemptTime;

        internal RebindKeybindRegistrar(ConveyorCannonTweaksConfig config, ManualLogSource logger)
        {
            _config = config;
            _logger = logger;
            _rebindHandles = new List<IDisposable>();
        }

        internal void Initialize()
        {
            _rebindRegistered = TryRegisterRebindKeybinds();
            _nextRebindAttemptTime = Time.unscaledTime + 1f;
        }

        internal void Tick()
        {
            if (_rebindRegistered)
            {
                return;
            }

            if (!IsRebindInstalled())
            {
                return;
            }

            if (Time.unscaledTime < _nextRebindAttemptTime)
            {
                return;
            }

            _rebindRegistered = TryRegisterRebindKeybinds();
            _nextRebindAttemptTime = Time.unscaledTime + 1f;
        }

        public void Dispose()
        {
            DisposeRebindHandles();
        }

        private void DisposeRebindHandles()
        {
            for (int i = 0; i < _rebindHandles.Count; i++)
            {
                try
                {
                    _rebindHandles[i].Dispose();
                }
                catch
                {
                    // Ignore disposal errors from optional integration handles.
                }
            }

            _rebindHandles.Clear();
            _rebindRegistered = false;
        }

        private bool TryRegisterRebindKeybinds()
        {
            if (_rebindRegistered || _rebindHandles.Count > 0)
            {
                return true;
            }

            if (!IsRebindInstalled())
            {
                return false;
            }

            Type rebindApiType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(ModInfo.RebindApiTypeName, false))
                .FirstOrDefault(type => type != null);
            if (rebindApiType == null)
            {
                return false;
            }

            MethodInfo shortcutMethod = rebindApiType.GetMethod(
                ModInfo.RebindRegisterMethodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(Func<KeyboardShortcut>),
                    typeof(Action<KeyboardShortcut>),
                    typeof(KeyboardShortcut),
                    typeof(Action<KeyboardShortcut>)
                },
                null);

            if (shortcutMethod != null && RegisterShortcutRebindRow(shortcutMethod, ModInfo.RebindOpenUiTitle, _config.OpenUiKey, ModInfo.DefaultOpenUiShortcut) > 0)
            {
                _logger.LogInfo("[ConveyorCannonTweaks] Registered keybind with Rebind (KeyboardShortcut).");
                return true;
            }

            MethodInfo legacyMethod = rebindApiType.GetMethod(
                ModInfo.RebindRegisterMethodName,
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(Func<KeyCode>),
                    typeof(Action<KeyCode>),
                    typeof(KeyCode),
                    typeof(Action<KeyCode>)
                },
                null);
            if (legacyMethod == null)
            {
                _logger.LogWarning("[ConveyorCannonTweaks] Rebind API found but no supported RegisterKeybind signature was available.");
                return false;
            }

            int rowsRegistered = RegisterLegacyRebindRow(
                legacyMethod,
                ModInfo.RebindOpenUiTitle,
                _config.OpenUiKey,
                ModInfo.DefaultOpenUiShortcut.MainKey);
            if (rowsRegistered > 0)
            {
                _logger.LogInfo("[ConveyorCannonTweaks] Registered keybind with Rebind (legacy KeyCode fallback).");
            }

            return rowsRegistered > 0;
        }

        private int RegisterShortcutRebindRow(
            MethodInfo registerMethod,
            string keybindTitle,
            ConfigEntry<KeyboardShortcut> configEntry,
            KeyboardShortcut defaultShortcut)
        {
            try
            {
                Func<KeyboardShortcut> getter = () => NormalizeShortcut(configEntry.Value);
                Action<KeyboardShortcut> setter = shortcut => configEntry.Value = NormalizeShortcut(shortcut);
                Action<KeyboardShortcut> onSaved = shortcut =>
                {
                    _config.SaveSafe();
                    _logger.LogInfo("[ConveyorCannonTweaks] Rebind: " + keybindTitle + " = " + shortcut);
                };

                object registration = registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModInfo.RebindSectionTitle,
                        keybindTitle,
                        getter,
                        setter,
                        NormalizeShortcut(defaultShortcut),
                        onSaved
                    });
                IDisposable handle = registration as IDisposable;
                if (handle == null)
                {
                    return 0;
                }

                _rebindHandles.Add(handle);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[ConveyorCannonTweaks] Failed to register Rebind row '" + keybindTitle + "': " + ex.Message);
                return 0;
            }
        }

        private int RegisterLegacyRebindRow(
            MethodInfo registerMethod,
            string keybindTitle,
            ConfigEntry<KeyboardShortcut> configEntry,
            KeyCode defaultKey)
        {
            try
            {
                Func<KeyCode> getter = () => configEntry.Value.MainKey;
                Action<KeyCode> setter = key =>
                {
                    configEntry.Value = key == KeyCode.None
                        ? KeyboardShortcut.Empty
                        : new KeyboardShortcut(key, Array.Empty<KeyCode>());
                };
                Action<KeyCode> onSaved = key =>
                {
                    _config.SaveSafe();
                    _logger.LogInfo("[ConveyorCannonTweaks] Rebind: " + keybindTitle + " = " + key);
                };

                object registration = registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModInfo.RebindSectionTitle,
                        keybindTitle,
                        getter,
                        setter,
                        defaultKey,
                        onSaved
                    });
                IDisposable handle = registration as IDisposable;
                if (handle == null)
                {
                    return 0;
                }

                _rebindHandles.Add(handle);
                return 1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[ConveyorCannonTweaks] Failed to register legacy Rebind row '" + keybindTitle + "': " + ex.Message);
                return 0;
            }
        }

        private static KeyboardShortcut NormalizeShortcut(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey != KeyCode.None)
            {
                return shortcut;
            }

            return KeyboardShortcut.Empty;
        }

        private static bool IsRebindInstalled()
        {
            return Chainloader.PluginInfos.ContainsKey(ModInfo.RebindPluginGuid);
        }
    }
}
