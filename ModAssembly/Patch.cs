using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Duckov.Scenes;

namespace Add_Custom_Teleport_Point
{
    // 用于模组传送位置的查询，用于兼容游戏原生的场景加载函数
    [HarmonyPatch(typeof(MultiSceneLocation), "GetLocationTransform")]
    public class MultiSceneLocation_GetLocationTransform_Patch
    {
        static MultiSceneLocation_GetLocationTransform_Patch()
        {
            // Debug.Log($"{Constant.LogPrefix} 已注入 MultiSceneLocation 对象的 GetLocationTransform 函数。");
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
            // Debug.Log($"{Constant.LogPrefix} 动态注入 InteractableBase 对象的 Awake 函数。");
        }
        [HarmonyPrefix]
        static bool Prefix(ref InteractableBase __instance)
        {
            // 增加 otherInterablesInGroup 为null的检测，防止报错。
            if (__instance.gameObject.name.StartsWith($"{Constant.CustomTeleporterPrefix}_")
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
                List<InteractableBase>? otherInterablesInGroup = RFH.GetFieldValue(__instance, "otherInterablesInGroup") as List<InteractableBase>;
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
}
