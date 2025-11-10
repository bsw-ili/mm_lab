using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保存和恢复 SceneRoot 下所有物体的 Transform 状态
/// </summary>
public class SceneTransformSaver : MonoBehaviour
{
    // 用于保存 Transform 数据的结构体
    [System.Serializable]
    public class TransformData
    {
        public string path;        // 相对于 SceneRoot 的层级路径
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    // 保存的 Transform 数据列表
    private List<TransformData> savedTransforms = new List<TransformData>();

    /// <summary>
    /// 保存 SceneRoot 下所有子物体的 Transform
    /// </summary>
    public void SaveSceneRootTransforms()
    {
        savedTransforms.Clear();

        Transform sceneRoot = GameObject.Find("SceneRoot")?.transform;
        if (sceneRoot == null)
        {
            Debug.LogError("❌ 未找到 SceneRoot 节点！");
            return;
        }

        foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
        {
            string path = GetHierarchyPath(sceneRoot, child);
            savedTransforms.Add(new TransformData
            {
                path = path,
                position = child.localPosition,
                rotation = child.localRotation,
                scale = child.localScale
            });
        }

        Debug.Log($"✅ 已保存 {savedTransforms.Count} 个物体的 Transform 信息。");
    }

    /// <summary>
    /// 恢复 SceneRoot 下所有子物体的 Transform
    /// </summary>
    public void RestoreSceneRootTransforms()
    {
        Transform sceneRoot = GameObject.Find("SceneRoot")?.transform;
        if (sceneRoot == null)
        {
            Debug.LogError("❌ 未找到 SceneRoot 节点！");
            return;
        }

        int restoredCount = 0;
        foreach (var data in savedTransforms)
        {
            Transform target = sceneRoot.Find(data.path);
            if (target != null)
            {
                target.localPosition = data.position;
                target.localRotation = data.rotation;
                target.localScale = data.scale;
                restoredCount++;
            }
        }

        Debug.Log($"✅ 已恢复 {restoredCount}/{savedTransforms.Count} 个物体的 Transform。");
    }

    /// <summary>
    /// 获取某个物体相对于 SceneRoot 的层级路径
    /// </summary>
    private string GetHierarchyPath(Transform root, Transform target)
    {
        if (target == root) return "";
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
