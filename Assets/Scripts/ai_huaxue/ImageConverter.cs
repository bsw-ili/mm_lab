using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class ImageConverter
{
    // ��Texture2Dת��ΪBase64�ַ�����PNG��ʽ��
    public static string TextureToBase64(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToPNG();
        return Convert.ToBase64String(bytes);
    }

    // �ӱ���·������ͼƬ��ת��ΪBase64������StreamingAssets�е�ͼƬ��
    public static string LoadImageToBase64(string localPath)
    {
        string fullPath = Path.Combine(Application.dataPath, "huaxue_images/"+localPath);
        byte[] bytes = File.ReadAllBytes(fullPath);
        return Convert.ToBase64String(bytes);
    }
}