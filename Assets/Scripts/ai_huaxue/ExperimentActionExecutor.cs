using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using LiquidVolumeFX;
/// <summary>
/// 🧪 实验动作执行器（优化版）
/// 根据 JSON 动作序列执行虚拟实验操作
/// </summary>
public class ExperimentActionExecutor : MonoBehaviour
{
    // ==============================
    // 🧩 缓存区与路径定义
    // ==============================
    private const string LIQUID_PATH = "states/liquid";
    private const string SOLID_PATH = "states/solid";
    private Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    // ==============================
    // 🧠 基础操作函数
    // ==============================

    /// <summary>
    /// 对齐物体锚点，并根据模式选择是否对齐法线方向。
    /// 支持 AlignPosition / AlignPositionRotation 模式。
    /// </summary>
    public void AlignByAnchor(GameObject sourceObj, Transform sourceAnchor, GameObject targetObj, Transform targetAnchor, string alignMode)
    {
        if (sourceObj == null || sourceAnchor == null || targetObj == null || targetAnchor == null) return;

        // 特殊逻辑：盖上酒精灯 → 关闭火焰
        if (sourceObj.name == "alcohol_lamp_cap" && targetObj.name == "alcohol_lamp")
        {
            GameObject fire = targetObj.transform.Find("Fire")?.gameObject;
            if (fire != null) fire.SetActive(false);
        }

        // 默认模式
        if (string.IsNullOrEmpty(alignMode)) alignMode = "AlignPositionRotation";
        string mode = alignMode.Trim().ToLowerInvariant();

        // -----------------------------
        // 1️⃣ 将 sourceAnchor 的世界位置/方向转换为 sourceObj 局部坐标
        // -----------------------------
        Vector3 anchorLocalPos = sourceObj.transform.InverseTransformPoint(sourceAnchor.position);
        Vector3 anchorLocalUp = sourceObj.transform.InverseTransformDirection(sourceAnchor.up);

        // 目标锚点世界信息
        Vector3 targetWorldPos = targetAnchor.position;
        Vector3 targetWorldUp = targetAnchor.up;

        // -----------------------------
        // 2️⃣ 仅对齐位置
        // -----------------------------
        if (mode == "alignposition")
        {
            Vector3 currentAnchorWorld = sourceObj.transform.TransformPoint(anchorLocalPos);
            Vector3 delta = targetWorldPos - currentAnchorWorld;
            sourceObj.transform.position += delta;
            return;
        }

        // -----------------------------
        // 3️⃣ 对齐旋转 + 位置
        // -----------------------------
        if (mode == "alignpositionrotation")
        {
            // 计算当前锚点朝向（局部方向转世界方向）
            Vector3 currentUpWorld = sourceObj.transform.TransformDirection(anchorLocalUp);

            // 旋转使 sourceAnchor.up 对齐 targetAnchor.up
            Quaternion rot = Quaternion.FromToRotation(currentUpWorld, targetWorldUp);
            sourceObj.transform.rotation = rot * sourceObj.transform.rotation;

            // 旋转后重新计算锚点世界位置
            Vector3 anchorWorldAfterRotate = sourceObj.transform.TransformPoint(anchorLocalPos);

            // 平移使锚点重合
            Vector3 translation = targetWorldPos - anchorWorldAfterRotate;
            sourceObj.transform.position += translation;
            return;
        }

        Debug.LogWarning($"⚠️ 未识别的对齐模式: {alignMode}");
    }


    /// <summary>
    /// 点燃物体（激活火焰）
    /// </summary>
    public IEnumerator Ignite(GameObject obj)
    {
        if (obj == null) yield break;

        GameObject fire = obj.transform.Find("Fire")?.gameObject;
        if (fire == null)
        {
            Debug.LogWarning($"对象 {obj.name} 下未找到 Fire 对象");
            yield break;
        }

        fire.GetComponent<ParticleSystem>().Play();

        // 4️⃣ 等待粒子系统状态刷新（重要：确保 Transform + 粒子同步）
        yield return new WaitForEndOfFrame();

        // 5️⃣ 可选：再等待 1 帧确保完全显示（防止偶尔闪烁或锚点更新延迟）
        yield return null;
    }


    /// <summary>
    /// 翻转物体（ReverseObject）
    /// </summary>
    public void ReverseObject(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.Rotate(Vector3.right, 180f, Space.Self);
    }

    /// <summary>
    /// 平放（LayFlat）
    /// </summary>
    public void LayFlat(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.rotation = Quaternion.Euler(0, 180f, 90f);
    }

    /// <summary>
    /// 向上倾斜（TiltUp）
    /// </summary>
    public void TiltUp(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.rotation = Quaternion.Euler(45f, 180f, 0f);
    }

    /// <summary>
    /// 向下倾斜（TiltDown）
    /// </summary>
    public void TiltDown(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.rotation = Quaternion.Euler(-45f, 180f, 0f);
    }

    /// <summary>
    /// 添加液体（强制刷新 LiquidVolumeFX 显示）
    /// </summary>
    public void AddLiquid(GameObject obj, string liquidName)
    {
        if (obj == null) return;

        // 获取颜色定义
        if (!ChemistryDefinitions.allowedLiquids_dict.TryGetValue(liquidName, out string colorHex))
        {
            Debug.LogWarning($"液体 {liquidName} 未定义，使用默认颜色白色");
            colorHex = "#FFFFFF";
        }

        if (!ColorUtility.TryParseHtmlString(colorHex, out Color liquidColor))
            liquidColor = Color.white;

        // 找到 LiquidVolume 组件
        Transform liquidObj = obj.transform.Find(LIQUID_PATH);
        if (liquidObj == null)
        {
            Debug.LogWarning($"对象 {obj.name} 下未找到 {LIQUID_PATH}");
            return;
        }

        LiquidVolume liquid = liquidObj.GetComponent<LiquidVolume>();
        if (liquid == null)
        {
            Debug.LogWarning($"未在 {liquidObj.name} 上找到 LiquidVolume 组件");
            return;
        }

        // === 修改属性 ===
        liquid.enabled = false;  // 🔄 防止未初始化状态影响
        liquid.enabled = true;   // 强制重新初始化（等价于重新挂载组件）

        liquid.level = 1.0f;
        liquid.liquidColor1 = liquidColor;
        liquid.liquidColor2 = liquidColor;
        liquid.alpha = 1.0f;
        liquid.murkiness = 0.0f;

        // === 关键刷新步骤 ===
        liquid.RefreshMaterialProperties();  // ✅ 刷新所有材质参数
        liquid.UpdateMaterialProperties();   // ✅ 最后同步到GPU
    }

    /// <summary>
    /// 填充液体（等价于 AddLiquid + 满液位）
    /// </summary>
    public void FillWithLiquid(GameObject obj, string liquidName)
    {
        AddLiquid(obj, liquidName);
    }

    /// <summary>
    /// 添加固体
    /// </summary>
    public void AddSolid(GameObject obj, string solidName)
    {
        if (obj == null) return;

        if (!ChemistryDefinitions.allowedSolids_dict.TryGetValue(solidName, out string colorHex))
        {
            Debug.LogWarning($"固体 {solidName} 未定义，使用默认颜色黑色");
            colorHex = "#000000";
        }

        if (!ColorUtility.TryParseHtmlString(colorHex, out Color solidColor))
            solidColor = Color.black;

        Transform solidObj = obj.transform.Find(SOLID_PATH);
        if (solidObj == null)
        {
            Debug.LogWarning($"对象 {obj.name} 下未找到 {SOLID_PATH}");
            return;
        }

        solidObj.gameObject.SetActive(true);
        Renderer renderer = solidObj.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = solidColor;
    }

    // ==============================
    // ⚙️ JSON 动作执行部分
    // ==============================

    /// <summary>
    /// 主执行入口：解析并依次执行步骤
    /// </summary>
    public void ExecuteSteps(string jsonText)
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("JSON 文本为空！");
            return;
        }

        JObject json;
        try
        {
            json = JObject.Parse(jsonText);
        }
        catch
        {
            Debug.LogError("JSON 解析失败！");
            return;
        }

        JArray steps = json["steps"] as JArray;
        if (steps == null)
        {
            Debug.LogError("未找到 steps 数组！");
            return;
        }

        foreach (var step in steps)
        {
            string op = step["op"]?.ToString();
            if (string.IsNullOrEmpty(op)) continue;

            switch (op)
            {
                case "AlignByAnchor":
                    {
                        var src = step["source"];
                        var tgt = step["target"];
                        string alignMode = step["alignMode"]?.ToString() ?? "AlignPositionRotation";

                        GameObject srcObj = FindOrCache(src?["equipment_name"]?.ToString());
                        GameObject tgtObj = FindOrCache(tgt?["equipment_name"]?.ToString());

                        Transform srcAnchor = srcObj?.transform.Find("Anchors/" + src?["anchor"]);
                        Transform tgtAnchor = tgtObj?.transform.Find("Anchors/" + tgt?["anchor"]);

                        AlignByAnchor(srcObj, srcAnchor, tgtObj, tgtAnchor, alignMode);
                    }
                    break;

                case "Ignite":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        StartCoroutine(Ignite(obj));
                    }
                    break;

                case "AddLiquid":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        string mat = step["material"]?.ToString();
                        AddLiquid(obj, mat);
                    }
                    break;

                case "AddSolid":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        string mat = step["material"]?.ToString();
                        AddSolid(obj, mat);
                    }
                    break;

                case "FillWithLiquid":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        string mat = step["material"]?.ToString();
                        FillWithLiquid(obj, mat);
                    }
                    break;

                case "ReverseObject":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        ReverseObject(obj);
                    }
                    break;

                case "LayFlat":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        LayFlat(obj);
                    }
                    break;

                case "TiltUp":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        TiltUp(obj);
                    }
                    break;

                case "TiltDown":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString());
                        TiltDown(obj);
                    }
                    break;

                default:
                    Debug.LogWarning($"⚠️ 未识别操作类型: {op}");
                    break;
            }
        }
    }

    // ==============================
    // 🔍 辅助函数：对象解析缓存
    // ==============================
    private GameObject FindOrCache(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var obj)) return obj;

        obj = GameObject.Find(name);
        if (obj != null) cache[name] = obj;
        else Debug.LogWarning($"❗ 未找到对象: {name}");
        return obj;
    }
}
