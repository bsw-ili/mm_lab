using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class ImageConverter
{
    // 将Texture2D转换为Base64字符串（PNG格式）
    public static string TextureToBase64(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToPNG();
        return Convert.ToBase64String(bytes);
    }

    // 从本地路径加载图片并转换为Base64（例如StreamingAssets中的图片）
    public static string LoadImageToBase64(string localPath)
    {
        string fullPath = Path.Combine(Application.dataPath, "huaxue_images/"+localPath);
        byte[] bytes = File.ReadAllBytes(fullPath);
        return Convert.ToBase64String(bytes);
    }
}