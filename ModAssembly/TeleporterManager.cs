using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Duckov.Scenes;
using Newtonsoft.Json;
using System.IO;

namespace Add_Custom_Teleport_Point
{
    // 传送点管理器
    public static class TeleporterManager
    {
        // 存储已经创建的传送点
        private static List<GameObject> createdTeleportPoints = new List<GameObject>();
        // 存储已经注册的传送点配置
        private static List<TeleportConfig> registeredConfigs = new List<TeleportConfig>();
        // 存储总的传送点配置注册数量，配置ID，防止名称相同
        private static int maxRegisteredConfigID = 0;

        /// <summary>
        /// 创建传送点实例
        /// </summary>
        /// <param name="config">传送点配置</param>
        /// <returns>如果创建成功则返回true，否则返回false</returns>
        private static bool createTeleportPoint(TeleportConfig config)
        {
            try
            {
                // 检查配置是否注册
                if (config.configID < 0)
                {
                    Debug.LogWarning($"{Constant.LogPrefix} 传入的config({config?.interactName})未注册");
                    return false;
                }
                Vector3 sourcePos = new Vector3(config.sourcePosition[0], config.sourcePosition[1], config.sourcePosition[2]);
                Vector3 targetPos = new Vector3(config.targetPosition[0], config.targetPosition[1], config.targetPosition[2]);
                // 创建空游戏对象
                GameObject teleportPoint = new GameObject();
                teleportPoint.transform.position = sourcePos;
                // 添加自定义传送点组件并初始化
                CustomTeleporter CustomTeleporter = teleportPoint.AddComponent<CustomTeleporter>();
                CustomTeleporter.Initialize(
                    config.configID, config.interactName,
                    config.sourceSceneId, sourcePos,
                    config.targetSceneId, targetPos,
                    disposable: config.disposable
                );
                // 检查传送点配置是否有效
                if (!CustomTeleporter.isValidTeleporter()) 
                {
                    Debug.LogError($"{Constant.LogPrefix} 传送点({config.interactName})创建失败: 配置无效");
                    UnityEngine.Object.Destroy(teleportPoint);
                    return false;
                }

                // 注册到场景系统
                if (MultiSceneCore.ActiveSubScene != null && MultiSceneCore.ActiveSubScene.HasValue)
                {
                    SceneManager.MoveGameObjectToScene(teleportPoint, MultiSceneCore.ActiveSubScene.Value);
                }
                else
                {
                    Debug.LogError($"{Constant.LogPrefix} 传送点({config.interactName})注册到场景系统失败: {MultiSceneCore.MainScene}");
                    return false;
                }
                // 添加到已创建列表
                createdTeleportPoints.Add(teleportPoint);

                Debug.Log($"{Constant.LogPrefix} 创建成功！传送点 TeleportPoint_{config.configID}: {config.sourceSceneId}({sourcePos}) => {config.targetSceneId}({targetPos})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 创建传送点({config.interactName})时发生异常: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 注册传送点配置
        /// </summary>
        /// <param name="sourceSceneId">源场景ID</param>
        /// <param name="sourcePosition">源位置坐标</param>
        /// <param name="targetSceneId">目标场景ID</param>
        /// <param name="targetPosition">目标位置坐标</param>
        /// <param name="interactName">交互显示名称</param>
        /// <param name="interactTime">交互时间</param>
        /// <param name="backTeleport">是否创建反向传送点</param>
        /// <param name="disposable">是否为一次性传送点</param>
        /// <returns>成功注册的传送点数量（正向+反向）</returns>
        public static int RegisterTeleportPointConfig(
            string sourceSceneId, float[] sourcePosition,
            string targetSceneId, float[] targetPosition,
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
                registerConfig(config);
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
                    registerConfig(backConfig);
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// 注册传送点配置到记录列表中
        /// </summary>
        /// <param name="config">要注册的传送点配置</param>
        private static void registerConfig(TeleportConfig config)
        {
            maxRegisteredConfigID += 1;
            config.configID = maxRegisteredConfigID;
            registeredConfigs.Add(config);
        }

        /// <summary>
        /// 移除所有已创建的传送点实例
        /// </summary>
        public static void RemoveCreatedTeleportPoint()
        {
            foreach (GameObject teleportPoint in createdTeleportPoints)
            {
                if (teleportPoint != null)
                {
                    UnityEngine.Object.Destroy(teleportPoint);
                }
            }
            createdTeleportPoints.Clear();
            Debug.Log($"{Constant.LogPrefix} 已经移除所有传送点");
        }

        /// <summary>
        /// 移除特定的传送点配置
        /// </summary>
        /// <param name="configID">要移除的传送点配置ID</param>
        /// <returns>如果找到并移除成功则返回true，否则返回false</returns>
        public static bool UnRegisterTeleportPointConfig(int configID)
        {
            foreach (var config in registeredConfigs)
            {
                if (config.configID == configID)
                {
                    registeredConfigs.Remove(config);
                    Debug.Log($"{Constant.LogPrefix} 已经移除传送点: {config.interactName} ID: {config.configID}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否是可注册的传送点配置
        /// </summary>
        /// <param name="checkConfig">要检查的传送点配置</param>
        /// <returns>如果配置有效且不重复则返回true</returns>
        private static bool isVaildConfig(TeleportConfig checkConfig)
        {
            // 检查必要字段
            if (string.IsNullOrEmpty(checkConfig.sourceSceneId)) return false;
            if (string.IsNullOrEmpty(checkConfig.targetSceneId)) return false;
            if (checkConfig.sourcePosition == null || checkConfig.sourcePosition.Length != 3) return false;
            if (checkConfig.targetPosition == null || checkConfig.targetPosition.Length != 3) return false;
            if (checkConfig.backTeleport) return false;
            // 检查是否重复注册
            foreach (var config in registeredConfigs)
            {
                if (
                    checkConfig.sourceSceneId == config.sourceSceneId &&
                    checkConfig.sourcePosition == config.sourcePosition
                ) return false;
            }
            return true;
        }

        /// <summary>
        /// 开始创建自定义传送点
        /// </summary>
        /// <remarks>
        /// 需要等MultiSceneCore.ActiveSubSceneID非空时创建，所以需要注意时间
        /// 目前在LevelManager.OnAfterLevelInitialized和MultiSceneCore.OnSubSceneLoaded时创建
        /// 跨主场景传送时会重复创建一次，但没有影响
        /// </remarks>
        private static void startCreateCustomTeleportPoint()
        {
            RemoveCreatedTeleportPoint();
            if (MultiSceneCore.ActiveSubSceneID == null)
            {
                Debug.LogWarning($"{Constant.LogPrefix} ActiveSubSceneID 为 null, 无法创建传送点");
                return;
            }
            Debug.Log($"{Constant.LogPrefix} 场景已加载{MultiSceneCore.ActiveSubSceneID}，已注册传送点{registeredConfigs.Count}个，准备创建传送点");
            foreach (var config in registeredConfigs)
            {
                if (MultiSceneCore.ActiveSubSceneID == config.sourceSceneId)
                {
                    createTeleportPoint(config);
                }
            }
        }

        /// <summary>
        /// 从 JSON 文件读取传送点配置并注册
        /// </summary>
        /// <param name="filePath">JSON 文件路径，如果为 null 则使用默认路径</param>
        public static void readConfigFromJson()
        {
            try
            {
                string filePath = Path.Combine(Duckov.Modding.ModManager.DefaultModFolderPath,Constant.ModId, Constant.ConfigJsonName);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"{Constant.LogPrefix} JSON 配置文件不存在: {filePath} 跳过读取。");
                    return;
                }

                string jsonContent = File.ReadAllText(filePath);
                var jsonConfigs = JsonConvert.DeserializeObject<List<TeleportConfig>>(jsonContent);

                if (jsonConfigs == null || jsonConfigs.Count == 0)
                {
                    Debug.LogWarning($"{Constant.LogPrefix} JSON 配置文件为空或格式错误");
                    return;
                }

                int successCount = 0;
                foreach (var jsonConfig in jsonConfigs)
                {
                    // 验证必要字段
                    if (string.IsNullOrEmpty(jsonConfig.sourceSceneId) ||
                        string.IsNullOrEmpty(jsonConfig.targetSceneId) ||
                        jsonConfig.sourcePosition == null || jsonConfig.sourcePosition.Length != 3 ||
                        jsonConfig.targetPosition == null || jsonConfig.targetPosition.Length != 3)
                    {
                        Debug.LogWarning($"{Constant.LogPrefix} 跳过无效的 JSON 配置: 缺少必要字段");
                        continue;
                    }
                    // 注册传送点配置
                    int registered = RegisterTeleportPointConfig(
                        sourceSceneId: jsonConfig.sourceSceneId,
                        sourcePosition: jsonConfig.sourcePosition,
                        targetSceneId: jsonConfig.targetSceneId,
                        targetPosition: jsonConfig.targetPosition,
                        interactName: jsonConfig.interactName,
                        interactTime: jsonConfig.interactTime,
                        backTeleport: jsonConfig.backTeleport,
                        disposable: jsonConfig.disposable
                    );

                    successCount += registered;
                }

                Debug.Log($"{Constant.LogPrefix} 从 JSON 文件成功注册了 {successCount} 个传送点配置");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Constant.LogPrefix} 读取 JSON 配置文件时发生异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 子场景加载完成时的事件回调函数
        /// </summary>
        /// <param name="core">多场景核心实例</param>
        /// <param name="scene">加载的场景</param>
        private static void onSubSceneLoaded(MultiSceneCore core, Scene scene)
        {
            startCreateCustomTeleportPoint();
        }

        /// <summary>
        /// 场景初始化完成后的事件回调函数
        /// </summary>
        /// <param name="context">场景加载上下文</param>
        private static void onAfterSceneInitialize(SceneLoadingContext context)
        {
            startCreateCustomTeleportPoint();
        }

        /// <summary>
        /// 初始化传送点管理器，订阅相关事件
        /// </summary>
        /// <remarks>
        /// 在ModBehaviour.OnEnable时调用
        /// </remarks>
        public static void Init()
        {
            readConfigFromJson();
            MultiSceneCore.OnSubSceneLoaded += onSubSceneLoaded;
            SceneLoader.onAfterSceneInitialize += onAfterSceneInitialize;
        }

        /// <summary>
        /// 清理传送点管理器，取消订阅事件并移除所有传送点
        /// </summary>
        /// <remarks>
        /// 在ModBehaviour.OnDisable时调用
        /// </remarks>
        public static void Cleanup()
        {
            MultiSceneCore.OnSubSceneLoaded -= onSubSceneLoaded;
            SceneLoader.onAfterSceneInitialize -= onAfterSceneInitialize;
            RemoveCreatedTeleportPoint();
        }

    }
}
