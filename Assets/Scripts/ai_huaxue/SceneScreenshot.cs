using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SceneScreenshot : MonoBehaviour
{
    public Camera targetCamera;   // Ŀ����������Ϊ�����������
    public int width = 1920;      // ��ͼ���
    public int height = 1080;     // ��ͼ�߶�
    public string fileName = "SceneCapture.png"; // �����ļ���
    //public OpenAIStreamReader teacher;
    private int idx;
    private int count = 0;

    //void Update()
    //{
    //    // ���� P ����ͼ
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

        // ����һ����ʱ�� RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        // ��Ⱦ����� RT
        targetCamera.Render();

        // �� RenderTexture ��ȡ�� Texture2D
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // ����
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        return screenshot;
    }

    public Texture2D Take2dScreenshot(List<GameObject> targets)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 1. ��ȡ����������Renderer
        Renderer[] allRenderers = GameObject.FindObjectsOfType<Renderer>();

        // 2. ��¼��ЩRendererҪ���ر�
        List<Renderer> disabledRenderers = new List<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            // �������Ŀ���б����������
            if (!targets.Contains(r.gameObject))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    disabledRenderers.Add(r);
                }
            }
        }

        // 3. ����һ����ʱ�� RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        // ��Ⱦ����� RT
        targetCamera.Render();

        // 4. �� RenderTexture ��ȡ�� Texture2D
        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        // 5. ����
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 6. �ָ������ص�Renderer
        foreach (Renderer r in disabledRenderers)
        {
            r.enabled = true;
        }

        return screenshot;
    }


    public void TakeScreenshot()
    {

        Texture2D screenshot = Take2dScreenshort();
        // ת���� PNG �����浽����
        byte[] bytes = screenshot.EncodeToPNG();
        fileName = "SceneCapture_" + count + ".png";
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(filePath, bytes);

        Debug.Log($"��ͼ�ѱ��浽: {filePath}");
    }
}
