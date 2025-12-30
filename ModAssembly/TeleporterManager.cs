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

        public static void Initialize()
        {
            createdTeleportPoint.Clear();
            registeredConfig.Clear();
            maxRegisteredConfig = 0;
        }

        // 创建传送点入口
        private static bool createTeleportPoint(TeleportConfig config)
        {
            // 未注册时，configID为 -1
            if (config.configID < 0)
            {
                Debug.LogWarning($"{Constant.LogPrefix} 传入的config未注册");
                return false;
            }
            try
            {
                // 创建传送点
                if (!createTpPointCustom(config)) return false;
                // 打印日志
                Debug.Log($"{Constant.LogPrefix} 创建成功！传送点 TeleportPoint_{config.configID}: {config.sourceSceneId}({config.sourcePosition}) => {config.targetSceneId}({config.targetPosition})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 创建失败！报错信息: {ex.Message}");
                Debug.LogError($"{Constant.LogPrefix} 堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }

        // 创建传送点实例对象
        private static bool createTpPointCustom(TeleportConfig config)
        {
            // 创建空游戏对象
            GameObject teleportPoint = new GameObject();
            teleportPoint.transform.position = config.sourcePosition;
            CustomTeleporter CustomTeleporter = teleportPoint.AddComponent<CustomTeleporter>();
            CustomTeleporter.Initialize(
                config.configID, config.interactName,
                config.sourceSceneId, config.sourcePosition,
                config.targetSceneId, config.targetPosition,
                disposable: config.disposable
            );
            if (!CustomTeleporter.isValidTeleporter()) return false;

            // 注册到场景系统
            if (MultiSceneCore.ActiveSubScene != null && MultiSceneCore.ActiveSubScene.HasValue)
            {
                SceneManager.MoveGameObjectToScene(teleportPoint, MultiSceneCore.ActiveSubScene.Value);
            }
            else
            {
                Debug.LogError($"{Constant.LogPrefix} 注册到场景系统失败: {MultiSceneCore.MainScene}");
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
            bool backTeleport = false,
            bool disposable = false
        )
        {
            int successCount = 0;
            // 注册正向传送点
            TeleportConfig config = new TeleportConfig(
                sourceSceneId: sourceSceneId,
                sourcePosition: sourcePosition,
                targetSceneId: targetSceneId,
                targetPosition: targetPosition,
                interactName: disposable ? interactName + " (一次性)" : interactName,
                interactTime: interactTime,
                disposable: disposable
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
                    interactName: disposable ? "[返回] " + interactName + " (一次性)" : "[返回] " + interactName,
                    interactTime: interactTime,
                    disposable: disposable
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
        public static void removeCreatedTeleportPoint()
        {
            foreach (GameObject teleportPoint in createdTeleportPoint)
            {
                if (teleportPoint != null)
                {
                    UnityEngine.Object.Destroy(teleportPoint);
                }
            }
            createdTeleportPoint.Clear();
            Debug.Log($"{Constant.LogPrefix} 已经移除所有传送点");
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

        // 创建传送点
        // 需要等 MultiSceneCore.ActiveSubSceneID 非空时创建
        // 所以需要注意时间
        // 目前在 LevelManager.OnAfterLevelInitialized
        // 和 MultiSceneCore.OnSubSceneLoaded 时创建
        public static void StartCreateCustomTeleportPoint()
        {
            removeCreatedTeleportPoint();
            if (MultiSceneCore.ActiveSubSceneID == null)
            {
                Debug.LogWarning($"{Constant.LogPrefix} ActiveSubSceneID 为 null, 无法创建传送点");
                return;
            }
            Debug.Log($"{Constant.LogPrefix} 场景已加载{MultiSceneCore.ActiveSubSceneID}，已注册传送点{registeredConfig.Count}个，准备创建传送点");

            foreach (var config in registeredConfig)
            {
                if (MultiSceneCore.ActiveSubSceneID == config.sourceSceneId)
                {
                    if (!createTeleportPoint(config)) Debug.LogError($"{Constant.LogPrefix} 传送点({config.interactName})创建失败");
                }
            }
        }

        // 初始化事件回调函数
        private static void OnSubSceneLoaded(MultiSceneCore core, Scene scene)
        {
            StartCreateCustomTeleportPoint();
        }

        // 初始化事件回调函数
        private static void onAfterSceneInitialize(SceneLoadingContext context)
        {
            StartCreateCustomTeleportPoint();
        }

        public static void addCallbackFunc()
        {
            MultiSceneCore.OnSubSceneLoaded += OnSubSceneLoaded;
            SceneLoader.onAfterSceneInitialize += onAfterSceneInitialize;
        }
        public static void removeCallbackFunc()
        {
            MultiSceneCore.OnSubSceneLoaded -= OnSubSceneLoaded;
            SceneLoader.onAfterSceneInitialize -= onAfterSceneInitialize;
        }

    }
}
