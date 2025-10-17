using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SceneScreenshot : MonoBehaviour
{
    public Camera targetCamera;   // 目标相机，如果为空则用主相机
    public int width = 1920;      // 截图宽度
    public int height = 1080;     // 截图高度
    public string fileName = "SceneCapture.png"; // 保存文件名
    //public OpenAIStreamReader teacher;
    private int idx;
    private int count = 0;

    //void Update()
    //{
    //    // 按下 P 键截图
    //    if (Input.GetKeyDown(KeyCode.P))
    //    {
    //        Texture2D student_anwswer = Take2dScreenshort();
    //        teacher.imagePaths_base64.Add(ImageConverter.TextureToBase64(student_anwswer));
    //        idx = teacher.imagePaths.Count;
    //        count += 1;
    //        TakeScreenshot();
    //        teacher.dialogue += "[Student]:[[image" + idx +"]]";
    //        StartCoroutine(teacher.StreamChat());
    //    }
    //}

    public Texture2D Take2dScreenshort()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 创建一个临时的 RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        // 渲染相机到 RT
        targetCamera.Render();

        // 把 RenderTexture 读取到 Texture2D
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // 清理
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        return screenshot;
    }

    public Texture2D Take2dScreenshot(List<GameObject> targets)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 1. 获取场景中所有Renderer
        Renderer[] allRenderers = GameObject.FindObjectsOfType<Renderer>();

        // 2. 记录哪些Renderer要被关闭
        List<Renderer> disabledRenderers = new List<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            // 如果不在目标列表里，就先隐藏
            if (!targets.Contains(r.gameObject))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    disabledRenderers.Add(r);
                }
            }
        }

        // 3. 创建一个临时的 RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        // 渲染相机到 RT
        targetCamera.Render();

        // 4. 把 RenderTexture 读取到 Texture2D
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // 5. 清理
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 6. 恢复被隐藏的Renderer
        foreach (Renderer r in disabledRenderers)
        {
            r.enabled = true;
        }

        return screenshot;
    }


    public void TakeScreenshot()
    {

        Texture2D screenshot = Take2dScreenshort();
        // 转换成 PNG 并保存到磁盘
        byte[] bytes = screenshot.EncodeToPNG();
        fileName = "SceneCapture_" + count + ".png";
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"截图已保存到: {filePath}");
    }
}
