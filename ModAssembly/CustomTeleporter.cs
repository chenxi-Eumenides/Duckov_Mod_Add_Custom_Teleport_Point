using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Duckov.Scenes;
using Duckov.Utilities;

namespace Add_Custom_Teleport_Point
{
    // 自定义传送组件，挂载到实例物体gameobject上
    public class CustomTeleporter : InteractableBase
    {
        private int id = -1;
        private bool isInitialized = false;
        private static bool isTeleporting;
        private static float lastTeleportTime;
        private SceneInfo sourceSceneInfo;
        private SceneInfo targetSceneInfo;

        private List<InteractableBase> otherInterablesInGroup = new List<InteractableBase>();

        public int ID => id;
        public bool showMarker = true;
        public Vector3 markerOffset = Vector3.up * 1f;
        public float teleportCooldown = 1f;
        public string displayName => InteractName;
        public bool isCrossLevel => targetSceneInfo.MainSceneID != sourceSceneInfo.MainSceneID;
        public bool isCrossSubScene => targetSceneInfo.SceneID != sourceSceneInfo.SceneID;
        public bool Disposable = false;

        public static bool IsTeleporting => isTeleporting;
        public static float LastTeleportTime => lastTeleportTime;

        /// <summary>
        /// 初始化自定义传送点组件，设置所有必要属性
        /// </summary>
        /// <param name="index">传送点的唯一标识符</param>
        /// <param name="displayContext">显示名称的本地化上下文列表，至少包含2个元素时使用第一个作为UI显示名称</param>
        /// <param name="sourceSceneID">源场景ID</param>
        /// <param name="sourcePosition">源位置坐标</param>
        /// <param name="targetSceneID">目标场景ID</param>
        /// <param name="targetPosition">目标位置坐标</param>
        /// <param name="disposable">是否为一次性传送点（使用后销毁）</param>
        public void Initialize(int index, List<string> displayContext,
            string sourceSceneID, Vector3 sourcePosition,
            string targetSceneID, Vector3 targetPosition,
            bool disposable = false)
        {
            // 设置唯一id和名称
            id = index;
            gameObject.name = $"{Constant.CustomTeleporterPrefix}_{index}";

            // 设置传送点信息
            sourceSceneInfo = new SceneInfo(sourceSceneID, sourcePosition, $"{Constant.CustomTeleporterPrefix}_{index}_source");
            targetSceneInfo = new SceneInfo(targetSceneID, targetPosition + Vector3.up * 0.2f, $"{Constant.CustomTeleporterPrefix}_{index}_target");

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
            Disposable=disposable;

            // 设置外观，模拟simple teleporter组件
            // 未完全生效
            GameObject r = new GameObject("r");
            r.transform.SetParent(gameObject.transform);
            r.transform.localPosition = Vector3.up;
            r.transform.localRotation = Quaternion.identity;
            r.transform.localScale = Vector3.one;
            // 增加 MeshRenderer 组件
            MeshRenderer renderer = r.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("SodaTeleporter");
            Material material = new Material(shader);
            material.enableInstancing = true;
            material.shaderKeywords = [
                "_DISTORTION_ON",
                "_FADING_ON",
                "_FLIPBOOKBLENDING_OFF",
                "_NORMALMAP",
                "_SOFTPARTICLES_ON",
                "_SURFACE_TYPE_TRANSPARENT"
            ];
            renderer.SetMaterials([material]);
            // 增加 MeshFilter 组件
            MeshFilter filter = r.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();
            mesh.name = "Sphere";
            mesh.RecalculateNormals();
            filter.mesh = mesh;
            // 增加子对象 light
            GameObject l = new GameObject("light");
            l.transform.SetParent(r.transform);
            l.transform.localPosition = Vector3.zero;
            // 增加 Light 组件
            Light light = l.AddComponent<Light>();
            light.color = new Color(0.45f, 0.73f, 1f);
            light.range = 2f;
            light.intensity = 2.61f;

            // 激活
            isInitialized = true;
            Awake();
        }
        /// <summary>
        /// 初始化自定义传送点组件（简化版本）
        /// </summary>
        /// <param name="index">传送点的唯一标识符</param>
        /// <param name="displayContext">显示名称</param>
        /// <param name="sourceSceneID">源场景ID</param>
        /// <param name="sourcePosition">源位置坐标</param>
        /// <param name="targetSceneID">目标场景ID</param>
        /// <param name="targetPosition">目标位置坐标</param>
        /// <param name="disposable">是否为一次性传送点（使用后销毁）</param>
        public void Initialize(int index, string displayContext,
            string sourceSceneID, Vector3 sourcePosition,
            string targetSceneID, Vector3 targetPosition,
            bool disposable = false)
        {
            Initialize(index, new List<string> { displayContext, displayContext }, sourceSceneID, sourcePosition, targetSceneID, targetPosition, disposable);
        }

        /// <summary>
        /// 重写Awake方法，确保在初始化完成后才执行基类Awake
        /// </summary>
        /// <remarks>
        /// InteractableBase的Awake方法可能需要某些属性，否则会报错
        /// 因此延后Awake的执行，直到初始化完成
        /// </remarks>
        protected override void Awake()
        {
            if (!isInitialized) return;
            base.Awake();
        }

        /// <summary>
        /// 当交互开始时调用，设置交互冷却时间和行为
        /// </summary>
        /// <param name="character">触发交互的角色</param>
        protected override void OnInteractStart(CharacterMainControl character)
        {
            // 设置交互时间和行为
            coolTime = 2f;
            finishWhenTimeOut = true;
        }

        /// <summary>
        /// 当交互完成时调用，执行传送逻辑
        /// </summary>
        /// <remarks>
        /// 处理传送逻辑，包括跨关卡、同关卡和同场景传送
        /// 如果是一次性传送点，使用后会被销毁
        /// </remarks>
        protected override void OnInteractFinished()
        {
            if (isTeleporting && isValidTeleporter() && !SceneLoader.IsSceneLoading) return;

            isTeleporting = true;
            lastTeleportTime = Time.time;
            if (Disposable) TeleporterManager.UnRegisterTeleportPointConfig(ID);

            try
            {
                teleport(sourceSceneInfo, targetSceneInfo);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 传送失败: {ex.Message}/n{ex.StackTrace}");
            }
            isTeleporting = false;
            StopInteract();
            if (Disposable) Destroy(gameObject);
        }

        /// <summary>
        /// 当交互停止时调用，清理交互状态
        /// </summary>
        protected override void OnInteractStop()
        {
            // 清理状态，允许重新交互
            finishWhenTimeOut = false;
        }

        /// <summary>
        /// 当组件销毁时调用，清理状态
        /// </summary>
        protected override void OnDestroy()
        {
            isInitialized = false;
            MarkerActive = false;
            GetComponent<BoxCollider>().enabled = true;
        }

        /// <summary>
        /// 验证传送点是否有效
        /// </summary>
        /// <returns>如果传送点已初始化且源场景和目标场景都有效，则返回true</returns>
        public bool isValidTeleporter()
        {
            return isInitialized &&
                sourceSceneInfo.IsValid &&
                targetSceneInfo.IsValid;
        }

        /// <summary>
        /// 重写交互条件检查，确保在可传送状态下才能交互
        /// </summary>
        /// <returns>如果满足所有交互条件则返回true</returns>
        /// <remarks>
        /// 检查条件包括：不在传送中、冷却时间已过、场景未加载、传送点有效
        /// </remarks>
        protected override bool IsInteractable()
        {
            return !isTeleporting &&
                   Time.time - lastTeleportTime > teleportCooldown &&
                   !SceneLoader.IsSceneLoading &&
                   isValidTeleporter();
        }

        /// <summary>
        /// 核心传送处理逻辑
        /// </summary>
        /// <param name="sourceSceneInfo">源场景信息</param>
        /// <param name="targetSceneInfo">目标场景信息</param>
        /// <remarks>
        /// 根据源场景和目标场景的关系，执行不同的传送逻辑：
        /// 1. 不同主场景：跨关卡传送
        /// 2. 相同主场景不同子场景：同关卡传送
        /// 3. 相同子场景：直接设置玩家位置
        /// </remarks>
        private void teleport(SceneInfo sourceSceneInfo, SceneInfo targetSceneInfo)
        {
            if (sourceSceneInfo.MainSceneID != targetSceneInfo.MainSceneID)
            {
                // 不同主场景，进行跨关卡传送
                InputManager.DisableInput(base.gameObject);
                SceneLoader.Instance.LoadScene(
                    sceneReference: targetSceneInfo.MainSceneReference,
                    // overrideCurtainScene: GameplayDataSettings.SceneManagement.PrologueScene,
                    location: targetSceneInfo.Location,
                    useLocation: targetSceneInfo.useLocation,
                    clickToConinue: targetSceneInfo.clickToConinue,
                    notifyEvacuation: targetSceneInfo.notifyEvacuation,
                    doCircleFade: targetSceneInfo.doCircleFade,
                    saveToFile: targetSceneInfo.saveToFile,
                    hideTips: targetSceneInfo.hideTips
                ).Forget();
            }
            else if (sourceSceneInfo.SceneID != targetSceneInfo.SceneID)
            {
                // 相同的主场景，不同子场景，执行同关卡传送
                // 可以传送到相同主场景下的子场景
                // 比如 零号区是 Level_GroundZero_main 下的 Level_GroundZero_1
                // 零号区山洞是 Level_GroundZero_main 下的 Level_GroundZero_Cave
                MultiSceneCore.Instance.LoadAndTeleport(targetSceneInfo.Location).Forget();
            }
            else
            {
                // 相同子场景内，直接传送
                CharacterMainControl mainCharacter = CharacterMainControl.Main;
                if (mainCharacter != null)
                {
                    mainCharacter.SetPosition(targetSceneInfo.Position);
                    if ((bool)LevelManager.Instance)
                    {
                        LevelManager.Instance.GameCamera.ForceSyncPos();
                    }
                }
            }
        }
    }
}
