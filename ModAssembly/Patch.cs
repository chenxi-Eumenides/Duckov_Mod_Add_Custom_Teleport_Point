using System;
using System.Collections.Generic;
using HarmonyLib;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Duckov.Quests;
using Duckov.Scenes;
using ItemStatsSystem;
using Eflatun.SceneReference;

namespace Add_Custom_Teleport_Point
{
    // 用于模组传送位置的查询，用于兼容游戏原生的场景加载函数
    [HarmonyPatch(typeof(MultiSceneLocation), "GetLocationTransform")]
    public class MultiSceneLocation_GetLocationTransform_Patch
    {
        static MultiSceneLocation_GetLocationTransform_Patch()
        {
            Debug.Log($"{Constant.LogPrefix} 已注入 MultiSceneLocation 对象的 GetLocationTransform 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref MultiSceneLocation __instance, ref Transform __result)
        {
            // 如果在模组中注册，就走模组的查询函数
            if (CustomLocation.TryGetLocation(__instance.SceneID, __instance.LocationName, out Transform tf))
            {
                if (tf != null)
                {
                    __result = tf;
                    return false; // 跳过原游戏实现
                }
                Debug.LogError($"{Constant.LogPrefix} ModLocation.TryGetLocation fail, return : {tf}");
                return true; // 继续执行原方法
            }
            return true; // 继续执行原方法
        }
    }

    // 场景加载时，若 otherInterablesInGroup 为null，会报错
    // 增加 otherInterablesInGroup 为null的检测，防止报错。
    [HarmonyPatch(typeof(InteractableBase), "Awake")]
    public class InteractableBase_Awake_Patch
    {
        static InteractableBase_Awake_Patch()
        {
            Debug.Log($"{Constant.LogPrefix} 动态注入 InteractableBase 对象的 Awake 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref InteractableBase __instance)
        {
            // 增加 otherInterablesInGroup 为null的检测，防止报错。
            if (__instance.gameObject.name.StartsWith($"{Constant.CustomTeleporterPrefix}_")
                // || __instance.gameObject.name.EndsWith("_test")
            )
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
            else if (__instance.gameObject.name.EndsWith("_test"))
            {
                var otherInterablesInGroup = RFH.GetFieldValue(__instance, "otherInterablesInGroup") as InteractableBase[];
                Debug.Log($"{Constant.LogPrefix} {__instance.gameObject.name} : {otherInterablesInGroup?.Length}");
                RFH.SetFieldValue(__instance, "otherInterablesInGroup", new List<InteractableBase>());
                return true; // 继续执行原方法
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
            Debug.Log($"{Constant.LogPrefix} 动态注入 QuestGiver 对象的 AnyQuestAvaliable 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref QuestGiver __instance, ref bool __result)
        {
            Debug.Log($"{Constant.LogPrefix} AnyQuestAvaliable 开始");
            return true;
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
            Debug.Log($"{Constant.LogPrefix} 动态注入 InteractableLootbox 对象的 GetOrCreateInventory 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(InteractableLootbox lootBox, ref Inventory __result)
        {
            Debug.Log($"{Constant.LogPrefix} GetOrCreateInventory 开始");
            if (LevelConfig.Instance == null)
            {
                Debug.LogError($"{Constant.LogPrefix} LevelConfig没有实例化，Object.FindFirstObjectByType<LevelConfig>()返回null");
                // return false; // 跳过原游戏实现
            }
            if (LevelManager.Instance == null)
            {
                Debug.LogError($"{Constant.LogPrefix} LevelManager没有实例化，Object.FindFirstObjectByType<LevelManager>()返回null");
                // return false; // 跳过原游戏实现
            }
            if (LevelManager.LootBoxInventories == null)
            {
                Debug.LogError($"{Constant.LogPrefix} LootBoxInventories 为 null");
                // return false; // 跳过原游戏实现
            }
            if (LevelManager.LootBoxInventoriesParent == null)
            {
                Debug.LogError($"{Constant.LogPrefix} LootBoxInventoriesParent 为 null");
                // return false; // 跳过原游戏实现
            }
            return true; // 继续原游戏实现
        }
        [HarmonyPostfix]
        static void Postfix(ref Inventory __result)
        {
            // Debug.Log($"{Constant.LogPrefix} GetOrCreateInventory 结束");
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
            Debug.Log($"{Constant.LogPrefix} 动态注入 SceneLoader 对象的 LoadScene 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref SceneLoader __instance,
            SceneReference sceneReference, SceneReference overrideCurtainScene,
            bool clickToConinue, bool notifyEvacuation, bool doCircleFade, bool useLocation,
            MultiSceneLocation location, bool saveToFile, bool hideTips,
            ref UniTask __result
        )
        {
            // Debug.Log($"{Constant.LogPrefix} SceneLoader.LoadScene 开始运行。");
            // var dcs = __instance.defaultCurtainScene;
            // Debug.Log($"{Constant.LogPrefix} defaultCurtainScene ：{dcs.Name} {dcs.Path} {dcs.BuildIndex}");
            // var cs = overrideCurtainScene;
            // Debug.Log($"{Constant.LogPrefix} overrideCurtainScene ：{cs?.Name} {cs?.Path} {cs?.BuildIndex}");

            return true; // 执行原游戏实现
        }
        [HarmonyPostfix]
        static void Postfix(ref SceneLoader __instance, ref UniTask __result)
        {
            // Debug.Log($"{Constant.LogPrefix} SceneLoader.Instance 返回: {__instance?.ToString() ?? "null"}");
        }
    }

    // 测试用
    // 目前没用
    // 未来可能用到，没删
    [HarmonyPatch(typeof(SceneLoaderProxy), "LoadScene")]
    public class SceneLoaderProxy_LoadScene_Patch
    {
        static SceneLoaderProxy_LoadScene_Patch()
        {
            Debug.Log($"{Constant.LogPrefix} 动态注入 SceneLoaderProxy 对象的 LoadScene 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref SceneLoaderProxy __instance)
        {
            // Debug.Log($"{Constant.LogPrefix} SceneLoaderProxy.LoadScene 开始运行。");

            // if (SceneLoader.Instance == null)
            // {
            //     Debug.LogWarning($"{Constant.LogPrefix} 没找到SceneLoader实例，已取消加载场景");
            //     return false; // 跳过原游戏实现
            // }
            // InputManager.DisableInput(__instance.gameObject);
            // RFH.InvokePrivateMethodUniTask(__instance, "Task").Forget();

            // return false; // 跳过原游戏实现

            return true; // 执行原游戏实现
        }
    }
}
