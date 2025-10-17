using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using static UnityEngine.GraphicsBuffer;

public class json2pic : MonoBehaviour
{
    #region === SceneConfig (旧版) ===
    [System.Serializable]
    public class SceneObject
    {
        public string name;
        public float[] position = new float[3] { 0, 0, 0 };
        public float[] rotation = new float[3] { 0, 0, 0 };
        public Dictionary<string, object> states = new Dictionary<string, object>();

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(name)
                && position != null && position.Length == 3
                && rotation != null && rotation.Length == 3;
        }
    }

    [System.Serializable]
    public class SceneConfig
    {
        public List<SceneObject> objects = new List<SceneObject>();
    }
    #endregion

    #region === UnifiedRelationSceneConfig (新版统一姿态结构) ===
    [System.Serializable]
    public class PoseInfo
    {
        [JsonProperty("rotation")]
        public float[] rotation { get; set; } = new float[3] { 0, 0, 0 };

        [JsonProperty("reference")]
        public string reference { get; set; } = "world";
    }

    [System.Serializable]
    public class ObjectInfo
    {
        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("anchor")]
        public string anchor { get; set; }

        [JsonProperty("state")]
        public string state { get; set; }

        [JsonProperty("pose")]
        public PoseInfo pose { get; set; } = new PoseInfo();
    }

    [System.Serializable]
    public class UnifiedRelationConfig
    {
        [JsonProperty("object")]
        public ObjectInfo obj { get; set; }

        [JsonProperty("target")]
        public ObjectInfo target { get; set; }

        [JsonProperty("position_offset")]
        public float[] position_offset { get; set; } = new float[3] { 0, 0, 0 };

        [JsonProperty("coordinate_system")]
        public string coordinate_system { get; set; } = "local";
    }
    #endregion


    [SerializeField, Tooltip("Screenshot capture component")]
    public SceneScreenshot screenshot;

    [SerializeField, Tooltip("Camera positioning component")]
    public CameraFramer cameraFramer;

    [SerializeField, Tooltip("Path to parent GameObject for scene objects")]
    public string rootPath = "SceneRoot";

    [SerializeField, Tooltip("Maximum number of objects to instantiate")]
    public int maxObjectCount = 20;

    [SerializeField, Tooltip("Allowed prefab paths for security")]
    public List<string> allowedPrefabs = new List<string>();

    [SerializeField, Tooltip("Enable this to log detailed debugging info")]
    private bool debugLogging = false;


    #region === 公共入口 ===
    public Texture2D json_pic_auto(string jsonConfig)
    {
        if (string.IsNullOrEmpty(jsonConfig))
        {
            Debug.LogError("JSON configuration is null or empty");
            return null;
        }

        // 尝试新版
        try
        {
            var unified = JsonConvert.DeserializeObject<UnifiedRelationConfig>(jsonConfig);
            if (unified != null)
            {
                LogDebug("Detected UnifiedRelationSceneConfig format");
                return json_relation_pic(unified);
            }
        }
        catch { }

        // 尝试旧版
        try
        {
            var sceneConfig = JsonConvert.DeserializeObject<SceneConfig>(jsonConfig);
            if (sceneConfig != null && sceneConfig.objects != null && sceneConfig.objects.Count > 0)
            {
                LogDebug("Detected SceneConfig format");
                return json_pic(sceneConfig);
            }
        }
        catch { }

        Debug.LogError("Unsupported or invalid JSON format");
        return null;
    }
    #endregion


    #region === SceneConfig 处理 ===
    public Texture2D json_pic(SceneConfig config)
    {
        if (screenshot == null || cameraFramer == null) return null;
        if (config == null || config.objects == null) return null;

        if (config.objects.Count > maxObjectCount)
        {
            Debug.LogWarning($"Object count exceeds max limit, truncating {config.objects.Count} → {maxObjectCount}");
            config.objects = config.objects.GetRange(0, maxObjectCount);
        }

        List<GameObject> instantiatedObjects = new List<GameObject>();
        try
        {
            GameObject root = GetOrCreateRootObject();
            ClearRootChildren(root);

            List<Transform> targets = new List<Transform>();

            foreach (var objConfig in config.objects)
            {
                if (!objConfig.IsValid()) continue;

                GameObject obj = InstantiatePrefabSafe(objConfig.name, root);
                if (obj == null) continue;
                instantiatedObjects.Add(obj);

                obj.transform.localPosition = new Vector3(objConfig.position[0], objConfig.position[1], objConfig.position[2]);
                obj.transform.localRotation = Quaternion.Euler(objConfig.rotation[0], objConfig.rotation[1], objConfig.rotation[2]);

                ApplyObjectStates(obj, objConfig.states);
                targets.Add(obj.transform);
            }

            if (targets.Count > 0)
            {
                cameraFramer.targets = targets;
                cameraFramer.camera_changing();
                return screenshot.Take2dScreenshort();
            }
        }
        finally
        {
            foreach (var o in instantiatedObjects) if (o != null) Destroy(o);
        }
        return null;
    }
    #endregion


    #region === UnifiedRelationSceneConfig 处理 ===
    public Texture2D json_relation_pic(UnifiedRelationConfig config)
    {
        if (screenshot == null || cameraFramer == null) return null;
        if (config == null || config.obj == null || config.target == null) return null;

        List<GameObject> instantiatedObjects = new List<GameObject>();

        try
        {
            GameObject root = GetOrCreateRootObject();
            ClearRootChildren(root);

            // === 1️⃣ 创建 target ===
            GameObject targetObj = InstantiatePrefabSafe(config.target.name, root);
            if (targetObj == null)
            {
                Debug.LogError("❌ Target prefab not found: " + config.target.name);
                return null;
            }
            instantiatedObjects.Add(targetObj);

            // === 2️⃣ 创建 obj ===
            GameObject obj = InstantiatePrefabSafe(config.obj.name, root);
            if (obj == null)
            {
                Debug.LogError("❌ Object prefab not found: " + config.obj.name);
                return null;
            }
            instantiatedObjects.Add(obj);

            // === 3️⃣ 应用旋转（世界坐标）===
            Quaternion objRot = Quaternion.Euler(
                new Vector3(config.obj.pose.rotation[0], config.obj.pose.rotation[1], config.obj.pose.rotation[2])
            );
            Quaternion targetRot = Quaternion.Euler(
                new Vector3(config.target.pose.rotation[0], config.target.pose.rotation[1], config.target.pose.rotation[2])
            );

            obj.transform.rotation = objRot;
            targetObj.transform.rotation = targetRot;

            // === 4️⃣ 锚点对齐 ===
            Transform objAnchor = obj.transform.Find("Anchors/" + config.obj.anchor);
            Transform targetAnchor = targetObj.transform.Find("Anchors/" + config.target.anchor);

            if (objAnchor != null && targetAnchor != null)
            {
                // 计算 obj 锚点的世界偏移，使 obj 的锚点与 target 锚点重合
                Vector3 anchorOffset = obj.transform.position - objAnchor.position;
                obj.transform.position = targetAnchor.position + anchorOffset;

                // === 5️⃣ 应用位置偏移 ===
                if (config.position_offset != null && config.position_offset.Length == 3)
                {
                    Vector3 offset = new Vector3(config.position_offset[0], config.position_offset[1], config.position_offset[2]);

                    if (config.coordinate_system == "local")
                        obj.transform.position += targetAnchor.TransformVector(offset);
                    else
                        obj.transform.position += offset;
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Missing anchor(s): " + config.obj.anchor + " or " + config.target.anchor);
            }

            // === 6️⃣ 应用状态 ===
            ApplyState(obj, config.obj.state);
            ApplyState(targetObj, config.target.state);

            // === 7️⃣ 拍摄截图 ===
            var targets = new List<Transform> { obj.transform, targetObj.transform };
            cameraFramer.targets = targets;
            cameraFramer.camera_changing();
            return screenshot.Take2dScreenshort();
        }
        finally
        {
            foreach (var o in instantiatedObjects)
                if (o != null) Destroy(o);
        }
    }
    #endregion


    #region === 工具函数 ===
    private GameObject GetOrCreateRootObject()
    {
        GameObject root = GameObject.Find(rootPath);
        if (root == null) root = new GameObject(rootPath);
        return root;
    }

    private void ClearRootChildren(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.transform.GetChild(i).gameObject);
    }

    private GameObject InstantiatePrefabSafe(string prefabName, GameObject root)
    {
        if (string.IsNullOrEmpty(prefabName)) return null;
        if (allowedPrefabs.Count > 0 && !allowedPrefabs.Contains(prefabName))
        {
            Debug.LogWarning($"Prefab not allowed: {prefabName}");
            return null;
        }

        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            Debug.LogWarning($"Prefab {prefabName} not found in Resources!");
            return null;
        }
        return Instantiate(prefab, root.transform);
    }

    private void ApplyObjectStates(GameObject instance, Dictionary<string, object> states)
    {
        if (states == null || states.Count == 0) return;
        var handlers = instance.GetComponents<EquipmentStateHandler>();
        foreach (var h in handlers)
        {
            try { h.ApplyStates(states); }
            catch (Exception ex) { Debug.LogError($"State error {instance.name}: {ex.Message}"); }
        }
    }

    private void ApplyState(GameObject obj, string state)
    {
        if (string.IsNullOrEmpty(state)) return;
        var handlers = obj.GetComponents<EquipmentStateHandler>();
        foreach (var h in handlers)
        {
            try { h.ApplyStates(new Dictionary<string, object> { { "state", state } }); }
            catch { }
        }
    }

    private void LogDebug(string msg)
    {
        if (debugLogging) Debug.Log("[json2pic] " + msg);
    }
    #endregion
}
