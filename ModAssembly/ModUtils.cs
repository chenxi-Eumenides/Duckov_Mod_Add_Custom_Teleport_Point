using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using Duckov.Quests;
using Duckov.Scenes;
using Duckov.Utilities;
using ItemStatsSystem;
using Eflatun.SceneReference;

// 没有使用，测试残留
using System.IO;
using System.Linq;
using Duckov;
using Duckov.Economy;
using Duckov.UI;
using Duckov.UI.Animations;
using ItemStatsSystem.Items;
using UnityEngine.EventSystems;


namespace Add_Custom_Teleport_Point
{
    // 包含了模组内部创建传送点所需的场景信息
    public struct SceneInfo
    {
        private string sceneID;
        private Vector3 position;
        private string locationName;

        private SceneInfoEntry scene;
        private MultiSceneLocation location;
        private bool isValid;


        public string SceneID => sceneID;
        public Vector3 Position => position;
        public string LocationName => locationName;
        public SceneInfoEntry Scene => scene;
        public MultiSceneLocation Location => location;
        public bool IsValid => isValid;
        public SceneReference? SceneReference => Scene != null ? Scene.SceneReference : null;
        public string? mainSceneID => Utils.getMainScene(SceneID);

        public bool useLocation = true;
        public bool clickToConinue = false;
        public bool notifyEvacuation = false;
        public bool doCircleFade = true;
        public bool saveToFile = true;
        public bool hideTips = false;

        public SceneInfo(string id, Vector3 pos, string name)
        {
            sceneID = id;
            position = pos;
            locationName = name;
            scene = SceneInfoCollection.GetSceneInfo(sceneID);
            location = new MultiSceneLocation
            {
                SceneID = sceneID,
                LocationName = locationName
            };
            isValid = CheckInfo();
            ModLocation.AddCustomLocation(sceneID, locationName, position);
        }
        private bool CheckInfo()
        {
            if (string.IsNullOrEmpty(sceneID)) return false;
            if (Position == null) return false;
            if (string.IsNullOrEmpty(locationName)) return false;
            if (Scene == null) return false;
            if (SceneReference == null) return false;
            if (string.IsNullOrEmpty(mainSceneID)) return false;
            return true;
        }
    }

    // 包含了用户创建传送点需要提供的信息
    public class TeleportConfig
    {
        public int configID = -1;
        public string sourceSceneId;
        public Vector3 sourcePosition;
        public string targetSceneId;
        public Vector3 targetPosition;
        public string interactName = Constant.DEFAULT_INTERACT_NAME;
        public float interactTime = Constant.DEFAULT_INTERACT_TIME;

        public TeleportConfig(
            string sourceSceneId, Vector3 sourcePosition,
            string targetSceneId, Vector3 targetPosition,
            string interactName = Constant.DEFAULT_INTERACT_NAME,
            float interactTime = Constant.DEFAULT_INTERACT_TIME
        )
        {
            this.sourceSceneId = sourceSceneId;
            this.sourcePosition = sourcePosition;
            this.targetSceneId = targetSceneId;
            this.targetPosition = targetPosition;
            this.interactName = interactName;
            this.interactTime = interactTime;
        }
    }

    // 模组传送点自定义存储
    public static class ModLocation
    {
        public static Dictionary<(string, string), Vector3> ModLocations = new Dictionary<(string, string), Vector3>();

        // 注册位置
        // locationName在原版需要不重复
        // 这里只需要 sceneID 和 locationName 都不重复
        public static void AddCustomLocation(string sceneID, string locationName, Vector3 position)
        {
            ModLocations[(sceneID, locationName)] = position;
        }

        // 获取位置
        // 在下面的harmony动态注入中使用
        public static bool TryGetLocation(string sceneID, string locationName, out Transform tf)
        {
            bool result = ModLocations.TryGetValue((sceneID, locationName), out Vector3 position);
            GameObject obj = new GameObject($"ModLocation_{sceneID}_{locationName}");
            obj.transform.position = position;
            tf = obj.transform;
            return result;
        }
    }

    // 一些工具函数
    public static class Utils
    {
        // 根据子场景名称获取主场景名称，主要用于判断是否属于统一关卡
        // 由于不知道有没有其他用处，没有写成判断两场景id是否为同一关
        public static string? getMainScene(string sceneID)
        {
            switch (sceneID)
            {
                case Constant.SCENE_ID_BASE: return Constant.MAIN_SCENE_ID_BASE;
                case Constant.SCENE_ID_BASE_2: return Constant.MAIN_SCENE_ID_BASE;
                case Constant.SCENE_ID_GROUNDZERO: return Constant.MAIN_SCENE_ID_GROUNDZERO;
                case Constant.SCENE_ID_GROUNDZERO_CAVE: return Constant.MAIN_SCENE_ID_GROUNDZERO;
                case Constant.SCENE_ID_HIDDENWAREHOUSE: return Constant.MAIN_SCENE_ID_HIDDENWAREHOUSE;
                case Constant.SCENE_ID_HIDDENWAREHOUSE_UNDER: return Constant.MAIN_SCENE_ID_HIDDENWAREHOUSE;
                case Constant.SCENE_ID_FARM: return Constant.MAIN_SCENE_ID_FARM;
                case Constant.SCENE_ID_FARM_JLAB_FACILITY: return Constant.MAIN_SCENE_ID_FARM;
                case Constant.SCENE_ID_JLAB_1: return Constant.MAIN_SCENE_ID_JLAB;
                case Constant.SCENE_ID_JLAB_2: return Constant.MAIN_SCENE_ID_JLAB;
                case Constant.SCENE_ID_STORM: return Constant.MAIN_SCENE_ID_STORM;
                case Constant.SCENE_ID_STORM_B0: return Constant.MAIN_SCENE_ID_STORM;
                case Constant.SCENE_ID_STORM_B1: return Constant.MAIN_SCENE_ID_STORM;
                case Constant.SCENE_ID_STORM_B2: return Constant.MAIN_SCENE_ID_STORM;
                case Constant.SCENE_ID_STORM_B3: return Constant.MAIN_SCENE_ID_STORM;
                case Constant.SCENE_ID_STORM_B4: return Constant.MAIN_SCENE_ID_STORM;
                default: return null;
            }
        }

        // 测试用
        // 打印所有的 SceneInfoEntry 包含了所有的游玩场景（包括测试场景）
        // 不包括加载界面场景，如撤离界面、黑屏过场界面等
        public static void PrintAllSceneInfo()
        {
            foreach (SceneInfoEntry SceneInfoEntry in SceneInfoCollection.Entries)
            {
                int buildIndex = SceneInfoEntry.BuildIndex;
                // if (buildIndex == -1) continue;
                string description = SceneInfoEntry.Description;
                string displayName = SceneInfoEntry.DisplayName;
                string displayNameRaw = SceneInfoEntry.DisplayNameRaw;
                string ID = SceneInfoEntry.ID;
                bool isLoaded = SceneInfoEntry.IsLoaded;
                Eflatun.SceneReference.SceneReference sceneReference = SceneInfoEntry.SceneReference;

                ModLogger.Log($"SceneInfoEntry : buildIndex={buildIndex} | description={description} | displayName={displayName} | displayNameRaw={displayNameRaw} | ID={ID} | isLoaded={isLoaded}");

                if (sceneReference.State == Eflatun.SceneReference.SceneReferenceState.Addressable)
                {
                    try
                    {
                        string srAddress = sceneReference.Address;
                        int srBuildIndex = sceneReference.BuildIndex;
                        string srGuid = sceneReference.Guid;
                        Scene srLoadedScene = sceneReference.LoadedScene;
                        string srName = sceneReference.Name;
                        string srPath = sceneReference.Path;
                        Eflatun.SceneReference.SceneReferenceState srState = sceneReference.State;
                        Eflatun.SceneReference.SceneReferenceUnsafeReason srUnsafeReason = sceneReference.UnsafeReason;

                        ModLogger.Log($"sceneReference : srAddress={srAddress} | srBuildIndex={srBuildIndex} | srGuid={srGuid} | srLoadedScene={srLoadedScene} | srName={srName} | srPath={srPath} | srState={srState} | srUnsafeReason={srUnsafeReason}");
                    }
                    catch (Exception ex)
                    {
                        ModLogger.LogError($"{ex.Message}");
                    }
                }
            }
        }

        // 测试用
        // 打印所有已加载的 InteractableBase 基类，搜索源文件可以看到具体有哪些种类。
        public static void PrintInteractableBases()
        {
            var instance = ModBehaviour.Instance;
            InteractableBase[] interactableBases = RFH.InvokePrivateMethod<InteractableBase[]>(instance!, "FindObjectsOfType", [])!;
            // instance.FindObjectsOfType<InteractableBase>();
            if (interactableBases == null) return;
            foreach (InteractableBase interactableBase in interactableBases)
            {
                interactableBase.OnInteractStartEvent.AddListener((CharacterMainControl character, InteractableBase interactable) =>
                {
                    var gameObj = interactableBase.gameObject;
                    ModLogger.Log($"name={gameObj.name}");
                    var components = gameObj.GetComponents<InteractableBase>();
                    if (components == null) return;
                    foreach (var component in components)
                    {
                        ModLogger.Log($"name={component.name} type={component.GetType()}");
                    }
                });
            }
        }

        // 测试用
        // 打印正在交互的物体
        public static void PrintInteractItem()
        {
            // 获取主玩家实例
            CharacterMainControl mainCharacter = CharacterMainControl.Main;
            // 获取当前交互的目标
            if (mainCharacter != null && mainCharacter.interactAction.Running)
            {
                InteractableBase currentInteractTarget = mainCharacter.interactAction.InteractingTarget;
                if (currentInteractTarget != null)
                {
                    // 当前正在与 currentInteractTarget 交互
                    ModLogger.Log($"玩家正在与 {currentInteractTarget.name} 交互");
                }
            }
        }

        // 测试用
        // 根据出发场景和目标场景，返回中间的加载过场场景
        // 因为 GameplayDataSettings.SceneManagement 中定义的场景，实际都用不到。
        // 实际用到的场景，暂时没找到方式静态获取，也不知道哪里定义的。
        // 所以目前该函数没用
        public static SceneReference getLoadScene(string sID, string tID)
        {
            var baseScene = GameplayDataSettings.SceneManagement.BaseScene;
            var escapeScene = GameplayDataSettings.SceneManagement.EvacuateScreenScene;
            var prologueScene = GameplayDataSettings.SceneManagement.PrologueScene;
            var failLoadScene = GameplayDataSettings.SceneManagement.FailLoadingScreenScene;
            var mainScene = GameplayDataSettings.SceneManagement.MainMenuScene;
            if (sID == Constant.SCENE_ID_BASE || sID == Constant.SCENE_ID_BASE_2)
                return prologueScene;
            else if (tID == Constant.SCENE_ID_BASE || tID == Constant.SCENE_ID_BASE_2)
                return escapeScene;
            else
                return baseScene;
        }
    }

    // 用于定义自定义事件，一次性将回调函数添加到需要的位置
    // 目前只用于添加创建传送点的函数 TeleporterManager.InitEvent
    public class customEvent
    {
        public static event Action? onSceneLoad
        {
            add
            {
                if (value != null)
                {
                    LevelManager.OnAfterLevelInitialized += value;
                    MultiSceneCore.OnSubSceneLoaded += (core, scene) => { value(); };
                }
            }
            remove
            {
                if (value != null)
                {
                    LevelManager.OnAfterLevelInitialized -= value;
                    MultiSceneCore.OnSubSceneLoaded -= (core, scene) => { value(); };
                }
            }
        }
    }

    // 用于模组传送位置的查询，用于兼容游戏原生的场景加载函数
    [HarmonyPatch(typeof(MultiSceneLocation), "GetLocationTransform")]
    public class MultiSceneLocation_GetLocationTransform_Patch
    {
        static MultiSceneLocation_GetLocationTransform_Patch()
        {
            ModLogger.Log($"已注入 MultiSceneLocation 对象的 GetLocationTransform 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref MultiSceneLocation __instance, ref Transform __result)
        {
            // 如果在模组中注册，就走模组的查询函数
            if (ModLocation.TryGetLocation(__instance.SceneID, __instance.LocationName, out Transform tf))
            {
                if (tf != null)
                {
                    __result = tf;
                    return false; // 跳过原游戏实现
                }
                ModLogger.LogError($"ModLocation.TryGetLocation fail, return : {tf}");
                return true; // 继续执行原方法
            }
            return true; // 继续执行原方法
        }
    }

    // 场景加载时，若 otherInterablesInGroup 为null，会报错
    // 添加了null的判断
    // 正常应该为 otherInterablesInGroup 初始化，但我测试时先尝试了这个，不报错后没有改动
    [HarmonyPatch(typeof(InteractableBase), "Awake")]
    public class InteractableBase_Awake_Patch
    {
        static InteractableBase_Awake_Patch()
        {
            ModLogger.Log($"动态注入 InteractableBase 对象的 Awake 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref InteractableBase __instance)
        {
            // 增加 otherInterablesInGroup 为null的检测，防止报错。
            if (__instance.gameObject.name.StartsWith("CrossLevelTeleportPoint_"))
            {
                Vector3 vector = __instance.transform.position * 10f;
                int x = Mathf.RoundToInt(vector.x);
                int y = Mathf.RoundToInt(vector.y);
                int z = Mathf.RoundToInt(vector.z);
                Vector3Int vector3Int = new Vector3Int(x, y, z);
                RFH.SetFieldValue(__instance, "requireItemDataKeyCached", $"Intact_{vector3Int}".GetHashCode());
                if (__instance.interactCollider == null)
                {
                    __instance.interactCollider = __instance.GetComponent<Collider>();
                    if (__instance.interactCollider == null)
                    {
                        __instance.interactCollider = __instance.gameObject.AddComponent<BoxCollider>();
                        __instance.interactCollider.enabled = false;
                    }
                }
                if (__instance.interactCollider != null)
                {
                    __instance.interactCollider.gameObject.layer = LayerMask.NameToLayer("Interactable");
                }
                var otherInterablesInGroup = RFH.GetFieldValue(__instance, "otherInterablesInGroup") as InteractableBase[];
                if (otherInterablesInGroup != null)
                {
                    foreach (InteractableBase item in otherInterablesInGroup)
                    {
                        if ((bool)item)
                        {
                            item.MarkerActive = false;
                            item.transform.position = __instance.transform.position;
                            item.transform.rotation = __instance.transform.rotation;
                            item.interactMarkerOffset = __instance.interactMarkerOffset;
                        }
                    }
                }
                else
                {
                    RFH.SetFieldValue(__instance, "otherInterablesInGroup", new List<InteractableBase>());
                }
                RFH.SetFieldValue(__instance, "_interactbleList", new List<InteractableBase>());
                return false; // 跳过原游戏实现
            }
            return true; // 继续执行原方法
        }
    }

    // 临时解决问题
    // QuestManager.Instance.GetQuestPrefab 会报空指针
    // 添加了null的判断
    // 正常应该让 GetQuestPrefab 能正常获取数据，但我不知道应该让其获取什么
    [HarmonyPatch(typeof(QuestGiver), "AnyQuestAvaliable")]
    public class QuestGiver_AnyQuestAvaliable_Patch
    {
        static QuestGiver_AnyQuestAvaliable_Patch()
        {
            ModLogger.Log($"动态注入 QuestGiver 对象的 AnyQuestAvaliable 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref QuestGiver __instance, ref bool __result)
        {
            try
            {
                var quests = RFH.GetFieldValue(__instance, "PossibleQuests") as IEnumerable<Quest>;
                if (quests == null)
                {
                    __result = false;
                    return false; // 跳过原游戏实现
                }
                foreach (Quest possibleQuest in quests)
                {
                    Quest? res = RFH.InvokePrivateMethod<Quest>(QuestManager.Instance, "GetQuestPrefab", [possibleQuest.ID]) as Quest;
                    if (res != null)
                    {
                        if (res.MeetsPrerequisit())
                        {
                            __result = true;
                            return false; // 跳过原游戏实现
                        }
                    }
                }
                __result = false;
                return false; // 跳过原游戏实现
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"AnyQuestAvaliable 报错 : {ex.Message}");
                return false; // 跳过原游戏实现
            }
        }
    }

    // 未解决问题
    // 跨关卡加载后，该函数会报错
    // LevelManager.LootBoxInventoriesParent 报空对象
    // LevelManager.LootBoxInventories 报空对象
    // 应该在该函数之前，也就是 InteractableLootbox 初始化前，先初始化 LevelConfig 和 LevelManager
    [HarmonyPatch(typeof(InteractableLootbox), "GetOrCreateInventory")]
    public class InteractableLootbox_GetOrCreateInventory_Patch
    {
        static InteractableLootbox_GetOrCreateInventory_Patch()
        {
            ModLogger.Log($"动态注入 InteractableLootbox 对象的 GetOrCreateInventory 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(InteractableLootbox lootBox, ref Inventory __result)
        {
            ModLogger.Log($"GetOrCreateInventory 开始");
            if (LevelConfig.Instance == null)
            {
                ModLogger.Log($"LevelConfig没有实例化，Object.FindFirstObjectByType<LevelConfig>()返回null");
            }
            if (LevelManager.Instance == null)
            {
                ModLogger.Log($"LevelManager没有实例化，Object.FindFirstObjectByType<LevelManager>()返回null");
            }
            try
            {
                if (lootBox == null)
                {
                    if (CharacterMainControl.Main != null)
                    {
                        CharacterMainControl.Main.PopText("ERROR:尝试创建Inventory, 但lootbox是null");
                    }
                    ModLogger.LogError("尝试创建Inventory, 但lootbox是null");
                    __result = null!; // 应该是null的，但为了消除警告，加了!
                    return false; // 跳过原游戏实现
                }

                ModLogger.Log($"检查 LootBoxInventoriesParent");
                var parent = LevelManager.LootBoxInventoriesParent;
                if (parent == null)
                {
                    ModLogger.LogError("LootBoxInventoriesParent 为 null");
                    __result = null!; // 应该是null的，但为了消除警告，加了!
                    return false;
                }

                ModLogger.Log($"检查 Inventories 字典");
                if (InteractableLootbox.Inventories == null)
                {
                    ModLogger.LogError("Inventories 字典为 null");
                    __result = null!; // 应该是null的，但为了消除警告，加了!
                    return false;
                }

                ModLogger.Log($"检查 lootBox 的 key");
                int? keyRes = RFH.InvokePrivateMethod<int>(lootBox, "GetKey");
                if (keyRes == null)
                {
                    __result = null!; // 应该是null的，但为了消除警告，加了!
                    return false; // 跳过原游戏实现
                }
                int key = (int)keyRes;

                ModLogger.Log($"检查Inventories的值");
                if (InteractableLootbox.Inventories.TryGetValue(key, out var value))
                {
                    if (!(value == null))
                    {
                        __result = value;
                        return false; // 跳过原游戏实现
                    }
                    CharacterMainControl.Main.PopText($"Inventory缓存字典里有Key: {key}, 但其对应值为null.重新创建Inventory。");
                    ModLogger.LogError($"Inventory缓存字典里有Key: {key}, 但其对应值为null.重新创建Inventory。");
                }

                ModLogger.Log($"创建游戏对象，初始化属性");
                GameObject obj = new GameObject($"Inventory_{key}");
                obj.transform.SetParent(InteractableLootbox.LootBoxInventoriesParent);
                obj.transform.position = lootBox.transform.position;

                ModLogger.Log($"添加背包组件，初始化属性");
                value = obj.AddComponent<Inventory>();
                value.NeedInspection = lootBox.needInspect;
                InteractableLootbox.Inventories.Add(key, value);
                LootBoxLoader component = lootBox.GetComponent<LootBoxLoader>();
                if ((bool)component && component.autoSetup)
                {
                    component.Setup().Forget();
                }

                ModLogger.Log($"返回背包组件实例");
                __result = value;
                return false; // 跳过原游戏实现
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"GetOrCreateInventory 报错 : {ex.Message}");
                __result = null!; // 应该是null的，但为了消除警告，加了!
                return false; // 跳过原游戏实现
            }
        }
        [HarmonyPostfix]
        static void Postfix(ref Inventory __result)
        {
            ModLogger.Log($"GetOrCreateInventory 结束");
        }
    }

    // 测试用
    // 查看当前加载用了哪个 CurtainScene
    // 这些 scene 不知道哪里创建的，反正都不在 GameplayDataSettings.SceneManagement 中
    [HarmonyPatch(typeof(SceneLoader), "LoadScene", new Type[] { typeof(SceneReference), typeof(SceneReference), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(MultiSceneLocation), typeof(bool), typeof(bool) })]
    public class SceneLoader_LoadScene_Patch
    {
        static SceneLoader_LoadScene_Patch()
        {
            ModLogger.Log($"动态注入 SceneLoader 对象的 LoadScene 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref SceneLoader __instance,
            SceneReference sceneReference, SceneReference overrideCurtainScene,
            bool clickToConinue, bool notifyEvacuation, bool doCircleFade, bool useLocation,
            MultiSceneLocation location, bool saveToFile, bool hideTips,
            ref UniTask __result
        )
        {
            ModLogger.Log($"SceneLoader.LoadScene 开始运行。");
            var dcs = __instance.defaultCurtainScene;
            ModLogger.Log($"defaultCurtainScene ：{dcs.Name} {dcs.Path} {dcs.BuildIndex}");
            var cs = overrideCurtainScene;
            if (cs != null)
            {
                ModLogger.Log($"overrideCurtainScene ：{cs.Name} {cs.Path} {cs.BuildIndex}");
            }

            return true; // 执行原游戏实现
        }
        [HarmonyPostfix]
        static void Postfix(ref SceneLoader __instance, ref UniTask __result)
        {
            ModLogger.Log($"SceneLoader.Instance 返回: {__instance?.ToString() ?? "null"}");
        }
    }

    // 测试用
    // 目前没用
    // 未来可能用到，没删
    [HarmonyPatch(typeof(LevelManager), "InitLevel")]
    public class LevelManager_InitLevel_Patch
    {
        static LevelManager_InitLevel_Patch()
        {
            ModLogger.Log($"动态注入 LevelManager 对象的 InitLevel 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref LevelManager __instance, SceneLoadingContext context)
        {
            ModLogger.Log($"LevelManager.InitLevel 开始运行。");

            return true; // 执行原游戏实现
        }
    }

    // 反射获取私有变量、属性、方法
    // ai写的，我做了一点修改，不好用，但能用
    // GetPrivateProperty 获取私有属性
    // GetPrivateMethod 获取私有方法
    // GetFieldValue 获取私有变量
    // SetFieldValue 设置私有变量的值
    // InvokePrivateMethod 执行私有函数
    public static class RFH
    {
        public static FieldInfo GetPrivateField(Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static PropertyInfo GetPrivateProperty(Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static MethodInfo GetPrivateMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }

        public static object? GetFieldValue(object obj, string fieldName)
        {
            FieldInfo field = GetPrivateField(obj.GetType(), fieldName);
            return field?.GetValue(obj);
        }

        public static void SetFieldValue(object obj, string fieldName, object value)
        {
            FieldInfo field = GetPrivateField(obj.GetType(), fieldName);
            field?.SetValue(obj, value);
        }

        public static void InvokePrivateMethod(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            method.Invoke(obj, finalParameters);
        }

        public static T InvokePrivateMethod<T>(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            return (T)method.Invoke(obj, finalParameters);
        }

        public static UniTask InvokePrivateMethodUniTask(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            var result = method.Invoke(obj, finalParameters);
            if (result is UniTask uniTask) return uniTask;
            else throw new Exception("not UniTask");
        }

        public static UniTask<T?> InvokePrivateMethodUniTask<T>(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = GetPrivateMethod(obj.GetType(), methodName);
            if (method == null) throw new Exception("no method");
            object[] finalParameters = HandleDefaultParameters(method, parameters);
            var result = method.Invoke(obj, finalParameters);
            if (result is UniTask<T?> uniTaskT) return uniTaskT;
            if (result is UniTask uniTask) throw new Exception("not UniTask<T?> instead UniTask");
            else throw new Exception("not UniTask<T?>");
        }

        public static object[] HandleDefaultParameters(MethodInfo method, object[] providedParams)
        {
            ParameterInfo[] methodParams = method.GetParameters();
            // 如果提供的参数数量已经匹配，直接返回
            if (providedParams != null && providedParams.Length == methodParams.Length)
            {
                return providedParams;
            }

            // 创建包含默认值的完整参数数组
            object[] finalParams = new object[methodParams.Length];

            for (int i = 0; i < methodParams.Length; i++)
            {
                if (providedParams != null && i < providedParams.Length)
                {
                    // 使用提供的参数
                    finalParams[i] = providedParams[i];
                }
                else
                {
                    // 使用参数的默认值
                    if (methodParams[i].DefaultValue != DBNull.Value)
                    {
                        finalParams[i] = methodParams[i].DefaultValue;
                    }
                    else
                    {
                        // 没有默认值，使用类型默认值
                        finalParams[i] = GetDefaultValue(methodParams[i].ParameterType)!;
                    }
                }
            }

            return finalParams;
        }

        public static object? GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool HasMethod(Type type, string methodName)
        {
            return type.GetMethod(methodName, Type.EmptyTypes) != null;
        }
    }
}
