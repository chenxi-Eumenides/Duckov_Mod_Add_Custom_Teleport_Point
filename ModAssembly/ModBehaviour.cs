using System;
using System.Collections.Generic;
using UnityEngine;

namespace Add_Custom_Teleport_Point
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance { get; private set; }
        public static Dictionary<int, Dictionary<string, object>> lastGameState = new Dictionary<int, Dictionary<string, object>>();

        // 创建时
        private void Awake()
        {
            Instance = this;
        }

        // 启用时
        private void OnEnable()
        {
            Debug.Log($"{Constant.ModName} Loaded");
            registerCallFuncs();

            registerCustomTeleportConfig();
            customEvent.onSceneLoad += TeleporterManager.Init;

            HarmonyLoader.Initialize();
        }

        // 禁用时
        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            customEvent.onSceneLoad -= TeleporterManager.Init;
            TeleporterManager.RemoveCreatedTeleportPoint();

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
            TeleporterManager.registerTeleportPoint(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-5f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_BASE_2,
                targetPosition: new Vector3(95f, 0f, -40f),
                interactName: "同地图传送",
                backTeleport: true
            );
            TeleporterManager.registerTeleportPoint(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-3f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_GROUNDZERO,
                targetPosition: new Vector3(321f, 0f, 185f),
                interactName: "跨地图传送-零号区",
                backTeleport: true
            );
            TeleporterManager.registerTeleportPoint(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-7f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_FARM_JLAB_FACILITY,
                targetPosition: new Vector3(910f, 0f, 600f),
                interactName: "跨地图传送-农场镇JLab",
                backTeleport: true
            );
        }

        // 注册用于测试的回调函数，测试用，所以不提供销毁函数了
        // 加载顺序为
        // SceneLoader.onStartedLoadingScene
        // SceneLoader.onFinishedLoadingScene
        // SceneLoader.onBeforeSetSceneActive
        // SceneLoader.onAfterSceneInitialize
        //   LevelManager.OnLevelBeginInitializing
        //   LevelManager.OnLevelInitialized
        //   LevelManager.OnAfterLevelInitialized
        // LevelManager 如果实例化时，正在加载场景，就会注册到 SceneLoader.onAfterSceneInitialize 中
        // 如果没有加载场景，就会直接执行
        // 所以顺序通常是按照我写的顺序，如果要在其他地方实例化 LevelManager，则顺序按实际来。
        private void registerCallFuncs()
        {
            SceneLoader.onStartedLoadingScene += onStartedLoadingScene;
            SceneLoader.onFinishedLoadingScene += onFinishedLoadingScene;
            SceneLoader.onBeforeSetSceneActive += onBeforeSetSceneActive;
            SceneLoader.onAfterSceneInitialize += onAfterSceneInitialize;
            LevelManager.OnLevelBeginInitializing += OnLevelBeginInitializing;
            LevelManager.OnLevelInitialized += OnLevelInitialized;
            LevelManager.OnAfterLevelInitialized += OnAfterLevelInitialized;
        }

        // 测试用
        private static void onStartedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onStartedLoadingScene 关卡加载开始");
        }

        // 测试用
        private static void onFinishedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onFinishedLoadingScene");
        }

        // 测试用
        private static void onBeforeSetSceneActive(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onBeforeSetSceneActive");
            // lastGameState = Test.CaptureCurrentState();
        }

        // 测试用
        private static void onAfterSceneInitialize(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onAfterSceneInitialize");
            // Test.CompareAndPrintStateDifferences(lastGameState, Test.CaptureCurrentState());
        }

        // 测试用
        private static void OnLevelBeginInitializing()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelBeginInitializing");
        }

        // 测试用
        private static void OnLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelInitialized");
        }

        // 测试用
        private static void OnAfterLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnAfterLevelInitialized 关卡加载结束");
        }

        public event Action? OnModDisabled;
    }
}
