using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    private int count = 0;
    private readonly Regex operationRegex = new Regex(@"-\s*(.+)");
    private string outputDir;
    public Transform parentTransform;

    private async void Start()
    {
        Debug.Log("Start text2json");
        outputDir = Path.Combine(Application.dataPath, "image_output");

        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir); // ✅ 自动创建输出目录

        string text_path = "D:\\postgraduate\\多模态大模型_化学实验\\data\\json\\chem_experiments.json";
        await ProcessJsonFileAsync(text_path);
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

    // ✅ 主处理流程
    private async Task ProcessJsonFileAsync(string textPath)
    {
        if (!File.Exists(textPath))
        {
            Debug.LogError($"❌ 文件不存在: {textPath}");
            return;
        }

        string jsonContent = await File.ReadAllTextAsync(textPath);
        var data = JsonConvert.DeserializeObject<Dictionary<string, ChemTrain>>(jsonContent);
        if (data == null)
        {
            Debug.LogError("❌ JSON反序列化失败。");
            return;
        }

        foreach (var kvp in data)
        {
            ChemTrain train = kvp.Value; // 遍历对话
            foreach (var d in train.Dialogues) 
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
                    if(i>0) break; 
                } 
                // 只处理第一个学生回复
                break;
            } 
        }

        // ✅ 保存为新文件
        //string newPath = Path.Combine(Path.GetDirectoryName(textPath), "updated_" + Path.GetFileName(textPath));
        //string updatedJson = JsonConvert.SerializeObject(data, Formatting.Indented);
        //await File.WriteAllTextAsync(newPath, updatedJson);
        //Debug.Log($"✅ 处理完成，结果已保存到: {newPath}");
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
    private (GameObject opObj, GameObject targetObj) place_object_begin(string optext)
    {
        JObject obj = JObject.Parse(optext);
        string opName = obj["操作物体"]?.ToString();
        string targetName = obj["被操作物体"]?.ToString();

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
        GameObject opObj = GameObject.Find("SceneRoot/" + opPrefab.name);
        GameObject targetObj = GameObject.Find("SceneRoot/" + targetPrefab.name);
        bool opExisted = opObj != null;
        bool targetExisted = targetObj != null;

        float padding = 0.1f;

        // ============ 初始化 targetObj ============
        if (!targetExisted)
        {
            targetObj = Instantiate(targetPrefab, Vector3.zero, Quaternion.identity, parentTransform);
            targetObj.name = targetPrefab.name;
        }
        else
        {
            // 若存在，重置其 transform（位置、旋转、缩放）
            targetObj.transform.SetParent(parentTransform);
            targetObj.transform.localPosition = Vector3.zero;
            targetObj.transform.localRotation = Quaternion.identity;
            targetObj.transform.localScale = Vector3.one;
        }

        // ============ 初始化 opObj ============
        if (!opExisted)
        {
            // 暂放右侧
            Vector3 tempPos = targetObj.transform.position +
                new Vector3((extractor_json.GetApproximateSize(opPrefab) + extractor_json.GetApproximateSize(targetPrefab)) + padding, 0f, 0f);
            opObj = Instantiate(opPrefab, tempPos, Quaternion.identity, parentTransform);
            opObj.name = opPrefab.name;
        }
        else
        {
            // 若存在，也重置 transform
            opObj.transform.SetParent(parentTransform);
            opObj.transform.localRotation = Quaternion.identity;
            opObj.transform.localScale = Vector3.one;

            // 将其移动到 target 右侧初始位置
            Vector3 tempPos = targetObj.transform.position +
                new Vector3((extractor_json.GetApproximateSize(opObj) + extractor_json.GetApproximateSize(targetObj)) + padding, 0f, 0f);
            opObj.transform.localPosition = tempPos;
        }

        // ============ 确保不重叠 ============
        EnsureNoOverlap_MoveOperatorRight(opObj, targetObj, padding);

        return (opObj, targetObj);
    }



    // ✅ 单条对话处理
    private async Task<DialogueItem> ProcessDialogueItem(DialogueItem item)
    {
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
            //if (op != "把导管另一端插入集气瓶") continue;
            int index = studentReply.IndexOf(op, StringComparison.Ordinal);
            if (index == -1)
            {
                Debug.LogWarning($"⚠ 未找到操作文本: {op}");
                continue;
            }

            Debug.Log(op);

            string opText = await GetText(op, studentReply);
            Debug.Log(opText);

            // 定义函数对物体摆放进行初始化
            var op_objects = place_object_begin(opText);
            
            List<string> begin_pic = new();
            List<string> end_pic = new();
            GameObject opObj = op_objects.opObj;
            GameObject targetObj = op_objects.targetObj;
            // 对上述两物体进行多角度截图,初始
            mfs.CaptureObjectsFromAngles(opObj, targetObj, op + "_start", (paths) => {
                foreach (var p in paths)
                    begin_pic.Add(p);
            });
            string opJson = await GetJson(opText,op_objects);
            Debug.Log(opJson);

            if (opJson != null)
            {
                //当json不为空时，执行动作序列，进行多角度截图
                actionExecutor.ExecuteSteps(opJson);
                mfs.CaptureObjectsFromAngles(opObj, targetObj, op + "_end", (paths) => {
                    foreach (var p in paths)
                        end_pic.Add(p);
                });
            }
            else
            {
                return null;
            }
            OpInfo op1 = new OpInfo
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

        return new DialogueItem
        {
            System = item.System,
            User = item.UserType,
            UserType = item.UserType,
            UserOps = userOps
        };
    }
}
