using System;
using UnityEngine;
using Duckov.Scenes;
using Eflatun.SceneReference;

namespace Add_Custom_Teleport_Point
{
    // 包含了模组内部创建传送点所需的场景信息
    public struct SceneInfo
    {
        private string sceneID;
        private string? mainSceneID => Utils.getMainScene(sceneID);
        private Vector3 position;
        private string locationName;

        private SceneInfoEntry scene;
        private SceneInfoEntry mainScene;
        private MultiSceneLocation location;
        private bool isValid;

        public string SceneID => sceneID;
        public Vector3 Position => position;
        public string LocationName => locationName;
        public SceneInfoEntry Scene => scene;
        public SceneInfoEntry MainScene => mainScene;
        public MultiSceneLocation Location => location;
        public bool IsValid => isValid;
        public SceneReference? SceneReference => scene != null ? scene.SceneReference : null;
        public SceneReference? MainSceneReference => mainScene != null ? mainScene.SceneReference : null;
        public SceneReference? overrideCurtainScene = null;
        public string? MainSceneID => mainSceneID;

        public bool useLocation = true;
        public bool clickToConinue = false;
        public bool notifyEvacuation = false;
        public bool doCircleFade = true;
        public bool saveToFile = false;
        public bool hideTips = false;

        /// <summary>
        /// 构造函数，创建场景信息
        /// </summary>
        /// <param name="sceneid">场景ID</param>
        /// <param name="pos">位置坐标</param>
        /// <param name="name">位置名称</param>
        public SceneInfo(string sceneid, Vector3 pos, string name)
        {
            sceneID = sceneid;
            position = pos;
            locationName = name;
            scene = SceneInfoCollection.GetSceneInfo(sceneID);
            mainScene = SceneInfoCollection.GetSceneInfo(mainSceneID);
            location = new MultiSceneLocation
            {
                SceneID = sceneID,
                LocationName = locationName
            };
            isValid = CheckInfo();
            CustomLocation.AddCustomLocation(sceneID, locationName, position);
        }
        /// <summary>
        /// 检查场景信息是否有效
        /// </summary>
        /// <returns>如果所有必需信息都有效则返回true</returns>
        private bool CheckInfo()
        {
            if (string.IsNullOrEmpty(sceneID)) return false;
            if (position == null) return false;
            if (string.IsNullOrEmpty(locationName)) return false;
            if (scene == null) return false;
            if (SceneReference == null) return false;
            if (mainScene == null) return false;
            if (MainSceneReference == null) return false;
            if (string.IsNullOrEmpty(mainSceneID)) return false;
            return true;
        }
    }

    // 包含了用户创建传送点需要提供的信息
    [Serializable]
    public class TeleportConfig
    {
        public int configID = -1;
        public string sourceSceneId = "";
        public float[] sourcePosition = [0f, 0f, 0f];
        public string targetSceneId = "";
        public float[] targetPosition = [0f, 0f, 0f];
        public string interactName = Constant.DEFAULT_INTERACT_NAME;
        public float interactTime = Constant.DEFAULT_INTERACT_TIME;
        public bool disposable = false;
        public bool backTeleport = false;

        /// <summary>
        /// 构造函数，创建传送点配置
        /// </summary>
        /// <param name="sourceSceneId">源场景ID</param>
        /// <param name="sourcePosition">源位置坐标</param>
        /// <param name="targetSceneId">目标场景ID</param>
        /// <param name="targetPosition">目标位置坐标</param>
        /// <param name="interactName">交互显示名称</param>
        /// <param name="interactTime">交互时间</param>
        /// <param name="disposable">是否为一次性传送点</param>
        public TeleportConfig(
            string sourceSceneId, float[] sourcePosition,
            string targetSceneId, float[] targetPosition,
            string interactName = Constant.DEFAULT_INTERACT_NAME,
            float interactTime = Constant.DEFAULT_INTERACT_TIME,
            bool disposable = false
        )
        {
            this.sourceSceneId = sourceSceneId;
            this.sourcePosition = sourcePosition;
            this.targetSceneId = targetSceneId;
            this.targetPosition = targetPosition;
            this.interactName = interactName;
            this.interactTime = interactTime;
            this.disposable = disposable;
        }
    }
}
