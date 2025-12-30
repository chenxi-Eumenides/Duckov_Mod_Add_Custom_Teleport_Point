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
    public static class Test
    {
        // 测试用 记录当前所有游戏对象和组件
        public static Dictionary<int, Dictionary<string, object>> CaptureCurrentState()
        {
            Dictionary<int, Dictionary<string, object>> state = new Dictionary<int, Dictionary<string, object>>();
            List<GameObject> allGameObjects = GameObject.FindObjectsOfType<GameObject>().ToList();
            foreach (var gameObject in allGameObjects)
            {
                var gameObjectState = new Dictionary<string, object>{
                { "gameobject",gameObject },
                { "component",gameObject.GetComponents<Component>().ToList()},
                { "instanceId", gameObject.GetInstanceID() },
                { "name", gameObject.name },
                { "active", gameObject.activeInHierarchy },
                { "tag", gameObject.tag },
                { "layer", gameObject.layer }
            };
                state[gameObject.GetInstanceID()] = gameObjectState;
            }
            return state;
        }

        public static void PrintState()
        {
            foreach (var gameobject in GameObject.FindObjectsOfType<GameObject>().ToList())
            {
                Debug.Log($"({gameobject.activeInHierarchy}) {gameobject.name}: {gameobject.transform.position}, {gameobject.layer}, {gameobject.tag}");
                foreach (var component in gameobject.GetComponents<Component>().ToList())
                {
                    Debug.Log($" - {component.name} {component.tag}");
                }
            }
        }

        // 比较两个状态并打印差异
        public static void CompareAndPrintStateDifferences(
            Dictionary<int, Dictionary<string, object>> beforeDict,
            Dictionary<int, Dictionary<string, object>> afterDict
        )
        {
            Debug.Log($"=== 状态差异分析 ===");

            // 2. 找出新增的对象
            List<int> newObjectIds = new List<int>();
            foreach (var kvp in afterDict)
            {
                if (!beforeDict.ContainsKey(kvp.Key))
                {
                    newObjectIds.Add(kvp.Key);
                }
            }

            if (newObjectIds.Count > 0)
            {
                Debug.Log($"新增对象 ({newObjectIds.Count} 个):");
                foreach (var instanceId in newObjectIds)
                {
                    var objDict = afterDict[instanceId];
                    string? name = objDict.TryGetValue("name", out var nameObj) ? nameObj as string : "Unknown";
                    int id = instanceId;
                    bool active = objDict.TryGetValue("active", out var activeObj) && activeObj is bool activeVal ? activeVal : false;
                    string? tag = objDict.TryGetValue("tag", out var tagObj) ? tagObj as string : "Untagged";
                    int layer = objDict.TryGetValue("layer", out var layerObj) && layerObj is int layerVal ? layerVal : 0;
                    Debug.Log($"对象: {name}, id={id}, actuve={active},tag={tag},layer={layer}");

                    List<string> componentTypes = new List<string>();
                    if (objDict.TryGetValue("component", out var compObj) && compObj is List<Component> components)
                    {
                        foreach (var comp in components)
                        {
                            if (comp != null)
                            {
                                string typeName = comp.GetType().Name;
                                if (!componentTypes.Contains(typeName))
                                {
                                    componentTypes.Add(typeName);
                                }
                            }
                        }
                        Debug.Log($"  [+] {name} (ID: {id}) - " +
                                 $"组件({(components != null ? components.Count : 0)}个): [{string.Join(", ", componentTypes)}] " +
                                 $"Tag: {tag}, Layer: {UnityEngine.LayerMask.LayerToName(layer)}, Active: {active}");
                    }
                }
            }
            else
            {
                Debug.Log("无新增对象");
            }

            // 3. 找出被删除的对象
            List<int> removedObjectIds = new List<int>();
            foreach (var kvp in beforeDict)
            {
                if (!afterDict.ContainsKey(kvp.Key))
                {
                    removedObjectIds.Add(kvp.Key);
                }
            }

            if (removedObjectIds.Count > 0)
            {
                Debug.Log($"删除对象 ({removedObjectIds.Count} 个):");
                foreach (var instanceId in removedObjectIds)
                {
                    var objDict = beforeDict[instanceId];
                    string? name = objDict.TryGetValue("name", out var nameObj) ? nameObj as string : "Unknown";
                    Debug.Log($"已删除对象 : {name} (ID: {instanceId})");
                }
            }
            else
            {
                Debug.Log("无删除对象");
            }

            // 4. 比较保留的对象的变化
            List<int> commonObjectIds = new List<int>();
            foreach (var kvp in beforeDict)
            {
                if (afterDict.ContainsKey(kvp.Key))
                {
                    commonObjectIds.Add(kvp.Key);
                }
            }

            int changedObjectsCount = 0;
            foreach (var instanceId in commonObjectIds)
            {
                var beforeObj = beforeDict[instanceId];
                var afterObj = afterDict[instanceId];

                List<string> changes = new List<string>();

                // 检查名称变化
                string? beforeName = beforeObj.TryGetValue("name", out var beforeNameObj) ? beforeNameObj as string : "Unknown";
                string? afterName = afterObj.TryGetValue("name", out var afterNameObj) ? afterNameObj as string : "Unknown";
                if (beforeName != afterName)
                {
                    changes.Add($"名称: '{beforeName}' → '{afterName}'");
                }

                // 检查激活状态变化
                bool beforeActive = beforeObj.TryGetValue("active", out var beforeActiveObj) && beforeActiveObj is bool beforeActiveVal ? beforeActiveVal : false;
                bool afterActive = afterObj.TryGetValue("active", out var afterActiveObj) && afterActiveObj is bool afterActiveVal ? afterActiveVal : false;
                if (beforeActive != afterActive)
                {
                    changes.Add($"激活状态: {beforeActive} → {afterActive}");
                }

                // 检查Tag变化
                string? beforeTag = beforeObj.TryGetValue("tag", out var beforeTagObj) ? beforeTagObj as string : "Untagged";
                string? afterTag = afterObj.TryGetValue("tag", out var afterTagObj) ? afterTagObj as string : "Untagged";
                if (beforeTag != afterTag)
                {
                    changes.Add($"Tag: '{beforeTag}' → '{afterTag}'");
                }

                // 检查Layer变化
                int beforeLayer = beforeObj.TryGetValue("layer", out var beforeLayerObj) && beforeLayerObj is int beforeLayerVal ? beforeLayerVal : 0;
                int afterLayer = afterObj.TryGetValue("layer", out var afterLayerObj) && afterLayerObj is int afterLayerVal ? afterLayerVal : 0;
                if (beforeLayer != afterLayer)
                {
                    changes.Add($"Layer: {UnityEngine.LayerMask.LayerToName(beforeLayer)} ({beforeLayer}) → {UnityEngine.LayerMask.LayerToName(afterLayer)} ({afterLayer})");
                }

                // 检查组件变化
                List<string> beforeComponentTypes = new List<string>();
                List<string> afterComponentTypes = new List<string>();

                if (beforeObj.TryGetValue("component", out var beforeCompObj) && beforeCompObj is List<Component> beforeComponents)
                {
                    foreach (var comp in beforeComponents)
                    {
                        if (comp != null)
                        {
                            string typeName = comp.GetType().Name;
                            if (!beforeComponentTypes.Contains(typeName))
                            {
                                beforeComponentTypes.Add(typeName);
                            }
                        }
                    }
                }

                if (afterObj.TryGetValue("component", out var afterCompObj) && afterCompObj is List<Component> afterComponents)
                {
                    foreach (var comp in afterComponents)
                    {
                        if (comp != null)
                        {
                            string typeName = comp.GetType().Name;
                            if (!afterComponentTypes.Contains(typeName))
                            {
                                afterComponentTypes.Add(typeName);
                            }
                        }
                    }
                }

                // 找出新增的组件类型
                List<string> addedComponents = new List<string>();
                foreach (var typeName in afterComponentTypes)
                {
                    if (!beforeComponentTypes.Contains(typeName))
                    {
                        addedComponents.Add(typeName);
                    }
                }

                // 找出删除的组件类型
                List<string> removedComponents = new List<string>();
                foreach (var typeName in beforeComponentTypes)
                {
                    if (!afterComponentTypes.Contains(typeName))
                    {
                        removedComponents.Add(typeName);
                    }
                }

                if (addedComponents.Count > 0)
                {
                    changes.Add($"新增组件: [{string.Join(", ", addedComponents)}]");
                }

                if (removedComponents.Count > 0)
                {
                    changes.Add($"删除组件: [{string.Join(", ", removedComponents)}]");
                }

                // 检查组件数量变化
                int beforeComponentCount = beforeObj.TryGetValue("component", out var beforeCountObj) && beforeCountObj is List<Component> beforeCompList ? beforeCompList.Count : 0;
                int afterComponentCount = afterObj.TryGetValue("component", out var afterCountObj) && afterCountObj is List<Component> afterCompList ? afterCompList.Count : 0;
                if (beforeComponentCount != afterComponentCount)
                {
                    changes.Add($"组件数量: {beforeComponentCount} → {afterComponentCount}");
                }

                if (changes.Count > 0)
                {
                    if (changedObjectsCount == 0)
                    {
                        Debug.Log("修改的对象:");
                    }
                    changedObjectsCount++;
                    Debug.Log($"  [Δ] {afterName} (ID: {instanceId}):");
                    foreach (var change in changes)
                    {
                        Debug.Log($"      {change}");
                    }
                }
            }

            if (changedObjectsCount == 0)
            {
                Debug.Log("无修改的对象");
            }

            // 5. 打印统计信息
            Debug.Log("=== 统计信息 ===");
            Debug.Log($"加载前: {beforeDict.Count} 个对象");
            Debug.Log($"加载后: {afterDict.Count} 个对象");
            Debug.Log($"新增: {newObjectIds.Count} 个对象");
            Debug.Log($"删除: {removedObjectIds.Count} 个对象");
            Debug.Log($"修改: {changedObjectsCount} 个对象");

            Debug.Log("=== 分析结束 ===\n");
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

                Debug.Log($"SceneInfoEntry : buildIndex={buildIndex} | description={description} | displayName={displayName} | displayNameRaw={displayNameRaw} | ID={ID} | isLoaded={isLoaded}");

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

                        Debug.Log($"sceneReference : srAddress={srAddress} | srBuildIndex={srBuildIndex} | srGuid={srGuid} | srLoadedScene={srLoadedScene} | srName={srName} | srPath={srPath} | srState={srState} | srUnsafeReason={srUnsafeReason}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{ex.Message}");
                    }
                }
            }
        }

        public static void PrintInteractableBaseEvent()
        {
            SceneLoaderProxy[] allSceneLoaderProxys = GameObject.FindObjectsOfType<SceneLoaderProxy>();
            if (allSceneLoaderProxys == null || allSceneLoaderProxys.Length == 0)
            {
                Debug.LogWarning($"not find interactableBase");
                return;
            }
            foreach (var sceneLoaderProxy in allSceneLoaderProxys)
            {
                if (sceneLoaderProxy == null) continue;
                InteractableBase interactableBase = sceneLoaderProxy.gameObject.GetComponent<InteractableBase>();
                Debug.Log($"{sceneLoaderProxy.gameObject.name} : {interactableBase?.InteractName}");
                PrintEvent(typeof(InteractableBase), "OnInteractFinishedEvent", interactableBase);
            }
        }

        public static void PrintEvent(Type type, string eventName, object? instance = null)
        {
            try
            {
                Delegate[] delegates = RFH.GetRegisteredDelegates(type, eventName, instance);
                if (delegates == null || delegates.Length == 0)
                {
                    Debug.Log($"{type.Name}.{eventName} 中没有找到任何注册的事件");
                    return;
                }
                Debug.Log($"{type.Name}.{eventName} 中已注册 : {delegates.Length}");
                foreach (Delegate del in delegates)
                {
                    Debug.Log($"  名称 {del.Method.Name} 类型 {del.Method.DeclaringType?.FullName}");
                    if (del.Target != null)
                    {
                        Debug.Log($"  对象类型: {del.Target.GetType().FullName}");
                        var monoBehaviour = del.Target as MonoBehaviour;
                        if (monoBehaviour != null)
                        {
                            Debug.Log($"    所在 GameObject: {monoBehaviour.gameObject.name} 标签: {monoBehaviour.gameObject.tag}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{ex.Message}");
            }
        }

        // 测试用
        public static void CheckLootBox()
        {
            var lootboxs = GameObject.FindObjectsOfType<InteractableLootbox>();
            if (lootboxs == null || lootboxs.Length == 0)
            {
                Debug.LogWarning($"not find lootbox");
                return;
            }
            foreach (var lootbox in lootboxs)
            {
                if (lootbox == null) continue;
                try
                {
                    var gameObj = lootbox?.gameObject;
                    var gamePObj = gameObj?.transform?.parent?.gameObject;
                    var gamePPObj = gamePObj?.transform?.parent?.gameObject;
                    Debug.Log($"{lootbox!.Inventory.DisplayName} : {gameObj!.name} -> {gamePObj?.name} -> {gamePPObj?.name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{ex.Message}");
                }
            }
            // 获取TimeOfDayController数据结构
            var todcObj = TimeOfDayController.Instance?.gameObject;
            var todcPObj = todcObj?.transform?.parent?.gameObject;
            var todcPPObj = todcPObj?.transform?.parent?.gameObject;
            Debug.Log($"TimeOfDayController:{todcObj?.name} -> {todcPObj?.name} -> {todcPPObj?.name}");
            // 获取TimeOfDayConfig数据结构
            var todconfig = (TimeOfDayConfig?)RFH.GetFieldValue(TimeOfDayController.Instance ?? new TimeOfDayController(), "config");
            var todconfigObj = todconfig?.transform.gameObject;
            var todconfigPObj = todconfigObj?.transform?.parent?.gameObject;
            var todconfigPPObj = todconfigPObj?.transform?.parent?.gameObject;
            Debug.Log($"TimeOfDayConfig:{todconfigObj?.name} -> {todconfigPObj?.name} -> {todconfigPPObj?.name}");
        }

        // 测试用
        // 打印所有已加载的 InteractableBase 基类，搜索源文件可以看到具体有哪些种类。
        public static void PrintAllInteractableBases()
        {
            var interactableBases = GameObject.FindObjectsOfType<InteractableBase>();
            if (interactableBases == null) return;
            Debug.Log($"Find interactableBase {interactableBases.Length}");
            foreach (InteractableBase interactableBase in interactableBases)
            {
                try
                {
                    if (interactableBase == null) continue;
                    Debug.Log($"{interactableBase.name} {interactableBase.InteractName}");
                    var gameObj = interactableBase?.gameObject;
                    var gamePObj = gameObj?.transform?.parent?.gameObject;
                    var gamePPObj = gamePObj?.transform?.parent?.gameObject;
                    Debug.Log($"gameObj.name={gameObj!.name} -> {gamePObj?.name} -> {gamePPObj?.name}");
                    var components = gameObj.GetComponents<InteractableBase>();
                    if (components == null) return;
                    foreach (var component in components)
                    {
                        Debug.Log($"component.name={component?.name} type={component?.GetType()}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{interactableBase.name}: {ex.Message}");
                }
            }
        }

        // 测试用
        public static void PrintAllLoadScene()
        {
            SceneLoaderProxy[] allSceneLoaderProxy = GameObject.FindObjectsOfType<SceneLoaderProxy>();
            foreach (var sceneLoaderProxy in allSceneLoaderProxy)
            {
                string? sceneID = RFH.GetFieldValue(sceneLoaderProxy, "sceneID") as string;
                Debug.Log($"{Constant.LogPrefix} SceneLoaderProxy -> {sceneID}");
                SceneReference? overrideCurtainScene = RFH.GetFieldValue(sceneLoaderProxy, "overrideCurtainScene") as SceneReference;
                try
                {
                    Debug.Log($"{Constant.LogPrefix}    {overrideCurtainScene?.Name} : {overrideCurtainScene?.BuildIndex}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{Constant.LogPrefix}    获取 SceneReference 失败: {ex.Message}");
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
}
