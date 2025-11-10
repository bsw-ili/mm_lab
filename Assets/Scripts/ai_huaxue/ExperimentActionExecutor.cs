using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using LiquidVolumeFX;
using System.Xml.Linq;
using System.Linq;
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
    private Transform currentRoot;
    //private Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

    // ==============================
    // 🧠 基础操作函数
    // ==============================

    /// <summary>
    /// 对齐物体锚点，并根据模式选择是否对齐法线方向。
    /// 支持 AlignPosition / AlignPositionRotation 模式。
    /// </summary>
    public void AlignByAnchor(GameObject sourceObj, GameObject targetObj, string alignMode, Dictionary<string, GameObject> cache,JToken src, JToken tgt)
    {
        if (sourceObj == null  || targetObj == null ) return;

        GameObject srcObj = sourceObj.name.EndsWith("_combined") ? sourceObj.transform.Find(src["equipment_name"].ToString())?.gameObject : sourceObj;

        if (srcObj == null)
        {
            Debug.LogWarning($"未找到可操作的原始物体");
            return;
        }

        GameObject tgtObj = targetObj.name.EndsWith("_combined") ? targetObj.transform.Find(tgt["equipment_name"].ToString())?.gameObject : targetObj;

        if (tgtObj == null)
        {
            Debug.LogWarning($"未找到可操作的原始物体");
            return;
        }
        Transform sourceAnchor = srcObj?.transform.Find("Anchors/" + src?["anchor"]);
        Transform targetAnchor = tgtObj?.transform.Find("Anchors/" + tgt?["anchor"]);

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
            // 这种操作需要把targetobj或sourceobj从combined中分离出来
            Transform sceneRoot = currentRoot;
            if (sceneRoot == null)
            {
                Debug.LogError("❌ 未找到 SceneRoot，无法正确设置父物体。");
                return;
            }

            // ✅ 若 sourceObj 或 targetObj 在 combined 中，则分离并挂到 SceneRoot 下
            if (srcObj.transform.parent != null && srcObj.transform.parent.name.EndsWith("_combined"))
            {
                string oldParent = srcObj.transform.parent.name;
                sourceObj.transform.SetParent(sceneRoot, true);
                Debug.Log($"🔹 已将 {srcObj.name} 从 {oldParent} 分离并挂到 SceneRoot 下");
            }

            if (tgtObj.transform.parent != null && tgtObj.transform.parent.name.EndsWith("_combined"))
            {
                string oldParent = tgtObj.transform.parent.name;
                tgtObj.transform.SetParent(sceneRoot, true);
                Debug.Log($"🔹 已将 {tgtObj.name} 从 {oldParent} 分离并挂到 {sceneRoot.name} 下");
            }

            // ✅ Reset 源物体和目标物体 Transform（重置到统一世界原点姿态）
            //srcObj.transform.localPosition = Vector3.zero;
            //srcObj.transform.localRotation = Quaternion.identity;
            //srcObj.transform.localScale = Vector3.one;

            tgtObj.transform.localPosition = Vector3.zero;
            tgtObj.transform.localRotation = Quaternion.identity;
            tgtObj.transform.localScale = Vector3.one;

            Debug.Log($"♻️ 已重置 {srcObj.name} 和 {tgtObj.name} 的 Transform");

            // ✅ Reset 后重新获取锚点（因为锚点世界坐标会改变）
            sourceAnchor = srcObj.transform.Find("Anchors/" + src?["anchor"]);
            targetAnchor = tgtObj.transform.Find("Anchors/" + tgt?["anchor"]);

            if (sourceAnchor == null || targetAnchor == null)
            {
                Debug.LogError($"❌ 未找到锚点：{srcObj.name} 或 {tgtObj.name}");
                return;
            }



            // ✅ 将源锚点世界坐标与目标锚点世界坐标对齐
            anchorLocalPos = srcObj.transform.InverseTransformPoint(sourceAnchor.position);
            targetWorldPos = targetAnchor.position;
            Vector3 currentAnchorWorld = srcObj.transform.TransformPoint(anchorLocalPos);
            Vector3 delta = targetWorldPos - currentAnchorWorld;
            srcObj.transform.position += delta;

            // ✳️ 对齐完成后检查是否重叠（沿目标锚点法线方向推开）
            EnsureNoOverlap_AlongNormal(srcObj, tgtObj, 0.01f);  // padding = 1cm

            cache[srcObj.name] = srcObj;
            cache[tgtObj.name] = tgtObj;
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
            // ----------------------------------------
            // ✅ 合并逻辑（新的父对象 = “source_target_combined”）
            // ----------------------------------------
            // ✅ 改进的合并逻辑（支持递归合并）
            // ✅ 通用合并逻辑（支持多层 combined 合并）
            // ✅ 改进的合并逻辑（保持单层 combined 结构）
            Transform parentTransform = currentRoot;
            GameObject combinedObj = null;

            // 判断 combined 状态
            bool sourceIsCombined = sourceObj.name.Contains("_combined");
            bool targetIsCombined = targetObj.name.Contains("_combined");

            // 1️⃣ 两者都不是 combined → 新建 combined 容器
            if (!sourceIsCombined && !targetIsCombined)
            {
                string combinedName = $"{sourceObj.name}_{targetObj.name}_combined";
                combinedObj = new GameObject(combinedName);
                combinedObj.transform.SetParent(parentTransform, false);
                combinedObj.transform.position = targetObj.transform.position;
                combinedObj.transform.rotation = targetObj.transform.rotation;

                sourceObj.transform.SetParent(combinedObj.transform, true);
                targetObj.transform.SetParent(combinedObj.transform, true);
            }
            // 2️⃣ target 是 combined → 把 source 加入 target 的 combined
            else if (!sourceIsCombined && targetIsCombined)
            {
                combinedObj = targetObj;
                sourceObj.transform.SetParent(combinedObj.transform, true);
                combinedObj.name = sourceObj.name+"_"+targetObj.name;
            }
            // 3️⃣ source 是 combined → 把 target 加入 source 的 combined
            else if (sourceIsCombined && !targetIsCombined)
            {
                combinedObj = sourceObj;
                targetObj.transform.SetParent(combinedObj.transform, true);
                combinedObj.name = targetObj.name + "_" + sourceObj.name;
            }
            // 4️⃣ 两者都是 combined → 合并两个 combined 的子物体
            else
            {
                if(sourceObj == targetObj)
                {
                    Debug.Log("⚠️ 尝试合并相同的 combined 对象，操作已跳过。");
                    return;
                }
                // 优先保留 targetObj 的 combined（也可以根据需要反过来）
                combinedObj = targetObj;

                // 将 sourceObj 的所有子物体搬运到 targetObj 下
                List<Transform> children = new List<Transform>();
                string combinedName = "";
                foreach (Transform child in sourceObj.transform)
                {
                    children.Add(child);
                    combinedName += child.name + "_";
                }


                foreach (Transform child in children)
                {
                    child.SetParent(combinedObj.transform, true);
                }

                combinedObj.name = combinedName+ combinedObj.name;

                // 然后销毁空的 sourceObj
                UnityEngine.Object.Destroy(sourceObj);
                // 确保缓存中不再引用已销毁的对象
                List<string> keysToRemove = new List<string>();
                foreach (var kv in cache)
                {
                    if (kv.Value == sourceObj)
                        keysToRemove.Add(kv.Key);
                }
                foreach (string key in keysToRemove)
                {
                    cache.Remove(key);
                }

                Debug.Log($"♻️ 已合并两个 combined：保留 {targetObj.name}，销毁 {sourceObj.name}");
            }

            Debug.Log($"✅ 合并完成：{sourceObj.name} 与 {targetObj.name} → {combinedObj.name}");

            // ✅ 更新缓存：统一引用到最终 combinedObj
            List<string> keysToUpdate = new List<string>();
            foreach (var kv in cache)
            {
                if (kv.Value == sourceObj || kv.Value == targetObj)
                    keysToUpdate.Add(kv.Key);
            }
            foreach (string key in keysToUpdate)
            {
                cache[key] = combinedObj;
            }

            return;
        }

        Debug.LogWarning($"⚠️ 未识别的对齐模式: {alignMode}");
    }

    #region ==== 🔧 防止穿模工具函数 ====

    private bool TryGetCombinedBounds(GameObject go, out Bounds bounds)
    {
        bounds = new Bounds();
        if (go == null) return false;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }
        else
        {
            float approx = 0.05f;
            bounds = new Bounds(go.transform.position, Vector3.one * approx * 2f);
            return true;
        }
    }

    /// <summary>
    /// 沿目标锚点法向量推开 sourceObj，避免穿模
    /// </summary>
    private void EnsureNoOverlap_AlongNormal(GameObject sourceObj, GameObject targetObj, float padding = 0.001f)
    {
        if (sourceObj == null || targetObj == null) return;

        Collider srcCol = sourceObj.GetComponentInChildren<Collider>();
        Collider tarCol = targetObj.GetComponentInChildren<Collider>();
        if (srcCol == null || tarCol == null) return;

        Vector3 direction;
        float distance;

        bool overlap = Physics.ComputePenetration(
            srcCol, sourceObj.transform.position, sourceObj.transform.rotation,
            tarCol, targetObj.transform.position, targetObj.transform.rotation,
            out direction, out distance
        );

        if (!overlap || distance <= 0f) return;

        // 根据严重重叠阈值，控制移动
        // 可以把 distance / targetColliderSize 作为 overlapRatio
        float overlapRatio = distance / Mathf.Max(tarCol.bounds.size.magnitude, 0.0001f);

        Vector3 move;
        
        // 沿穿透方向移动
        float scaleFactor = 0.5f; // 缩小移动幅度，避免过大
        move = direction.normalized * distance * scaleFactor;
        

        Vector3 newPos = sourceObj.transform.position + move;
        sourceObj.transform.position = newPos;

        Rigidbody rb = sourceObj.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            rb.MovePosition(newPos);
    }
    #endregion


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
        fire.SetActive(true);
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
    //public void AddLiquid(GameObject obj, string liquidName)
    //{
    //    if (obj == null) return;

    //    // 获取颜色定义
    //    if (!ChemistryDefinitions.allowedLiquids_dict.TryGetValue(liquidName, out string colorHex))
    //    {
    //        Debug.LogWarning($"液体 {liquidName} 未定义，使用默认颜色白色");
    //        colorHex = "#FFFFFF";
    //    }

    //    if (!ColorUtility.TryParseHtmlString(colorHex, out Color liquidColor))
    //        liquidColor = Color.white;

    //    // 找到 LiquidVolume 组件
    //    Transform liquidObj = obj.transform.Find(LIQUID_PATH);
    //    if (liquidObj == null)
    //    {
    //        Debug.LogWarning($"对象 {obj.name} 下未找到 {LIQUID_PATH}");
    //        return;
    //    }

    //    LiquidVolume liquid = liquidObj.GetComponent<LiquidVolume>();
    //    if (liquid == null)
    //    {
    //        Debug.LogWarning($"未在 {liquidObj.name} 上找到 LiquidVolume 组件");
    //        return;
    //    }

    //    // === 修改属性 ===
    //    liquid.enabled = false;  // 🔄 防止未初始化状态影响
    //    liquid.enabled = true;   // 强制重新初始化（等价于重新挂载组件）

    //    liquid.level = 1.0f;
    //    liquid.liquidColor1 = liquidColor;
    //    liquid.liquidColor2 = liquidColor;
    //    liquid.alpha = 1.0f;
    //    liquid.murkiness = 0.0f;

    //    liquidObj.localEulerAngles = new Vector3(0, liquidObj.localEulerAngles.y, 0);

    //    // === 关键刷新步骤 ===
    //    liquid.RefreshMaterialProperties();  // ✅ 刷新所有材质参数
    //    liquid.UpdateMaterialProperties();   // ✅ 最后同步到GPU
    //}

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

    public void AddLiquid(GameObject obj, string liquidName)
    {
        if (obj == null) return;

        if (!ChemistryDefinitions.allowedLiquids_dict.TryGetValue(liquidName, out string colorHex))
        {
            Debug.LogWarning($"液体 {liquidName} 未定义，使用默认颜色白色");
            colorHex = "#FFFFFF";
        }

        if (!ColorUtility.TryParseHtmlString(colorHex, out Color solidColor))
            solidColor = Color.white;

        Transform solidObj = obj.transform.Find(LIQUID_PATH);
        if (solidObj == null)
        {
            Debug.LogWarning($"对象 {obj.name} 下未找到 {LIQUID_PATH}");
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
    public void ExecuteSteps(string jsonText, Dictionary<string, GameObject> cache, Transform currentSceneRoot)
    {
        currentRoot = currentSceneRoot;
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
            // 列表记录每一步操作
            JToken src = "";
            JToken tgt = "";
            string alignMode = "";
            GameObject srcObj = null;
            GameObject tgtObj = null;
            if (op == "AlignByAnchor")
            {
                src = step["source"];
                tgt = step["target"];
                alignMode = step["alignMode"]?.ToString() ?? "AlignPositionRotation";
                srcObj = FindOrCache(src?["equipment_name"]?.ToString(), cache);
                tgtObj = FindOrCache(tgt?["equipment_name"]?.ToString(), cache);
            }

            switch (op)
            {
                case "AlignByAnchor":
                    {
                        AlignByAnchor(srcObj, tgtObj, alignMode,cache,src,tgt);
                    }
                    break;

                case "Ignite":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(), cache);
                        // 如果 obj 是 combinedObj，确保找到原始物体
                        GameObject targetObj = obj.name.EndsWith("_combined") ? obj.transform.Find(step["equipment"]?["equipment_name"]?.ToString())?.gameObject : obj;

                        if (targetObj == null)
                        {
                            Debug.LogWarning($"未找到可操作的原始物体");
                            return;
                        }
                        StartCoroutine(Ignite(targetObj));
                    }
                    break;

                case "AddLiquid":
                    {

                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(),cache);
                        // 如果 obj 是 combinedObj，确保找到原始物体
                        GameObject targetObj = obj.name.EndsWith("_combined") ? obj.transform.Find(step["equipment"]?["equipment_name"]?.ToString())?.gameObject : obj;

                        if (targetObj == null)
                        {
                            Debug.LogWarning($"未找到可操作的原始物体");
                            return;
                        }
                        string mat = step["material"]?.ToString();
                        AddLiquid(targetObj, mat);
                    }
                    break;

                case "AddSolid":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(), cache);
                        // 如果 obj 是 combinedObj，确保找到原始物体
                        GameObject targetObj = obj.name.EndsWith("_combined") ? obj.transform.Find(step["equipment"]?["equipment_name"]?.ToString())?.gameObject : obj;

                        if (targetObj == null)
                        {
                            Debug.LogWarning($"未找到可操作的原始物体");
                            return;
                        }
                        string mat = step["material"]?.ToString();
                        AddSolid(targetObj, mat);
                    }
                    break;

                case "FillWithLiquid":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(),cache);
                        // 如果 obj 是 combinedObj，确保找到原始物体
                        GameObject targetObj = obj.name.EndsWith("_combined") ? obj.transform.Find(step["equipment"]?["equipment_name"]?.ToString())?.gameObject : obj;

                        if (targetObj == null)
                        {
                            Debug.LogWarning($"未找到可操作的原始物体");
                            return;
                        }
                        string mat = step["material"]?.ToString();
                        FillWithLiquid(targetObj, mat);
                    }
                    break;

                case "ReverseObject":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(), cache);
                        ReverseObject(obj);
                        if(srcObj != null && tgtObj != null)
                        AlignByAnchor(srcObj, tgtObj, alignMode= "alignposition", cache,src,tgt);
                    }
                    break;

                case "LayFlat":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(),cache);
                        LayFlat(obj);
                        if(srcObj != null && tgtObj != null)
                        AlignByAnchor(srcObj, tgtObj, alignMode = "alignposition", cache, src, tgt);


                    }
                    break;

                case "TiltUp":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(), cache);
                        TiltUp(obj);
                        if(srcObj != null && tgtObj != null)
                        AlignByAnchor(srcObj, tgtObj, alignMode = "alignposition", cache, src, tgt);
                    }
                    break;

                case "TiltDown":
                    {
                        GameObject obj = FindOrCache(step["equipment"]?["equipment_name"]?.ToString(),cache);
                        TiltDown(obj);
                        if(srcObj != null && tgtObj != null)
                        AlignByAnchor(srcObj, tgtObj, alignMode = "alignposition", cache, src, tgt);
                    }
                    break;

                default:
                    Debug.LogWarning($"⚠️ 未识别操作类型: {op}");
                    break;
            }
        }
    }
    public void Transfer(GameObject sourceObj, GameObject targetObj)
    {
        Transform solidObj = sourceObj.transform.Find(LIQUID_PATH);
        if (solidObj == null)
        {
            Debug.LogWarning($"对象 {sourceObj.name} 下未找到 {LIQUID_PATH}");
            return;
        }

        Renderer renderer = solidObj.GetComponent<Renderer>();

        Transform tarObj = targetObj.transform.Find(LIQUID_PATH);
        if (tarObj == null)
        {
            Debug.LogWarning($"对象 {tarObj.name} 下未找到 {LIQUID_PATH}");
            return;
        }
        
        Renderer renderer1 = tarObj.GetComponent<Renderer>();
        if (renderer != null&& renderer1!=null) renderer1.material.color = renderer.material.color;
        solidObj.gameObject.SetActive(false);
        tarObj.gameObject.SetActive(true);
    }

    public void AlignByAnchor_c(string objAName,string anchorA, string objBName, string anchorB,string alignMode,bool reverse=false)
    {
        
        if (reverse)
        {
            // 交换物体和锚点
            string tempObj = objAName;
            objAName = objBName;
            objBName = tempObj;

            string tempAnchor = anchorA;
            anchorA = anchorB;
            anchorB = tempAnchor;
        }
        GameObject srcObj = GameObject.Find(objAName);
        GameObject tgtObj = GameObject.Find(objBName);

        if (srcObj == null || tgtObj == null)
        {
            Debug.LogError($"❌ 找不到物体：{objAName} 或 {objBName}");
            return;
        }

        Transform sourceAnchor = srcObj.transform.Find("Anchors/" + anchorA);
        Transform targetAnchor = tgtObj.transform.Find("Anchors/" + anchorB);

        if (sourceAnchor == null || targetAnchor == null)
        {
            Debug.LogError($"❌ 找不到锚点：{anchorA} 或 {anchorB}");
            return;
        }

        // 特殊逻辑：盖上酒精灯 → 关闭火焰
        if (srcObj.name == "alcohol_lamp_cap" && tgtObj.name == "alcohol_lamp")
        {
            GameObject fire = tgtObj.transform.Find("Fire")?.gameObject;
            if (fire != null) fire.SetActive(false);
        }

        // 默认模式
        if (string.IsNullOrEmpty(alignMode))
            alignMode = "AlignPositionRotation";

        string mode = alignMode.Trim().ToLowerInvariant();

        // -----------------------------
        // 1️⃣ 计算锚点相对信息
        // -----------------------------
        Vector3 anchorLocalPos = srcObj.transform.InverseTransformPoint(sourceAnchor.position);
        Vector3 anchorLocalUp = srcObj.transform.InverseTransformDirection(sourceAnchor.up);
        Vector3 targetWorldPos = targetAnchor.position;
        Vector3 targetWorldUp = targetAnchor.up;

        // -----------------------------
        // 2️⃣ 仅对齐位置
        // -----------------------------
        if (mode == "alignposition")
        {
            Vector3 currentAnchorWorld = srcObj.transform.TransformPoint(anchorLocalPos);
            Vector3 delta = targetWorldPos - currentAnchorWorld;
            srcObj.transform.position += delta;

            // 避免重叠
            EnsureNoOverlap_AlongNormal(srcObj, tgtObj, 0.01f);
            Debug.Log($"✅ 已完成位置对齐：{objAName} → {objBName}");
            return;
        }

        // -----------------------------
        // 3️⃣ 对齐旋转 + 位置
        // -----------------------------
        if (mode == "alignpositionrotation")
        {
            Vector3 currentUpWorld = srcObj.transform.TransformDirection(anchorLocalUp);
            Quaternion rot = Quaternion.FromToRotation(currentUpWorld, targetWorldUp);
            srcObj.transform.rotation = rot * srcObj.transform.rotation;

            Vector3 anchorWorldAfterRotate = srcObj.transform.TransformPoint(anchorLocalPos);
            Vector3 translation = targetWorldPos - anchorWorldAfterRotate;
            srcObj.transform.position += translation;

            Debug.Log($"✅ 已完成位置+旋转对齐：{objAName} → {objBName}");

            // ✅ 添加连接记录
            ConnectionTracker trackerA = srcObj.GetComponent<ConnectionTracker>();
            if (trackerA == null) trackerA = srcObj.AddComponent<ConnectionTracker>();

            ConnectionTracker trackerB = tgtObj.GetComponent<ConnectionTracker>();
            if (trackerB == null) trackerB = tgtObj.AddComponent<ConnectionTracker>();

            trackerA.AddConnection(objBName, anchorA, anchorB, alignMode);
            trackerB.AddConnection(objAName, anchorB, anchorA, alignMode);
            Debug.Log($"✅ 已完成位置+旋转对齐并建立连接：{objAName} ↔ {objBName}");
            return;
        }

        Debug.LogWarning($"⚠️ 未识别的对齐模式: {alignMode}");
    }

    /// <summary>
    /// 将物体竖直放置并沿 Z 轴平移到不与 SceneRoot 中其他物体重叠的位置。
    /// </summary>
    private void PlaceSafely_AtMaxZ(GameObject obj, Transform sceneRoot, float padding = 0.01f)
    {
        if (obj == null || sceneRoot == null) return;
        Collider objCol = obj.GetComponentInChildren<Collider>();
        if (objCol == null) return;

        // 竖直放置tool
        obj.transform.eulerAngles = Vector3.zero;

        // 2️⃣ 获取当前场景中所有其他碰撞体的最大 Z 边界
        float maxZ = float.MinValue;
        Collider[] allCols = sceneRoot.GetComponentsInChildren<Collider>(true);
        foreach (var col in allCols)
        {
            if (col == objCol) continue;
            float zMax = col.bounds.max.z;
            if (zMax > maxZ) maxZ = zMax;
        }

        // 3️⃣ 将当前物体沿 Z 轴平移到该最大 Z 边界之后
        float objHalfZ = objCol.bounds.extents.z;
        Vector3 pos = obj.transform.position;
        pos.z = maxZ + objHalfZ + padding;
        obj.transform.position = pos;
    }


    public void ex_xdl(XElement action,string apiResponse)
    {
        string name = action.Name.LocalName;

        // 提取参与对象
        string objA = null;
        string objB = null;

        if (string.IsNullOrEmpty(apiResponse))
        {
            Debug.LogError("❌ apiResponse 为空！");
            return;
        }

        JObject json;
        try
        {
            json = JObject.Parse(apiResponse);
        }
        catch
        {
            Debug.LogError("❌ JSON 解析失败：" + apiResponse);
            return;
        }

        string objAName = json["objectA"]?.ToString();
        string objBName = json["objectB"]?.ToString();
        string anchorA = json["anchorA"]?.ToString();
        string anchorB = json["anchorB"]?.ToString();

        if (string.IsNullOrEmpty(objAName) || string.IsNullOrEmpty(objBName) ||
            string.IsNullOrEmpty(anchorA) || string.IsNullOrEmpty(anchorB))
        {
            Debug.LogError("❌ apiResponse 缺少必要字段（objectA, objectB, anchorA, anchorB）");
            return;
        }

        switch (name)
        {
            case "Attach":
                GameObject gameobjA = GameObject.Find(objAName);
                GameObject gameobjB = GameObject.Find(objBName);
                ConnectionTracker trackerA = gameobjA.GetComponent<ConnectionTracker>();
                ConnectionTracker trackerB = gameobjB.GetComponent<ConnectionTracker>();
                bool reverse = false;
                if (trackerA == null && trackerB ==null)
                {
                    Debug.LogError("❌ ConnectionTracker 组件未找到" );
                    break;
                }
                if (trackerA.connections.Count > trackerB.connections.Count)
                {
                    reverse = true;
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                    for(int i =0; i<trackerA.connections.Count;i++ )
                    {
                        var conn = trackerA.connections[i];
                        if (i< trackerA.connections.Count-1)
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation");
                        }
                        else
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation", reverse);
                        }
                    }
                }
                else
                {
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                }

                break;

            case "Insert":
                gameobjA = GameObject.Find(objAName);
                gameobjB = GameObject.Find(objBName);
                trackerA = gameobjA.GetComponent<ConnectionTracker>();
                trackerB = gameobjB.GetComponent<ConnectionTracker>();
                reverse = false;
                if (trackerA == null && trackerB == null)
                {
                    Debug.LogError("❌ ConnectionTracker 组件未找到");
                    break;
                }
                if (trackerA.connections.Count > trackerB.connections.Count)
                {
                    reverse = true;
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                    for (int i = 0; i < trackerA.connections.Count; i++)
                    {
                        var conn = trackerA.connections[i];
                        if (i < trackerA.connections.Count - 1)
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation");
                        }
                        else
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation", reverse);
                        }
                    }
                }
                else
                {
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                }
                break;

            case "Add":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                string reagent = action.Attribute("reagent")?.Value;

                if (string.IsNullOrEmpty(objA) || string.IsNullOrEmpty(objB))
                    break;

                GameObject toolObj = GameObject.Find(objA);
                GameObject vesselObj = GameObject.Find(objB);
                if (toolObj == null || vesselObj == null)
                    break;

                Transform sceneRoot = GameObject.Find("SceneRoot")?.transform;
                // ===== Step 2：竖直放置并移动到 SceneRoot 最远的 Z 位置 =====
                PlaceSafely_AtMaxZ(vesselObj, sceneRoot, 0.01f);
                if (objA.Contains("spatula"))
                {
                    // 平放tool
                    LayFlat(toolObj);
                    AddSolid(toolObj, reagent);
                    AddSolid(vesselObj, reagent);

                    // 平放容器
                    LayFlat(vesselObj);
                    // 对齐锚点及法向量
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                }
                else if(objA.Contains("dropper"))
                {
                    AddLiquid(toolObj, reagent);
                    AddLiquid(vesselObj, reagent);
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                }
                else
                {
                    // 如果是液体，则倒入
                    // 旋转tool
                    TiltUp(toolObj);
                    AddLiquid(toolObj, reagent);
                    AddLiquid(vesselObj, reagent);
                    AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPosition");
                }

                break;

            case "Transfer":
                objA = action.Attribute("from_vessel")?.Value;
                objB = action.Attribute("to_vessel")?.Value;

                if (string.IsNullOrEmpty(objA) || string.IsNullOrEmpty(objB))
                    break;

                toolObj = GameObject.Find(objA);
                vesselObj = GameObject.Find(objB);
                if (toolObj == null || vesselObj == null)
                    break;
                sceneRoot = GameObject.Find("SceneRoot")?.transform;
                // ===== Step 2：竖直放置并移动到 SceneRoot 最远的 Z 位置 =====
                PlaceSafely_AtMaxZ(vesselObj, sceneRoot, 0.01f);
                
                TiltUp(toolObj);
                Transfer(toolObj, vesselObj);
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPosition");
                break;

            case "Stir":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;

                if (string.IsNullOrEmpty(objA) || string.IsNullOrEmpty(objB))
                    break;

                toolObj = GameObject.Find(objA);
                vesselObj = GameObject.Find(objB);
                if (toolObj == null || vesselObj == null)
                    break;
                sceneRoot = GameObject.Find("SceneRoot")?.transform;
                // ===== Step 2：竖直放置并移动到 SceneRoot 最远的 Z 位置 =====
                PlaceSafely_AtMaxZ(vesselObj, sceneRoot, 0.01f);
                // 对齐锚点及法向量
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                break;

            case "Heat":
                objA = action.Attribute("tool")?.Value;
                GameObject fire = GameObject.Find(objA);
                StartCoroutine(Ignite(fire));
                // 对齐锚点
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPosition");
                break;
            case "Cool":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;

                if (string.IsNullOrEmpty(objA) || string.IsNullOrEmpty(objB))
                    break;

                toolObj = GameObject.Find(objA);
                vesselObj = GameObject.Find(objB);
                if (toolObj == null || vesselObj == null)
                    break;
                sceneRoot = GameObject.Find("SceneRoot")?.transform;
                // ===== Step 2：竖直放置并移动到 SceneRoot 最远的 Z 位置 =====
                PlaceSafely_AtMaxZ(vesselObj, sceneRoot, 0.01f);
                // 对齐锚点
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPosition");
                break;

            case "MeasureTemperature":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;

                if (string.IsNullOrEmpty(objA) || string.IsNullOrEmpty(objB))
                    break;

                toolObj = GameObject.Find(objA);
                vesselObj = GameObject.Find(objB);
                if (toolObj == null || vesselObj == null)
                    break;
                sceneRoot = GameObject.Find("SceneRoot")?.transform;
                // ===== Step 2：竖直放置并移动到 SceneRoot 最远的 Z 位置 =====
                PlaceSafely_AtMaxZ(vesselObj, sceneRoot, 0.01f);
                // 对齐锚点及法向量
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPositionRotation");
                break;

            case "Filter":
                // 对齐锚点

                // 倒入
                objA = action.Attribute("from_vessel")?.Value;
                // 旋转tool
                GameObject tool1 = GameObject.Find(objA);
                TiltUp(tool1);
                AlignByAnchor_c(objAName, anchorA, objBName, anchorB, "AlignPosition");
                break;

            case "CollectGas":
                objA = action.Attribute("collector")?.Value;
                GameObject obj = GameObject.Find(objA);
                ReverseObject(obj);
                gameobjA = FindObjectsOfType<GameObject>()
                .FirstOrDefault(obj => obj.name.Contains("rubber_stopper_with_delivery_tube")); ;
                gameobjB = GameObject.Find(objBName);
                trackerA = gameobjA.GetComponent<ConnectionTracker>();
                trackerB = gameobjB.GetComponent<ConnectionTracker>();
                reverse = false;
                if (trackerA == null && trackerB == null)
                {
                    Debug.LogError("❌ ConnectionTracker 组件未找到");
                    break;
                }
                if (trackerA.connections.Count > trackerB.connections.Count)
                {
                    reverse = true;
                    AlignByAnchor_c(objAName, "tube_exit_port", objBName, anchorB, "AlignPositionRotation");
                    for (int i = 0; i < trackerA.connections.Count; i++)
                    {
                        var conn = trackerA.connections[i];
                        if (i < trackerA.connections.Count - 1)
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation");
                        }
                        else
                        {
                            AlignByAnchor_c(trackerA.gameObject.name, conn.anchorA, conn.connectedObject, conn.anchorB, "AlignPositionRotation", reverse);
                        }
                    }
                }
                else
                {
                    AlignByAnchor_c(objAName, "tube_exit_port", objBName, anchorB, "AlignPositionRotation");
                }
                break;
        }
    }

    // ==============================
    // 🔍 辅助函数：对象解析缓存
    // ==============================
    private GameObject FindOrCache(string name, Dictionary<string, GameObject> cache)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var obj)) return obj;

        obj = currentRoot.transform.Find(name)?.gameObject;
        if (obj != null) cache[name] = obj;
        else Debug.LogWarning($"❗ 未找到对象: {name}");
        return obj;
    }
}
