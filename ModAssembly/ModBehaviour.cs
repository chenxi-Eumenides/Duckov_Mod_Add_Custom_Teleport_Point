using System;
using UnityEngine;

namespace Add_Custom_Teleport_Point
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public static ModBehaviour? Instance { get; private set; }

        // 创建时
        private void Awake()
        {
            Instance = this;
        }

        // 启用时
        private void OnEnable()
        {
            ModLogger.Log($"{Constant.ModName} Loaded");

            registerCustomConfig();

            customEvent.onSceneLoad += TeleporterManager.Init;

            HarmonyLoader.Initialize();
        }

        // 禁用时
        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            TeleporterManager.RemoveCreatedTeleportPoint();

            customEvent.onSceneLoad -= TeleporterManager.Init;

            HarmonyLoader.Uninitialize();
        }

        // 销毁时
        private void OnDestroy()
        {
            HarmonyLoader.Uninitialize();

            Instance = null;
        }

        // 在这里注册自定义的传送点配置
        // 为了整洁，仅此
        private void registerCustomConfig()
        {
            TeleporterManager.registerTeleportPoint(
                sourceSceneId: Constant.SCENE_ID_BASE,
                sourcePosition: new Vector3(-5f, 0f, -85f),
                targetSceneId: Constant.SCENE_ID_BASE_2,
                targetPosition: new Vector3(95f, 0f, -70f),
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
                interactName: "跨地图传送-农场镇jlab",
                backTeleport: true
            );
        }

        // 注册用于测试的回调函数，测试用，所以不提供销毁函数了
        // 加载顺序为
        // SceneLoader.onStartedLoadingScene
        // SceneLoader.onFinishedLoadingScene
        // SceneLoader.onBeforeSetSceneActive
        // SceneLoader.onAfterSceneInitialize
        // LevelManager.OnLevelBeginInitializing
        // LevelManager.OnLevelInitialized
        // LevelManager.OnAfterLevelInitialized
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
            ModLogger.Log($"onStartedLoadingScene 关卡加载开始");
        }

        // 测试用
        private static void onFinishedLoadingScene(SceneLoadingContext context)
        {
            ModLogger.Log($"onFinishedLoadingScene");
        }

        // 测试用
        private static void onBeforeSetSceneActive(SceneLoadingContext context)
        {
            ModLogger.Log($"onBeforeSetSceneActive");
        }

        // 测试用
        private static void onAfterSceneInitialize(SceneLoadingContext context)
        {
            ModLogger.Log($"onAfterSceneInitialize");
            if (LevelConfig.Instance != null)
            {
                var tdc = LevelConfig.Instance.timeOfDayConfig;
                var defaultEntry = (TimeOfDayEntry?)RFH.GetFieldValue(tdc, "defaultEntry");
                var cloudyEntry = (TimeOfDayEntry?)RFH.GetFieldValue(tdc, "cloudyEntry");
                var rainyEntry = (TimeOfDayEntry?)RFH.GetFieldValue(tdc, "rainyEntry");
                var stormIEntry = (TimeOfDayEntry?)RFH.GetFieldValue(tdc, "stormIEntry");
                var stormIIEntry = (TimeOfDayEntry?)RFH.GetFieldValue(tdc, "stormIIEntry");
                // var lbl = new LootBoxLoader();
                var lm = LevelManager.Instance;
                var lbi = LevelManager.LootBoxInventories;
                ModLogger.LogWarning($"LevelManager: {lm} {lbi} {lbi.Count}");
            }
        }

        // 测试用
        private static void OnLevelBeginInitializing()
        {
            ModLogger.Log($"OnLevelBeginInitializing");
            // 获取数据结构
            var todcObj = TimeOfDayController.Instance?.gameObject;
            var todcPObj = todcObj?.transform?.parent?.gameObject;
            var todcPPObj = todcPObj?.transform?.parent?.gameObject;
            ModLogger.Log($"TimeOfDayController:{todcObj?.name} -> {todcPObj?.name} -> {todcPPObj?.name}");
            var todconfig = (TimeOfDayConfig?)RFH.GetFieldValue(TimeOfDayController.Instance ?? new TimeOfDayController(), "config");
            var todconfigObj = todconfig?.transform.gameObject;
            var todconfigPObj = todconfigObj?.transform?.parent?.gameObject;
            var todconfigPPObj = todconfigPObj?.transform?.parent?.gameObject;
            ModLogger.Log($"TimeOfDayController:{todconfigObj?.name} -> {todconfigPObj?.name} -> {todconfigPPObj?.name}");
        }

        // 测试用
        private static void OnLevelInitialized()
        {
            ModLogger.Log($"OnLevelInitialized");
        }

        // 测试用
        private static void OnAfterLevelInitialized()
        {
            ModLogger.Log($"OnAfterLevelInitialized 关卡加载结束");
        }

        public event Action? OnModDisabled;
    }
}
