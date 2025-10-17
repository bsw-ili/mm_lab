using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

public class DialogueItem
{
    public string System { get; set; }
    public string User { get; set; }
    public string UserType { get; set; }

    // ✅ 改为路径列表
    public List<string> Imgs { get; set; } = new List<string>();
}

public class ChemTrain
{
    [JsonProperty("chem_experiment")]
    public string ChemExperiment { get; set; }

    [JsonProperty("chem_phenomenon")]
    public string ChemPhenomenon { get; set; }

    [JsonProperty("dialogues")]
    public Dictionary<string, List<DialogueItem>> Dialogues { get; set; }

    [JsonProperty("chem_steps")]
    public List<string> ChemSteps { get; set; }

    [JsonProperty("chem_analysis")]
    public string ChemAnalysis { get; set; }
}

public class text2json : MonoBehaviour
{
    public OpenAIExtractor extractor;
    public OpenAIExtractor_json extractor_json;
    public OpenAIExtractor_text extractor_text;
    public json2pic json2Pic;

    private int count = 0;
    private readonly Regex operationRegex = new Regex(@"-\s*(.+)");
    private string outputDir;

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
    public Task<string> GetJson(string reply) => SafeExtract(() => extractor_json.Extract(reply));
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
                        item.User = new_item.User ?? item.User; 
                        item.Imgs = new_item.Imgs ?? item.Imgs; 
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

        string rebuiltUser = "";
        int preIndex = 0;
        List<string> imgPaths = new();

        foreach (var op in operations)
        {
            int index = studentReply.IndexOf(op, preIndex, StringComparison.Ordinal);
            if (index == -1)
            {
                Debug.LogWarning($"⚠ 未找到操作文本: {op}");
                continue;
            }

            Debug.Log(op);
            rebuiltUser += studentReply.Substring(preIndex, index - preIndex);

            string opText = await GetText(op, studentReply);
            Debug.Log(opText);
            string opJson = await GetJson(opText);
            Debug.Log(opJson);
            Texture2D pic = json2Pic.json_pic_auto(opJson);

            if (pic != null)
            {
                rebuiltUser += "<image>";
                string safeOpName = Regex.Replace(op, @"[\\/:*?""<>|]", "_"); // ✅ 防止非法文件名
                string fileName = $"{safeOpName}_{count++}.png";
                string filePath = Path.Combine(outputDir, fileName);
                File.WriteAllBytes(filePath, pic.EncodeToPNG());
                imgPaths.Add(filePath);
            }
            else
            {
                Debug.LogWarning($"⚠ 未能生成图片，保留原文本: {op}");
                rebuiltUser += op;
            }

            preIndex = index + op.Length;
        }

        if (preIndex < studentReply.Length)
            rebuiltUser += studentReply.Substring(preIndex);

        return new DialogueItem
        {
            System = item.System,
            User = rebuiltUser,
            UserType = item.UserType,
            Imgs = imgPaths
        };
    }
}
