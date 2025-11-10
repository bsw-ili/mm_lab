using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UI;
using System.Xml.Linq;

public class XDLExecutor : MonoBehaviour
{
    private class ChemTrain
    {
        [JsonProperty("chem_experiment")]
        public string ChemExperiment { get; set; }

        [JsonProperty("chem_analysis")]
        public string ChemAnalysis { get; set; }

        [JsonProperty("xdl_expriment")]
        public string XdlExpriment { get; set; }


        [JsonProperty("user_ops")]
        public List<Dictionary<string, OpInfo>> UserOps { get; set; } = new();

        [JsonProperty("chem_steps")]
        public List<string> ChemSteps { get; set; }

        [JsonProperty("dialogues")]
        public Dictionary<string, List<DialogueItem>> Dialogues { get; set; }
    }

    private class DialogueItem
    {
        [JsonProperty("system")]
        public string System { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("user_type")]
        public string UserType { get; set; }
    }

    public XDLGenerator XDLGenerator;
    public XDL_Extend xDL_Extend;
    public MultiAngleScreenshot mfs;
    public OpenAIExtractor_json extractor_json;
    public OpenAIExtractor OpenAIExtractor;
    public SceneTransformSaver scene_saver;
    public ExperimentActionExecutor ex;


    private readonly Regex operationRegex = new Regex(@"-\s*(.+)");
    public Transform parentTransform;
    public string saveFolder = "D:\\postgraduate\\多模态大模型_化学实验\\output\\update_json_1";
    private List<string> allowedHardware;
    private List<string> liquidList;
    private List<string> solidList;
    private List<string> allowedReagents;
    string XDL_description_build = "Assets\\Scripts\\ai_huaxue\\xdl_description_build.txt";


    private async void Start()
    {

        allowedHardware = ChemistryDefinitions.AnchorDict.Keys.ToList();
        liquidList = ChemistryDefinitions.allowedLiquids_dict.Keys.ToList();
        solidList = ChemistryDefinitions.allowedSolids_dict.Keys.ToList();
        allowedReagents = new List<string>();
        allowedReagents.AddRange(liquidList);
        allowedReagents.AddRange(solidList);
        Debug.Log("Start text2json");
        string folderPath = @"D:\postgraduate\多模态大模型_化学实验\output\json_results"; // 文件夹路径

        // 获取文件夹下的所有文件路径
        string[] files = Directory.GetFiles(folderPath);

        foreach (var file in files.Skip(7))
        {
            await ProcessJsonFileAsync(file);
            //break; // 只处理第一个文件进行测试
        }
        
    }
    
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
        await ProcessDialogueItem(data);



        // ✅ 保存为新文件
        string newPath = Path.Combine(saveFolder, "updated_" + Path.GetFileName(textPath));
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

    // ========== 工具函数：去除编号 ==========
    private static string NormalizeHardwareId(string id)
    {
        return Regex.Replace(id, @"[_\-]?\d+$", "");
    }
    /// <summary>
    /// 沿 X 轴自动循环摆放一组物体，保证它们不重叠。
    /// </summary>
    private void place_object_begin(IEnumerable<XElement> hardware, float basePadding = 0.1f)
    {
        List<GameObject> placedObjects = new List<GameObject>();
        float currentX = 0f; // 从世界原点开始，也可以自定义起点

        foreach (var comp in hardware)
        {
            string id = comp.Attribute("id")?.Value ?? "unknown";
            string contains = comp.Attribute("contains")?.Value;
            string typeName = NormalizeHardwareId(id);

            // 加载预制体
            GameObject prefab = Resources.Load<GameObject>(typeName);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab for type '{typeName}' not found.");
                continue;
            }

            // 实例化物体
            GameObject obj = GameObject.Instantiate(prefab);
            obj.name = id;

            if (!string.IsNullOrEmpty(contains)&&contains!= "empty")
            {
                if(ChemistryDefinitions.allowedLiquids_dict.ContainsKey(contains))
                {
                    ex.AddLiquid(obj, contains);
                }else if (ChemistryDefinitions.allowedSolids_dict.ContainsKey(contains))
                {
                    ex.AddSolid(obj, contains);
                }

            }

            // 设置父节点（先设置，防止坐标错位）
            obj.transform.SetParent(parentTransform, false);

            // 获取物体包围盒尺寸
            if (!TryGetCombinedBounds(obj, out Bounds objBounds))
                continue;

            float halfWidth = objBounds.extents.x;

            // 第一个物体放在起点
            if (placedObjects.Count == 0)
            {
                obj.transform.position = new Vector3(currentX + halfWidth, 0, 0);
            }
            else
            {
                GameObject lastObj = placedObjects.Last();

                if (TryGetCombinedBounds(lastObj, out Bounds lastBounds))
                {
                    // 将当前物体放在上一个物体右侧
                    float targetX = lastBounds.max.x + halfWidth + basePadding;
                    obj.transform.position = new Vector3(targetX, 0, 0);
                }
            }

            placedObjects.Add(obj);
        }

        Debug.Log($"✅ 共摆放 {placedObjects.Count} 个物体。");
    }

    public string ToSafeFilename(XElement element)
    {
        // 取标签名
        string name = element.Name.LocalName;

        // 获取所有属性值并连接
        var values = element.Attributes().Select(a => a.Value);

        // 拼接成字符串：Add-test_tube_1-calcium_carbonate-spatula_1
        string raw = name + "-" + string.Join("-", values);

        // 替换非法文件字符（Windows 不允许的字符）
        string safe = Regex.Replace(raw, @"[<>:""/\\|?*]", "_");

        return safe;
    }

    // ✅ 单条对话处理
    private async Task ProcessDialogueItem(ChemTrain data)
    {
        // ✅ 等所有截图协程结束后再安全销毁
        ClearSceneRoot();
        // ✅ Step 1: 获取学生化学分析文本
        string studentReply = data.ChemAnalysis;

        // ✅ Step 2: 调用 LLM 翻译成英文
        Debug.Log("🔄 调用大模型将 ChemAnalysis 翻译为英文...");
        string translatedReply = await OpenAIExtractor.TranslateToEnglish(studentReply);

        if (string.IsNullOrWhiteSpace(translatedReply))
        {
            Debug.LogWarning("⚠️ 翻译失败，使用原始文本继续。");
            translatedReply = studentReply;
        }
        else
        {
            Debug.Log($"🌍 翻译结果:\n{translatedReply}");
        }

        // ✅ Step 3: 使用英文文本生成 XDL
        var (ok, xdl_build, errors) = await XDLGenerator.GenerateXDL(translatedReply, XDL_description_build, allowedHardware);

        Debug.Log(xdl_build);
        if (!ok)
        {
            File.WriteAllText("Assets\\Scripts\\ai_huaxue\\errors.json", JsonConvert.SerializeObject(errors, Formatting.Indented));
            return;
        }

        if (string.IsNullOrWhiteSpace(xdl_build))
            return;
        XDocument doc = XDocument.Parse(xdl_build);
        var stages = doc.Descendants("Stage");
        List<Dictionary<string, OpInfo>> userOps = new();
        var hardware = doc.Descendants("Component");
        try
        {
            // 定义函数对物体摆放进行初始化
            place_object_begin(hardware);
            
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 初始化错误: {ex.Message}");
            return;
        }
        foreach (var stage in stages)
        {
            // 首先完成场景建造阶段
            if (stage.Attribute("type")?.Value == "hardware")
            {

                foreach (var op in stage.Elements())
                {
                    string op_text = op.ToString(SaveOptions.DisableFormatting);
                    string op_file = ToSafeFilename(op);
                    List<string> end_pic = new();
                    List<string> begin_pic = new();
                    // ✅ 存放该操作涉及到的 GameObject 列表
                    List<GameObject> opObjects = new();

                    // ✅ 依次检查常见字段：vessel, from_vessel, to_vessel, tool, support
                    foreach (string v in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support" })
                    {
                        XAttribute attr = op.Attribute(v);
                        if (attr != null)
                        {
                            string objName = attr.Value;
                            GameObject go = parentTransform.Find(objName)?.gameObject;
                            opObjects.Add(go);
                        }
                    }
                    // 对上述两物体进行多角度截图,初始
                    begin_pic = await mfs.CapturePics(op_file + "_start", opObjects);
                    try
                    {
                        await xDL_Extend.ProcessSingleAction(op); // 根据操作执行场景变化
                        end_pic = await mfs.CapturePics(op_file + "_end", opObjects);
                    }
                    catch
                    {
                        Debug.LogWarning($"获取动作序列json错误");
                        continue;
                    }
                    var op1 = new OpInfo
                    {
                        OpeningImage = begin_pic,
                        Steps = op_text,
                        EndPicture = end_pic
                    };

                    Dictionary<string, OpInfo> userOp = new();
                    userOp[op_text] = op1;
                    userOps.Add(userOp);

                }
            }
        }
        scene_saver.SaveSceneRootTransforms();
        foreach (var stage in stages)
        {
            // 接着完成实验操作截图阶段
            // 执行操作，截图
            // 恢复场景初始状态
            // 记录场景中每个物体的初始transform状态,以便后续操作复原
            if (stage.Attribute("type")?.Value == "operation")
            {
                foreach (var op in stage.Elements())
                {

                    string op_text = op.ToString(SaveOptions.DisableFormatting);
                    string op_file = ToSafeFilename(op);
                    List<string> end_pic = new();
                    List<string> begin_pic = new();

                    // ✅ 存放该操作涉及到的 GameObject 列表
                    List<GameObject> opObjects = new();

                    // ✅ 依次检查常见字段：vessel, from_vessel, to_vessel, tool, support
                    foreach (string v in new[] { "vessel", "from_vessel", "to_vessel", "tool", "support" })
                    {
                        XAttribute attr = op.Attribute(v);
                        if (attr != null)
                        {
                            string objName = attr.Value;
                            GameObject go = parentTransform.Find(objName)?.gameObject;
                            opObjects.Add(go);

                        }
                    }

                    // 对上述两物体进行多角度截图,初始
                    begin_pic = await mfs.CapturePics(op_file + "_start", opObjects);
                    try
                    {
                        await xDL_Extend.ProcessSingleAction(op); // 根据操作执行场景变化
                        end_pic = await mfs.CapturePics(op_file + "_end", opObjects);
                    }
                    catch
                    {
                        Debug.LogWarning($"获取动作序列json错误");
                        continue;
                    }
                    var op1 = new OpInfo
                    {
                        OpeningImage = begin_pic,
                        Steps = op_text,
                        EndPicture = end_pic
                    };

                    Dictionary<string, OpInfo> userOp = new();
                    userOp[op_text] = op1;
                    userOps.Add(userOp);
                    if (op.Name.LocalName == "Heat")
                    {
                        scene_saver.SaveSceneRootTransforms(); // 保存加热后状态
                    }
                    scene_saver.RestoreSceneRootTransforms(); // 恢复场景初始状态
                }
            }
        }
        Resources.UnloadUnusedAssets();
        GC.Collect();
        await Task.Delay(100);

        data.XdlExpriment = xdl_build;
        data.UserOps = userOps;
        
    }
}
