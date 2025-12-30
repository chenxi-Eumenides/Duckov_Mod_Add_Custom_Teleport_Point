using System;
using UnityEngine;

namespace Add_Custom_Teleport_Point
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public event Action? OnModDisabled;
        public static ModBehaviour? Instance { get; private set; }

        // 创建时
        private void Awake()
        {
            Instance = this;
        }

        // 启用时
        private void OnEnable()
        {
            Debug.Log($"{Constant.ModName} Loaded");

            TeleporterManager.Init();
            HarmonyLoader.Initialize();
        }

        // 禁用时
        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            TeleporterManager.Cleanup();
            HarmonyLoader.Uninitialize();
        }

        // 销毁时
        private void OnDestroy()
        {
            HarmonyLoader.Uninitialize();

            Instance = null;
        }
    }
}
