using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;

public class DialogueItem
{
    [JsonProperty("system")]
    public string System { get; set; }

    [JsonProperty("user")]
    public string User { get; set; }

    [JsonProperty("user_type")]
    public string UserType { get; set; }

    // ✅ 改为路径列表
    public List<Dictionary<string, OpInfo>> UserOps { get; set; } = new();
}

public class OpInfo
{
    public List<string> OpeningImage { get; set; }
    public string Steps { get; set; }
    public List<string> EndPicture { get; set; }
}

public class ChemTrain
{
    [JsonProperty("chem_experiment")]
    public string ChemExperiment { get; set; }

    [JsonProperty("chem_analysis")]
    public string ChemAnalysis { get; set; }

    [JsonProperty("chem_steps")]
    public List<string> ChemSteps { get; set; }

    [JsonProperty("dialogues")]
    public Dictionary<string, List<DialogueItem>> Dialogues { get; set; }
}

public class text2json : MonoBehaviour
{
    public OpenAIExtractor extractor;
    public OpenAIExtractor_json extractor_json;
    public OpenAIExtractor_text extractor_text;
    public json2pic json2Pic;
    public MultiAngleScreenshot mfs;
    public ExperimentActionExecutor actionExecutor;

    private readonly Regex operationRegex = new Regex(@"-\s*(.+)");
    private string outputDir;
    public Transform parentTransform;

    private async void Start()
    {
        Debug.Log("Start text2json");
        outputDir = Path.Combine(Application.dataPath, "image_output");

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir); // ✅ 自动创建输出目录
        string folderPath = @"D:\postgraduate\多模态大模型_化学实验\output\json_results"; // 文件夹路径

        // 获取文件夹下的所有文件路径
        string[] files = Directory.GetFiles(folderPath);

        foreach (var file in files)
        {
            await ProcessJsonFileAsync(file);
            //break; // 只处理第一个文件进行测试
        }
        
    }

    private async Task<string> SafeExtract(Func<Task<string>> func)
    {
        try { return await func(); }
        catch (Exception ex)
        {
            Debug.LogError($"❌ OpenAI提取错误: {ex.Message}");
            return string.Empty;
        }
    }

    public Task<string> GetOp(string reply) => SafeExtract(() => extractor.Extract(reply));
    public Task<string> GetJson(string reply, (GameObject opObj, GameObject targetObj) op_objects) => SafeExtract(() => extractor_json.Extract(reply, op_objects));
    public Task<string> GetText(string reply, string premise) => SafeExtract(() => extractor_text.Extract(reply, premise));

    /// <summary>
    /// 清空 SceneRoot 下的所有子物体（但保留 SceneRoot 本身）
    /// </summary>
    public void ClearSceneRoot()
    {
        if (parentTransform == null)
        {
            Debug.LogWarning("SceneRoot 未指定！");
            return;
        }

        // 用逆序遍历防止子物体销毁时索引变化
        for (int i = parentTransform.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = parentTransform.transform.GetChild(i);
            Destroy(child.gameObject);
        }

        Debug.Log("✅ SceneRoot 已清空");
    }

    // ✅ 主处理流程
    private async Task ProcessJsonFileAsync(string textPath)
    {
        if (!File.Exists(textPath))
        {
            Debug.LogError($"❌ 文件不存在: {textPath}");
            return;
        }

        string jsonContent = await File.ReadAllTextAsync(textPath);
        var data = JsonConvert.DeserializeObject<ChemTrain>(jsonContent);
        if (data == null)
        {
            Debug.LogError("❌ JSON反序列化失败。");
            return;
        }

        
        foreach (var d in data.Dialogues) 
        { 
            for (int i = 0; i < d.Value.Count; i++) 
            { 
                var item = d.Value[i]; 
                if (string.IsNullOrEmpty(item.User)) continue; 
                DialogueItem new_item = await ProcessDialogueItem(item); 
                if (new_item != null) { 
                    // 只更新指定字段
                    item.UserOps = new_item.UserOps ?? item.UserOps; 
                    // 其他字段保持不变
                }
                //break;
            }
            //break;
        } 
        

        // ✅ 保存为新文件
        string newPath = Path.Combine(Path.GetDirectoryName(textPath), "updated_" + Path.GetFileName(textPath));
        string updatedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(newPath, updatedJson);
        Debug.Log($"✅ 处理完成，结果已保存到: {newPath}");
    }

    
    /// <summary>
    /// 计算实例对象的合并包围盒（世界坐标）
    /// 若没有 Renderer，返回一个基于 transform 的近似 Bounds（center = transform.position，size 使用默认）
    /// </summary>
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
            // 没有 renderer 时，使用 transform.position 和基于名字/缓存的默认尺寸做近似
            float approx = extractor_json.GetApproximateSize(go);
            bounds = new Bounds(go.transform.position, Vector3.one * approx * 2f); // extents = approx
            return true;
        }
    }

    /// <summary>
    /// 将 operator 移到 target 的右侧以保证不重叠（仅沿全局 X 轴移动）。
    /// 保留 operator 的 Y/Z。
    /// </summary>
    private void EnsureNoOverlap_MoveOperatorRight(GameObject operatorObj, GameObject targetObj, float padding)
    {
        if (operatorObj == null || targetObj == null) return;

        // 获取当前世界包围盒
        if (!TryGetCombinedBounds(operatorObj, out Bounds opBounds) || !TryGetCombinedBounds(targetObj, out Bounds tarBounds))
            return;

        // 若 operator 的最小 X 小于等于 target 的最大 X + padding，说明有重叠或贴近，需要移动 operator
        float desiredMinX = tarBounds.max.x + padding;
        float currentOpMinX = opBounds.min.x;

        if (currentOpMinX <= desiredMinX)
        {
            float shift = desiredMinX - currentOpMinX;
            Vector3 newPos = operatorObj.transform.position + new Vector3(shift, 0f, 0f);
            operatorObj.transform.position = newPos;

            // 如果 operator 有 Rigidbody 并且不是 kinematic，建议同步移动刚体位置（避免物理冲突）
            Rigidbody rb = operatorObj.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.MovePosition(newPos);
            }

            // 可选：如果 operator 有父物体，移动父物体（本实现直接移动 object 自身）
        }
    }
    private (GameObject opObj, GameObject targetObj) place_object_begin(string optext, Dictionary<string, GameObject> cache,string opName,string targetName,Transform currentSceneRoot)
    {
        if (string.IsNullOrEmpty(opName) || string.IsNullOrEmpty(targetName))
        {
            Debug.LogError("JSON 中缺少 '操作物体' 或 '被操作物体' 字段");
            return (null, null);
        }

        // 加载预制体
        GameObject opPrefab = Resources.Load<GameObject>(opName);
        GameObject targetPrefab = Resources.Load<GameObject>(targetName);
        if (opPrefab == null || targetPrefab == null)
        {
            Debug.LogError($"无法在 Resources 找到预制体：{opName} 或 {targetName}");
            return (null, null);
        }

        // 场景中查找
        GameObject opObj = FindOrCache(opPrefab.name,cache, currentSceneRoot);
        GameObject targetObj = FindOrCache(targetPrefab.name,cache, currentSceneRoot);
        bool opExisted = opObj != null;
        bool targetExisted = targetObj != null;

        float padding = 0.1f;

        // ============ 初始化 targetObj ============
        if (!targetExisted)
        {
            targetObj = Instantiate(targetPrefab, Vector3.zero, Quaternion.identity, currentSceneRoot);
            targetObj.name = targetPrefab.name;
            cache[targetObj.name] = targetObj;
        }
        else
        {
            // 若存在，重置其 transform（位置、旋转、缩放）
            targetObj.transform.SetParent(currentSceneRoot);
        }

        // ============ 初始化 opObj ============
        if (!opExisted)
        {
            // 暂放右侧
            Vector3 tempPos = targetObj.transform.position +
                new Vector3((extractor_json.GetApproximateSize(opPrefab) + extractor_json.GetApproximateSize(targetPrefab)) + padding, 0f, 0f);
            opObj = Instantiate(opPrefab, tempPos, Quaternion.identity, currentSceneRoot);
            opObj.name = opPrefab.name;
            cache[opObj.name] = opObj;
        }
        else
        {
            // 若存在，也重置 transform
            opObj.transform.SetParent(currentSceneRoot);

            // 将其移动到 target 右侧初始位置
            Vector3 tempPos = targetObj.transform.position +
                new Vector3((extractor_json.GetApproximateSize(opObj) + extractor_json.GetApproximateSize(targetObj)) + padding, 0f, 0f);
            opObj.transform.localPosition = tempPos;
        }
        // ============ 对齐 opObj 的法向量 ============
        //Quaternion rotationDelta = Quaternion.FromToRotation(opObj.transform.up, targetObj.transform.up);
        //opObj.transform.rotation = rotationDelta * opObj.transform.rotation;


        // ============ 确保不重叠 ============
        EnsureNoOverlap_MoveOperatorRight(opObj, targetObj, padding);

        return (opObj, targetObj);
    }
    // ==============================
    // 🔍 辅助函数：对象解析缓存
    // ==============================
    private GameObject FindOrCache(string name, Dictionary<string, GameObject> cache,Transform currentSceneRoot)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (cache.TryGetValue(name, out var obj)) return obj;

        obj = currentSceneRoot.transform.Find(name)?.gameObject;
        if (obj != null) cache[name] = obj;
        //else Debug.LogWarning($"❗ 未找到对象: {name}");
        return obj;
    }


    // ✅ 单条对话处理
    private async Task<DialogueItem> ProcessDialogueItem(DialogueItem item)
    {
        Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        // 每轮创建独立 SceneRoot 节点
        //Transform currentSceneRoot = new GameObject($"SceneRoot_{Guid.NewGuid()}").transform;
        //currentSceneRoot.SetParent(parentTransform);
        // 等待上一个截图任务结束后再清空
        //ClearSceneRoot();
        string studentReply = item.User;
        string operationsText = await GetOp(studentReply);

        Debug.Log(studentReply);

        if (string.IsNullOrWhiteSpace(operationsText))
            return null;

        var matches = operationRegex.Matches(operationsText);
        List<string> operations = new();
        foreach (Match match in matches)
        {
            string op = match.Groups[1].Value.Trim();
            if (op.Contains("操作文本") || op == "无操作文本") continue;
            operations.Add(op);
        }
        

        if (operations.Count == 0) return null;

        List<Dictionary<string,OpInfo>> userOps = new();
        
        foreach (var op in operations)
        {
            try
            {
                //if (op != "在试管里加入少量大理石块") continue;
                int index = studentReply.IndexOf(op, StringComparison.Ordinal);
                if (index == -1)
                {
                    Debug.LogWarning($"⚠ 未找到操作文本: {op}");
                    continue;
                }

                Debug.Log(op);
            }
            catch
            {
                continue;
            }
            
            OpInfo op1 = null;
            
            string opText = await GetText(op, studentReply);
            Debug.Log(opText);
            JObject obj = new();
            try
            {
                obj = JObject.Parse(opText);
            }
            catch
            {
                continue;
            }
            string opName = "";
            string targetName = "";
            try
            {
                opName = obj["操作物体"]?.ToString();
                targetName = obj["被操作物体"]?.ToString();
            }
            catch
            {
                Debug.LogWarning($"JSON 解析错误，跳过该操作文本");
                continue;
            }
            
            (GameObject opObj, GameObject targetObj) op_objects = new();
            List<string> begin_pic = new();
            List<string> end_pic = new();
            GameObject opObj = null;
            GameObject targetObj = null;
            try
            {
                // 定义函数对物体摆放进行初始化
                op_objects = place_object_begin(opText, cache, opName, targetName, parentTransform);
                opObj = op_objects.opObj;
                targetObj = op_objects.targetObj;
                if (opObj == null || targetObj == null) continue;
                // 对上述两物体进行多角度截图,初始
                begin_pic = await mfs.CaptureObjectsFromAnglesAsync(opObj, targetObj, op + "_start",opName,targetName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 初始化错误: {ex.Message}");
                continue;
            }
            

            GameObject op_1 = opObj.name.EndsWith("_combined") ? opObj.transform.Find(opName)?.gameObject : opObj;
            GameObject op_2 = targetObj.name.EndsWith("_combined") ? targetObj.transform.Find(targetName)?.gameObject : targetObj;
            if (op_1 == null || op_2 == null)
            {
                Debug.LogWarning($"未找到可操作的原始物体");
            }
            string opJson = "";

            try
            {
                opJson = await GetJson(opText, (op_1, op_2));
                Debug.Log(opJson);
            }
            catch
            {
                Debug.LogWarning($"获取动作序列json错误");
                continue;
            }

            if (opJson != null)
            {
                try
                {
                    //当json不为空时，执行动作序列，进行多角度截图
                    actionExecutor.ExecuteSteps(opJson, cache, parentTransform);
                    opObj = cache[opName];
                    targetObj = cache[targetName];
                    if (opObj == null || targetObj == null) continue;
                    end_pic = await mfs.CaptureObjectsFromAnglesAsync(opObj, targetObj, op + "_end", opName, targetName);
                }
                catch
                {
                    Debug.LogWarning($"执行动作序列错误");
                    continue;
                }

            }
            else
            {
                continue;
            }
            var parsed = JsonConvert.DeserializeObject(opJson); // 先解析
            opJson = JsonConvert.SerializeObject(parsed, Formatting.Indented); // 再美化
            op1 = new OpInfo
            {
                OpeningImage = begin_pic,
                Steps = opJson,
                EndPicture = end_pic
            };
            
            
            Dictionary<string, OpInfo> userOp = new();
            userOp[op] = op1;
            userOps.Add(userOp);
            //break;
        }
        // ✅ 等所有截图协程结束后再安全销毁
        ClearSceneRoot();

        return new DialogueItem
        {
            System = item.System,
            User = item.UserType,
            UserType = item.UserType,
            UserOps = userOps
        };
    }
}
