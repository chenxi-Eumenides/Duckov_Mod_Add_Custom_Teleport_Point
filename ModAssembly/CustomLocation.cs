using System;
using System.Collections.Generic;
using UnityEngine;


namespace Add_Custom_Teleport_Point
{
    // 模组传送点自定义存储
    public static class CustomLocation
    {
        public static Dictionary<(string, string), Vector3> CustomLocations = new Dictionary<(string, string), Vector3>();

        // 注册位置
        // locationName在原版需要不重复
        // 这里只需要 sceneID 和 locationName 都不重复
        public static void AddCustomLocation(string sceneID, string locationName, Vector3 position)
        {
            CustomLocations[(sceneID, locationName)] = position;
        }

        // 获取位置
        // 在下面的harmony动态注入中使用
        public static bool TryGetLocation(string sceneID, string locationName, out Transform tf)
        {
            if (CustomLocations.TryGetValue((sceneID, locationName), out Vector3 position))
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
