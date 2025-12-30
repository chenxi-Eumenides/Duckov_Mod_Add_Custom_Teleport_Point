using System;
using System.Collections.Generic;
using UnityEngine;
using Duckov.Scenes;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks.Triggers;

namespace Add_Custom_Teleport_Point
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public event Action? OnModDisabled;
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
            TeleporterManager.addCallbackFunc();

            HarmonyLoader.Initialize();
        }

        // 禁用时
        private void OnDisable()
        {
            OnModDisabled?.Invoke();

            TeleporterManager.removeCallbackFunc();
            TeleporterManager.removeCreatedTeleportPoint();

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
                targetPosition: new Vector3(322f, 0f, 185f),
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
        // SceneLoaderProxy.LoadScene 加载顺序为
        //   SceneLoader.onStartedLoadingScene
        //   SceneLoader.onBeforeSetSceneActive
        //   SceneLoader.onFinishedLoadingScene
        //     LevelManager.OnLevelBeginInitializing
        //       *MultiSceneCore.OnSubSceneWillBeUnloaded
        //       MultiSceneCore.OnSubSceneLoaded
        //     LevelManager.OnLevelInitialized
        //   SceneLoader.onAfterSceneInitialize
        //     LevelManager.OnAfterLevelInitialized
        // MultiSceneCore.LoadAndTeleport 加载顺序为
        //   *MultiSceneCore.OnSubSceneWillBeUnloaded
        //   MultiSceneCore.OnSubSceneLoaded
        // LevelManager 如果实例化时，正在加载场景，就会注册到 SceneLoader.onAfterSceneInitialize 中
        // 如果没有加载场景，就会直接执行
        // 所以顺序通常是按照我写的顺序，如果要在其他地方实例化 LevelManager，则顺序按实际来。
        private void registerCallFuncs()
        {
            SceneLoader.onStartedLoadingScene += onStartedLoadingScene;
            SceneLoader.onBeforeSetSceneActive += onBeforeSetSceneActive;
            SceneLoader.onFinishedLoadingScene += onFinishedLoadingScene;
            LevelManager.OnLevelBeginInitializing += onLevelBeginInitializing;
            MultiSceneCore.OnSubSceneWillBeUnloaded += onSubSceneWillBeUnloaded;
            MultiSceneCore.OnSubSceneLoaded += onSubSceneLoaded;
            LevelManager.OnLevelInitialized += onLevelInitialized;
            LevelManager.OnAfterLevelInitialized += onAfterLevelInitialized;
            SceneLoader.onAfterSceneInitialize += onAfterSceneInitialize;
        }

        private static void onStartedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onStartedLoadingScene 场景加载开始 {context.sceneName}");
            Test.PrintEvent(typeof(SceneLoader),"onStartedLoadingScene");
        }

        private static void onBeforeSetSceneActive(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onBeforeSetSceneActive 场景激活前 {context.sceneName}");
            Test.PrintEvent(typeof(SceneLoader),"onBeforeSetSceneActive");
        }

        private static void onFinishedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onFinishedLoadingScene 场景加载结束 {context.sceneName}");
            Test.PrintEvent(typeof(SceneLoader),"onFinishedLoadingScene");
        }

        private static void onLevelBeginInitializing()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelBeginInitializing 关卡初始化开始");
            Test.PrintEvent(typeof(LevelManager),"OnLevelBeginInitializing");
        }

        private static void onSubSceneWillBeUnloaded(MultiSceneCore core, Scene scene)
        {
            Debug.Log($"{Constant.LogPrefix} OnSubSceneWillBeUnloaded 子场景即将卸载: {core.DisplayName}");
            Test.PrintEvent(typeof(MultiSceneCore),"OnSubSceneWillBeUnloaded");
        }

        private static void onSubSceneLoaded(MultiSceneCore core, Scene scene)
        {
            Debug.Log($"{Constant.LogPrefix} OnSubSceneLoaded 子场景已加载: {core.DisplayName}");
            Test.PrintEvent(typeof(MultiSceneCore),"OnSubSceneLoaded");
            if (MultiSceneCore.ActiveSubSceneID == Constant.SCENE_ID_BASE)
            {
                try
                {
                    createTpPointTest();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{Constant.LogPrefix} 创建传送点时出错: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        private static void onLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelInitialized 关卡初始化结束");
            Test.PrintEvent(typeof(LevelManager),"OnLevelInitialized");
        }

        private static void onAfterLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnAfterLevelInitialized 关卡初始化完全结束");
            Test.PrintEvent(typeof(LevelManager),"OnAfterLevelInitialized");
        }

        private static void onAfterSceneInitialize(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onAfterSceneInitialize 场景加载完全结束 {context.sceneName}");
            // lastGameState = Test.CaptureCurrentState();
            // Test.CompareAndPrintStateDifferences(lastGameState, Test.CaptureCurrentState());
            Test.PrintEvent(typeof(SceneLoader),"onAfterSceneInitialize");
        }

        public static bool createTpPointTest()
        {
            Debug.Log($"{Constant.LogPrefix} 创建测试传送点");
            string name = "GoToGrtoundZero_test";
            string locationName = "Special/BunkerEntry";
            string UIKey = "UI_Interact_ExitBunker";
            string sceneID = "Level_GroundZero_1";
            Vector3 position = new Vector3(-5f, 0f, -83f);
            
            Debug.Log($"{Constant.LogPrefix} 创建gameobject");
            GameObject teleportPoint = new GameObject(name);
            teleportPoint.transform.position = position;
            teleportPoint.layer = LayerMask.NameToLayer("Interactable");

            Debug.Log($"{Constant.LogPrefix} 添加InteractableBase组件");
            InteractableBase interactableBase = teleportPoint.AddComponent<InteractableBase>();
            interactableBase.InteractName = UIKey;
            interactableBase.interactMarkerOffset = Vector3.up * 1f;
            interactableBase.MarkerActive = true;

            Debug.Log($"{Constant.LogPrefix} 添加BoxCollider组件");
            var collider = teleportPoint.GetComponent<BoxCollider>();
            if (collider == null) collider = teleportPoint.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 2f, 1f);
            collider.isTrigger = true;
            collider.enabled = true;

            Debug.Log($"{Constant.LogPrefix} 添加SceneLoaderProxy组件");
            SceneLoaderProxy sceneLoader = teleportPoint.AddComponent<SceneLoaderProxy>();
            RFH.SetFieldValue(sceneLoader, "sceneID", Utils.getMainScene(sceneID)!);
            RFH.SetFieldValue(sceneLoader, "useLocation", true);
            RFH.SetFieldValue(sceneLoader, "location", new MultiSceneLocation
            {
                SceneID = sceneID,
                LocationName = locationName
            });
            RFH.SetFieldValue(sceneLoader, "notifyEvacuation", false);
            RFH.SetFieldValue(sceneLoader, "hideTips", false);
            RFH.SetFieldValue(sceneLoader, "overrideCurtainScene", null!);

            Debug.Log($"{Constant.LogPrefix} 添加回调");
            interactableBase.OnInteractFinishedEvent.AddListener((character, interactable) => sceneLoader.LoadScene());

            Debug.Log($"{Constant.LogPrefix} 注册到场景");
            if (MultiSceneCore.ActiveSubScene != null && MultiSceneCore.ActiveSubScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(teleportPoint, MultiSceneCore.ActiveSubScene.Value);
            }
            else
            {
                Debug.LogError($"{Constant.LogPrefix} 注册到场景失败");
                return false;
            }
            return true;
        }
    }
}
