using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class XDLGenerator : MonoBehaviour
{
    private string apiKey;
    private string apiUrl;

    private const string MODEL_NAME = "gpt-4.1-mini"; // ✅ 提取常量

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY，请先在系统中设置！");
            return;
        }

        apiUrl = "https://api.vveai.com/v1/chat/completions";
    }

    /// <summary>
    /// 从实验说明文件生成 XDL 格式
    /// </summary>
    public async Task<(bool correct, string xdl, Dictionary<int, object> errors)> GenerateXDL(
        string filePath, string XDL_description,
        List<string> availableHardware = null,
        List<string> availableReagents = null)
    {
        string instructions;
        if (File.Exists(filePath))
            instructions = File.ReadAllText(filePath);
        else
            instructions = filePath; // 直接传入内容

        string description = File.ReadAllText(XDL_description);
        bool correctSyntax = false;
        string gptOutput = "";
        var errors = new Dictionary<int, object>();
        string constraints = "";

        if (availableHardware != null)
            constraints += $"\nThe available Hardware is: {string.Join(", ", availableHardware)}\n";
        if (availableReagents != null)
            constraints += $"\nThe available Reagents are: {string.Join(", ", availableReagents)}\n";

        string prevInstr = instructions;

        for (int step = 0; step < 10; step++)
        {
            try
            {
                gptOutput = await Prompt(instructions, description, 1000, constraints);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"⚠️ 调用失败 {ex.Message}，尝试使用更短 max_tokens。");
                gptOutput = await Prompt(instructions, description, 750, constraints);
            }

            // 截取 <XDL> 开头部分
            int idx = gptOutput.IndexOf("<XDL>");
            if (idx >= 0)
                gptOutput = gptOutput.Substring(idx);

            // 调用 verify 函数（需你在 Unity 项目中实现 verify.verify_xdl）
            var compileErrors = Verify.VerifyXDL(gptOutput, availableHardware, availableReagents);
            errors[step] = new
            {
                errors = compileErrors,
                instructions = instructions,
                gpt3_output = gptOutput
            };

            if (compileErrors == null || compileErrors.Count == 0)
            {
                correctSyntax = true;
                break;
            }
            else
            {
                HashSet<string> errorList = new HashSet<string>();
                foreach (var e in compileErrors)
                {
                    if (e.errors != null)
                    {
                        foreach (var err in e.errors)
                            errorList.Add(err);
                    }
                }

                string errorMsg = $"\n{gptOutput}\nThis XDL was not correct. These were the errors:\n{string.Join("\n", errorList)}\nPlease fix the errors.";
                instructions = prevInstr + " " + errorMsg;
            }
        }

        if (correctSyntax)
            return (true, gptOutput, errors);
        else
            return (false, "The correct XDL could not be generated.", errors);
    }

    /// <summary>
    /// 调用 OpenAI模型
    /// </summary>
    private async Task<string> Prompt(string instructions, string description, int maxTokens, string constraints)
    {
        // 按 chat 接口格式组织请求体
        var body = new
        {
            model = MODEL_NAME,
            messages = new[]
            {
            new { role = "system", content = "You are an expert in chemical XDL synthesis file generation." },
            new { role = "user", content = description + constraints + "\nConvert to XDL:\n" + instructions }
        },
            temperature = 0,
            max_tokens = maxTokens,
            top_p = 1,
            frequency_penalty = 0,
            presence_penalty = 0
        };

        string jsonBody = JsonConvert.SerializeObject(body);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatResponse>(www.downloadHandler.text);
                    return response.choices[0].message.content.Trim();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ 解析返回 JSON 失败: {e.Message}\n响应内容: {www.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"❌ 网络错误: {www.error}\n{www.downloadHandler.text}");
            }
        }

        return "";
    }


    // ✅ 示例运行
    //async void Start()
    //{
    //    string inputFile = "Assets\\Scripts\\ai_huaxue\\exp0.txt";
    //    var allowedHardware = ChemistryDefinitions.AnchorDict.Keys.ToList();
    //    var liquidList = ChemistryDefinitions.allowedLiquids_dict.Keys.ToList();
    //    var solidList = ChemistryDefinitions.allowedSolids_dict.Keys.ToList();
    //    var allowedReagents = new List<string>();
    //    allowedReagents.AddRange(liquidList);
    //    allowedReagents.AddRange(solidList);
    //    string XDL_description_build = "Assets\\Scripts\\ai_huaxue\\xdl_description_build.txt";
    //    var (ok, xdl_build, errors) = await GenerateXDL(inputFile, XDL_description_build, allowedHardware, allowedReagents);

    //    if (ok)
    //        Debug.Log($"✅ 生成成功：\n{xdl_build}");
    //    else
    //        Debug.LogWarning($"❌ 生成失败：\n{xdl_build}");

    //    // 保存结果
    //    File.WriteAllText("Assets\\Scripts\\ai_huaxue\\result_xdl.txt", xdl_build);
    //    File.WriteAllText("Assets\\Scripts\\ai_huaxue\\errors.json", JsonConvert.SerializeObject(errors, Formatting.Indented));
    //}
}

/// <summary>
/// OpenAI API 响应格式
/// </summary>
[Serializable]
public class OpenAIResponse
{
    public ChoiceText[] choices;
}

[Serializable]
public class ChoiceText
{
    public string text;
}

