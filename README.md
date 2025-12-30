# 自定义传送点 Mod

## 项目概述

**Add_Custom_TelePort_Point** 是一个为游戏《Escape from Duckov》开发的模组，旨在为游戏添加自定义传送点功能。该模组允许玩家在不同地图之间快速传送，提高游戏体验的便利性。

默认为游戏添加2个传送点。

一个用于到达农场镇，用于在找不到二级卡时，能够不回家，重新进入J-Lab实验室；

另一个用于在打败boss后到达零号区，能够不回家进入风暴区。

### 主要功能

当前功能

- 创建自定义传送点
- 可配置传送点名称、位置
- 支持生成双向传送点
- 支持生成一次性传送点

未来计划

- 生成类似游戏本身传送点的外观
- 支持json配置
- 支持其他模组调用

## 项目结构

```
Duckov_Add_Custom_Teleport_Point/
├── ModAssembly/
│   ├── ModBehaviour.cs          # 模组入口类
│   ├── TeleporterManager.cs     # 传送点管理器
│   ├── CustomTeleporter.cs      # 自定义传送组件
│   ├── CustomLocation.cs        # 自定义位置类
│   ├── DataStructure.cs         # 自定义数据结构定义
│   ├── Constant.cs              # 常量定义
│   ├── HarmonyLoader.cs         # Harmony 加载器
│   ├── Patch.cs                 # Harmony 补丁类
│   ├── RFH.cs                   # 反射辅助类
│   ├── Utils.cs                 # 工具类
│   ├── ModAssembly.csproj       # 项目文件
│   ├── info.ini                 # 模组信息配置
│   └── Test.cs                  # 开发测试工具类，未使用
├── README.md
└── .gitignore
```

## 开发说明

注册传送点配置

```csharp
TeleporterManager.registerTeleportPoint(
    sourceSceneId: Constant.SCENE_ID_BASE,      // 在该子场景下创建传送点
    sourcePosition: new Vector3(-5f, 0f, -85f), // 在该坐标创建传送点
    targetSceneId: Constant.SCENE_ID_BASE_2,    // 传送目标的子场景名
    targetPosition: new Vector3(95f, 0f, -40f), // 传送目标的坐标
    interactName: "同地图传送",                  // 显示的名称
    backTeleport: true,                         // 是否创建双向传送点
    disposable: false                           // 是否是一次性传送点
);
```

### 游戏代码研究

跨主场景使用`SceneLoader.Instance.LoadScene`函数。sceneReference参数需要是目标主场景的scene对象，location中需要是目标子场景的sceneid。

同主场景使用`MultiSceneCore.Instance.LoadAndTeleport`函数。

同子场景直接设置玩家坐标。

游戏场景加载时的事件有：

- `SceneLoader.LoadScene` 加载顺序为
  1. `SceneLoader.onStartedLoadingScene`
  2. `SceneLoader.onBeforeSetSceneActive`
  3. `SceneLoader.onFinishedLoadingScene`
  4. `LevelManager.OnLevelBeginInitializing`
  5. *`MultiSceneCore.OnSubSceneWillBeUnloaded`
  6. `MultiSceneCore.OnSubSceneLoaded`
  7. `LevelManager.OnLevelInitialized`
  8. `SceneLoader.onAfterSceneInitialize`
  9. `LevelManager.OnAfterLevelInitialized`
- `MultiSceneCore.LoadAndTeleport` 加载顺序为
  1. *`MultiSceneCore.OnSubSceneWillBeUnloaded`
  2. `MultiSceneCore.OnSubSceneLoaded`



## 致谢

本项目在开发过程中参考和使用了以下开源项目，特此致谢：

1. [duckovAPI](https://github.com/xiaomao-miao/duckovAPI)
2. [Duckov_Modding_Template](https://github.com/BAKAOLC/Duckov_Modding_Template)
3. [duckovsrc](https://github.com/obscurefreeman/duckovsrc)
