//using System;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;
//using UnityEngine;
//using UnityEngine.Networking;
//using Newtonsoft.Json.Linq;
//using Newtonsoft.Json;
//using System.Security.Cryptography;

//public class OpenAIExtractor_json : MonoBehaviour
//{
//    [SerializeField, Tooltip("API URL override (leave empty to use default)")]
//    private string customApiUrl = "";

//    private string apiKey;
//    private string apiUrl;
//    private readonly int maxRetries = 3;
//    private readonly float retryDelay = 1.5f; // seconds

//    private static readonly Regex OperationObjectRegex = new Regex(@"操作物体：([^\r\n]+)", RegexOptions.Compiled);
//    private static readonly Regex TargetObjectRegex = new Regex(@"被操作物体：([^\r\n]+)", RegexOptions.Compiled);
//    private static readonly Regex JsonExtractRegex = new Regex(@"\{[\s\S]*\}", RegexOptions.Compiled);

//    async void Awake()
//    {
//        // Securely retrieve API key from environment variables
//        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

//        if (string.IsNullOrEmpty(apiKey))
//        {
//            Debug.LogError("未找到环境变量 OPENAI_API_KEY，请先在系统中设置！");
//            return;
//        }

//        // Allow for URL override if needed, otherwise use default
//        apiUrl = string.IsNullOrEmpty(customApiUrl)
//            ? "https://api.chatanywhere.tech/v1/chat/completions"
//            : customApiUrl;

//        await Extract("操作物体：烧杯  \r\n        被操作物体：集气瓶  \r\n        操作步骤：使用烧杯缓缓向集气瓶中倒水，使得集气瓶中装满水。  \r\n        操作结果：烧杯剩部分水，集气瓶中装满水。  \r\n        操作后的画面：集气瓶保持正立，烧杯位于集气瓶侧上方，集气瓶与烧杯隔着一点距离，烧杯倾斜合适角度往集气瓶倒水，烧杯剩部分水，集气瓶中装满水。  \r\n");
//    }

//    /// <summary>
//    /// 提取 JSON，返回 string
//    /// </summary>
//    /// <param name="reply">实验操作描述</param>
//    /// <returns>提取出的JSON字符串</returns>
//    public async Task<string> Extract(string reply)
//    {
//        if (string.IsNullOrEmpty(reply))
//        {
//            Debug.LogError("提供的实验操作描述为空");
//            return null;
//        }

//        try
//        {
//            // 提取操作物体和被操作物体
//            var opObjInfo = ExtractObjectInfo(reply, OperationObjectRegex, "操作物体");
//            var targetObjInfo = ExtractObjectInfo(reply, TargetObjectRegex, "被操作物体");

//            if (opObjInfo.gameObject == null || targetObjInfo.gameObject == null)
//            {
//                return null; // Error messages already logged in ExtractObjectInfo
//            }

//            string prompt = BuildPrompt(reply, opObjInfo, targetObjInfo);
//            return await SendApiRequest(prompt);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"提取JSON时发生错误: {ex.Message}");
//            return null;
//        }
//    }

//    private (string name, GameObject gameObject, Vector3 topPosition, string bounds) ExtractObjectInfo(
//        string text, Regex regex, string objectType)
//    {
//        // 从文本中提取对象名称
//        var match = regex.Match(text);
//        if (!match.Success)
//        {
//            Debug.LogError($"无法从输入中提取{objectType}名称");
//            return (null, null, Vector3.zero, null);
//        }

//        string objectName = match.Groups[1].Value.Trim();

//        // 加载对象并检查是否存在
//        GameObject obj = Resources.Load<GameObject>(objectName);
//        if (obj == null)
//        {
//            Debug.LogWarning($"Prefab {objectName} 在Resources文件夹中不存在！");
//            return (objectName, null, Vector3.zero, null);
//        }

//        // 获取渲染器组件和边界信息
//        Renderer renderer = obj.GetComponentInChildren<Renderer>();
//        if (renderer == null)
//        {
//            Debug.LogWarning($"{objectType} {objectName} 没有 Renderer 组件！");
//            return (objectName, obj, Vector3.zero, "未知");
//        }

//        // 计算顶部位置
//        float height = renderer.bounds.size.y;
//        Vector3 topPosition = new Vector3(0, height, 0); // 底部默认为(0,0,0)
//        string bounds = renderer.bounds.size.ToString();

//        Debug.Log($"{objectType} {objectName} 高度: {height}, 顶部位置: {topPosition}");

//        return (objectName, obj, topPosition, bounds);
//    }

//    private string BuildPrompt(string reply,
//        (string name, GameObject gameObject, Vector3 topPosition, string bounds) opObjInfo,
//        (string name, GameObject gameObject, Vector3 topPosition, string bounds) targetObjInfo)
//    {
//        StringBuilder promptBuilder = new StringBuilder();

//        promptBuilder.AppendLine(@"
//        你是一个虚拟化学实验场景生成器。 
//        用户会输入实验操作的具体信息，你需要将其转换为严格的JSON 数据。 
//        输出要求: 只输出 JSON，不包含任何解释、注释或多余文字。JSON 顶层键为 objects。每个对象包含以下字段：
//       {
//          ""name"": ""器材名称"",
//          ""bottomCenter"":[x, y, z],
//          ""topCenter"":[x, y, z],
//          ""position"": [x, y, z],
//          ""rotation"": [x, y, z],
//          ""states"": {
//            ""状态名"": 状态值
//          }
//        }");

//        promptBuilder.AppendLine(@"
//        规则 
//        1.保证 JSON 可解析。输出中不得包含多余的文字或解释，只要JSON。
//        2.默认值：
//            position: [0, 0, 0]
//            rotation: [0, 0, 0]
//            如果未提及状态，则填入合理的默认值。
//        3. 状态示例（可扩展）： 
//            水相关：""WaterLevel"": ""Empty"" | ""Half"" | ""Full"" 
//            溶液存在：""HasSolution"": true/false 
//            试剂瓶：""HasWater"": true/false 
//            火焰：""IsHeating"": true/false 
//            盖子：""IsClosed"": true/false 
//        4. 输出保证只有""操作物体""和""被操作物体""两个对象。
//        5. 物体的名称不包含其余信息，单纯只是器材名称。"

//        );

//        promptBuilder.AppendLine($@"
//        提示
//        1. 物体底部中心为物体的Pivot，
//          ""操作物体""的大小为{opObjInfo.bounds}，
//          ""被操作物体""的大小为{targetObjInfo.bounds}，大小即（长宽高）。
//        2.我们假设物体position中的x始终保持为0，以便简化物体position的设置，此时position的y代表上下（正值为上），position的z代表左右（正值为右）
//        3.我们假设物体rotation中的y,z始终保持为0，以便简化物体rotation的设置，此时rotation的x代表旋转多少度（正值代表顺时针旋转）。
//        4.未进行操作时，""操作物体""的初始底部中心坐标（bottomCenter）为（0,0,0）,初始顶部中心坐标（topCenter）为{opObjInfo.topPosition.ToString()}。
//          ""被操作物体""的初始底部中心坐标（bottomCenter）为（0,0,0）,初始顶部中心坐标（topCenter）为{targetObjInfo.topPosition.ToString()}。
//        5.根据""操作后的画面""确定操作后""被操作物体""以及""操作物体""的position和rotation。
//        6.结合""操作结果""和""操作后的画面""得到""被操作物体""以及""操作物体""的状态。
//        7.请你给出""被操作物体""及""操作物体""的底部和顶部中心的位置即物体的顶部和底部，保证操作后两物体的顶部和底部位于合理的位置。
//        8.物体旋转会导致顶部中心的位移，可能会造成物体组合出现异常，如集气瓶倒置水槽中，倒置集气瓶后，集气瓶顶部中心将会处于底部中心下面一个集气瓶高度的位置。");

//        promptBuilder.AppendLine(@"
//        示例：
//        #实验操作：
//        操作物体：烧杯  
//        被操作物体：集气瓶  
//        操作步骤：使用烧杯缓缓向集气瓶中倒水，使得集气瓶中装满水。  
//        操作结果：烧杯剩部分水，集气瓶中装满水。  
//        操作后的画面：集气瓶保持正立，烧杯位于集气瓶侧上方，烧杯倾斜合适角度往集气瓶倒水，烧杯剩部分水，集气瓶中装满水。  
 
        
//        #JSON输出: 
//        {
//          ""objects"": [
//            {
//              ""name"": ""烧杯"",
//              ""bottomCenter"":[0, 0.11, -0.11],
//              ""topCenter"":[0, 0.23, -0.11],
//              ""position"": [0, 0.14, -0.14],
//              ""rotation"": [45, 0, 0],
//              ""states"": {
//                ""HasWater"": true
//              }
//            },
//            {
//              ""name"": ""集气瓶"",
//              ""bottomCenter"":[0, 0, 0],
//              ""topCenter"":[0, 0.10, 0],
//              ""position"": [0, 0, 0],
//              ""rotation"": [0, 0, 0],
//              ""states"": {
//                ""WaterLevel"": ""Full""
//              }
//            }
//          ]
//        }");

//        promptBuilder.AppendLine($@"
//         #实验操作：{reply}
//         #JSON输出:");

//        return promptBuilder.ToString();
//    }

//    private async Task<string> SendApiRequest(string prompt)
//    {
//        if (string.IsNullOrEmpty(apiKey))
//        {
//            Debug.LogError("API Key未设置，无法发送请求");
//            return null;
//        }

//        ChatRequest requestData = new ChatRequest
//        {
//            model = "gpt-4.1",
//            messages = new ChatMessage[]
//            {
//                new ChatMessage { role = "system", content = "你是一个虚拟化学实验场景生成器。" },
//                new ChatMessage { role = "user", content = prompt }
//            },
//            max_tokens = 512
//        };

//        string jsonBody = JsonConvert.SerializeObject(requestData);

//        // 实现重试逻辑
//        for (int attempt = 1; attempt <= maxRetries; attempt++)
//        {
//            try
//            {
//                using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
//                {
//                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
//                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
//                    www.downloadHandler = new DownloadHandlerBuffer();
//                    www.SetRequestHeader("Content-Type", "application/json");
//                    www.SetRequestHeader("Authorization", "Bearer " + apiKey);

//                    // 添加超时设置
//                    www.timeout = 15; // 15秒超时

//                    var operation = www.SendWebRequest();
//                    while (!operation.isDone) await Task.Yield();

//                    if (www.result == UnityWebRequest.Result.Success)
//                    {
//                        string responseJson = www.downloadHandler.text;
//                        try
//                        {
//                            JObject parsedResponse = JObject.Parse(responseJson);
//                            JArray choices = (JArray)parsedResponse["choices"];

//                            if (choices != null && choices.Count > 0)
//                            {
//                                string extractedText = choices[0]["message"]["content"].ToString().Trim();
//                                string extractedJson = ExtractJson(extractedText);

//                                if (ValidateJson(extractedJson))
//                                {
//                                    return extractedJson;
//                                }
//                                else
//                                {
//                                    Debug.LogError($"无法解析返回的JSON: {extractedText}");
//                                }
//                            }
//                        }
//                        catch (JsonException ex)
//                        {
//                            Debug.LogError($"解析API响应时发生错误: {ex.Message}");
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogError($"API请求失败 (尝试 {attempt}/{maxRetries}): {www.error}\n响应: {www.downloadHandler.text}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.LogError($"发送API请求时发生异常 (尝试 {attempt}/{maxRetries}): {ex.Message}");
//            }

//            if (attempt < maxRetries)
//            {
//                // 等待重试
//                float backoffTime = retryDelay * attempt;
//                Debug.Log($"等待 {backoffTime} 秒后重试...");
//                await Task.Delay(TimeSpan.FromSeconds(backoffTime));
//            }
//        }

//        Debug.LogError($"在 {maxRetries} 次尝试后仍无法获取有效响应");
//        return null;
//    }

//    private string ExtractJson(string text)
//    {
//        Match match = JsonExtractRegex.Match(text);
//        return match.Success ? match.Value : text;
//    }

//    private bool ValidateJson(string json)
//    {
//        if (string.IsNullOrEmpty(json))
//            return false;

//        try
//        {
//            JObject obj = JObject.Parse(json);

//            if (obj["objects"] == null || !(obj["objects"] is JArray))
//                return false;

//            JArray objects = (JArray)obj["objects"];
//            if (objects.Count != 2)
//            {
//                Debug.LogWarning($"预期JSON应包含恰好2个对象，但找到了 {objects.Count} 个");
//            }

//            foreach (var item in objects)
//            {
//                // 验证必需字段
//                if (item["name"] == null) return false;
//                if (item["bottomCenter"] == null) return false;
//                if (item["topCenter"] == null) return false;
//                if (item["position"] == null) return false;
//                if (item["rotation"] == null) return false;
//                if (item["states"] == null) return false;

//                // 验证数组长度
//                if (((JArray)item["bottomCenter"]).Count != 3) return false;
//                if (((JArray)item["topCenter"]).Count != 3) return false;
//                if (((JArray)item["position"]).Count != 3) return false;
//                if (((JArray)item["rotation"]).Count != 3) return false;
//            }

//            return true;
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"JSON 验证错误: {ex.Message}");
//            return false;
//        }
//    }

//    // 安全地处理和清理API密钥
//    private void OnDestroy()
//    {
//        // 清除内存中的apiKey
//        if (!string.IsNullOrEmpty(apiKey))
//        {
//            apiKey = null;
//            // 请求GC回收以确保敏感信息从内存中清除
//            GC.Collect();
//        }
//    }
//}

//[Serializable]
//public class ChatMessage
//{
//    public string role;
//    public string content;
//}

//[Serializable]
//public class ChatResponse
//{
//    public string id;
//    public string model;
//    public ChatResponseChoice[] choices;
//    public ChatUsage usage;
//}

//[Serializable]
//public class ChatResponseChoice
//{
//    public int index;
//    public ChatMessage message;
//    public string finish_reason;
//}

//[Serializable]
//public class ChatUsage
//{
//    public int prompt_tokens;
//    public int completion_tokens;
//    public int total_tokens;
//}

//// API 请求数据结构
//[Serializable]
//public class ChatRequest
//{
//    public string model;
//    public ChatMessage[] messages;
//    public int max_tokens;
//    public float temperature = 0.7f;
//    public float top_p = 1.0f;
//    public ResponseFormat response_format;
//}

//[Serializable]
//public class ResponseFormat
//{
//    public string type;
//    public JsonSchemaFormat json_schema;
//}

//[Serializable]
//public class JsonSchemaFormat
//{
//    public string name;
//    public object schema; // 用 object 承载 JObject
//}
