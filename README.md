# 自定义传送点 Mod

***警告*** 目前模组未完成，本人能力有限，进展缓慢，欢迎有大佬能完善，或给我提供一些帮助。

## 项目概述

**Add_Custom_TelePort_Point** 是一个为游戏《Escape from Duckov》开发的模组，旨在为游戏添加自定义传送点功能。该模组允许玩家在不同地图之间快速传送，提高游戏体验的便利性。

### 主要功能

当前功能
- 在指定位置创建自定义传送点
- 支持同地图、子场景内传送
- 支持跨地图传送（目前不可用）
- *可配置传送点名称、位置和交互时间
- *可配置自动生成返回传送点

未来计划
- 支持生成一次性传送点
- 支持json配置
- 支持其他模组调用
- 支持modsetting模组进行配置
  - 添加几个内置位置，通过快捷键快捷创造一个当前位置到内置位置的传送点

## 项目结构

```
Add_Custom_TelePort_Point/
├── ModAssembly/
│   ├── ModAssembly.csproj       # 项目文件
│   ├── ModBehaviour.cs          # 模组入口
│   ├── TeleporterManager.cs     # 传送点管理器
│   ├── CustomTeleporter.cs      # 自定义传送组件
│   ├── ModUtils.cs              # 工具类集合
│   ├── Constant.cs              # 常量定义
│   ├── HarmonyLoader.cs         # Harmony 加载器
│   └── info.ini                 # 模组信息
├── README.md                    # 项目说明
└── .gitignore                   # Git忽略文件
```

## 当前问题

1. 跨地图传送功能目前不可用，缺少必要的组件初始化
  - 使用 `SceneLoader.Instance.LoadScene()` 进行跨地图传送时，`LevelConfig` 没有及时创建
  - `LevelManager` 无法正确初始化，导致 `LevelManager.LootBoxInventories` 和 `LootBoxInventoriesParent` 获取失败
  - `TimeOfDayController` 未及时创建，属性没有正确初始化,出现Start()、Update()空指针异常
  - `GetQuestPrefab` 未正确设置，可能有影响

2. 传送点配置硬编码在`registerCustomConfig()`中，打算未来支持json配置

## 致谢

本项目在开发过程中参考和使用了以下开源项目，特此致谢：

1. [duckovAPI](https://github.com/xiaomao-miao/duckovAPI)
2. [Duckov_Modding_Template](https://github.com/BAKAOLC/Duckov_Modding_Template)
3. [duckovsrc](https://github.com/obscurefreeman/duckovsrc)
