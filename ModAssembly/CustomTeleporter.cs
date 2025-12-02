using System;
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

        public string ID => id;
        public string displayName = "";
        public bool showMarker = true;
        public Vector3 markerOffset = Vector3.up * 1f;
        public float teleportCooldown = 1f;
        public bool isCrossLevel => targetSceneInfo.mainSceneID != sourceSceneInfo.mainSceneID;

        public static bool IsTeleporting => isTeleporting;
        public static float LastTeleportTime => lastTeleportTime;

        // 初始化，设置属性
        public void Initialize(int index, string displayContext,
            string sourceSceneID, Vector3 sourcePosition,
            string targetSceneID, Vector3 targetPosition)
        {
            id = $"CrossLevelTeleportPoint_{index}";
            gameObject.name = id;
            displayName = displayContext;
            sourceSceneInfo = new SceneInfo(sourceSceneID, sourcePosition, $"CrossLevelTeleportPoint_source_{index}");
            targetSceneInfo = new SceneInfo(targetSceneID, targetPosition + Vector3.up * 0.2f, $"CrossLevelTeleportPoint_target_{index}");

            transform.position = sourceSceneInfo.Position;
            InteractName = displayName;
            interactMarkerOffset = markerOffset;
            MarkerActive = showMarker;
            var collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 2f, 1f);
            collider.isTrigger = true;
            collider.enabled = true;
            gameObject.layer = LayerMask.NameToLayer("Interactable");

            isInitialized = true;
            Awake();
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
                isTeleporting = false;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"传送失败: {ex.Message}");
                ModLogger.LogError($"调用栈: {ex.StackTrace}");
            }
            finally
            {
                StopInteract();
            }
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
            cancelRegisterCallFuncs();
        }

        // 验证是否有效
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

        // 核心处理
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
            // 尝试解决缺少必要的组件或属性问题，未成功
            // 正常关卡的父子关系是
            // LevelConfig -> LevelManager -> TimeOfDayController
            // LevelConfig -> TimeOfDayConfig
            LevelConfig lcObj;
            if (LevelConfig.Instance == null)
            {
                ModLogger.Log($"Start init LevelConfig");
                lcObj = new GameObject("LevelConfig").AddComponent<LevelConfig>();
                RFH.InvokePrivateMethod(lcObj, "Awake"); // 应该会创建 levelmanager
            }
            else
            {
                lcObj = LevelConfig.Instance;
            }
            if (LevelManager.Instance == null)
            {
                ModLogger.Log($"Start init LevelManager");
            }
            else
            {
                var tdControllerObj = new GameObject("TimeOfDayController").AddComponent<TimeOfDayController>();
                tdControllerObj.gameObject.transform.SetParent(LevelManager.Instance.transform);
            }
            if (FindObjectsOfType<TimeOfDayConfig>().Length == 0)
            {
                TimeOfDayConfig tdConfigObj = new GameObject("TimeOfDayConfig").AddComponent<TimeOfDayConfig>();
                tdConfigObj.gameObject.transform.SetParent(lcObj.gameObject.transform);
            }
        }

        private void onFinishedLoadingScene(SceneLoadingContext context)
        {
        }

        private void onAfterSceneInitialize(SceneLoadingContext context)
        {
        }

        private void OnLevelBeginInitializing()
        {
        }

        private void OnLevelInitialized()
        {
        }

        private void OnAfterLevelInitialized()
        {
        }
    }
}
