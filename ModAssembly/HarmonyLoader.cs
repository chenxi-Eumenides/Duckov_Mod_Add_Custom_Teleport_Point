using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using UnityEngine;
using Duckov.Modding;

namespace Add_Custom_Teleport_Point
{
    /// <summary>
    ///     Loader and manager for Harmony patches.
    /// </summary>
    public static class HarmonyLoader
    {
        public static object? _harmonyInstance;
        public static bool _isInitialized;

        public static event Action? OnReadyToPatch;

        public static void Initialize()
        {
            Uninitialize();
            if (_harmonyInstance != null) return;

            if (!LoadHarmony())
            {
                Debug.LogError($"{Constant.LogPrefix} Failed to load Harmony. Waiting for mod activation to retry.");
                RegisterModActivatedEvents();
                return;
            }
            HarmonyLoader.OnReadyToPatch += () =>
            {
                Debug.Log($"{Constant.LogPrefix} Harmony patch all");
                HarmonyLoader.PatchAll(typeof(ModBehaviour).Assembly);
                InvokeModEntryMethod("Initialize");
            };

            Debug.Log($"{Constant.LogPrefix} Harmony Initialized Successfully");
            Debug.Log($"{Constant.LogPrefix} Triggering ReadyToPatch Event");
            ReadyToPatch();
        }

        public static void Uninitialize()
        {
            if (_harmonyInstance == null) return;

            Debug.Log($"{Constant.LogPrefix} _harmonyInstance : {_harmonyInstance} _isInitialized : {_isInitialized}");
            if (!UnpatchAll())
            {
                Debug.LogError($"{Constant.LogPrefix} Failed to unpatch Harmony patches during uninitialization.");
            }
            InvokeModEntryMethod("Uninitialize");

            _harmonyInstance = null;
            _isInitialized = false;
        }

        public static bool PatchAll(Assembly assembly)
        {
            if (!_isInitialized)
            {
                Debug.LogError($"{Constant.LogPrefix} Harmony is not initialized. Cannot apply patches.");
                return false;
            }

            try
            {
                var patchAllMethod = _harmonyInstance!.GetType().GetMethod("PatchAll", [typeof(Assembly)]);
                patchAllMethod!.Invoke(_harmonyInstance, [assembly]);
                Debug.Log($"{Constant.LogPrefix} Harmony Patches Applied Successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} Error Applying Harmony Patches: {ex}");
                Debug.LogError($"{Constant.LogPrefix} Stack Trace: {ex.StackTrace}");
            }

            return false;
        }

        public static bool UnpatchAll()
        {
            if (!_isInitialized)
            {
                Debug.LogError($"{Constant.LogPrefix} Harmony is not initialized. Cannot remove patches.");
                return false;
            }

            if (_harmonyInstance == null)
            {
                Debug.LogError($"{Constant.LogPrefix} Harmony instance is null. Cannot remove patches.");
                return false;
            }

            try
            {
                var unpatchAllMethod = _harmonyInstance.GetType().GetMethod("UnpatchAll", [typeof(string)]);
                unpatchAllMethod!.Invoke(_harmonyInstance, [Constant.HarmonyId]);
                Debug.Log($"{Constant.LogPrefix} Harmony Patches Removed Successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} Error Removing Harmony Patches: {ex}");
            }

            return false;
        }

        public static void RegisterModActivatedEvents()
        {
            UnregisterModActivatedEvents();
            ModManager.OnModActivated += OnModActivated;
        }

        public static void UnregisterModActivatedEvents()
        {
            ModManager.OnModActivated -= OnModActivated;
        }

        public static void ReadyToPatch()
        {
            Debug.Log($"{Constant.LogPrefix} Harmony start Patch");
            _isInitialized = true;
            OnReadyToPatch?.Invoke();
        }

        public static void OnModActivated(ModInfo modInfo, Duckov.Modding.ModBehaviour modBehaviour)
        {
            if (modBehaviour.GetType().Assembly == typeof(HarmonyLoader).Assembly) return;
            Debug.Log($"{Constant.LogPrefix} Mod Activated: {modInfo.name}. Attempting to initialize Harmony again.");

            if (!LoadHarmony()) return;

            Debug.Log($"{Constant.LogPrefix} Harmony Initialized Successfully on Mod Activation");
            UnregisterModActivatedEvents();
            ReadyToPatch();
        }

        public static bool LoadHarmony()
        {
            try
            {
                var harmonyType = Type.GetType("HarmonyLib.Harmony, 0Harmony");
                if (harmonyType == null)
                {
                    if (!FindHarmonyLibLocally(out var harmonyAssembly))
                    {
                        Debug.LogError($"{Constant.LogPrefix} HarmonyLib not found. Please ensure Harmony is installed.");
                        return false;
                    }

                    harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony");
                    if (harmonyType == null)
                    {
                        Debug.LogError($"{Constant.LogPrefix} HarmonyLib.Harmony type not found in Harmony assembly.");
                        return false;
                    }
                }

                _harmonyInstance = Activator.CreateInstance(harmonyType, Constant.HarmonyId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} Error initializing Harmony: {ex}");
            }

            return false;
        }

        public static bool FindHarmonyLibLocally([NotNullWhen(true)] out Assembly? harmonyAssembly)
        {
            harmonyAssembly = null;
            try
            {
                var path = Path.GetDirectoryName(typeof(HarmonyLoader).Assembly.Location);
                if (path == null) return false;

                var targetAssemblyFile = Path.Combine(path, "0Harmony.dll");
                if (!File.Exists(targetAssemblyFile)) return false;

                try
                {
                    Debug.Log($"{Constant.LogPrefix} Loading Assembly from: {targetAssemblyFile}");

                    var bytes = File.ReadAllBytes(targetAssemblyFile);
                    var targetAssembly = Assembly.Load(bytes);
                    harmonyAssembly = targetAssembly;

                    Debug.Log($"{Constant.LogPrefix} HarmonyLib Assembly Loaded Successfully");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Constant.LogPrefix} Error loading HarmonyLib assembly: {ex}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} Error finding HarmonyLib assembly: {ex}");
            }

            return false;
        }

        public static void InvokeModEntryMethod(string methodName)
        {
            var modEntryType = typeof(ModBehaviour).Assembly.GetType($"{Constant.ModId}.ModEntry");
            if (modEntryType == null)
            {
                Debug.LogWarning($"{Constant.LogPrefix} ModEntry type not found in target assembly. Skipping method invocation.");
                return;
            }

            var method = modEntryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Debug.LogWarning($"{Constant.LogPrefix} ModEntry.{methodName} method not found. Skipping invocation.");
                return;
            }

            try
            {
                method.Invoke(null, null);
                Debug.Log($"{Constant.LogPrefix} ModEntry.{methodName} invoked successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} Error invoking ModEntry.{methodName}: {ex}");
            }
        }

    }
}
