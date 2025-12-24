using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Duckov.Scenes;

namespace Add_Custom_Teleport_Point
{
    // 自定义传送组件，挂载到实例物体gameobject上
    public class CustomTeleporter : InteractableBase
    {
        private string id = "";
        private bool isInitialized = false;
        private static bool isTeleporting;
        private static float lastTeleportTime;
        private SceneInfo sourceSceneInfo;
        private SceneInfo targetSceneInfo;

        private List<InteractableBase> otherInterablesInGroup = new List<InteractableBase>();

        public string ID => id;
        public bool showMarker = true;
        public Vector3 markerOffset = Vector3.up * 1f;
        public float teleportCooldown = 1f;
        public string displayName => InteractName;
        public bool isCrossLevel => targetSceneInfo.mainSceneID != sourceSceneInfo.mainSceneID;

        public static bool IsTeleporting => isTeleporting;
        public static float LastTeleportTime => lastTeleportTime;

        // 初始化，设置属性
        public void Initialize(int index, List<string> displayContext,
            string sourceSceneID, Vector3 sourcePosition,
            string targetSceneID, Vector3 targetPosition)
        {
            // 设置唯一id和名称
            id = $"{Constant.CustomTeleporterPrefix}_{index}";
            gameObject.name = id;

            // 设置传送点信息
            sourceSceneInfo = new SceneInfo(sourceSceneID, sourcePosition, $"{Constant.CustomTeleporterPrefix}_source_{index}");
            targetSceneInfo = new SceneInfo(targetSceneID, targetPosition + Vector3.up * 0.2f, $"{Constant.CustomTeleporterPrefix}_target_{index}");

            // 设置交互显示名称和本地化
            string UI_context = Constant.DEFAULT_INTERACT_NAME;
            if (displayContext.Count >= 2)
            {
                UI_context = displayContext[0];
                for (int i = 1; i < displayContext.Count; i++)
                {
                    SodaCraft.Localizations.LocalizationManager.SetOverrideText(UI_context, displayContext[i]);
                }
            }
            else if (displayContext.Count == 1)
            {
                UI_context = displayContext[0];
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(UI_context, UI_context);
            }
            else
            {
                SodaCraft.Localizations.LocalizationManager.SetOverrideText(UI_context, UI_context);
            }
            InteractName = UI_context;

            // 设置其他属性
            transform.position = sourceSceneInfo.Position;
            interactMarkerOffset = markerOffset;
            MarkerActive = showMarker;
            var collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 2f, 1f);
            collider.isTrigger = true;
            collider.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Interactable");

            // 激活
            isInitialized = true;
            Awake();
        }
        public void Initialize(int index, string displayContext,
            string sourceSceneID, Vector3 sourcePosition,
            string targetSceneID, Vector3 targetPosition)
        {
            Initialize(index, new List<string> { displayContext, displayContext }, sourceSceneID, sourcePosition, targetSceneID, targetPosition);
        }

        // 先初始化，再Awake
        // InteractableBase 的 Awake 有可能需要某些属性，否则会报错
        // 所以延后 Awake 的事件
        protected override void Awake()
        {
            if (!isInitialized) return;
            base.Awake();
        }

        // 交互开始时的处理
        protected override void OnInteractStart(CharacterMainControl character)
        {
            // 设置交互时间和行为
            coolTime = 2f;
            finishWhenTimeOut = true;
            registerCallFuncs();
        }

        // 交互完成时的处理
        protected override void OnInteractFinished()
        {
            if (isTeleporting && isValidTeleporter() && !SceneLoader.IsSceneLoading) return;

            isTeleporting = true;
            lastTeleportTime = Time.time;

            try
            {
                Teleport(sourceSceneInfo, targetSceneInfo).Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 传送失败: {ex.Message}");
                Debug.LogError($"{Constant.LogPrefix} 调用栈: {ex.StackTrace}");
            }
            isTeleporting = false;
            StopInteract();
        }

        // 交互停止时的处理
        protected override void OnInteractStop()
        {
            // 清理状态，允许重新交互
            finishWhenTimeOut = false;
            cancelRegisterCallFuncs();
        }

        // 销毁时的处理
        protected override void OnDestroy()
        {
            isInitialized = false;
            MarkerActive = false;
            GetComponent<BoxCollider>().enabled = true;
            cancelRegisterCallFuncs();
        }

        // 验证是否传传送点有效
        public bool isValidTeleporter()
        {
            return isInitialized &&
                sourceSceneInfo.IsValid &&
                targetSceneInfo.IsValid;
        }

        // 重写交互条件检查 - 确保在可传送状态下才能交互
        protected override bool IsInteractable()
        {
            return !isTeleporting &&
                   Time.time - lastTeleportTime > teleportCooldown &&
                   !SceneLoader.IsSceneLoading &&
                   isValidTeleporter();
        }

        // 核心传送处理
        private async UniTask Teleport(SceneInfo sourceSceneInfo, SceneInfo targetSceneInfo)
        {
            if (isCrossLevel)
            {
                // 不同主场景，进行跨关卡传送
                // 先执行 SceneLoader.LoadScene
                // 再创建 LevelManager
                // 此时 LevelManager 会根据 SceneLoader.IsSceneLoading
                // 执行 InitLevel 或在 onFinishedLoadingScene 阶段执行
                // 目前不可用，缺少必要的组件或属性
                // 目前已知问题：
                // 1. LevelConfig 没有及时创建，导致 LevelManager 没有创建，
                // 导致lootbox获取 LevelManager.LootBoxInventories 和 LootBoxInventoriesParent 失败
                // 2. TimeOfDayController 未及时创建，报空指针、空对象，还未找具体是哪行代码导致的
                // TimeOfDayController 可能需要 TimeOfDayConfig
                InputManager.DisableInput(base.gameObject);
                await SceneLoader.Instance.LoadScene(
                    sceneReference: targetSceneInfo.SceneReference,
                    location: targetSceneInfo.Location,
                    useLocation: targetSceneInfo.useLocation,
                    clickToConinue: targetSceneInfo.clickToConinue,
                    notifyEvacuation: targetSceneInfo.notifyEvacuation,
                    doCircleFade: targetSceneInfo.doCircleFade,
                    saveToFile: targetSceneInfo.saveToFile,
                    hideTips: targetSceneInfo.hideTips
                );
            }
            else
            {
                // 如果是相同的主场景，就执行同关卡传送
                // 可以传送到相同主场景下的子场景
                // 比如 零号区是 Level_GroundZero_main 下的 Level_GroundZero_1
                // 零号区山洞是 Level_GroundZero_main 下的 Level_GroundZero_Cave
                // 目前可以正常运行，没有问题
                await MultiSceneCore.Instance.LoadAndTeleport(targetSceneInfo.Location);
            }
        }

        // 注册各种回调函数
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

        // 注销各种回调函数
        // 暂时不知道在哪注销比较合适
        // 这7个的具体时机，请看 SceneLoader.LoadScene 以及 LevelManager.InitLevel
        private void cancelRegisterCallFuncs()
        {
            SceneLoader.onStartedLoadingScene -= onStartedLoadingScene;
            SceneLoader.onFinishedLoadingScene -= onFinishedLoadingScene;
            SceneLoader.onBeforeSetSceneActive -= onBeforeSetSceneActive;
            SceneLoader.onAfterSceneInitialize -= onAfterSceneInitialize;
            LevelManager.OnLevelBeginInitializing -= OnLevelBeginInitializing;
            LevelManager.OnLevelInitialized -= OnLevelInitialized;
            LevelManager.OnAfterLevelInitialized -= OnAfterLevelInitialized;
        }

        private void onStartedLoadingScene(SceneLoadingContext context)
        {
        }

        private void onBeforeSetSceneActive(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onBeforeSetSceneActive: 开始场景初始化准备");

            // 确保LevelConfig存在并初始化
            // EnsureGameCoreComponents();
        }

        private void onFinishedLoadingScene(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onFinishedLoadingScene: 场景加载完成");

            // 场景加载完成后，再次检查核心组件
            // PostLoadComponentCheck();
        }

        private void onAfterSceneInitialize(SceneLoadingContext context)
        {
            Debug.Log($"{Constant.LogPrefix} onAfterSceneInitialize: 场景初始化完成");

            // 最终验证所有组件
            // FinalComponentValidation();
        }

        // 核心辅助方法：确保游戏核心组件存在并初始化
        private void EnsureGameCoreComponents()
        {
            Debug.Log($"{Constant.LogPrefix} 确保游戏核心组件初始化...");

            // 1. 确保LevelConfig存在
            EnsureLevelConfigExists();

            // 2. 确保LevelManager被创建
            EnsureLevelManagerCreated();

            // 3. 确保TimeOfDayConfig存在
            EnsureTimeOfDayConfigExists();

            // 4. 确保TimeOfDayController有正确引用
            EnsureTimeOfDayControllerLinked();

            Debug.Log($"{Constant.LogPrefix} 游戏核心组件初始化完成");
        }

        // 确保LevelConfig存在
        private void EnsureLevelConfigExists()
        {
            if (LevelConfig.Instance == null)
            {
                Debug.Log($"{Constant.LogPrefix} 创建LevelConfig...");

                // 创建LevelConfig GameObject
                GameObject levelConfigObj = new GameObject("LevelConfig");
                LevelConfig levelConfig = levelConfigObj.AddComponent<LevelConfig>();

                // 调用Awake方法以创建LevelManager
                try
                {
                    RFH.InvokePrivateMethod(levelConfig, "Awake");
                    Debug.Log($"{Constant.LogPrefix} LevelConfig.Awake()调用成功");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Constant.LogPrefix} 调用LevelConfig.Awake()失败: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} LevelConfig已存在: {LevelConfig.Instance.gameObject.name}");
            }
        }

        // 确保LevelManager被创建
        private void EnsureLevelManagerCreated()
        {
            if (LevelManager.Instance == null)
            {
                Debug.Log($"{Constant.LogPrefix} 等待LevelManager创建...");

                // 检查LevelConfig下是否有LevelManager
                if (LevelConfig.Instance != null)
                {
                    LevelManager manager = LevelConfig.Instance.GetComponentInChildren<LevelManager>();
                    if (manager != null)
                    {
                        Debug.Log($"{Constant.LogPrefix} 在LevelConfig下找到LevelManager: {manager.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"{Constant.LogPrefix} LevelConfig下未找到LevelManager");
                        // 尝试手动查找
                        manager = FindAnyObjectByType<LevelManager>();
                        if (manager != null)
                        {
                            Debug.Log($"{Constant.LogPrefix} 在场景中找到LevelManager: {manager.gameObject.name}");
                        }
                    }
                }

                // 如果仍然没有找到，记录错误
                if (LevelManager.Instance == null)
                {
                    Debug.LogError($"{Constant.LogPrefix} LevelManager未创建，游戏可能无法正常运行");
                }
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} LevelManager已存在: {LevelManager.Instance.gameObject.name}");
            }
        }

        // 确保TimeOfDayConfig存在
        private void EnsureTimeOfDayConfigExists()
        {
            // 查找场景中所有的TimeOfDayConfig
            TimeOfDayConfig[] configs = FindObjectsOfType<TimeOfDayConfig>();

            if (configs.Length == 0)
            {
                Debug.Log($"{Constant.LogPrefix} 创建TimeOfDayConfig...");

                // 创建TimeOfDayConfig
                GameObject configObj = new GameObject("TimeOfDayConfig");
                TimeOfDayConfig config = configObj.AddComponent<TimeOfDayConfig>();

                // 设置父对象
                if (LevelConfig.Instance != null)
                {
                    configObj.transform.SetParent(LevelConfig.Instance.transform);
                    Debug.Log($"{Constant.LogPrefix} TimeOfDayConfig已附加到LevelConfig");
                }

                // 设置默认值
                try
                {
                    RFH.SetFieldValue(config, "forceSetTime", false);
                    RFH.SetFieldValue(config, "forceSetWeather", false);
                    Debug.Log($"{Constant.LogPrefix} TimeOfDayConfig默认值设置完成");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Constant.LogPrefix} 设置TimeOfDayConfig默认值失败: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} 找到{configs.Length}个TimeOfDayConfig实例");

                // 确保至少一个TimeOfDayConfig被LevelConfig引用
                if (LevelConfig.Instance != null && LevelConfig.Instance.timeOfDayConfig == null)
                {
                    LevelConfig.Instance.timeOfDayConfig = configs[0];
                    Debug.Log($"{Constant.LogPrefix} 已将TimeOfDayConfig分配给LevelConfig");
                }
            }
        }

        // 确保TimeOfDayController有正确引用
        private void EnsureTimeOfDayControllerLinked()
        {
            if (LevelManager.Instance == null) return;

            var timeOfDayController = LevelManager.Instance.TimeOfDayController;

            if (timeOfDayController == null)
            {
                Debug.Log($"{Constant.LogPrefix} 创建TimeOfDayController...");

                // 创建TimeOfDayController
                GameObject controllerObj = new GameObject("TimeOfDayController");
                TimeOfDayController controller = controllerObj.AddComponent<TimeOfDayController>();

                // 附加到LevelManager
                controllerObj.transform.SetParent(LevelManager.Instance.transform);
                Debug.Log($"{Constant.LogPrefix} TimeOfDayController已附加到LevelManager");

                // 设置config引用
                if (LevelConfig.Instance != null && LevelConfig.Instance.timeOfDayConfig != null)
                {
                    try
                    {
                        RFH.SetFieldValue(controller, "config", LevelConfig.Instance.timeOfDayConfig);
                        Debug.Log($"{Constant.LogPrefix} TimeOfDayController.config已设置");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{Constant.LogPrefix} 设置TimeOfDayController.config失败: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} TimeOfDayController已存在: {timeOfDayController.gameObject.name}");

                // 确保config引用正确
                try
                {
                    var config = RFH.GetFieldValue(timeOfDayController, "config");
                    if (config == null && LevelConfig.Instance != null && LevelConfig.Instance.timeOfDayConfig != null)
                    {
                        RFH.SetFieldValue(timeOfDayController, "config", LevelConfig.Instance.timeOfDayConfig);
                        Debug.Log($"{Constant.LogPrefix} 已修复TimeOfDayController.config引用");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Constant.LogPrefix} 检查TimeOfDayController.config失败: {ex.Message}");
                }
            }
        }

        // 场景加载后的组件检查
        private void PostLoadComponentCheck()
        {
            Debug.Log($"{Constant.LogPrefix} 场景加载后组件检查...");

            // 再次确保核心组件
            EnsureGameCoreComponents();

            // 检查LootBoxInventories
            try
            {
                var parent = LevelManager.LootBoxInventoriesParent;
                if (parent != null)
                {
                    Debug.Log($"{Constant.LogPrefix} LootBoxInventoriesParent存在: {parent.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 检查LootBoxInventories失败: {ex.Message}");
            }

            Debug.Log($"{Constant.LogPrefix} 场景加载后组件检查完成");
        }

        // 最终组件验证
        private void FinalComponentValidation()
        {
            Debug.Log($"{Constant.LogPrefix} 最终组件验证...");

            bool allOk = true;

            // 验证LevelConfig
            if (LevelConfig.Instance == null)
            {
                Debug.LogError($"{Constant.LogPrefix} ❌ LevelConfig缺失");
                allOk = false;
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} ✓ LevelConfig存在");
            }

            // 验证LevelManager
            if (LevelManager.Instance == null)
            {
                Debug.LogError($"{Constant.LogPrefix} ❌ LevelManager缺失");
                allOk = false;
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} ✓ LevelManager存在");

                // 验证关键组件
                if (LevelManager.Instance.TimeOfDayController == null)
                {
                    Debug.LogWarning($"{Constant.LogPrefix} ⚠ TimeOfDayController缺失");
                }
                else
                {
                    Debug.Log($"{Constant.LogPrefix} ✓ TimeOfDayController存在");
                }

                if (LevelManager.Instance.InputManager == null)
                {
                    Debug.LogWarning($"{Constant.LogPrefix} ⚠ InputManager缺失");
                }
                else
                {
                    Debug.Log($"{Constant.LogPrefix} ✓ InputManager存在");
                }
            }

            // 验证TimeOfDayConfig
            TimeOfDayConfig[] configs = FindObjectsOfType<TimeOfDayConfig>();
            if (configs.Length == 0)
            {
                Debug.LogWarning($"{Constant.LogPrefix} ⚠ TimeOfDayConfig缺失");
            }
            else
            {
                Debug.Log($"{Constant.LogPrefix} ✓ 找到{configs.Length}个TimeOfDayConfig");
            }

            if (allOk)
            {
                Debug.Log($"{Constant.LogPrefix} 所有关键组件验证通过");
            }
            else
            {
                Debug.LogWarning($"{Constant.LogPrefix} 部分关键组件有问题，游戏可能无法正常运行");
            }

            Debug.Log($"{Constant.LogPrefix} 最终组件验证完成");
        }

        private void OnLevelBeginInitializing()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelBeginInitializing: 关卡开始初始化");
        }

        private void OnLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnLevelInitialized: 关卡初始化完成");
        }

        private void OnAfterLevelInitialized()
        {
            Debug.Log($"{Constant.LogPrefix} OnAfterLevelInitialized: 关卡后初始化完成");
        }
    }
}
