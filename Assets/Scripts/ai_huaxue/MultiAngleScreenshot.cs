using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.CompilerServices;


public class MultiAngleScreenshot : MonoBehaviour
{
    public Camera captureCamera;
    public int imageWidth = 1024;
    public int imageHeight = 1024;
    public string saveFolder = "Screenshots_1";
    public float paddingFactor = 1.2f; // 稍微拉远一点，防止边缘裁切
    public bool transparentBackground = true; // ✅ 是否导出透明背景
    public Material lineMaterial;

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

    /// <summary>
    /// 计算 SceneRoot 下所有渲染器的整体包围盒
    /// </summary>
    private Bounds CalculateSceneBounds(Transform sceneRoot)
    {
        Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(sceneRoot.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1))
            bounds.Encapsulate(r.bounds);
        return bounds;
    }


    // 计算物体及子物体包围盒
    private Bounds CalculateBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(root.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // GL 绘制红色线框
    private void DrawWireframe(Bounds bounds)
    {
        Vector3 c = bounds.center;
        Vector3 s = bounds.extents;

        Vector3[] corners = new Vector3[8];
        corners[0] = c + new Vector3(-s.x, -s.y, -s.z);
        corners[1] = c + new Vector3(s.x, -s.y, -s.z);
        corners[2] = c + new Vector3(s.x, -s.y, s.z);
        corners[3] = c + new Vector3(-s.x, -s.y, s.z);

        corners[4] = c + new Vector3(-s.x, s.y, -s.z);
        corners[5] = c + new Vector3(s.x, s.y, -s.z);
        corners[6] = c + new Vector3(s.x, s.y, s.z);
        corners[7] = c + new Vector3(-s.x, s.y, s.z);

        int[,] lines = {
            {0,1},{1,2},{2,3},{3,0}, // 底面
            {4,5},{5,6},{6,7},{7,4}, // 顶面
            {0,4},{1,5},{2,6},{3,7}  // 竖边
        };

        GL.Begin(GL.LINES);
        GL.Color(Color.red);
        for (int i = 0; i < lines.GetLength(0); i++)
        {
            GL.Vertex(corners[lines[i, 0]]);
            GL.Vertex(corners[lines[i, 1]]);
        }
        GL.End();
    }

    // 截图方法（带操作物体红框）
    public async Task<List<string>> CapturePics(string filename, List<GameObject> activeObjects)
    {
        List<string> capturedPaths = new List<string>();
        Transform sceneRoot = GameObject.Find("SceneRoot")?.transform;
        if (sceneRoot == null)
        {
            Debug.LogError("❌ 未找到 SceneRoot！");
            return capturedPaths;
        }

        Bounds bounds = CalculateBounds(sceneRoot);
        Vector3 center = bounds.center;

        if (captureCamera == null)
        {
            Debug.LogError("❌ 未指定截图相机！");
            return capturedPaths;
        }

        captureCamera.gameObject.SetActive(true);
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0x4F / 255f, 0x6B / 255f, 0x4C / 255f);
        captureCamera.cullingMask = ~0;

        // 多角度视图
        Vector3[] angles =
        {
        new Vector3(0, 180, 0),      // 正面
        new Vector3(30, 225, 0),     // 右上
        new Vector3(60, 180, 0)      // 顶视
    };
        string[] angleLabels = { "front", "topright", "top" };

        string folderPath = Path.Combine(@"E:\llm_lab", saveFolder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        // ✅ 动态计算相机距离（根据视野和物体尺寸）
        float radius = bounds.extents.magnitude;
        float fovRad = captureCamera.fieldOfView * Mathf.Deg2Rad;
        float baseDistance = (radius / Mathf.Sin(fovRad * 0.5f)) * paddingFactor;

        // ✅ 距离平滑（避免突然跳动）
        baseDistance = Mathf.Clamp(baseDistance, 0.3f, 10f);

        // ✅ 添加 OutlineDrawer（关键部分）
        OutlineDrawer outline = captureCamera.GetComponent<OutlineDrawer>();
        if (outline == null)
            outline = captureCamera.gameObject.AddComponent<OutlineDrawer>();

        outline.lineMaterial = lineMaterial;
        outline.SetTargets(activeObjects);

        // ✅ 遍历角度并截图
        foreach (var euler in angles)
        {
            Quaternion rotation = Quaternion.Euler(euler);
            Vector3 dir = rotation * Vector3.forward;
            Vector3 camPos = center - dir * baseDistance;

            captureCamera.transform.position = camPos;
            captureCamera.transform.LookAt(center);

            await new WaitForEndOfFrameAwaiter(); // 等待渲染完成

            string fileName = $"{filename}_{angleLabels[System.Array.IndexOf(angles, euler)]}_{timestamp}.png";
            string filePath = Path.Combine(folderPath, fileName);

            SaveCameraView(captureCamera, filePath);
            capturedPaths.Add(filePath);
        }

        Debug.Log($"✅ 多角度截图完成，共 {capturedPaths.Count} 张，保存路径：{folderPath}");
        return capturedPaths;
    }




    private float GetCameraDistance(Bounds bounds, Camera cam, float padding)
    {
        float radius = bounds.extents.magnitude;
        float halfFOV = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float distance = radius / Mathf.Sin(halfFOV);
        return distance * padding;
    }
    /// <summary>
    /// 将文件路径中非法字符替换为安全字符，并确保文件夹存在
    /// </summary>
    /// <param name="originalPath">原始文件路径</param>
    /// <param name="replacement">替换字符，默认使用下划线</param>
    /// <returns>可安全保存的完整文件路径</returns>
    public static string GetSafeFilePath(string originalPath, string replacement = "_")
    {
        // 获取目录和文件名
        string folder = Path.GetDirectoryName(originalPath);
        string fileName = Path.GetFileName(originalPath);

        // 替换文件夹路径中的非法字符
        string safeFolder = Regex.Replace(folder, @"[<>:""/\\|?*]", replacement);

        // 替换文件名中的非法字符
        string safeFileName = Regex.Replace(fileName, @"[<>:""/\\|?*]", replacement);

        // 如果文件夹不存在，创建它
        if (!Directory.Exists(safeFolder))
        {
            Directory.CreateDirectory(safeFolder);
        }

        // 返回完整安全路径
        return Path.Combine(safeFolder, safeFileName);
    }

    private void SaveCameraView(Camera cam, string filePath)
    {
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenShot.Apply();
        //filePath = GetSafeFilePath(filePath);
        byte[] bytes = screenShot.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        // ✅ 释放 Texture2D 显存
        UnityEngine.Object.DestroyImmediate(screenShot);

        // ✅ 释放 RenderTexture
        cam.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
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


public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private static UnityMainThreadDispatcher _instance;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            var obj = new GameObject("UnityMainThreadDispatcher");
            _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }
        return _instance;
    }

    public void Enqueue(Action action)
    {
        if (action == null) return;
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}


public class WaitForEndOfFrameAwaiter : CustomYieldInstruction
{
    private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

    public WaitForEndOfFrameAwaiter()
    {
        // 开启协程等待渲染帧结束
        UnityMainThreadDispatcher.Instance().StartCoroutine(WaitForEnd());
    }

    private IEnumerator WaitForEnd()
    {
        yield return new WaitForEndOfFrame();
        _tcs.TrySetResult(true);
    }

    public TaskAwaiter<bool> GetAwaiter() => _tcs.Task.GetAwaiter();

    public override bool keepWaiting => !_tcs.Task.IsCompleted;
}

[RequireComponent(typeof(Camera))]
public class OutlineDrawer : MonoBehaviour
{
    public Material lineMaterial;
    private List<GameObject> targetObjects = new List<GameObject>();
    private Color lineColor = Color.red;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    /// <summary>
    /// 设置要绘制边框的目标物体列表
    /// </summary>
    public void SetTargets(List<GameObject> objects)
    {
        targetObjects.Clear();
        foreach (var obj in objects)
        {
            if (obj != null)
                targetObjects.Add(obj);
        }
    }

    void OnPostRender()
    {
        if (lineMaterial == null || targetObjects.Count == 0) return;
        if (cam == null) cam = GetComponent<Camera>();

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, cam.pixelWidth, 0, cam.pixelHeight);
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        foreach (var obj in targetObjects)
        {
            if (obj == null) continue;

            // 获取所有子 Renderer，忽略粒子系统
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>()
                                      .Where(r => !(r is ParticleSystemRenderer))
                                      .ToArray();
            if (renderers.Length == 0) continue;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue);
            bool anyVisible = false;

            foreach (var r in renderers)
            {
                Bounds b = r.bounds;
                Vector3 c = b.center;
                Vector3 s = b.extents;

                // 八个角点
                Vector3[] corners = new Vector3[8];
                corners[0] = c + new Vector3(-s.x, -s.y, -s.z);
                corners[1] = c + new Vector3(s.x, -s.y, -s.z);
                corners[2] = c + new Vector3(s.x, -s.y, s.z);
                corners[3] = c + new Vector3(-s.x, -s.y, s.z);
                corners[4] = c + new Vector3(-s.x, s.y, -s.z);
                corners[5] = c + new Vector3(s.x, s.y, -s.z);
                corners[6] = c + new Vector3(s.x, s.y, s.z);
                corners[7] = c + new Vector3(-s.x, s.y, s.z);

                // 转为屏幕空间
                foreach (var corner in corners)
                {
                    Vector3 screenPos = cam.WorldToScreenPoint(corner);
                    if (screenPos.z < 0) continue; // 背面忽略
                    anyVisible = true;
                    min.x = Mathf.Min(min.x, screenPos.x);
                    min.y = Mathf.Min(min.y, screenPos.y);
                    max.x = Mathf.Max(max.x, screenPos.x);
                    max.y = Mathf.Max(max.y, screenPos.y);
                }
            }

            if (anyVisible)
                DrawRect(min, max);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void DrawRect(Vector3 min, Vector3 max)
    {
        // 绘制矩形边框
        GL.Vertex3(min.x, min.y, 0);
        GL.Vertex3(max.x, min.y, 0);

        GL.Vertex3(max.x, min.y, 0);
        GL.Vertex3(max.x, max.y, 0);

        GL.Vertex3(max.x, max.y, 0);
        GL.Vertex3(min.x, max.y, 0);

        GL.Vertex3(min.x, max.y, 0);
        GL.Vertex3(min.x, min.y, 0);
    }
}