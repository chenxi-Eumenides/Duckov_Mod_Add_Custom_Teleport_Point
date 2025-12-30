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

            // 注册自定义传送点配置
            registerCustomTeleportConfig();

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

        // 在这里注册自定义的传送点配置
        private void registerCustomTeleportConfig()
        {
            TeleporterManager.RegisterTeleportPointConfig(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-5f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_BASE_2,
                targetPosition: new Vector3(95f, 0f, -40f),
                interactName: "同地图传送",
                backTeleport: true
            );
            TeleporterManager.RegisterTeleportPointConfig(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-3f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_GROUNDZERO,
                targetPosition: new Vector3(322f, 0f, 185f),
                interactName: "跨地图传送-零号区",
                backTeleport: true
            );
            TeleporterManager.RegisterTeleportPointConfig(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-7f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_FARM_JLAB_FACILITY,
                targetPosition: new Vector3(910f, 0f, 600f),
                interactName: "跨地图传送-农场镇JLab",
                backTeleport: true,
                disposable: true
            );
        }
    }
}
