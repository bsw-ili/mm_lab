using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using LiquidVolumeFX; // 用于解析 JSON

public class ExperimentActionExecutor : MonoBehaviour
{
    // 动作函数定义

    /// <summary>
    /// 缩放物体大小
    /// </summary>
    public void ScaleObject(GameObject obj, float scale)
    {
        if (obj == null) return;
        obj.transform.localScale *= scale;
    }

    /// <summary>
    /// 移动物体，使源物体的指定锚点与目标物体锚点对齐，并可添加偏移
    /// </summary>
    /// <summary>
    /// 移动物体，使源物体的指定锚点与目标物体锚点对齐，并可添加偏移，同时面对齐
    /// </summary>
    public void MoveToAnchor(GameObject sourceObj, Transform sourceAnchor, GameObject targetObj, Transform targetAnchor, Vector3 offset)
    {
        if (sourceObj == null || sourceAnchor == null || targetObj == null || targetAnchor == null) return;

        // 特殊处理酒精灯盖火焰
        if (sourceObj.name == "alcohol_lamp_cap" && targetObj.name == "alcohol_lamp")
        {
            GameObject fire = sourceObj.transform.Find("Fire")?.gameObject;
            if (fire != null) fire.SetActive(false);
        }

        // 1️⃣ 计算位置偏移，使锚点重合
        Vector3 desiredPosition = targetAnchor.position + offset;
        Vector3 anchorToObject = sourceObj.transform.position - sourceAnchor.position;
        sourceObj.transform.position = desiredPosition + anchorToObject;

        // 2️⃣ 旋转对齐：源锚点法向量 -> 目标锚点法向量
        Vector3 sourceNormal = sourceAnchor.up;   // 源锚点法向量
        Vector3 targetNormal = targetAnchor.up;   // 目标锚点法向量

        // 计算旋转，让源锚点法向量对齐目标锚点法向量
        Quaternion rotationOffset = Quaternion.FromToRotation(sourceNormal, targetNormal);

        // 应用旋转到整个物体，同时保持锚点位置不变
        sourceObj.transform.rotation = rotationOffset * sourceObj.transform.rotation;

        // 重新调整位置，确保锚点重合
        sourceObj.transform.position = desiredPosition + (sourceObj.transform.position - sourceAnchor.position);
    }



    /// <summary>
    /// 围绕自身锚点旋转
    /// </summary>
    public void RotateAroundAnchor(GameObject sourceObj, Transform sourceAnchor, Vector3 rotation)
    {
        if (sourceObj == null || sourceAnchor == null) return;

        // 以锚点为中心旋转
        sourceObj.transform.RotateAround(sourceAnchor.position, Vector3.right, rotation.x);
        sourceObj.transform.RotateAround(sourceAnchor.position, Vector3.up, rotation.y);
        sourceObj.transform.RotateAround(sourceAnchor.position, Vector3.forward, rotation.z);
    }

    /// <summary>
    /// 点燃物体（示例：激活 ParticleSystem 或火焰对象）
    /// </summary>
    public void Ignite(GameObject obj)
    {
        if (obj == null) return;
        // 假设物体下有 ParticleSystem 表示火焰
        GameObject fire = obj.transform.Find("Fire")?.gameObject;
        if (fire != null) fire.SetActive(true);
    }

    /// <summary>
    /// 添加液体（示例：改变材质或激活液体对象）
    /// </summary>
    public void AddLiquid(GameObject obj, string liquidName)
    {
        if (obj == null) return;

        Debug.Log($"Add liquid {liquidName} to {obj.name}");

        // 从字典获取十六进制颜色
        if (ChemistryDefinitions.allowedLiquids_dict.TryGetValue(liquidName, out string colorHex))
        {
            // 尝试解析十六进制颜色
            if (ColorUtility.TryParseHtmlString(colorHex, out Color liquidColor))
            {   
                if(obj.transform.Find("states/liquid") == null) 
                {
                    Debug.LogWarning($"对象 {obj.name} 下未找到 states/liquid 子对象，无法设置液体颜色");
                    return;
                }
                LiquidVolume liquid = obj.transform.Find("states/liquid").GetComponent<LiquidVolume>();
                if (liquid != null)
                {
                    liquid.level = 1;
                    liquid.liquidColor1 = liquidColor;
                    liquid.liquidColor2 = liquidColor;
                }
            }
            else
            {
                Debug.LogWarning($"无法解析颜色: {colorHex}, 使用默认颜色白色");
                if (obj.transform.Find("states/liquid") == null)
                {
                    Debug.LogWarning($"对象 {obj.name} 下未找到 states/liquid 子对象，无法设置液体颜色");
                    return;
                }
                LiquidVolume liquid = obj.transform.GetComponent<LiquidVolume>();
                if (liquid != null)
                {
                    liquid.level = 1;
                    liquid.liquidColor1 = Color.white;
                    liquid.liquidColor2 = Color.white;
                }
            }
        }
        else
        {
            Debug.LogWarning($"液体 {liquidName} 未在字典中定义, 使用默认颜色白色");
            if (obj.transform.Find("states/liquid") == null)
            {
                Debug.LogWarning($"对象 {obj.name} 下未找到 states/liquid 子对象，无法设置液体颜色");
                return;
            }
            LiquidVolume liquid = obj.transform.GetComponent<LiquidVolume>();
            if (liquid != null)
            {
                liquid.level = 1;
                liquid.liquidColor1 = Color.white;
                liquid.liquidColor2 = Color.white;
            }
        }
    }

    /// <summary>
    /// 添加固体（示例：激活固体模型）
    /// </summary>
    public void AddSolid(GameObject obj, string solidName)
    {
        if (obj == null) return;
        Debug.Log($"Add solid {solidName} to {obj.name}");
        // 可实现显示固体的逻辑
        // 从字典获取十六进制颜色
        if (ChemistryDefinitions.allowedSolids_dict.TryGetValue(solidName, out string colorHex))
        {
            // 尝试解析十六进制颜色
            if (ColorUtility.TryParseHtmlString(colorHex, out Color solidColor))
            {
                Transform solid = obj.transform.Find("states/solid");
                if (solid != null)
                {
                    solid.gameObject.SetActive(true);
                    Renderer renderer = solid.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = solidColor;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"液体 {solidName} 未在字典中定义, 使用默认颜色黑色");
                Transform solid = obj.transform.Find("states/solid");
                if (solid != null)
                {
                    solid.gameObject.SetActive(true);
                    Renderer renderer = solid.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Color.black;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"液体 {solidName} 未在字典中定义, 使用默认颜色黑色");
            Transform solid = obj.transform.Find("states/solid");
            if (solid != null)
            {
                solid.gameObject.SetActive(true);
                Renderer renderer = solid.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.black;
                }
            }
        }
    }

    /// <summary>
    /// 执行动作列表
    /// </summary>
    public void ExecuteSteps(string jsonText)
    {
        JObject json = JObject.Parse(jsonText);
        JArray steps = (JArray)json["steps"];
        foreach (var step in steps)
        {
            string op = step["op"].ToString();
            JObject source = (JObject)step["source"];
            JObject target = step["target"] as JObject;
            GameObject sourceObj = GameObject.Find(source["equipment"].ToString());
            Transform sourceAnchor = sourceObj != null && source["anchor"] != null ? sourceObj.transform.Find("Anchors/" + source["anchor"].ToString()) : sourceObj?.transform;

            switch (op)
            {
                case "ScaleObject":
                    float scale = step["scale"].ToObject<float>();
                    ScaleObject(sourceObj, scale);
                    break;

                case "MoveToAnchor":
                    GameObject targetObj = GameObject.Find(target["equipment"].ToString());
                    Transform targetAnchor = targetObj != null && target["anchor"] != null ? targetObj.transform.Find("Anchors/"+target["anchor"].ToString()) : targetObj?.transform;
                    JToken offsetToken = step["offset"];
                    Vector3 offset = Vector3.zero;

                    if (offsetToken != null && offsetToken.Type == JTokenType.Array)
                    {
                        float x = offsetToken[0].ToObject<float>();
                        float y = offsetToken[1].ToObject<float>();
                        float z = offsetToken[2].ToObject<float>();
                        offset = new Vector3(x, y, z);
                    }
                    MoveToAnchor(sourceObj, sourceAnchor, targetObj, targetAnchor, offset);
                    break;

                case "RotateAroundAnchor":
                    JToken rotationToken = step["rotation"];

                    Vector3 rotation = Vector3.zero;
                    if (rotationToken != null && rotationToken.Type == JTokenType.Array)
                    {
                        float x = rotationToken[0].ToObject<float>();
                        float y = rotationToken[1].ToObject<float>();
                        float z = rotationToken[2].ToObject<float>();
                        rotation = new Vector3(x, y, z);
                    }
                    RotateAroundAnchor(sourceObj, sourceAnchor, rotation);
                    break;

                case "Ignite":
                    Ignite(sourceObj);
                    break;

                case "AddLiquid":
                    string liquid = step["material"] != null ? step["material"].ToString() : "default_liquid";
                    AddLiquid(sourceObj, liquid);
                    break;

                case "AddSolid":
                    string solid = step["material"] != null ? step["material"].ToString() : "default_solid";
                    AddSolid(sourceObj, solid);
                    break;
            }
        }
    }
}
