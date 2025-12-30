using System;
using System.Collections.Generic;
using UnityEngine;


namespace Add_Custom_Teleport_Point
{
    // 模组传送点自定义存储
    public static class CustomLocation
    {
        private static Dictionary<(string, string), Vector3> customLocations = new Dictionary<(string, string), Vector3>();
        public static Dictionary<(string, string), Vector3> CustomLocations => customLocations;

        /// <summary>
        /// 注册自定义位置
        /// </summary>
        /// <param name="sceneID">场景ID</param>
        /// <param name="locationName">位置名称</param>
        /// <param name="position">位置坐标</param>
        /// <remarks>
        /// locationName在原版需要不重复，这里只需要sceneID和locationName都不重复
        /// </remarks>
        public static void AddCustomLocation(string sceneID, string locationName, Vector3 position)
        {
            customLocations[(sceneID, locationName)] = position;
        }

        /// <summary>
        /// 尝试获取自定义位置
        /// </summary>
        /// <param name="sceneID">场景ID</param>
        /// <param name="locationName">位置名称</param>
        /// <param name="tf">输出参数，如果找到则返回对应的Transform</param>
        /// <returns>如果找到位置则返回true，否则返回false</returns>
        /// <remarks>
        /// 在Harmony动态注入中使用
        /// </remarks>
        public static bool TryGetLocation(string sceneID, string locationName, out Transform tf)
        {
            if (customLocations.TryGetValue((sceneID, locationName), out Vector3 position))
            {
                string objName = $"{Constant.CustomLocationPrefix}_{sceneID}_{locationName}";
                GameObject obj = GameObject.Find(objName);
                if (obj == null)
                {
                    obj = new GameObject(objName);
                }
                obj.transform.position = position;
                tf = obj.transform;
                return true;
            }
            else
            {
                tf = default!;
                return false;
            }
        }
    }
}
