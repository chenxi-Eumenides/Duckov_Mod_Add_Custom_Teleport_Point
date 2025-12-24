using System;
using Duckov.Scenes;


namespace Add_Custom_Teleport_Point
{
    // 一些工具函数
    public static class Utils
    {
        // 根据子场景名称获取主场景名称，主要用于判断是否属于统一关卡
        // 由于不知道有没有其他用处，没有写成判断两场景id是否为同一关
        public static string? getMainScene(string sceneID)
        {
            return sceneID switch
            {
                Constant.SCENE_ID_BASE => (string)Constant.MAIN_SCENE_ID_BASE,
                Constant.SCENE_ID_BASE_2 => (string)Constant.MAIN_SCENE_ID_BASE,
                Constant.SCENE_ID_GROUNDZERO => (string)Constant.MAIN_SCENE_ID_GROUNDZERO,
                Constant.SCENE_ID_GROUNDZERO_CAVE => (string)Constant.MAIN_SCENE_ID_GROUNDZERO,
                Constant.SCENE_ID_HIDDENWAREHOUSE => (string)Constant.MAIN_SCENE_ID_HIDDENWAREHOUSE,
                Constant.SCENE_ID_HIDDENWAREHOUSE_UNDER => (string)Constant.MAIN_SCENE_ID_HIDDENWAREHOUSE,
                Constant.SCENE_ID_FARM => (string)Constant.MAIN_SCENE_ID_FARM,
                Constant.SCENE_ID_FARM_JLAB_FACILITY => (string)Constant.MAIN_SCENE_ID_FARM,
                Constant.SCENE_ID_JLAB_1 => (string)Constant.MAIN_SCENE_ID_JLAB,
                Constant.SCENE_ID_JLAB_2 => (string)Constant.MAIN_SCENE_ID_JLAB,
                Constant.SCENE_ID_STORM => (string)Constant.MAIN_SCENE_ID_STORM,
                Constant.SCENE_ID_STORM_B0 => (string)Constant.MAIN_SCENE_ID_STORM,
                Constant.SCENE_ID_STORM_B1 => (string)Constant.MAIN_SCENE_ID_STORM,
                Constant.SCENE_ID_STORM_B2 => (string)Constant.MAIN_SCENE_ID_STORM,
                Constant.SCENE_ID_STORM_B3 => (string)Constant.MAIN_SCENE_ID_STORM,
                Constant.SCENE_ID_STORM_B4 => (string)Constant.MAIN_SCENE_ID_STORM,
                _ => null,
            };
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
}
