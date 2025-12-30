using System;
using Duckov.Scenes;
using UnityEngine;


namespace Add_Custom_Teleport_Point
{
    // 一些工具函数
    public static class Utils
    {
        /// <summary>
        /// 根据子场景名称获取主场景名称
        /// </summary>
        /// <param name="sceneID">子场景ID</param>
        /// <returns>对应的主场景名称，如果未找到则返回null</returns>
        /// <remarks>
        /// 主要用于判断是否属于同一关卡
        /// </remarks>
        public static string? getMainScene(string sceneID)
        {
            return sceneID switch
            {
                Constant.SCENE_ID_BASE => (string)Constant.MAIN_SCENE_ID_BASE,
                Constant.SCENE_ID_BASE_2 => (string)Constant.MAIN_SCENE_ID_BASE,
                Constant.SCENE_ID_BASE_SUB => (string)Constant.MAIN_SCENE_ID_BASE,
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
                Constant.SCENE_ID_DEMO => (string)Constant.MAIN_SCENE_ID_DEMO,
                Constant.SCENE_ID_SNOW => (string)Constant.MAIN_SCENE_ID_SNOW,
                Constant.SCENE_ID_ZOMBIE => (string)Constant.MAIN_SCENE_ID_ZOMBIE,
                _ => null,
            };
        }
    }
}
