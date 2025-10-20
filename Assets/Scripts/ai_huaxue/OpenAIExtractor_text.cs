using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine.Networking;
using UnityEngine;

public class OpenAIExtractor_text : MonoBehaviour
{
    private string apiKey;
    private string apiUrl;
    private const string MODEL_NAME = "gpt-4.1";

    // ✅ 允许的实验器材（锚点枚举一致）
    private readonly string[] allowedInstruments = ChemistryDefinitions.AnchorDict.Keys.ToArray();

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
    /// 异步提取标准操作文本（带 schema 格式）
    /// </summary>
    public async Task<string> Extract(string reply, string premise)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            Debug.LogWarning("⚠️ 输入文本为空。");
            return "无操作文本";
        }

        JObject schema = BuildSchemaJson();
        string prompt = BuildPrompt(reply, premise);
        ChatRequest request = CreateRequest(prompt, schema);

        string jsonBody = JsonConvert.SerializeObject(request);
        try
        {
            using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);

                // ✅ 真正异步等待
                await www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    return ParseResponse(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"❌ 网络错误: {www.error}\n{www.downloadHandler.text}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ OpenAIExtractor_text 异常: {ex.Message}");
        }

        return "无操作文本";
    }

    // ✅ 构造 Schema JSON
    private JObject BuildSchemaJson()
    {
        var schema = JObject.Parse(JsonConvert.SerializeObject(new
        {
            type = "object",
            properties = new
            {
                操作物体 = new { type = "string", enum_ = allowedInstruments },
                被操作物体 = new { type = "string", enum_ = allowedInstruments },
                操作步骤 = new { type = "string" },
                物体状态 = new { type = "string" },
                操作后的画面 = new { type = "string" }
            },
            required = new[] { "操作物体", "被操作物体", "操作步骤", "物体状态", "操作后的画面" }
        }, new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
            {
                NamingStrategy = new Newtonsoft.Json.Serialization.DefaultNamingStrategy()
            }
        }));

        // ✅ 手动修改所有 "enum_" → "enum"
        RenameEnumKey(schema);

        return schema;
    }

    // ✅ 手动递归替换 enum_ → enum
    private void RenameEnumKey(JObject obj)
    {
        foreach (var property in obj.Properties().ToList())
        {
            if (property.Name == "enum_")
            {
                var value = property.Value;
                property.Remove();
                obj["enum"] = value;
            }
            else if (property.Value is JObject nested)
            {
                RenameEnumKey(nested);
            }
        }
    }

    // ✅ 构造提示词
    private static string BuildPrompt(string reply, string premise)
    {
        return $@"
        你是一个化学实验助手，任务是将输入的步骤结合整体步骤信息转换为标准实验操作文本。

        【输出要求】
        - 严格按照 schema 输出 JSON。
        - 操作物体和被操作物体必须来自 schema 中的枚举列表。
        - 操作步骤需详细，不得省略。
        - 物体状态和操作后的画面需符合逻辑。

        #整体步骤信息：{premise}
        #当前步骤：{reply}
        #输出：";
    }

    // ✅ 构造请求体
    private ChatRequest CreateRequest(string prompt, JObject schema)
    {
        return new ChatRequest
        {
            model = MODEL_NAME,
            messages = new[]
            {
                new ChatMessage { role = "system", content = "你是一个虚拟化学实验场景生成器。" },
                new ChatMessage { role = "user", content = prompt }
            },
            max_tokens = 512,
            response_format = new ResponseFormat
            {
                type = "json_schema",
                json_schema = new JsonSchemaFormat
                {
                    name = "chemistry_operation_schema",
                    schema = schema
                }
            }
        };
    }

    // ✅ 解析返回 JSON
    private string ParseResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogWarning("⚠️ 返回内容为空。");
            return "无操作文本";
        }

        try
        {
            var response = JsonConvert.DeserializeObject<ChatResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                string content = response.choices[0].message?.content?.Trim();
                return string.IsNullOrEmpty(content) ? "无操作文本" : content;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ JSON解析错误: {ex.Message}\n返回内容: {json}");
        }

        return "无操作文本";
    }
}

// ✅ 内部数据结构定义
[Serializable]
public class ChatRequest
{
    public string model;
    public ChatMessage[] messages;
    public int max_tokens;
    public float temperature = 0.7f;
    public float top_p = 1.0f;
    public ResponseFormat response_format;
}

[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ResponseFormat
{
    public string type;
    public JsonSchemaFormat json_schema;
}

[Serializable]
public class JsonSchemaFormat
{
    public string name;
    public object schema;
}

[Serializable]
public class ChatResponse
{
    public string id;
    public string model;
    public ChatResponseChoice[] choices;
    public ChatUsage usage;
}

[Serializable]
public class ChatResponseChoice
{
    public int index;
    public ChatMessage message;
    public string finish_reason;
}

[Serializable]
public class ChatUsage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}
