using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;


public class XDLGenerator : MonoBehaviour
{
    private string apiKey;
    private string apiUrl = "https://api.openai.com/v1/completions";
    private const string MODEL_NAME = "text-davinci-003";

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY，请在系统环境变量中设置！");
        }
    }

    /// <summary>
    /// 从实验说明文件生成 XDL 格式
    /// </summary>
    public async Task<(bool correct, string xdl, Dictionary<int, object> errors)> GenerateXDL(
        string filePath,
        List<string> availableHardware = null,
        List<string> availableReagents = null)
    {
        string instructions;
        if (File.Exists(filePath))
            instructions = File.ReadAllText(filePath);
        else
            instructions = filePath; // 直接传入内容

        string description = File.ReadAllText("./clairify/XDL_description.txt");
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
    /// 调用 OpenAI text-davinci-003 模型
    /// </summary>
    private async Task<string> Prompt(string instructions, string description, int maxTokens, string constraints)
    {
        var body = new
        {
            model = MODEL_NAME,
            prompt = description + constraints + "\nConvert to XDL:\n" + instructions,
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
                    var response = JsonConvert.DeserializeObject<OpenAIResponse>(www.downloadHandler.text);
                    return response.choices[0].text.Trim();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ 解析返回 JSON 失败: {e.Message}");
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
    async void Start()
    {
        string inputFile = "Assets/Input/example.txt";
        var (ok, xdl, errors) = await GenerateXDL(inputFile);

        if (ok)
            Debug.Log($"✅ 生成成功：\n{xdl}");
        else
            Debug.LogWarning($"❌ 生成失败：\n{xdl}");

        // 保存结果
        File.WriteAllText("Assets/Output/result_xdl.txt", xdl);
        File.WriteAllText("Assets/Output/errors.json", JsonConvert.SerializeObject(errors, Formatting.Indented));
    }
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

