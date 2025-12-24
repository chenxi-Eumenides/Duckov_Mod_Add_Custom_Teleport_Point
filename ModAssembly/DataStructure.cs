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
        private Vector3 position;
        private string locationName;

        private SceneInfoEntry scene;
        private MultiSceneLocation location;
        private bool isValid;

        public string SceneID => sceneID;
        public Vector3 Position => position;
        public string LocationName => locationName;
        public SceneInfoEntry Scene => scene;
        public MultiSceneLocation Location => location;
        public bool IsValid => isValid;
        public SceneReference? SceneReference => Scene != null ? Scene.SceneReference : null;
        public string? mainSceneID => Utils.getMainScene(SceneID);

        public bool useLocation = true;
        public bool clickToConinue = false;
        public bool notifyEvacuation = false;
        public bool doCircleFade = true;
        public bool saveToFile = false;
        public bool hideTips = false;

        public SceneInfo(string sceneid, Vector3 pos, string name)
        {
            sceneID = sceneid;
            position = pos;
            locationName = name;
            scene = SceneInfoCollection.GetSceneInfo(sceneID);
            location = new MultiSceneLocation
            {
                SceneID = sceneID,
                LocationName = locationName
            };
            if ("Base" == sceneID)
            {
                saveToFile = true;
            }
            isValid = CheckInfo();
            CustomLocation.AddCustomLocation(sceneID, locationName, position);
        }
        private bool CheckInfo()
        {
            if (string.IsNullOrEmpty(sceneID)) return false;
            if (Position == null) return false;
            if (string.IsNullOrEmpty(locationName)) return false;
            if (Scene == null) return false;
            if (SceneReference == null) return false;
            if (string.IsNullOrEmpty(mainSceneID)) return false;
            return true;
        }
    }

    // 包含了用户创建传送点需要提供的信息
    public class TeleportConfig
    {
        public int configID = -1;
        public string sourceSceneId;
        public Vector3 sourcePosition;
        public string targetSceneId;
        public Vector3 targetPosition;
        public string interactName = Constant.DEFAULT_INTERACT_NAME;
        public float interactTime = Constant.DEFAULT_INTERACT_TIME;
        public bool disposable = false;

        public TeleportConfig(
            string sourceSceneId, Vector3 sourcePosition,
            string targetSceneId, Vector3 targetPosition,
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
