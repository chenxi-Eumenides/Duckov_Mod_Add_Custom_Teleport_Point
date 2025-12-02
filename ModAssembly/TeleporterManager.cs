using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Duckov.Scenes;

namespace Add_Custom_Teleport_Point
{
    // 传送点管理器
    public static class TeleporterManager
    {
        // 存储已经创建的传送点
        private static List<GameObject> createdTeleportPoint = new List<GameObject>();
        // 存储已经注册的传送点配置
        private static List<TeleportConfig> registeredConfig = new List<TeleportConfig>();
        // 存储总的传送点配置注册数量，配置ID，防止名称相同
        private static int maxRegisteredConfig = 0;

        // 创建传送点入口
        private static bool createTeleportPoint(TeleportConfig config)
        {
            // 未注册时，configID为 -1
            if (config.configID < 0)
            {
                ModLogger.LogWarning("传入的config未注册");
                return false;
            }
            try
            {
                // 创建传送点
                if (!createTpPoint(config)) return false;
                // 打印日志
                ModLogger.Log($"创建成功！传送点 TeleportPoint_{config.configID}: {config.sourceSceneId}({config.sourcePosition}) => {config.targetSceneId}({config.targetPosition})");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.LogError($"创建失败！报错信息: {ex.Message}");
                ModLogger.LogError($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }

        // 创建传送点实例对象
        private static bool createTpPoint(TeleportConfig config)
        {
            // 创建空游戏对象
            GameObject teleportPoint = new GameObject();
            teleportPoint.transform.position = config.sourcePosition;
            CustomTeleporter CustomTeleporter = teleportPoint.AddComponent<CustomTeleporter>();
            CustomTeleporter.Initialize(
                createdTeleportPoint.Count, config.interactName,
                config.sourceSceneId, config.sourcePosition,
                config.targetSceneId, config.targetPosition
            );
            if (!CustomTeleporter.isValidTeleporter()) return false;

            // 注册到场景系统
            if (MultiSceneCore.MainScene != null && MultiSceneCore.MainScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(teleportPoint, MultiSceneCore.MainScene.Value);
            }
            else
            {
                ModLogger.LogError($"注册到场景系统失败");
                return false;
            }
            // 添加到已创建列表
            createdTeleportPoint.Add(teleportPoint);
            return true;
        }

        // 构造传送点配置
        public static int registerTeleportPoint(
            string sourceSceneId, Vector3 sourcePosition,
            string targetSceneId, Vector3 targetPosition,
            string interactName = Constant.DEFAULT_INTERACT_NAME,
            float interactTime = Constant.DEFAULT_INTERACT_TIME,
            bool backTeleport = false
        )
        {
            int successCount = 0;
            // 注册正向传送点
            TeleportConfig config = new TeleportConfig(
                sourceSceneId: sourceSceneId,
                sourcePosition: sourcePosition,
                targetSceneId: targetSceneId,
                targetPosition: targetPosition,
                interactName: interactName,
                interactTime: interactTime
            );
            if (isVaildConfig(config))
            {
                registerTeleportPointConfig(config);
                successCount++;
            }
            if (backTeleport)
            {
                // 注册反向传送点
                TeleportConfig backConfig = new TeleportConfig(
                    sourceSceneId: targetSceneId,
                    sourcePosition: targetPosition,
                    targetSceneId: sourceSceneId,
                    targetPosition: sourcePosition,
                    interactName: interactName + "-返回",
                    interactTime: interactTime
                );
                if (isVaildConfig(backConfig))
                {
                    registerTeleportPointConfig(backConfig);
                    successCount++;
                }
            }
            return successCount;
        }

        // 注册传送点配置
        private static void registerTeleportPointConfig(TeleportConfig config)
        {
            maxRegisteredConfig += 1;
            config.configID = maxRegisteredConfig;
            registeredConfig.Add(config);
        }

        // 移除所有已创建传送点
        public static void RemoveCreatedTeleportPoint()
        {
            foreach (GameObject teleportPoint in createdTeleportPoint)
            {
                if (teleportPoint != null)
                {
                    UnityEngine.Object.Destroy(teleportPoint);
                }
            }
            createdTeleportPoint.Clear();
            ModLogger.Log("已经移除所有传送点");
        }

        public static bool CancelTeleportPointConfig(int configID)
        {
            foreach (var config in registeredConfig)
            {
                if (config.configID == configID)
                {
                    registeredConfig.Remove(config);
                    return true;
                }
            }
            return false;
        }

        // 检查是否是可注册的传送点配置
        private static bool isVaildConfig(TeleportConfig checkConfig)
        {
            if (string.IsNullOrEmpty(checkConfig.sourceSceneId)) return false;
            if (string.IsNullOrEmpty(checkConfig.targetSceneId)) return false;
            foreach (var config in registeredConfig)
            {
                if (
                    checkConfig.sourceSceneId == config.sourceSceneId &&
                    checkConfig.sourcePosition == config.sourcePosition &&
                    checkConfig.targetSceneId == config.targetSceneId &&
                    checkConfig.targetPosition == config.targetPosition
                ) return false;
            }

            return true;
        }

        // 场景加载回调，创建传送点
        // 需要等 MultiSceneCore.ActiveSubSceneID 非空时创建
        // 所以需要注意时间
        // 目前在 LevelManager.OnAfterLevelInitialized
        // 和 MultiSceneCore.OnSubSceneLoaded 时创建
        public static void Init()
        {
            RemoveCreatedTeleportPoint();
            if (MultiSceneCore.ActiveSubSceneID == null)
            {
                ModLogger.LogWarning("ActiveSubSceneID 为 null, 无法创建传送点");
                return;
            }
            ModLogger.Log($"场景已加载{MultiSceneCore.ActiveSubSceneID}，已注册传送点{registeredConfig.Count}个，准备创建传送点");

            foreach (var config in registeredConfig)
            {
                if (MultiSceneCore.ActiveSubSceneID == config.sourceSceneId)
                {
                    if (!createTeleportPoint(config)) ModLogger.LogError($"传送点({config.interactName})创建失败");
                }
            }
        }
    }
}
