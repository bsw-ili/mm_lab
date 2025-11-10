using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

/// <summary>
/// Extended XDL executor with anchor inference prompt generation.
/// </summary>
public class XDL_Extend : MonoBehaviour
{
    // 假设外部可注入或由场景加载
    public Dictionary<string, Dictionary<string, string>> AnchorDatabase =
        new Dictionary<string, Dictionary<string, string>>();

    private OpenAIExtractor_test openAI; // ✅ 直接内置接口对象
    public ExperimentActionExecutor ea;

    private void Awake()
    {
        openAI = gameObject.AddComponent<OpenAIExtractor_test>();
        AnchorDatabase = ChemistryDefinitions.AnchorDict;
    }

    // ====== ③ Single Action Processing Pipeline ======
    public async Task ProcessSingleAction(XElement action)
    {
        string actionName = action.Name.LocalName;
        string apiResponse = "";
        try
        {
            // === Stage 1: XML → Parameter Extraction ===
            LogStep(actionName, "XML parsed successfully");

            if (actionName != "Observe" || actionName != "Wait")
            {
                // === Stage 2: Generate Prompt (anchor inference style) ===
                string prompt = await BuildAnchorPromptForAction(action);
                if (string.IsNullOrEmpty(prompt))
                    throw new Exception("Prompt generation failed");
                LogStep(actionName, "Anchor inference prompt generated", prompt);

                // ✅ 调用 OpenAI 接口
                apiResponse = await openAI.QueryAnchorInference(prompt);
                LogStep(actionName, "API executed successfully", apiResponse);
            }

            // === Stage 4: Action Execution (可扩展到 Unity 场景控制) ===
            ea.ex_xdl(action, apiResponse);
            LogStep(actionName, "Action execution completed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [{actionName}] Pipeline error: {ex.Message}");
        }
    }

    // ====== ⑤ Anchor Inference Prompt Builder ======
    private async Task<string> BuildAnchorPromptForAction(XElement action)
    {
        string name = action.Name.LocalName;

        // 提取参与对象
        string objA = null;
        string objB = null;

        switch (name)
        {
            case "Attach":
                objA = action.Attribute("vessel")?.Value;
                objB = action.Attribute("support")?.Value;
                break;

            case "Insert":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;

            case "Add":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;

            case "Transfer":
                objA = action.Attribute("from_vessel")?.Value;
                objB = action.Attribute("to_vessel")?.Value;
                break;

            case "Stir":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;

            case "Heat":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;
            case "Cool":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;

            case "MeasureTemperature":
                objA = action.Attribute("tool")?.Value;
                objB = action.Attribute("vessel")?.Value;
                break;

            case "Filter":
                objA = action.Attribute("from_vessel")?.Value;
                objB = action.Attribute("to_vessel")?.Value;
                break;

            case "CollectGas":
                objA = action.Attribute("source_vessel")?.Value;
                objB = action.Attribute("collector")?.Value;
                break;

            default:
                return $"⚠️ Unknown action '{name}' — cannot infer anchors.";
        }

        // 获取锚点数据
        if (!AnchorDatabase.ContainsKey(NormalizeHardwareId(objA)) || !AnchorDatabase.ContainsKey(NormalizeHardwareId(objB)))
            return $"❌ Missing anchor dictionary for '{objA}' or '{objB}'.";

        var anchorsA = AnchorDatabase[NormalizeHardwareId(objA)];
        var anchorsB = AnchorDatabase[NormalizeHardwareId(objB)];

        // 调用 Prompt 构造器
        string prompt = XDLPromptBuilder.BuildAnchorInferencePrompt(
            name, objA, anchorsA, objB, anchorsB
        );

        await Task.Yield(); // 保持异步一致性
        return prompt;
    }
    // ========== 工具函数：去除编号 ==========
    private static string NormalizeHardwareId(string id)
    {
        return Regex.Replace(id, @"[_\-]?\d+$", "");
    }

    // ====== ⑥ Simulated API Call (Replace with actual LLM integration) ======
    private async Task<string> SendApiRequest(string prompt)
    {
        await Task.Delay(200); // 模拟延迟
        return $"[Simulated LLM] → {prompt.Substring(0, Math.Min(80, prompt.Length))}...";
    }

    // ====== ⑦ Logging Utility ======
    private void LogStep(string action, string step, string details = null)
    {
        if (string.IsNullOrEmpty(details))
            Debug.Log($"🧩 [{action}] {step}");
        else
            Debug.Log($"🧩 [{action}] {step} → {Truncate(details, 100)}");
    }

    private string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }
}


// ===============================================================
// ✅ 附：Anchor 推理 Prompt 构造器（独立类）
// ===============================================================
public static class XDLPromptBuilder
{
    public static string BuildAnchorInferencePrompt(string actionType,
                                                    string objAName, Dictionary<string, string> objAAnchors,
                                                    string objBName, Dictionary<string, string> objBAnchors)
    {
        string objAInfo = FormatAnchorDict(objAAnchors);
        string objBInfo = FormatAnchorDict(objBAnchors);

        string actionHint = GetActionHint(actionType);

        return
        $@"You are an expert in virtual chemistry lab equipment alignment.
        Your task is to infer the most suitable pair of anchors to connect between '{objAName}' and '{objBName}' 
        for the following operation: **{actionType}**.

        ### Operation Purpose
        {actionHint}

        ### Anchor Dictionaries
        - {objAName}:
        {objAInfo}

        - {objBName}:
        {objBInfo}

        ### Expected Output
        Return the result strictly in JSON format:
        {{
          ""objectA"": ""{objAName}"",
          ""anchorA"": ""<anchor_name_from_{objAName}>"",
          ""objectB"": ""{objBName}"",
          ""anchorB"": ""<anchor_name_from_{objBName}>"",
          ""reason"": ""<short reasoning why these anchors match>""
        }}";
    }

    private static string FormatAnchorDict(Dictionary<string, string> dict)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in dict)
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }
        return sb.ToString();
    }

    private static string GetActionHint(string actionType)
    {
        switch (actionType)
        {
            case "Attach":
                return "Secure a vessel to a support, holder, or clamp tool. The anchors typically align between the vessel body and the support's clamp or holder.";
            case "Insert":
                return "Insert a tool or probe into a vessel. The anchors likely match the opening or inlet of the vessel with the tip or insert part of the tool.";
            case "Add":
                return "Add a reagent to a vessel. The connection involves the tool’s output or dispensing point and the vessel’s inlet or mouth.";
            case "Transfer":
                return "Transfer liquid from one vessel to another. The anchors likely correspond to the source vessel’s outlet and the destination vessel’s inlet.";
            case "Stir":
                return "Place the stirring device in or below the vessel. The anchors usually connect the stirrer's top or base point with the vessel's center or bottom.";
            case "Heat":
                return "Heat a vessel using a heating device. The anchors should align the vessel’s base with the heating tool’s top or heating surface.";
            case "Cool":
                return "Cool a vessel using a cooling device. The anchors typically align the vessel’s outer wall or bottom with the cooler’s contact surface or bath center.";
            case "MeasureTemperature":
                return "Measure the vessel’s temperature using a thermometer. The thermometer’s sensor tip aligns with the vessel’s interior or opening.";
            case "MeasureMass":
                return "Measure reagent mass using a balance. The reagent’s container or surface aligns with the balance’s weighing plate.";
            case "Filter":
                return "Filter mixture from one vessel to another. The anchors match the source vessel’s outlet, the filter or funnel’s inlet, and the receiving vessel’s top.";
            case "CollectGas":
                return "Collect gas from a reaction vessel to a collector. The anchors align the gas outlet of the reaction vessel with the collector’s inlet or mouth.";
            case "Observe":
                return "Observe the reaction in a vessel. Align the observation tool’s viewpoint anchor with the vessel’s observable surface or opening.";
            case "Wait":
                return "Wait is a time operation — no anchor connection inference required.";
            default:
                return "Infer anchor alignment based on the functional relationship between the two objects.";
        }
    }
}

// ===============================================================
// ✅ OpenAI 接口类（带 JSON Schema 输出）
// ===============================================================


public class OpenAIExtractor_test : MonoBehaviour
{
    [Serializable]
    private class ChatMessage { public string role; public string content; }

    [Serializable]
    private class ChatRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
        public object response_format; // for schema
    }

    [Serializable]
    private class Choice { public ChatMessage message; }

    [Serializable]
    private class ChatResponse { public Choice[] choices; }

    private string apiKey;
    private string apiUrl;
    private const string MODEL_NAME = "gpt-4.1";

    private void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY");
        apiUrl = "https://api.vveai.com/v1/chat/completions";
    }

    /// <summary>
    /// Anchor 推理调用
    /// </summary>
    public async Task<string> QueryAnchorInference(string prompt)
    {
        ChatRequest request = new ChatRequest
        {
            model = MODEL_NAME,
            messages = new[]
            {
                new ChatMessage{ role="system", content="You are a virtual chemistry assistant that outputs JSON only." },
                new ChatMessage{ role="user", content=prompt }
            },
            max_tokens = 512,
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "anchor_schema",
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            objectA = new { type = "string" },
                            anchorA = new { type = "string" },
                            objectB = new { type = "string" },
                            anchorB = new { type = "string" },
                            reason = new { type = "string" }
                        },
                        required = new[] { "objectA", "anchorA", "objectB", "anchorB", "reason" },
                        additionalProperties = false
                    }
                }
            }
        };

        string body = JsonConvert.SerializeObject(request);
        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] raw = Encoding.UTF8.GetBytes(body);
            www.uploadHandler = new UploadHandlerRaw(raw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            await www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                return ParseResponse(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"❌ OpenAI 请求失败: {www.error}");
                return $"{{\"error\":\"{www.error}\"}}";
            }
        }
    }

    private static string ParseResponse(string json)
    {
        try
        {
            var resp = JsonConvert.DeserializeObject<ChatResponse>(json);
            if (resp?.choices != null && resp.choices.Length > 0)
                return resp.choices[0].message.content.Trim();
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ JSON解析错误: {ex.Message}");
        }
        return "{\"error\":\"parse_error\"}";
    }
}
