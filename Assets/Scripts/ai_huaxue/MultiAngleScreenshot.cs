using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

public class MultiAngleScreenshot : MonoBehaviour
{
    public Camera captureCamera;
    public int imageWidth = 1024;
    public int imageHeight = 1024;
    public string saveFolder = "Screenshots_1";
    public float paddingFactor = 1.2f; // 稍微拉远一点，防止边缘裁切
    public bool transparentBackground = true; // ✅ 是否导出透明背景

    public void CaptureObjectsFromAngles(GameObject opObj, GameObject targetObj, string filename, string opName, string targetName,Action<List<string>> onComplete)
    {
        if (captureCamera == null)
        {
            Debug.LogError("请在 Inspector 中指定一个截图相机！");
            return;
        }
        StartCoroutine(CaptureRoutine(opObj, targetObj, filename, opName, targetName,onComplete));
    }

    private IEnumerator CaptureRoutine(GameObject opObj, GameObject targetObj, string filename, string opName, string targetName,Action<List<string>> onComplete)
    {
        // ✅ 找 SceneRoot
        Transform sceneRoot = GameObject.Find("SceneRoot")?.transform;
        if (sceneRoot == null)
        {
            Debug.LogError("❌ 未找到 SceneRoot，请确认场景结构正确。");
            yield break;
        }

        Bounds bounds = CalculateCombinedBounds(opObj, targetObj);
        Vector3 center = bounds.center;

        // ✅ 只处理 SceneRoot 下的子节点
        Dictionary<GameObject, bool> originalStates = new Dictionary<GameObject, bool>();
        foreach (Transform child in sceneRoot)
        {
            if (child == null) continue;
            bool keepVisible = (child.gameObject == opObj || child.gameObject == targetObj ||
                                opObj.transform.IsChildOf(child) || targetObj.transform.IsChildOf(child));
            originalStates[child.gameObject] = child.gameObject.activeSelf;
            child.gameObject.SetActive(keepVisible); // 只保留目标对象
        }

        // ✅ 锁定 LODGroup
        //List<LODGroup> lodGroups = new List<LODGroup>();
        //lodGroups.AddRange(opObj.GetComponentsInChildren<LODGroup>(true));
        //if (targetObj != null)
        //    lodGroups.AddRange(targetObj.GetComponentsInChildren<LODGroup>(true));

        //foreach (var lodGroup in lodGroups)
        //    lodGroup.ForceLOD(0); // 锁定最高 LOD

        // ✅ 确保相机存在且启用
        if (captureCamera == null)
        {
            Debug.LogError("❌ 未指定截图相机！");
            yield break;
        }
        captureCamera.gameObject.SetActive(true);
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0x4F / 255f, 0x6B / 255f, 0x4C / 255f);
        captureCamera.cullingMask = ~0; // 渲染所有层

        // ✅ 多角度视图
        Vector3[] angles = new Vector3[]
        {
        new Vector3(0, 0, 0),      // 正面
        new Vector3(0, 90, 0),     // 右侧
        new Vector3(0, -90, 0),    // 左侧
        new Vector3(30, 45, 0),    // 右上
        new Vector3(60, 0, 0)      // 顶视
        };
        string[] angleLabels = { "front", "right", "left", "topright", "top" };

        // ✅ 文件路径
        string folderPath = Path.Combine(@"E:\llm_lab", saveFolder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        List<string> capturedPaths = new List<string>();

        // ✅ 自动计算相机距离
        float baseDistance = GetCameraDistance(bounds, captureCamera, paddingFactor);

        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 euler = angles[i];
            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 dir = rotation * Vector3.forward;
            Vector3 camPos = center - dir * baseDistance;

            captureCamera.transform.position = camPos;
            captureCamera.transform.LookAt(center);

            yield return new WaitForEndOfFrame();
            string filePath = "";
            try
            {
                string fileName = $"{filename}_{opName}_{targetName}_{angleLabels[i]}_{timestamp}.png";
                filePath = Path.Combine(folderPath, fileName);
            }
            catch
            {
                string fileName = $"{filename}_{angleLabels[i]}_{timestamp}.png";
                filePath = Path.Combine(folderPath, fileName);
            }
            

            SaveCameraView(captureCamera, filePath);
            capturedPaths.Add(filePath);
        }

        // ✅ 恢复原有显隐状态
        foreach (var kvp in originalStates)
        {
            if (kvp.Key != null)
                kvp.Key.SetActive(kvp.Value);
        }

        // ✅ 恢复 LOD 自动
        //foreach (var lodGroup in lodGroups)
        //    lodGroup.ForceLOD(-1);

        Debug.Log($"✅ 多角度截图完成，共 {angles.Length} 张，保存路径：{folderPath}");
        onComplete?.Invoke(capturedPaths);
    }


    private float GetCameraDistance(Bounds bounds, Camera cam, float padding)
    {
        float radius = bounds.extents.magnitude;
        float halfFOV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = radius / Mathf.Sin(halfFOV);
        return distance * padding;
    }

    private void SaveCameraView(Camera cam, string filePath)
    {
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);
    }

    private Bounds CalculateCombinedBounds(GameObject a, GameObject b)
    {
        Renderer[] aRenderers = a.GetComponentsInChildren<Renderer>();
        Renderer[] bRenderers = b.GetComponentsInChildren<Renderer>();

        Bounds bounds = new Bounds(a.transform.position, Vector3.zero);
        foreach (var r in aRenderers) bounds.Encapsulate(r.bounds);
        foreach (var r in bRenderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    public async Task<List<string>> CaptureObjectsFromAnglesAsync(GameObject opObj, GameObject targetObj, string filename,string opName,string targetName)
    {
        TaskCompletionSource<List<string>> tcs = new TaskCompletionSource<List<string>>();
        CaptureObjectsFromAngles(opObj, targetObj, filename, opName,targetName,paths => tcs.SetResult(paths));
        return await tcs.Task;
    }

}
