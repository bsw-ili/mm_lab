using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using static OpenAIExtractor_text;
using System.Text.RegularExpressions;

public class OpenAIExtractor_json : MonoBehaviour
{
    [SerializeField, Tooltip("API URL override (leave empty to use default)")]
    private string customApiUrl = "";

    private string apiKey;
    private string apiUrl;
    private readonly int maxRetries = 3;
    private readonly float retryDelay = 1.5f;

    private static readonly System.Text.RegularExpressions.Regex JsonExtractRegex =
        new System.Text.RegularExpressions.Regex(@"\{[\s\S]*\}", System.Text.RegularExpressions.RegexOptions.Compiled);

    private string schemaJson;

    void Awake()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("❌ 未找到环境变量 OPENAI_API_KEY，请先在系统中设置！");
            return;
        }

        apiUrl = string.IsNullOrEmpty(customApiUrl)
            ? "https://api.vveai.com/v1/chat/completions"
            : customApiUrl;
    }

    /// <summary>
    /// 提取 JSON，返回字符串
    /// </summary>
    public async Task<string> Extract(string reply)
    {
        if (string.IsNullOrEmpty(reply))
        {
            Debug.LogError("提供的实验操作描述为空");
            return null;
        }
        
        try
        {
            JObject obj = JObject.Parse(reply);
            string operator1 = obj["操作物体"]?.ToString();
            string operator2 = obj["被操作物体"]?.ToString();

            var op1 = ExtractObjectInfo(operator1);
            var op2 = ExtractObjectInfo(operator2);

            string prompt = BuildPrompt(reply, op1, op2);
            return await SendApiRequest(prompt, op1, op2);
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ 提取JSON时发生错误: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, string> ExtractObjectInfo(string objectName)
    {
        GameObject obj = Resources.Load<GameObject>(objectName);
        var info = new Dictionary<string, string>();
        info["object_name"] = objectName;

        if (obj != null)
        {
            Renderer renderer = obj.GetComponentInChildren<Renderer>();
            info["bounding_box"] = renderer != null ? renderer.bounds.size.ToString() : "null";

            // 锚点信息
            if (ChemistryDefinitions.AnchorDict.ContainsKey(objectName))
            {
                info["key_points"] = JsonConvert.SerializeObject(ChemistryDefinitions.AnchorDict[objectName].Keys);
                info["key_points_info"] = JsonConvert.SerializeObject(ChemistryDefinitions.AnchorDict[objectName], Formatting.Indented);

                // 获取锚点信息 
                // 找到Anchors子节点
                Transform anchors = obj.transform.Find("Anchors"); 
                if (anchors == null) { Debug.LogError("Anchors child not found!"); }
                // 存储锚点坐标的列表（使用Vector3）
                Dictionary<string,string> anchorPositions = new Dictionary<string, string>(); 
                // 遍历Anchors下的所有直接子节点（锚点）
                foreach (Transform child in anchors)
                { 
                    // 获取世界坐标
                    Vector3 worldPos = child.position; 
                    anchorPositions[child.name] = worldPos.ToString(); 
                }
                info["key_points_position"] = JsonConvert.SerializeObject(anchorPositions,Formatting.Indented);
            }

            // 状态信息
            if (ChemistryDefinitions.StateDict.ContainsKey(objectName))
            {
                info["states"] = JsonConvert.SerializeObject(ChemistryDefinitions.StateDict[objectName].Keys);
                info["states_info"] = JsonConvert.SerializeObject(ChemistryDefinitions.StateDict[objectName], Formatting.Indented);
            }
        }
        else
        {
            Debug.LogWarning($"Prefab {objectName} 在Resources文件夹中不存在！");
        }

        return info;
    }

    private JObject BuildPoseSchema(string defaultReference)
    {
        return new JObject(
            new JProperty("type", "object"),
            new JProperty("properties", new JObject(
                new JProperty("rotation", new JObject(
                    new JProperty("type", "array"),
                    new JProperty("items", new JObject(new JProperty("type", "number"))),
                    new JProperty("minItems", 3),
                    new JProperty("maxItems", 3)
                )),
                new JProperty("reference", new JObject(
                    new JProperty("type", "string"),
                    new JProperty("enum", new JArray("world")),
                    new JProperty("default", defaultReference)
                ))
            )),
            new JProperty("required", new JArray("position_offset", "rotation", "reference")),
            new JProperty("additionalProperties", false)
        );
    }


    private string BuildPrompt(string reply, Dictionary<string, string> op1, Dictionary<string, string> op2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一个3D虚拟实验助手，负责根据实验描述生成物体摆放与状态数据。");
        sb.AppendLine("请严格按照 JSON Schema 输出，不要添加多余文本。");
        sb.AppendLine("输出采用统一姿态描述结构 (pose-based structure)。");

        sb.AppendLine("#操作物体:");
        sb.AppendLine(JsonConvert.SerializeObject(op1, Formatting.Indented));
        sb.AppendLine("#被操作物体:");
        sb.AppendLine(JsonConvert.SerializeObject(op2, Formatting.Indented));
        sb.AppendLine("我们首先对上述物体按照世界坐标进行旋转，然后操作物体与被操作物体对齐锚点并平移（object的锚点相对于target的锚点移动）。");

        sb.AppendLine(@"
        #示例输入：将试管放在酒精灯火焰上方 5cm，倾斜 45 度。
        #示例输出JSON:
        {
          ""target"": {
            ""name"": ""alcohol_lamp"",
            ""anchor"": ""flame_center"",
            ""state"": ""lit"",
            ""pose"": {
              ""rotation"": [0.0, 0.0, 0.0],
              ""reference"": ""world""
            }
          },
          ""object"": {
            ""name"": ""test_tube"",
            ""anchor"": ""mouth"",
            ""state"": ""contains_liquid"",
            ""pose"": {
              ""rotation"": [45.0, 0.0, 0.0],
              ""reference"": ""world""
            }
          },
          ""position_offset"": [0.0, 0.05, 0.0],
          ""coordinate_system"": ""local""
        }");
        sb.AppendLine($"#用户输入: {reply}");
        // ✅ 构建统一姿态结构的 JSON Schema
        var schema = new JObject(
            new JProperty("type", "object"),
            new JProperty("properties", new JObject(
                new JProperty("target", new JObject(
                    new JProperty("type", "object"),
                    new JProperty("properties", new JObject(
                        new JProperty("name", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", new JArray(op2["object_name"]))
                        )),
                        new JProperty("anchor", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", JArray.Parse(op2["key_points"]))
                        )),
                        new JProperty("state", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", JArray.Parse(op2["states"]))
                        )),
                        new JProperty("pose", BuildPoseSchema("world"))
                    )),
                    new JProperty("required", new JArray("name", "anchor", "state", "pose"))
                )),
                new JProperty("object", new JObject(
                    new JProperty("type", "object"),
                    new JProperty("properties", new JObject(
                        new JProperty("name", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", new JArray(op1["object_name"]))
                        )),
                        new JProperty("anchor", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", JArray.Parse(op1["key_points"]))
                        )),
                        new JProperty("state", new JObject(
                            new JProperty("type", "string"),
                            new JProperty("enum", JArray.Parse(op1["states"]))
                        )),
                        new JProperty("pose", BuildPoseSchema("world"))
                    )),
                    new JProperty("required", new JArray("name", "anchor", "state", "pose"))
                )),
                new JProperty("position_offset", new JObject(
                    new JProperty("type", "array"),
                    new JProperty("items", new JObject(new JProperty("type", "number"))),
                    new JProperty("minItems", 3),
                    new JProperty("maxItems", 3)
                )),
                new JProperty("coordinate_system", new JObject(
                    new JProperty("type", "string"),
                    new JProperty("enum", new JArray("local", "world"))
                ))
            )),
            new JProperty("required", new JArray("object", "target", "position_offset","coordinate_system")),
            new JProperty("additionalProperties", false)
        );

        schemaJson = schema.ToString(Formatting.None);
        return sb.ToString();
    }


    private async Task<string> SendApiRequest(string prompt, Dictionary<string, string> op1, Dictionary<string, string> op2)
    {
        if (string.IsNullOrEmpty(apiKey)) return null;

        var requestData = new ChatRequest
        {
            model = "gpt-4.1",
            messages = new ChatMessage[]
            {
            new ChatMessage { role="system", content="你是一个3D虚拟实验助手，严格输出符合 JSON Schema 的 JSON。" },
            new ChatMessage { role="user", content=prompt }
            },
            max_tokens = 512,
            response_format = new ResponseFormat
            {
                type = "json_schema",
                json_schema = new JsonSchemaFormat
                {
                    name = "ExperimentPlacement",
                    schema = JObject.Parse(schemaJson)
                }
            }
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var www = new UnityWebRequest(apiUrl, "POST");
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", "Bearer " + apiKey);
                www.timeout = 15;

                await SendWebRequestAsync(www);

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // ✅ 直接解析返回 JSON
                    return ParseResponse(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogWarning($"API请求失败 ({attempt}/{maxRetries}): {www.error}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"发送API请求异常 ({attempt}/{maxRetries}): {ex.Message}");
            }

            if (attempt < maxRetries)
                await Task.Delay(TimeSpan.FromSeconds(retryDelay * attempt));
        }

        Debug.LogError($"在 {maxRetries} 次尝试后仍无法获取有效响应");
        return null;
    }

    // ✅ 解析返回 JSON
    private string ParseResponse(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("返回的 JSON 为空");
            return null;
        }

        try
        {
            var response = JsonConvert.DeserializeObject<ChatResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                string content = response.choices[0].message?.content?.Trim();
                return string.IsNullOrEmpty(content) ? "无Json文本" : content;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析 JSON 时发生异常: {ex.Message}");
            return json;
        }

        return "无Json文本";

    }


    private async Task SendWebRequestAsync(UnityWebRequest www)
    {
    #if UNITY_2020_1_OR_NEWER
            await www.SendWebRequest();
    #else
            var op = www.SendWebRequest();
            while (!op.isDone) await Task.Yield();
    #endif
        }
}
